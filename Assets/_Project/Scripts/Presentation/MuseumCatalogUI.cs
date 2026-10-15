using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAtlas.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    public sealed partial class MuseumUI
    {
        public void Catalog(string initialStatus = "")
        {
            if (Host.Catalog == null) { Error("Katalog belum tersedia. Periksa paket lokal lalu jalankan ulang aplikasi."); return; }
            var p = Panel("KOLEKSI / " + (Host.Catalog.isSample ? "DATA SINTETIS" : "KATALOG LOKAL"), initialStatus == "archived" ? "Arsip proyek" : "Katalog proyek");
            var search = Input(p.transform, "Cari judul, ringkasan, tim atau tag..."); Rect(search.gameObject, new Vector2(.05f, .70f), new Vector2(.95f, .78f));
            var body = Scroll(p.transform, new Vector2(.05f, .17f), new Vector2(.95f, .60f));
            var footer = Text(p.transform, "", 20, Teal); Rect(footer.gameObject, new Vector2(.31f, .06f), new Vector2(.69f, .13f)); footer.alignment = TextAlignmentOptions.Center;
            int page = 0, year = 0; string status = initialStatus;
            var categories = new HashSet<string>();
            var categoryButton = Button(p.transform, "Kategori: semua", () => { }); Rect(categoryButton.gameObject, new Vector2(.05f, .62f), new Vector2(.36f, .69f));
            var yearButton = Button(p.transform, "Tahun: semua", () => { }); Rect(yearButton.gameObject, new Vector2(.37f, .62f), new Vector2(.59f, .69f));
            var statusButton = Button(p.transform, "Status: " + StatusLabel(status), () => { }); Rect(statusButton.gameObject, new Vector2(.60f, .62f), new Vector2(.83f, .69f));
            Button prev = null, next = null;
            Action draw = () =>
            {
                Empty(body);
                var all = CatalogQuery.SearchCategories(Host.Catalog, search.text, categories.ToArray(), year, status);
                page = Mathf.Clamp(page, 0, Mathf.Max(0, (all.Length - 1) / 8));
                foreach (var item in CatalogQuery.Page(all, page))
                {
                    var captured = item;
                    var b = Button(body, item.title + " / " + item.year + " / " + StatusLabel(item.developmentStatus) + (item.isSample ? " / SINTETIS" : "") + "\n" + item.summary, () => Detail(captured.id));
                    b.GetComponent<LayoutElement>().preferredHeight = 92 * Host.Preferences.textScale;
                }
                if (all.Length == 0) Paragraph(body, "Tidak ada proyek yang cocok. Ubah pencarian atau reset filter.");
                footer.text = all.Length + " proyek / " + (page + 1) + " dari " + Mathf.Max(1, (all.Length + 7) / 8);
                if (prev != null) prev.interactable = page > 0;
                if (next != null) next.interactable = (page + 1) * 8 < all.Length;
                body.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1;
            };
            categoryButton.onClick.AddListener(() =>
            {
                var modal = Panel("FILTER / PILIH SATU ATAU BEBERAPA", "Kategori proyek");
                var list = Scroll(modal.transform, new Vector2(.05f, .08f), new Vector2(.95f, .77f));
                Button(list, "Semua kategori", () => { categories.Clear(); Back(); page = 0; SetLabel(categoryButton, "Kategori: semua"); draw(); });
                foreach (var c in Host.Catalog.categories.OrderBy(x => x.sortOrder))
                {
                    Button option = null;
                    option = Button(list, (categories.Contains(c.id) ? "[x] " : "[ ] ") + c.label, () =>
                    {
                        if (!categories.Add(c.id)) categories.Remove(c.id);
                        SetLabel(option, (categories.Contains(c.id) ? "[x] " : "[ ] ") + c.label);
                        SetLabel(categoryButton, categories.Count == 0 ? "Kategori: semua" : "Kategori: " + categories.Count + " dipilih"); page = 0; draw();
                    });
                }
            });
            yearButton.onClick.AddListener(() => Choose("Tahun", new[] { "Semua tahun" }.Concat(Host.Catalog.projects.Select(x => x.year.ToString()).Distinct().OrderByDescending(x => x)).ToArray(), value => { year = int.TryParse(value, out var y) ? y : 0; SetLabel(yearButton, "Tahun: " + (year == 0 ? "semua" : year.ToString())); page = 0; draw(); }));
            var statuses = new[] { "", "planned", "in_progress", "completed", "archived" };
            statusButton.onClick.AddListener(() => Choose("Status pengembangan", statuses.Select(StatusLabel).ToArray(), value => { status = statuses.First(x => StatusLabel(x) == value); SetLabel(statusButton, "Status: " + value); page = 0; draw(); }));
            var reset = Button(p.transform, "Reset", () => { categories.Clear(); year = 0; status = ""; search.SetTextWithoutNotify(""); SetLabel(categoryButton, "Kategori: semua"); SetLabel(yearButton, "Tahun: semua"); SetLabel(statusButton, "Status: semua"); page = 0; draw(); }); Rect(reset.gameObject, new Vector2(.84f, .62f), new Vector2(.95f, .69f));
            prev = Button(p.transform, "Sebelumnya", () => { page--; draw(); }); Rect(prev.gameObject, new Vector2(.05f, .05f), new Vector2(.29f, .14f));
            next = Button(p.transform, "Berikutnya", () => { page++; draw(); }); Rect(next.gameObject, new Vector2(.71f, .05f), new Vector2(.95f, .14f));
            search.onValueChanged.AddListener(_ => { page = 0; draw(); }); draw();
        }
        public void Detail(string id)
        {
            var item = Host.Catalog.projects.FirstOrDefault(x => x.id == id);
            if (item == null) { Error("Proyek tidak ditemukan."); return; }
            var p = Panel((item.isSample ? "CONTOH SINTETIS / " : "PROYEK / ") + item.year + " / " + StatusLabel(item.developmentStatus), item.title);
            var body = Scroll(p.transform, new Vector2(.05f, .19f), new Vector2(.95f, .66f));
            Action<int> draw = tab =>
            {
                Empty(body);
                if (tab == 0)
                {
                    Paragraph(body, item.title);
                    Heading(body, "RINGKASAN"); Paragraph(body, item.summary);
                    Heading(body, "MASALAH"); Paragraph(body, item.problem);
                    Heading(body, "SOLUSI"); Paragraph(body, item.solution);
                    Heading(body, "HASIL"); Paragraph(body, string.Join("\n", ContentValidator.Safe(item.outcomes)));
                    Heading(body, "TIM"); Paragraph(body, string.Join("\n", ContentValidator.Safe(item.team).Select(x => x.displayName + " — " + x.role)));
                    Heading(body, "TEKNOLOGI"); Paragraph(body, string.Join(" · ", ContentValidator.Safe(item.techStack)));
                    foreach (var related in ContentValidator.Safe(item.relatedProjectIds)) { var r = Host.Catalog.projects.First(x => x.id == related); Button(body, "Terkait: " + r.title, () => Detail(r.id)); }
                    Paragraph(body, "Diperbarui: " + item.updatedAt + (item.isSample ? " / Sumber: fixture sintetis lokal" : " / Paket offline"));
                }
                if (tab == 1)
                {
                    var docs = Host.Catalog.documents.Where(d => ContentValidator.Safe(item.documentIds).Contains(d.id)).ToArray();
                    var filter = Button(body, "Jenis: semua", () => { });
                    var files = new GameObject("Documents", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); files.transform.SetParent(body, false);
                    files.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false; files.GetComponent<VerticalLayoutGroup>().spacing = 12; files.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    Action<string> filesDraw = mime => { Empty(files.transform); foreach (var doc in docs) { var version = Host.Catalog.documentVersions.First(v => v.id == doc.currentVersionId); if (mime != "" && mime != version.mimeType) continue; Button(files.transform, doc.title + " / " + version.versionLabel + " / " + version.mimeType, () => DocumentViewer(doc.id)); } if (!docs.Any(d => mime == "" || Host.Catalog.documentVersions.First(v => v.id == d.currentVersionId).mimeType == mime)) Paragraph(files.transform, "Dokumentasi belum tersedia untuk jenis ini."); };
                    filter.onClick.AddListener(() => Choose("Jenis dokumen", new[] { "Semua jenis" }.Concat(docs.Select(d => Host.Catalog.documentVersions.First(v => v.id == d.currentVersionId).mimeType).Distinct()).ToArray(), type => { SetLabel(filter, "Jenis: " + type); filesDraw(type == "Semua jenis" ? "" : type); })); filesDraw("");
                }
                if (tab == 2 || tab == 3)
                {
                    var ids = tab == 2 ? item.galleryMediaIds : item.demoMediaIds;
                    if (ContentValidator.Safe(ids).Length == 0) Paragraph(body, "Media belum tersedia.");
                    foreach (var mediaId in ContentValidator.Safe(ids)) Button(body, "Buka " + mediaId, () => OpenMedia(mediaId));
                }
                if (tab == 4)
                {
                    var events = Host.Catalog.milestones.Where(m => m.projectId == id).OrderBy(m => m.date, StringComparer.Ordinal).ThenBy(m => m.id, StringComparer.Ordinal).ToArray();
                    if (events.Length == 0) Paragraph(body, "Timeline belum tersedia.");
                    foreach (var m in events) { Heading(body, m.date + " / " + m.title); Paragraph(body, m.description); foreach (var doc in ContentValidator.Safe(m.evidenceDocumentIds)) Button(body, "Bukti: " + Host.Catalog.documents.First(d => d.id == doc).title, () => DocumentViewer(doc)); }
                }
                body.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1;
            };
            var tabs = new[] { "Ringkasan", "Dokumentasi", "Galeri", "Demo", "Timeline" };
            for (int i = 0; i < tabs.Length; i++) { int index = i; var b = Button(p.transform, tabs[i], () => draw(index)); Rect(b.gameObject, new Vector2(.05f + i * .182f, .69f), new Vector2(.22f + i * .182f, .77f)); }
            var image = Button(p.transform, "Gambar", () => Viewer(item.coverMediaId)); Rect(image.gameObject, new Vector2(.05f, .06f), new Vector2(.31f, .15f)); image.interactable = !string.IsNullOrEmpty(item.coverMediaId);
            Button bookmark = null;
            bookmark = Button(p.transform, HasBookmark(id) ? "Hapus bookmark" : "Simpan bookmark", () => { ToggleBookmark(id); SetLabel(bookmark, HasBookmark(id) ? "Hapus bookmark" : "Simpan bookmark"); }); Rect(bookmark.gameObject, new Vector2(.35f, .06f), new Vector2(.65f, .15f));
            var gallery = Button(p.transform, "Lihat di galeri", () => Host.ShowInGallery(id)); Rect(gallery.gameObject, new Vector2(.69f, .06f), new Vector2(.95f, .15f)); draw(0);
        }
        public void Exhibitions()
        {
            if (Host.Catalog == null) { Error("Koleksi belum tersedia. Periksa paket katalog lokal."); return; }
            var p = Panel("AUDITORIUM / KOLEKSI TERKURASI", "Mode pameran");
            var body = Scroll(p.transform, new Vector2(.05f, .07f), new Vector2(.95f, .77f));
            Button(body, "Masuk ruang auditorium", () => Host.Navigate("Showcase"));
            if (Host.Catalog.exhibitions.Length == 0) Paragraph(body, "Koleksi presentasi belum tersedia.");
            foreach (var exhibition in Host.Catalog.exhibitions)
            {
                Heading(body, exhibition.title);
                foreach (var id in ContentValidator.Safe(exhibition.projectIds)) Button(body, Host.Catalog.projects.First(x => x.id == id).title, () => Detail(id));
                foreach (var id in ContentValidator.Safe(exhibition.presentationMediaIds)) Button(body, "Putar: " + id, () => OpenMedia(id));
            }
        }
        void Choose(string title, string[] choices, Action<string> selected)
        {
            var p = Panel("PILIH FILTER", title); var list = Scroll(p.transform, new Vector2(.05f, .07f), new Vector2(.95f, .77f));
            foreach (var choice in choices) Button(list, choice, () => { Back(); selected(choice); });
        }
        static string StatusLabel(string status) => status == "planned" ? "Direncanakan" : status == "in_progress" ? "Dikerjakan" : status == "completed" ? "Selesai" : status == "archived" ? "Arsip" : "semua";
        static void Empty(Transform body) { foreach (Transform child in body) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
        static void SetLabel(Button button, string value) { button.GetComponentInChildren<TMP_Text>().text = value; }
        void Heading(Transform body, string text) { var t = Text(body, text, 20, Teal); t.gameObject.AddComponent<LayoutElement>().minHeight = 36 * Host.Preferences.textScale; }
        void Paragraph(Transform body, string text) { var t = Text(body, string.IsNullOrWhiteSpace(text) ? "Belum tersedia" : text, 24, Ink); t.overflowMode = TextOverflowModes.Overflow; t.gameObject.AddComponent<LayoutElement>().minHeight = 48; }
    }
}
