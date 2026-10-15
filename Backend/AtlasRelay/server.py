"""Atlas room-code presence relay. Python 3.10+, standard library, TLS required online.

No catalog documents/media are transferred. Room codes are not written to logs.
"""
import argparse
import asyncio
from dataclasses import dataclass, field
import math
import secrets
import ssl
import struct
import time

ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'
MAX_FRAME, MAX_VISITORS, MAX_ROOMS = 4096, 8, 128
BOUNDS = {'Lobby': (24, 24), 'Gallery': (32, 24), 'Archive': (28, 24), 'Showcase': (28, 22), 'Courtyard': (24, 18), 'Frontend': (200, 200)}

def number(value): return struct.pack('<i', value)
def text(value):
    data = value.encode('utf-8')
    return number(len(data)) + data

class Packet:
    def __init__(self, data): self.data, self.pos = data, 0
    def take(self, count):
        if count < 0 or self.pos + count > len(self.data): raise ValueError('Paket terpotong.')
        value = self.data[self.pos:self.pos+count]; self.pos += count
        return value
    def byte(self): return self.take(1)[0]
    def integer(self): return struct.unpack('<i', self.take(4))[0]
    def string(self, limit):
        count = self.integer()
        if count < 0 or count > limit: raise ValueError('Teks melebihi batas.')
        return self.take(count).decode('utf-8')
    def done(self):
        if self.pos != len(self.data): raise ValueError('Data tambahan tidak valid.')

async def read_frame(reader, timeout=15):
    async def read():
        length = struct.unpack('<i', await reader.readexactly(4))[0]
        if not 1 <= length <= MAX_FRAME: raise ValueError('Ukuran paket tidak valid.')
        return Packet(await reader.readexactly(length))
    return await asyncio.wait_for(read(), timeout)

async def send(writer, data):
    if len(data) > MAX_FRAME: raise ValueError('Frame too large')
    writer.write(number(len(data)) + data)
    await asyncio.wait_for(writer.drain(), 3)

def clean_name(value):
    result = ''.join(c for c in value if c.isprintable() and c not in '<>')[:24].strip()
    return result or 'Pengunjung'

@dataclass
class Guest:
    id: int
    name: str
    writer: asyncio.StreamWriter
    zone: str = 'Frontend'
    pose: tuple = (0., 0., 0., 0.)
    queue: asyncio.Queue = field(default_factory=lambda: asyncio.Queue(maxsize=1))
    def encode(self): return number(self.id) + text(self.name) + text(self.zone) + struct.pack('<ffff', *self.pose)

@dataclass
class Room:
    code: str
    catalog: str
    guests: dict = field(default_factory=dict)
    next_id: int = 1
    task: asyncio.Task = None

