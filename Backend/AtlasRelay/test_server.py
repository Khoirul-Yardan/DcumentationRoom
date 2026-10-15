import asyncio
import pathlib
import ssl
import struct
import subprocess
import unittest
from server import Relay, Packet, number, text, send, read_frame

ROOT = pathlib.Path(__file__).resolve().parents[2]

class RelayTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.relay = Relay()
        self.server = await asyncio.start_server(self.relay.handle, '127.0.0.1', 0)
        self.port = self.server.sockets[0].getsockname()[1]
        self.writers = []
    async def asyncTearDown(self):
        for writer in self.writers: writer.close()
        await asyncio.gather(*(w.wait_closed() for w in self.writers), return_exceptions=True)
        self.server.close(); await self.server.wait_closed(); await self.relay.close()
        await asyncio.sleep(.02)
    async def connect(self, code='', catalog='sample-test', protocol=1, port=None, context=None):
        reader, writer = await asyncio.open_connection('127.0.0.1', port or self.port, ssl=context, server_hostname='localhost' if context else None)
        self.writers.append(writer)
        await send(writer, bytes([11 if code else 10])+number(protocol)+text(catalog)+text('Synthetic Visitor')+text(code))
        result = await read_frame(reader)
        kind = result.byte()
        if kind == 5: return reader, writer, None, result.string(256)
        self.assertEqual(kind, 12)
        return reader, writer, result.integer(), result.string(16)
    async def snapshot(self, reader):
        packet = await read_frame(reader); self.assertEqual(packet.byte(), 4); count=packet.integer()
        entries=[]
        for _ in range(count): entries.append((packet.integer(),packet.string(128),packet.string(32),struct.unpack('<ffff',packet.take(16))))
        return entries
    async def test_room_capacity_eight(self):
        _,_,_,code = await self.connect()
        for _ in range(7): self.assertIsNotNone((await self.connect(code))[2])
        _,_,ident,reason=await self.connect(code)
        self.assertIsNone(ident); self.assertIn('penuh',reason)
    async def test_rooms_are_isolated(self):
        a,_,_,code_a=await self.connect(); b,_,_,code_b=await self.connect()
        self.assertNotEqual(code_a,code_b)
        await self.connect(code_a)
        await asyncio.sleep(.2)
        for _ in range(3): snap_a=await self.snapshot(a); snap_b=await self.snapshot(b)
        self.assertEqual(len(snap_a),2); self.assertEqual(len(snap_b),1)
    async def test_mismatched_protocol_and_catalog(self):
        _,_,_,code=await self.connect()
        self.assertIn('katalog berbeda',(await self.connect(code,catalog='different'))[3])
        self.assertIn('protokol berbeda',(await self.connect(protocol=99))[3])
    async def test_unknown_code(self):
        self.assertIn('tidak ditemukan',(await self.connect('ZZZZZZZZ'))[3])
    async def test_nan_pose_rejected_without_server_crash(self):
        r,w,ident,code=await self.connect()
        await send(w,b'\x03'+number(ident)+text('spoof')+text('Lobby')+struct.pack('<ffff',float('nan'),0,0,0))
        while True:
            packet=await read_frame(r)
            if packet.byte()==5: self.assertIn('tidak valid',packet.string(256)); break
        self.assertIsNotNone((await self.connect())[2])
    async def test_truncated_and_oversized_frames(self):
        for frame in [number(999999),number(-1),number(3)+b'\x0a']:
            r,w=await asyncio.open_connection('127.0.0.1',self.port); self.writers.append(w); w.write(frame); await w.drain(); w.close()
        self.assertIsNotNone((await self.connect())[2])
    async def test_disconnect_closes_room(self):
        _,host,_,code=await self.connect(); r,_,_,_=await self.connect(code)
        host.close(); await host.wait_closed(); await asyncio.sleep(.15)
        self.assertNotIn(code,self.relay.rooms)
        self.assertIn('tidak ditemukan',(await self.connect(code))[3])
    async def test_tls_encrypted_room(self):
        folder=ROOT/'.atlas-validation/RelayTls'; folder.mkdir(parents=True,exist_ok=True)
        cert,key=folder/'test-cert.pem',folder/'test-key.pem'
        openssl=pathlib.Path('C:/Program Files/Git/mingw64/bin/openssl.exe')
        if not openssl.exists(): self.skipTest('OpenSSL unavailable for local TLS fixture')
        subprocess.run([str(openssl),'req','-x509','-newkey','rsa:2048','-nodes','-keyout',str(key),'-out',str(cert),'-days','1','-subj','/CN=localhost','-addext','subjectAltName=DNS:localhost,IP:127.0.0.1'],check=True,capture_output=True)
        context=ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER); context.minimum_version=ssl.TLSVersion.TLSv1_2; context.load_cert_chain(cert,key)
        client=ssl.create_default_context(cafile=str(cert))
        tls_server=await asyncio.start_server(self.relay.handle,'127.0.0.1',0,ssl=context)
        try:
            port=tls_server.sockets[0].getsockname()[1]
            r,w,ident,code=await self.connect(port=port,context=client)
            self.assertEqual(ident,1); self.assertEqual(len(code),8); self.assertIsNotNone(w.get_extra_info('ssl_object'))
            self.assertEqual(len(await self.snapshot(r)),1)
        finally: tls_server.close(); await tls_server.wait_closed()

if __name__=='__main__': unittest.main(verbosity=2)
