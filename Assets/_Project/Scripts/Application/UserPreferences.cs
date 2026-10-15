using System;

namespace ProjectAtlas.Application
{
    [Serializable]
    public sealed class UserPreferences
    {
        public int version = 1;
        public float volume = .7f, sensitivity = .1f, fieldOfView = 65, textScale = 1;
        public int quality = 1;
        public bool reduceMotion = true;
        public string relayAddress = "", visitorName = "Pengunjung";
        public int relayPort = 47778;
        public string[] bookmarks = Array.Empty<string>();
        public void Sanitize()
        {
            volume = Clamp(volume, 0, 1, .7f);
            sensitivity = Clamp(sensitivity, .03f, .3f, .1f);
            fieldOfView = Clamp(fieldOfView, 55, 90, 65);
            textScale = Clamp(textScale, 1, 1.5f, 1);
            quality = Math.Max(0, quality);
            bookmarks = bookmarks ?? Array.Empty<string>();
        }
        static float Clamp(float v, float min, float max, float fallback) => float.IsNaN(v) || float.IsInfinity(v) ? fallback : Math.Max(min, Math.Min(max, v));
    }
}
