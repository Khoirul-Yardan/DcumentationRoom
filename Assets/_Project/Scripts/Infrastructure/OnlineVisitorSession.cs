using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using ProjectAtlas.Application;

namespace ProjectAtlas.Infrastructure
{
    // TLS relay client. Network threads never access Unity; snapshots are copied at the boundary.
    public sealed class OnlineVisitorSession : IVisitorSession
    {
        readonly object _gate = new object();
        readonly bool _loopbackTest;
        VisitorPose[] _visitors = Array.Empty<VisitorPose>();
        Peer _peer;
        TcpClient _pending;
        int _generation, _localId, _port = 47778;
        string _address = "", _status = "Belum terhubung", _room = "", _name;
        bool _host, _connected;
        public OnlineVisitorSession(bool allowInsecureLoopbackForTests = false) { _loopbackTest = allowInsecureLoopbackForTests; }
        public string Status { get { lock (_gate) return _status; } }
        public string RoomCode { get { lock (_gate) return _room; } }
        public bool Connected { get { lock (_gate) return _connected; } }
        public bool IsHost { get { lock (_gate) return _host; } }
        public int LocalId { get { lock (_gate) return _localId; } }
        public VisitorPose[] Visitors { get { lock (_gate) return _visitors.Select(v => v.Copy()).ToArray(); } }
        static void Run(ThreadStart action) { new Thread(action) { IsBackground = true }.Start(); }
        static BinaryReader Reader(byte[] bytes) => new BinaryReader(new MemoryStream(bytes), Encoding.UTF8);
        public void ConfigureRelay(string hostname, int port)
        {
            lock (_gate) { _address = (hostname ?? "").Trim(); _port = port; }
        }
        public void CreateRoom(string name, string catalogVersion) => Connect("", name, catalogVersion, true);
        public void JoinRoom(string code, string name, string catalogVersion) => Connect((code ?? "").Trim().ToUpperInvariant(), name, catalogVersion, false);
        void Connect(string code, string name, string catalog, bool host)
        {
            Leave(); string address; int port, generation;
            lock (_gate)
            {
                address = _address; port = _port; generation = _generation; _name = VisitorWire.Name(name);
                if (Uri.CheckHostName(address) == UriHostNameType.Unknown || address.Contains("://") || port < 1 || port > 65535) { _status = "Alamat server online belum valid. Isi domain server dan port."; return; }
                if (!host && !ValidRoomCode(code)) { _status = "Kode ruang harus 8 karakter huruf/angka dari host."; return; }
                _status = host ? "Membuat ruang online..." : "Menggabungkan ke ruang...";
            }
            Run(() => ConnectionLoop(address, port, code, VisitorWire.Name(name), catalog, host, generation));
        }
        public static bool ValidRoomCode(string code) => code != null && code.Length == 8 && code.All(c => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(c));
        void ConnectionLoop(string address, int port, string code, string name, string catalog, bool host, int generation)
        {
            TcpClient client = null; Stream stream = null; Peer peer = null;
            try
            {
                client = new TcpClient { NoDelay = true, ReceiveTimeout = 12000, SendTimeout = 5000 };
                lock (_gate) { if (generation != _generation) return; _pending = client; }
                var pending = client.BeginConnect(address, port, null, null);
                using (var completion = pending.AsyncWaitHandle) { if (!completion.WaitOne(6000)) throw new IOException("Connect timeout"); }
                client.EndConnect(pending); stream = client.GetStream();
                bool localTest = _loopbackTest && (address == "localhost" || (IPAddress.TryParse(address, out var ip) && IPAddress.IsLoopback(ip)));
                if (!localTest)
                {
                    var tls = new SslStream(stream, false); stream = tls;
                    tls.AuthenticateAsClient(address, null, SslProtocols.Tls12, true);
                }
                VisitorWire.Write(stream, VisitorWire.Frame(w => { w.Write((byte)(host ? 10 : 11)); w.Write(VisitorWire.Protocol); VisitorWire.Text(w, catalog, 256); VisitorWire.Text(w, name, 128); VisitorWire.Text(w, code, 16); }));
                int id; string room;
                using (var reader = Reader(VisitorWire.Read(stream)))
                {
                    byte kind = reader.ReadByte(); if (kind == 5) throw new InvalidDataException(VisitorWire.Text(reader, 256));
                    if (kind != 12) throw new InvalidDataException("Balasan server tidak valid.");
                    id = reader.ReadInt32(); room = VisitorWire.Text(reader, 16);
                    if (id < 1 || id > 1000000 || !ValidRoomCode(room)) throw new InvalidDataException("Identitas ruang tidak valid.");
                }
                peer = new Peer(client, stream); client = null; stream = null;
                lock (_gate)
                {
                    if (generation != _generation) return;
                    _pending = null; _peer = peer; _localId = id; _room = room; _connected = true; _host = host; _status = host ? "Host online" : "Terhubung online";
                }
                while (true)
                {
                    var snapshot = new List<VisitorPose>();
                    using (var reader = Reader(VisitorWire.Read(peer.Stream)))
                    {
                        byte kind = reader.ReadByte(); if (kind == 5) throw new InvalidDataException(VisitorWire.Text(reader, 256));
                        if (kind != 4) throw new InvalidDataException("Snapshot tidak valid.");
                        int count = reader.ReadInt32(); if (count < 1 || count > VisitorWire.MaxVisitors) throw new InvalidDataException("Jumlah pengunjung tidak valid.");
                        for (int i = 0; i < count; i++) { var pose = VisitorWire.Pose(reader); if (pose.id < 1 || snapshot.Any(p => p.id == pose.id)) throw new InvalidDataException("ID duplikat."); snapshot.Add(pose); }
                    }
                    lock (_gate) { if (generation != _generation) return; _visitors = snapshot.ToArray(); }
                }
            }
            catch (Exception e) when (NetworkError(e))
            {
                lock (_gate) if (generation == _generation)
                {
                    _connected = _host = false; _visitors = Array.Empty<VisitorPose>(); _room = "";
                    _status = e is InvalidDataException ? e.Message : e is AuthenticationException ? "Sertifikat TLS server tidak valid. Hubungi pengelola server." : "Koneksi online gagal/terputus. Periksa alamat server atau coba gabung ulang.";
                }
            }
            finally
            {
                peer?.Close(); stream?.Dispose(); client?.Close();
                lock (_gate) if (generation == _generation) { _pending = null; _peer = null; }
            }
        }
        public void Publish(VisitorPose pose)
        {
            if (!VisitorWire.Valid(pose)) return;
            lock (_gate)
            {
                if (!_connected || _peer == null) return;
                var safe = pose.Copy(); safe.id = _localId; safe.name = _name;
                _peer.Queue(VisitorWire.Frame(w => { w.Write((byte)3); VisitorWire.Pose(w, safe); }));
            }
        }
        public void Leave()
        {
            lock (_gate)
            {
                _generation++; _pending?.Close(); _pending = null; _peer?.Close(); _peer = null;
                _visitors = Array.Empty<VisitorPose>(); _connected = _host = false; _localId = 0; _room = ""; _status = "Solo / belum terhubung";
            }
        }
        public void Dispose() => Leave();
        static bool NetworkError(Exception e) => e is IOException || e is InvalidDataException || e is SocketException || e is ObjectDisposedException || e is InvalidOperationException || e is ArgumentException || e is AuthenticationException || e is System.Security.SecurityException || e is NotSupportedException;
        sealed class Peer
        {
            readonly TcpClient _client;
            readonly object _gate = new object();
            readonly AutoResetEvent _wake = new AutoResetEvent(false);
            byte[] _latest; bool _closed;
            public Stream Stream { get; }
            public Peer(TcpClient client, Stream stream) { _client = client; Stream = stream; Run(WriteLoop); }
            public void Queue(byte[] frame) { lock (_gate) { if (_closed) return; _latest = frame; _wake.Set(); } }
            void WriteLoop()
            {
                try
                {
                    while (true)
                    {
                        _wake.WaitOne(); byte[] frame;
                        lock (_gate) { if (_closed) return; frame = _latest; _latest = null; }
                        if (frame != null) VisitorWire.Write(Stream, frame);
                    }
                }
                catch (Exception e) when (NetworkError(e)) { Close(); }
                finally { _wake.Dispose(); }
            }
            public void Close() { lock (_gate) { if (_closed) return; _closed = true; _client.Close(); try { Stream.Dispose(); } catch (Exception e) when (NetworkError(e)) { } _wake.Set(); } }
        }
    }
}
