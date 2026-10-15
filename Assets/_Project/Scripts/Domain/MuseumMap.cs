using System;
using System.Linq;

namespace ProjectAtlas.Domain
{
    public sealed class ZoneMap
    {
        public readonly string id, label;
        public readonly float width, depth, campusX, campusZ;
        public ZoneMap(string id, string label, float width, float depth, float x, float z)
        { this.id = id; this.label = label; this.width = width; this.depth = depth; campusX = x; campusZ = z; }
        public float MapX(float x) => Math.Max(0, Math.Min(1, x / width + .5f));
        public float MapY(float z) => Math.Max(0, Math.Min(1, z / depth + .5f));
        public bool Contains(float x, float z, float margin = 0) => Math.Abs(x) <= width / 2 - margin && Math.Abs(z) <= depth / 2 - margin;
    }
    public static class MuseumMap
    {
        public static readonly ZoneMap[] Zones = {
            new ZoneMap("Lobby", "Lobby", 24, 24, 0, 0),
            new ZoneMap("Gallery", "Galeri proyek", 32, 24, -36, 0),
            new ZoneMap("Archive", "Arsip", 28, 24, 34, 0),
            new ZoneMap("Showcase", "Auditorium", 28, 22, 0, 35),
            new ZoneMap("Courtyard", "Taman", 24, 18, 0, -33)
        };
        public static ZoneMap Find(string id) => Zones.FirstOrDefault(z => string.Equals(z.id, id, StringComparison.OrdinalIgnoreCase));
    }
}
