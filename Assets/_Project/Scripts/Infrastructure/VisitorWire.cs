using System;
using System.IO;
using System.Text;
using ProjectAtlas.Application;
using ProjectAtlas.Domain;

namespace ProjectAtlas.Infrastructure
{
    // Bounded binary protocol. No deserialization of arbitrary types or file paths.
    public static class VisitorWire
    {
        public const int Protocol = 1, MaxVisitors = 8, MaxFrame = 4096;
        public static byte[] Frame(Action<BinaryWriter> write)
        {
            using (var data = new MemoryStream())
            {
                using (var writer = new BinaryWriter(data, Encoding.UTF8, true)) write(writer);
                if (data.Length > MaxFrame) throw new InvalidDataException("Frame too large");
                return data.ToArray();
            }
        }
        public static void Text(BinaryWriter writer, string text, int limit)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? ""); if (bytes.Length > limit) throw new InvalidDataException("Text too long");
            writer.Write(bytes.Length); writer.Write(bytes);
        }
        public static string Text(BinaryReader reader, int limit)
        {
            int count = reader.ReadInt32(); if (count < 0 || count > limit) throw new InvalidDataException("Invalid text length");
            var bytes = reader.ReadBytes(count); if (bytes.Length != count) throw new EndOfStreamException();
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        public static string Name(string text)
        {
            var clean = (text ?? "Pengunjung").Trim(); var result = new StringBuilder();
            foreach (char c in clean) if (!char.IsControl(c) && c != '<' && c != '>' && result.Length < 24) result.Append(c);
            return result.Length == 0 ? "Pengunjung" : result.ToString();
        }
        public static bool Valid(VisitorPose pose)
        {
            if (pose == null || !Finite(pose.x) || !Finite(pose.y) || !Finite(pose.z) || !Finite(pose.yaw)) return false;
            if (pose.y < -2 || pose.y > 8 || Math.Abs(pose.yaw) > 36000) return false;
            if (pose.zone == "Frontend") return Math.Abs(pose.x) < 100 && Math.Abs(pose.z) < 100;
            return MuseumMap.Find(pose.zone)?.Contains(pose.x, pose.z) == true;
        }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        public static void Pose(BinaryWriter w, VisitorPose p)
        { w.Write(p.id); Text(w, Name(p.name), 128); Text(w, p.zone, 32); w.Write(p.x); w.Write(p.y); w.Write(p.z); w.Write(p.yaw); }
        public static VisitorPose Pose(BinaryReader r)
        {
            var p = new VisitorPose { id = r.ReadInt32(), name = Text(r, 128), zone = Text(r, 32), x = r.ReadSingle(), y = r.ReadSingle(), z = r.ReadSingle(), yaw = r.ReadSingle() };
            if (!Valid(p)) throw new InvalidDataException("Invalid visitor position"); return p;
        }
        public static void Write(Stream stream, byte[] data)
        { var length = BitConverter.GetBytes(data.Length); stream.Write(length, 0, length.Length); stream.Write(data, 0, data.Length); }
        public static byte[] Read(Stream stream)
        {
            var header = ReadExact(stream, 4); int length = BitConverter.ToInt32(header, 0);
            if (length < 1 || length > MaxFrame) throw new InvalidDataException("Invalid frame length"); return ReadExact(stream, length);
        }
        static byte[] ReadExact(Stream stream, int length)
        {
            var data = new byte[length]; int offset = 0;
            while (offset < length) { int read = stream.Read(data, offset, length - offset); if (read == 0) throw new EndOfStreamException(); offset += read; }
            return data;
        }
    }
}
