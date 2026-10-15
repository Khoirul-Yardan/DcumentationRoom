using System;
using System.IO;
using System.Linq;
using System.Threading;
using ProjectAtlas.Application;
using ProjectAtlas.Domain;
using ProjectAtlas.Infrastructure;

public static class WorldNetworkChecks
{
    static int _checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); Console.WriteLine("PASS " + label); _checks++; }
    static VisitorPose Pose(string zone = "Lobby", float x = 0) => new VisitorPose { zone = zone, x = x, y = .05f, z = 0, yaw = 45, id = 999, name = "forged" };
    static void Wait(Func<bool> condition, string label, params OnlineVisitorSession[] sessions)
    {
        var until = DateTime.UtcNow.AddSeconds(8);
        while (!condition() && DateTime.UtcNow < until) { foreach (var session in sessions) session.Publish(Pose()); Thread.Sleep(40); }
        Check(condition(), label);
    }
    public static int Main(string[] args)
    {
        try
        {
            foreach (var map in MuseumMap.Zones)
            {
                Check(map.MapX(0) == .5f && map.MapY(0) == .5f, map.id + " minimap center");
                Check(map.MapX(-map.width / 2) == 0 && map.MapY(map.depth / 2) == 1, map.id + " minimap corners / north-up");
                Check(!map.Contains(map.width, 0) && map.MapX(map.width) == 1, map.id + " bounds / marker clamp");
            }
            Check(MuseumMap.Find("gallery").id == "Gallery" && MuseumMap.Find("unknown") == null, "case-insensitive zone binding, unknown zone rejected");
            Check(!VisitorWire.Valid(Pose("Lobby", float.NaN)) && !VisitorWire.Valid(Pose("Lobby", 50)) && !VisitorWire.Valid(Pose("Unknown")), "invalid/nonfinite/out-of-bounds network poses rejected");
            Check(VisitorWire.Name(" <b>Visitor\n</b> ").Length <= 24 && !VisitorWire.Name("<b>Visitor</b>").Contains("<"), "visitor label sanitized");
            Check(OnlineVisitorSession.ValidRoomCode("ABCDEFGH") && !OnlineVisitorSession.ValidRoomCode("1234") && !OnlineVisitorSession.ValidRoomCode("AAAAAAAAA"), "room code shape enforced");
            var pose = Pose(); pose.name = "Tamu Unicode";
            var frame = VisitorWire.Frame(w => VisitorWire.Pose(w, pose));
            using (var reader = new BinaryReader(new MemoryStream(frame))) Check(VisitorWire.Pose(reader).yaw == 45, "pose binary roundtrip");
            bool rejected = false;
            try { VisitorWire.Read(new MemoryStream(BitConverter.GetBytes(99999))); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "oversized frame rejected before allocating payload");
            int port = int.Parse(args[0]);
            using (var host = new OnlineVisitorSession(true))
            using (var first = new OnlineVisitorSession(true))
            using (var second = new OnlineVisitorSession(true))
            using (var mismatch = new OnlineVisitorSession(true))
            {
                foreach (var session in new[] { host, first, second, mismatch }) session.ConfigureRelay("127.0.0.1", port);
                host.CreateRoom("Host Atlas", "sample-test"); Wait(() => host.Connected, "create online room through relay", host);
                Check(OnlineVisitorSession.ValidRoomCode(host.RoomCode), "server issues room code");
                first.JoinRoom(host.RoomCode, "Visitor One", "sample-test"); second.JoinRoom(host.RoomCode, "Visitor Two", "sample-test");
                Wait(() => first.Connected && second.Connected && first.Visitors.Length == 3 && host.Visitors.Length == 3, "two clients join room and receive host snapshot", host, first, second);
                Check(first.Visitors.Select(p => p.id).Distinct().Count() == 3 && first.LocalId != second.LocalId, "server assigns unique identities");
                first.Publish(Pose("Gallery", 2));
                Wait(() => second.Visitors.Any(p => p.id == first.LocalId && p.zone == "Gallery" && p.x == 2), "remote movement and independent zone replicated", host, second);
                Check(second.Visitors.First(p => p.id == first.LocalId).name == "Visitor One", "client cannot spoof name or host ID");
                var copy = host.Visitors; copy[0].name = "mutated"; Check(host.Visitors.All(p => p.name != "mutated"), "snapshot reads cannot mutate shared state");
                mismatch.JoinRoom(host.RoomCode, "Mismatch", "wrong-version"); Wait(() => mismatch.Status.Contains("Versi katalog berbeda"), "catalog mismatch rejected", host, first, second);
                int gone = first.LocalId; first.Leave(); Wait(() => second.Visitors.All(p => p.id != gone) && second.Visitors.Length == 2, "disconnect removes remote visitor", host, second);
                first.JoinRoom(host.RoomCode, "Reconnect", "sample-test"); Wait(() => first.Connected && first.Visitors.Length == 3, "reconnect after explicit leave", host, first, second);
                string closed = host.RoomCode; host.Leave(); Wait(() => !first.Connected && !second.Connected, "host departure closes room and clients return solo", first, second);
                mismatch.JoinRoom(closed, "Late", "sample-test"); Wait(() => mismatch.Status.Contains("Ruang tidak ditemukan"), "closed room code cannot be reused");
                host.CreateRoom("New Host", "sample-test"); Wait(() => host.Connected, "create a fresh room after leaving", host);
                Check(host.RoomCode != closed, "new room gets a fresh code");
            }
            using (var secure = new OnlineVisitorSession())
            {
                secure.ConfigureRelay("127.0.0.1", port); secure.CreateRoom("TLS required", "sample-test");
                Wait(() => secure.Status.Contains("gagal") || secure.Status.Contains("Sertifikat"), "production client rejects a plaintext relay");
                Check(!secure.Connected, "no plaintext fallback in production client");
            }
            Console.WriteLine("TOTAL PASS " + _checks + " / network integration used explicit loopback test mode; public internet/TLS certificate deployment not exercised");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