class Relay:
    def __init__(self):
        self.rooms = {}
        self.attempts = {}
        self.connections = 0
    async def broadcast(self, room):
        try:
            while True:
                payload = b'\x04' + number(len(room.guests)) + b''.join(g.encode() for g in room.guests.values())
                for guest in tuple(room.guests.values()):
                    if guest.queue.full(): guest.queue.get_nowait()
                    guest.queue.put_nowait(payload)
                await asyncio.sleep(.1)
        except asyncio.CancelledError:
            pass
    async def write_guest(self, guest):
        try:
            while True: await send(guest.writer, await guest.queue.get())
        except (ConnectionError, OSError, asyncio.TimeoutError):
            guest.writer.close()
        except asyncio.CancelledError:
            pass
    async def reject(self, writer, reason):
        try: await send(writer, b'\x05' + text(reason))
        except (ConnectionError, OSError, asyncio.TimeoutError): pass
    async def handle(self, reader, writer):
        room = guest = sender = None
        self.connections += 1
        try:
            if self.connections > MAX_ROOMS * MAX_VISITORS + 32: raise ValueError('Server penuh.')
            ip = writer.get_extra_info('peername')[0]
            now = time.monotonic()
            self.attempts = {key: value for key, value in self.attempts.items() if now-value[0] < 60}
            started, count = self.attempts.get(ip, (now, 0)); self.attempts[ip] = (started, count+1)
            if count >= 60: raise ValueError('Terlalu banyak percobaan. Coba lagi sebentar.')
            packet = await read_frame(reader, 6)
            kind, protocol = packet.byte(), packet.integer()
            if kind not in (10, 11) or protocol != 1: raise ValueError('Versi protokol berbeda.')
            catalog, name, code = packet.string(256), clean_name(packet.string(128)), packet.string(16)
            packet.done()
            if not catalog: raise ValueError('Versi katalog wajib.')
            if kind == 10:
                if len(self.rooms) >= MAX_ROOMS: raise ValueError('Server penuh. Coba lagi nanti.')
                code = ''.join(secrets.choice(ALPHABET) for _ in range(8))
                while code in self.rooms: code = ''.join(secrets.choice(ALPHABET) for _ in range(8))
                room = Room(code, catalog); self.rooms[code] = room
            else:
                if len(code) != 8 or any(c not in ALPHABET for c in code): raise ValueError('Kode ruang tidak valid.')
                room = self.rooms.get(code)
                if room is None: raise ValueError('Ruang tidak ditemukan atau sudah ditutup.')
                if room.catalog != catalog: raise ValueError('Versi katalog berbeda. Gunakan paket museum yang sama.')
                if len(room.guests) >= MAX_VISITORS: raise ValueError('Ruang penuh (maksimum 8 pengunjung).')
            guest = Guest(room.next_id, name, writer); room.next_id += 1; room.guests[guest.id] = guest
            await send(writer, b'\x0c' + number(guest.id) + text(room.code))
            sender = asyncio.create_task(self.write_guest(guest))
            if room.task is None: room.task = asyncio.create_task(self.broadcast(room))
            window, frames = time.monotonic(), 0
            while True:
                packet = await read_frame(reader)
                if packet.byte() != 3: raise ValueError('Jenis paket tidak valid.')
                packet.integer(); packet.string(128)  # IDs/names belong to the server handshake.
                zone = packet.string(32); pose = struct.unpack('<ffff', packet.take(16)); packet.done()
                if time.monotonic()-window >= 1: window, frames = time.monotonic(), 0
                frames += 1
                if frames > 60: raise ValueError('Terlalu banyak pembaruan posisi.')
                if zone not in BOUNDS or not all(math.isfinite(v) for v in pose): raise ValueError('Posisi tidak valid.')
                x, y, z, yaw = pose; width, depth = BOUNDS[zone]
                if abs(x) > width/2 or abs(z) > depth/2 or not -2 <= y <= 8 or abs(yaw) > 36000: raise ValueError('Posisi di luar ruang.')
                guest.zone, guest.pose = zone, pose
        except (ValueError, UnicodeError, struct.error) as error:
            await self.reject(writer, str(error))
        except (asyncio.IncompleteReadError, asyncio.TimeoutError, ConnectionError, OSError):
            pass
        finally:
            if sender:
                sender.cancel(); await asyncio.gather(sender, return_exceptions=True)
            if room is not None and guest is not None:
                room.guests.pop(guest.id, None)
                if guest.id == 1:
                    self.rooms.pop(room.code, None)
                    if room.task: room.task.cancel()
                    # Closing host closes the room. Guests return to solo; there is no hidden migration.
                    for other in tuple(room.guests.values()): other.writer.close()
            writer.close()
            try: await writer.wait_closed()
            except (ConnectionError, OSError, asyncio.CancelledError): pass
            self.connections -= 1
    async def close(self):
        tasks = []
        for room in tuple(self.rooms.values()):
            if room.task: room.task.cancel(); tasks.append(room.task)
            for guest in tuple(room.guests.values()): guest.writer.close()
        self.rooms.clear()
        await asyncio.gather(*tasks, return_exceptions=True)

async def main(args):
    context = None
    if args.insecure_loopback:
        if args.host not in ('127.0.0.1', '::1'): raise SystemExit('Test mode only permits loopback binding.')
    else:
        if not args.cert or not args.key: raise SystemExit('Online relay requires --cert and --key; use --insecure-loopback only for local tests.')
        context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER); context.minimum_version = ssl.TLSVersion.TLSv1_2
        context.load_cert_chain(args.cert, args.key)
    relay = Relay()
    server = await asyncio.start_server(relay.handle, args.host, args.port, ssl=context, limit=MAX_FRAME*2)
    print('ATLAS relay listening on port', server.sockets[0].getsockname()[1], 'TLS' if context else 'LOOPBACK TEST ONLY', flush=True)
    try:
        async with server: await server.serve_forever()
    finally: await relay.close()

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--host', default='0.0.0.0'); parser.add_argument('--port', type=int, default=47778)
    parser.add_argument('--cert'); parser.add_argument('--key'); parser.add_argument('--insecure-loopback', action='store_true')
    try: asyncio.run(main(parser.parse_args()))
    except KeyboardInterrupt: pass
