using System.Linq;
using ProjectAtlas.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    public sealed partial class MuseumUI
    {
        public void OpenMedia(string id)
        {
            var media = Host.Catalog.media.FirstOrDefault(x => x.id == id);
            if (media == null) { Error("Media tidak ditemukan."); return; }
            if (media.kind == "video") Video(id); else Viewer(id);
        }
        public void DocumentViewer(string id)
        {
            var document = Host.Catalog.documents.FirstOrDefault(d => d.id == id);
            if (document == null) { Error("Dokumen tidak ditemukan."); return; }
            var version = Host.Catalog.documentVersions.First(v => v.id == document.currentVersionId);
            var p = Panel("DOKUMENTASI / " + version.versionLabel + " / " + version.modifiedAt, document.title);
            var pages = ContentValidator.Safe(version.previewMediaIds); int page = 0;
            var info = Text(p.transform, "", 19, Ink); Rect(info.gameObject, new Vector2(.05f, .07f), new Vector2(.56f, .15f));
            if (pages.Length == 0 || version.previewState != "ready")
            {
                var message = Text(p.transform, "Preview belum tersedia offline.\nJenis: " + version.mimeType + "\nStatus: " + version.previewState + "\n" + (ContentValidator.ApprovedUrl(version.sourceRef?.webUrl) ? "Gunakan Buka sumber untuk membaca berkas asli." : "Tautan sumber belum tersedia untuk dokumen ini."), 24, Ink); Rect(message.gameObject, new Vector2(.05f, .3f), new Vector2(.95f, .75f));
            }
            else
            {
                var viewer = ImageArea(p.transform, new Vector2(.05f, .25f), new Vector2(.95f, .78f), info);
                var count = Text(p.transform, "", 20, Ink); Rect(count.gameObject, new Vector2(.38f, .16f), new Vector2(.60f, .23f));
                Button prev = null, next = null;
                System.Action draw = () => { viewer.Open(Host.Catalog.media.First(x => x.id == pages[page])); count.text = "Halaman " + (page + 1) + " / " + pages.Length; prev.interactable = page > 0; next.interactable = page < pages.Length - 1; };
                prev = Button(p.transform, "Sebelumnya", () => { page--; draw(); }); Rect(prev.gameObject, new Vector2(.05f, .16f), new Vector2(.29f, .23f));
                next = Button(p.transform, "Berikutnya", () => { page++; draw(); }); Rect(next.gameObject, new Vector2(.69f, .16f), new Vector2(.95f, .23f));
                var reset = Button(p.transform, "Reset", viewer.ResetView); Rect(reset.gameObject, new Vector2(.59f, .06f), new Vector2(.71f, .14f)); draw();
            }
            var source = Button(p.transform, "Buka sumber", () => { if (ContentValidator.ApprovedUrl(version.sourceRef?.webUrl)) UnityEngine.Application.OpenURL(version.sourceRef.webUrl); }); Rect(source.gameObject, new Vector2(.73f, .06f), new Vector2(.95f, .14f)); source.interactable = ContentValidator.ApprovedUrl(version.sourceRef?.webUrl);
        }
        ImageViewer ImageArea(Transform parent, Vector2 min, Vector2 max, TMPro.TMP_Text status)
        {
            var area = Box(parent, "ImageViewport", Ink); Rect(area, min, max); area.AddComponent<RectMask2D>();
            var raw = new GameObject("Image", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)); raw.transform.SetParent(area.transform, false); Rect(raw, Vector2.zero, Vector2.one); raw.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var viewer = area.AddComponent<ImageViewer>(); viewer.image = raw.GetComponent<RawImage>(); viewer.status = status; return viewer;
        }
        public void Video(string id)
        {
            var p = Panel("VIDEO / PAKET OFFLINE", "Demo proyek");
            var area = Box(p.transform, "VideoViewport", Ink); Rect(area, new Vector2(.05f, .29f), new Vector2(.95f, .78f)); area.AddComponent<RectMask2D>();
            var raw = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)); raw.transform.SetParent(area.transform, false); Rect(raw, Vector2.zero, Vector2.one); raw.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var viewer = area.AddComponent<VideoViewer>(); viewer.image = raw.GetComponent<RawImage>(); viewer.image.color = Color.clear;
            viewer.status = Text(p.transform, "", 18, Ink); Rect(viewer.status.gameObject, new Vector2(.05f, .06f), new Vector2(.57f, .13f));
            viewer.play = Button(p.transform, "Putar", () => { }); Rect(viewer.play.gameObject, new Vector2(.05f, .17f), new Vector2(.23f, .25f)); viewer.play.interactable = false;
            viewer.seek = Slider(p.transform, new Vector2(.27f, .17f), new Vector2(.95f, .25f)); viewer.seek.interactable = false;
            var volume = Text(p.transform, "Volume", 18, Ink); Rect(volume.gameObject, new Vector2(.59f, .05f), new Vector2(.70f, .13f));
            viewer.volume = Slider(p.transform, new Vector2(.72f, .06f), new Vector2(.95f, .13f)); viewer.volume.SetValueWithoutNotify(1);
            viewer.Open(Host.Catalog.media.FirstOrDefault(m => m.id == id));
        }
        Slider Slider(Transform parent, Vector2 min, Vector2 max)
        {
            var box = Box(parent, "Slider", new Color32(212, 224, 225, 255)); Rect(box, min, max);
            var slider = box.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            var fill = Box(box.transform, "Fill", Teal); Rect(fill, Vector2.zero, Vector2.one); slider.fillRect = (RectTransform)fill.transform;
            var handle = Box(box.transform, "Handle", Ink); Rect(handle, new Vector2(0, 0), new Vector2(0, 1)); ((RectTransform)handle.transform).sizeDelta = new Vector2(20, 0); slider.handleRect = (RectTransform)handle.transform; slider.targetGraphic = handle.GetComponent<Image>(); return slider;
        }
    }
}
