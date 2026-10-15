using System;
using System.Linq;
using ProjectAtlas.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    public sealed partial class MuseumUI
    {
        MuseumMinimap _minimap;
        public void AttachMinimap(Transform player)
        {
            var go = new GameObject("Minimap", typeof(RectTransform)); go.transform.SetParent(transform, false); go.transform.SetAsFirstSibling();
            _minimap = go.AddComponent<MuseumMinimap>(); _minimap.Initialize(this, Host, player);
        }
        public void BindMinimap(string zone, Transform root) => _minimap?.Bind(zone, root);
        public void RefreshMinimap(bool exploring) => _minimap?.RefreshVisibility(exploring);
        public void Map()
        {
            var p = Panel("DENAH MUSEUM / UTARA DI ATAS", "Peta & perjalanan cepat");
            var area = Box(p.transform, "Campus map", new Color32(227, 235, 230, 255)); Rect(area, new Vector2(.04f, .09f), new Vector2(.69f, .77f));
            // Conceptual campus positions: zones load separately; lines denote portal travel.
            var horizontal = Box(area.transform, "East West route", new Color32(157, 181, 175, 255)); Rect(horizontal, new Vector2(.18f, .485f), new Vector2(.82f, .515f));
            var vertical = Box(area.transform, "North South route", new Color32(157, 181, 175, 255)); Rect(vertical, new Vector2(.485f, .18f), new Vector2(.515f, .82f));
            foreach (var zone in MuseumMap.Zones)
            {
                var center = new Vector2(.5f + zone.campusX / 120, .5f + zone.campusZ / 108);
                var size = zone.id == "Lobby" ? new Vector2(.28f, .20f) : new Vector2(.30f, .20f);
                var b = Button(area.transform, zone.label + (Host.Zone == zone.id ? "\nAnda di sini" : ""), () => Host.Navigate(zone.id));
                Rect(b.gameObject, center - size / 2, center + size / 2);
                if (Host.Zone == zone.id) b.GetComponent<Image>().color = Ink;
            }
            var guide = Scroll(p.transform, new Vector2(.72f, .1f), new Vector2(.96f, .77f));
            Heading(guide, "LOKASI ANDA"); Paragraph(guide, MuseumMap.Find(Host.Zone)?.label ?? "Menu utama");
            Paragraph(guide, "Klik ruang untuk berpindah. Garis menunjukkan hubungan portal antarruang.");
            Heading(guide, "MINIMAP"); Paragraph(guide, "Biru: posisi dan arah Anda\nHijau: stan / terminal\nJingga: portal\nUngu: pengunjung lain");
            Button(guide, "Ke Lobby", () => Host.Navigate("Lobby"));
        }
        public void Multiplayer()
        {
            if (Host.Catalog == null) { Error("Katalog harus tersedia sebelum membuka sesi."); return; }
            var p = Panel("JELAJAH BERSAMA / ONLINE", "Multiplayer museum");
            var body = Scroll(p.transform, new Vector2(.05f, .08f), new Vector2(.95f, .77f));
            Paragraph(body, "Buat ruang lalu bagikan kode kepada teman. Maksimum 8 pengunjung. Semua memakai paket museum yang sama; katalog dan perpindahan ruang dapat digunakan masing-masing.");
            Heading(body, "NAMA PENGUNJUNG"); var name = Input(body, "Nama Anda"); name.text = Host.Preferences.visitorName; name.characterLimit = 24; name.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            Heading(body, "KODE RUANG"); var code = Input(body, "8 karakter dari host"); code.characterLimit = 8; code.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var status = Text(body, Host.Session.Status, 23, Teal); status.gameObject.AddComponent<LayoutElement>().minHeight = 90;
            var live = p.AddComponent<SessionStatusLabel>(); live.Initialize(Host, status);
            Button(body, "Buat ruang online", () => { Host.Session.ConfigureRelay(Host.Preferences.relayAddress, Host.Preferences.relayPort); Host.Preferences.visitorName = name.text; Host.SavePreferences(); Host.Session.CreateRoom(name.text, Host.Catalog.catalogVersion); });
            Button(body, "Gabung dengan kode", () => { Host.Session.ConfigureRelay(Host.Preferences.relayAddress, Host.Preferences.relayPort); Host.Preferences.visitorName = name.text; Host.SavePreferences(); Host.Session.JoinRoom(code.text, name.text, Host.Catalog.catalogVersion); });
            Button(body, "Salin kode ruang", () => { if (Host.Session.Connected) GUIUtility.systemCopyBuffer = Host.Session.RoomCode; });
            Button(body, "Tutup panel dan jelajahi", () => { if (Host.Zone == "Frontend") Host.Navigate("Lobby"); else Clear(); });
            Button(body, "Keluar dari ruang", () => Host.Session.Leave());
            Button(body, "Pengaturan server", RelaySettings);
            if (string.IsNullOrWhiteSpace(Host.Preferences.relayAddress)) Paragraph(body, "Server online belum dikonfigurasi. Pengelola museum perlu mengaktifkan server sebelum ruang online dapat dibuat.");
            Paragraph(body, "Saat host keluar, ruang ditutup dan pengunjung kembali ke mode solo. Voice chat dan pemutaran video bersama belum tersedia.");
        }
        void RelaySettings()
        {
            var p = Panel("KONEKSI MUSEUM", "Server online"); var body = Scroll(p.transform, new Vector2(.05f, .08f), new Vector2(.95f, .77f));
            Paragraph(body, "Masukkan alamat server yang diberikan pengelola museum. Semua pengunjung menggunakan server yang sama.");
            var address = Input(body, "Domain server"); address.text = Host.Preferences.relayAddress; address.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var port = Input(body, "Port"); port.text = Host.Preferences.relayPort.ToString(); port.characterLimit = 5; port.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            Button(body, "Simpan server", () => { if (!int.TryParse(port.text, out int value) || value < 1 || value > 65535 || Uri.CheckHostName(address.text.Trim()) == UriHostNameType.Unknown) { Error("Alamat atau port server tidak valid."); return; } Host.Preferences.relayAddress = address.text.Trim(); Host.Preferences.relayPort = value; Host.SavePreferences(); Back(); });
        }
    }
}
