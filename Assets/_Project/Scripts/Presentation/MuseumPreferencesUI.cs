using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    public sealed partial class MuseumUI
    {
        public bool HasBookmark(string id) => Host.Preferences.bookmarks.Contains(id);
        public void ToggleBookmark(string id)
        {
            var ids = Host.Preferences.bookmarks.ToList();
            if (!ids.Remove(id)) ids.Add(id);
            Host.Preferences.bookmarks = ids.Distinct().ToArray(); Host.SavePreferences();
        }
        public void Bookmarks()
        {
            if (Host.Catalog == null) { Error("Katalog belum tersedia. Bookmark tetap disimpan pada perangkat ini."); return; }
            var p = Panel("TERSIMPAN DI PERANGKAT INI", "Bookmark proyek");
            var list = Scroll(p.transform, new Vector2(.05f, .07f), new Vector2(.95f, .77f));
            if (Host.Preferences.bookmarks.Length == 0) Paragraph(list, "Belum ada bookmark. Buka proyek lalu pilih Simpan bookmark.");
            foreach (var id in Host.Preferences.bookmarks)
            {
                var project = Host.Catalog.projects.FirstOrDefault(x => x.id == id);
                Button(list, project != null ? project.title : id + " / tidak tersedia — hapus bookmark", () => { if (project != null) Detail(id); else { ToggleBookmark(id); Back(); Bookmarks(); } });
            }
        }
        public void Settings()
        {
            var p = Panel("PENGATURAN / TERSIMPAN OTOMATIS", "Sesuaikan pengalaman");
            var list = Scroll(p.transform, new Vector2(.05f, .07f), new Vector2(.95f, .77f));
            Setting(list, "Volume audio", () => Host.Preferences.volume.ToString("P0"), d => Host.Preferences.volume = Mathf.Clamp01(Host.Preferences.volume + d * .1f));
            Setting(list, "Sensitivitas mouse", () => Host.Preferences.sensitivity.ToString("F2"), d => Host.Preferences.sensitivity = Mathf.Clamp(Host.Preferences.sensitivity + d * .01f, .03f, .3f));
            Setting(list, "Sudut pandang (FOV)", () => Host.Preferences.fieldOfView.ToString("F0"), d => Host.Preferences.fieldOfView = Mathf.Clamp(Host.Preferences.fieldOfView + d * 5, 55, 90));
            Setting(list, "Ukuran teks", () => Host.Preferences.textScale.ToString("P0"), d => Host.Preferences.textScale = Mathf.Clamp(Host.Preferences.textScale + d * .1f, 1, 1.5f));
            Setting(list, "Kualitas", () => QualitySettings.names[Mathf.Clamp(Host.Preferences.quality, 0, QualitySettings.names.Length - 1)], d => Host.Preferences.quality = Mathf.Clamp(Host.Preferences.quality + d, 0, QualitySettings.names.Length - 1));
            Paragraph(list, "Gerakan kamera dekoratif dan head bob dinonaktifkan. Gunakan katalog langsung jika lebih nyaman membaca tanpa berjalan.");
            Button(list, "Reset pengaturan", () => { var defaults = new ProjectAtlas.Application.UserPreferences(); var prefs = Host.Preferences; prefs.volume = defaults.volume; prefs.sensitivity = defaults.sensitivity; prefs.fieldOfView = defaults.fieldOfView; prefs.quality = defaults.quality; prefs.textScale = defaults.textScale; prefs.reduceMotion = true; Host.SavePreferences(); Back(); Settings(); });
        }
        void Setting(Transform list, string title, System.Func<string> value, System.Action<int> change)
        {
            var row = Box(list, title, Color.white); row.AddComponent<LayoutElement>().preferredHeight = 85;
            var label = Text(row.transform, title + ": " + value(), 23, Ink); Rect(label.gameObject, new Vector2(.025f, .1f), new Vector2(.72f, .9f));
            var minus = Button(row.transform, "−", () => { change(-1); Host.SavePreferences(); label.text = title + ": " + value(); }); Rect(minus.gameObject, new Vector2(.75f, .12f), new Vector2(.85f, .88f));
            var plus = Button(row.transform, "+", () => { change(1); Host.SavePreferences(); label.text = title + ": " + value(); }); Rect(plus.gameObject, new Vector2(.87f, .12f), new Vector2(.97f, .88f));
        }
        public void ApplyTextScale()
        {
            foreach (var label in GetComponentsInChildren<AtlasTextSize>(true)) label.Apply(Host.Preferences.textScale);
        }
    }
}
