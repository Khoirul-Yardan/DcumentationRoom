using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAtlas.Application;
using ProjectAtlas.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    public sealed partial class MuseumUI : MonoBehaviour
    {
        public TMP_FontAsset font;
        public IAtlasHost Host { get; private set; }
        public bool HasModal => _panels.Count > 0;
        readonly Stack<GameObject> _panels = new Stack<GameObject>();
        readonly Stack<GameObject> _focus = new Stack<GameObject>();
        TMP_Text _hud, _prompt;
        Transform _root;
        static readonly Color Ink = new Color32(34, 53, 73, 255), Teal = new Color32(47, 143, 145, 255), Paper = new Color32(245, 242, 234, 255);

        public void Initialize(IAtlasHost host)
        {
            Host = host; _root = transform;
            _hud = Text(_root, "", 21, Ink); Rect(_hud.gameObject, new Vector2(.03f, .92f), new Vector2(.97f, .98f));
            _prompt = Text(_root, "", 23, Ink); _prompt.alignment = TextAlignmentOptions.Center; Rect(_prompt.gameObject, new Vector2(.15f, .07f), new Vector2(.85f, .14f));
            var cross = Text(_root, "+", 24, Teal); cross.alignment = TextAlignmentOptions.Center; Rect(cross.gameObject, new Vector2(.49f, .49f), new Vector2(.51f, .51f));
        }
        public void SetHud(string zone, string prompt) { _hud.text = "ATLAS / " + zone.ToUpperInvariant() + "    /    TAB Katalog    M Peta    ESC Menu"; _prompt.text = HasModal ? "" : prompt; }
        public void Clear() { while (_panels.Count > 0) { var p = _panels.Pop(); p.SetActive(false); Destroy(p); } _focus.Clear(); Host.SetModal(false); }
        public void Back()
        {
            if (_panels.Count == 0) { Menu(); return; }
            var p = _panels.Pop(); p.SetActive(false); Destroy(p);
            var focus = _focus.Pop();
            if (_panels.Count > 0) _panels.Peek().SetActive(true);
            if (EventSystem.current != null && focus != null) EventSystem.current.SetSelectedGameObject(focus);
            Host.SetModal(HasModal);
            if (!HasModal && Host.Zone == "Frontend") Menu();
        }
        GameObject Panel(string eyebrow, string title)
        {
            _focus.Push(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null);
            if (_panels.Count > 0) _panels.Peek().SetActive(false);
            var overlay = Box(_root, "Modal", new Color32(34, 53, 73, 170)); Rect(overlay, Vector2.zero, Vector2.one);
            var card = Box(overlay.transform, "Paper", Paper); Rect(card, new Vector2(.06f, .06f), new Vector2(.94f, .94f));
            var e = Text(card.transform, eyebrow, 18, Teal); Rect(e.gameObject, new Vector2(.045f, .90f), new Vector2(.74f, .96f));
            var t = Text(card.transform, title, 38, Ink); Rect(t.gameObject, new Vector2(.045f, .79f), new Vector2(.88f, .90f)); t.fontStyle = FontStyles.Bold;
            var back = Button(card.transform, "Kembali / Esc", Back); Rect(back.gameObject, new Vector2(.78f, .91f), new Vector2(.97f, .975f));
            _panels.Push(overlay); Host.SetModal(true); EventSystem.current?.SetSelectedGameObject(back.gameObject);
            return card;
        }
        public void Menu()
        {
            Clear(); var p = Panel("MUSEUM PROYEK DIGITAL / MVP LOKAL SINTETIS", "PROJECT ATLAS");
            var intro = Text(p.transform, "Sebuah ruang untuk ide, proses, dan hasil.\n\nJelajahi karya dalam museum 3D atau temukan informasi langsung melalui katalog.\n\nSemua isi pada versi ini adalah data sintetis untuk demonstrasi, bukan proyek asli pengguna.", 25, Ink);
            Rect(intro.gameObject, new Vector2(.06f, .30f), new Vector2(.56f, .73f));
            var list = Scroll(p.transform, new Vector2(.64f, .18f), new Vector2(.94f, .77f));
            Button(list, Host.Zone == "Frontend" ? "Jelajahi museum  →" : "Lanjutkan jelajah  →", () => { if (Host.Zone == "Frontend") Host.Navigate("Lobby"); else { Clear(); Host.Resume(); } });
            Button(list, "Katalog proyek", () => Catalog());
            Button(list, "Mode pameran", Exhibitions);
            Button(list, "Bookmark", Bookmarks);
            Button(list, "Pengaturan", Settings);
            Button(list, "Peta museum", Map);
            Button(list, "Multiplayer online", Multiplayer);
            if (Host.Zone != "Frontend") Button(list, "Kembali ke Lobby", () => Host.Navigate("Lobby"));
            Button(list, "Panduan kontrol", Help);
            Button(list, "Keluar", () => UnityEngine.Application.Quit());
            var foot = Text(p.transform, "01  LOBBY     /     02  GALERI     /     03  CERITA DI BALIK KARYA", 18, Teal); Rect(foot.gameObject, new Vector2(.06f, .09f), new Vector2(.93f, .16f));
        }
        public void Help() { var p = Panel("PANDUAN", "Eksplorasi dengan nyaman"); var t = Text(p.transform, "W A S D    Bergerak\nMouse    Melihat sekeliling\nShift    Berjalan cepat\nE    Interaksi dalam jarak 2,5 meter\nTab    Katalog dari mana saja\nM    Peta dan perjalanan cepat\nEsc    Tutup panel teratas / menu\n\nSaat panel terbuka, gerakan pemain berhenti.\nKatalog dapat digunakan tanpa berjalan di museum.", 25, Ink); Rect(t.gameObject, new Vector2(.06f, .15f), new Vector2(.94f, .75f)); }
        public void Error(string message) { var p = Panel("INFORMASI", "Konten belum dapat dibuka"); var t = Text(p.transform, message, 23, Ink); Rect(t.gameObject, new Vector2(.06f, .16f), new Vector2(.94f, .76f)); }
        public void Loading(string zone) { Clear(); var p = Panel("PERJALANAN", "Memuat " + zone + "..."); foreach (var b in p.GetComponentsInChildren<Button>()) b.interactable = false; }
        public void Viewer(string id)
        {
            var media = Host.Catalog.media.FirstOrDefault(x => x.id == id);
            var p = Panel("MEDIA / CONTOH SINTETIS", "Gambar proyek");
            var area = Box(p.transform, "ImageViewport", Ink); Rect(area, new Vector2(.05f, .18f), new Vector2(.95f, .78f)); area.AddComponent<RectMask2D>();
            var raw = new GameObject("Image", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)); raw.transform.SetParent(area.transform, false); Rect(raw, Vector2.zero, Vector2.one);
            raw.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var viewer = area.AddComponent<ImageViewer>(); viewer.image = raw.GetComponent<RawImage>(); viewer.status = Text(p.transform, "", 17, Ink); Rect(viewer.status.gameObject, new Vector2(.05f, .07f), new Vector2(.70f, .14f));
            var reset = Button(p.transform, "Reset gambar", viewer.ResetView); Rect(reset.gameObject, new Vector2(.74f, .06f), new Vector2(.95f, .14f)); viewer.Open(media);
        }
        TMP_InputField Input(Transform parent, string placeholder)
        {
            var box = Box(parent, "Search", Color.white); var input = box.AddComponent<TMP_InputField>();
            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D)); area.transform.SetParent(box.transform, false); Rect(area, new Vector2(.02f, .1f), new Vector2(.98f, .9f));
            var text = Text(area.transform, "", 24, Ink); Rect(text.gameObject, Vector2.zero, Vector2.one);
            var hint = Text(area.transform, placeholder, 24, new Color(.35f, .42f, .47f)); Rect(hint.gameObject, Vector2.zero, Vector2.one);
            input.textViewport = (RectTransform)area.transform; input.textComponent = text; input.placeholder = hint; input.characterLimit = 200; input.lineType = TMP_InputField.LineType.SingleLine; return input;
        }
        Transform Scroll(Transform parent, Vector2 min, Vector2 max)
        {
            var box = Box(parent, "Scroll", new Color(1, 1, 1, .25f)); Rect(box, min, max); var scroll = box.AddComponent<ScrollRect>();
            var viewport = Box(box.transform, "Viewport", Color.clear); Rect(viewport, Vector2.zero, Vector2.one); viewport.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); content.transform.SetParent(viewport.transform, false);
            var rt = (RectTransform)content.transform; rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f, 1); rt.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.padding = new RectOffset(10, 10, 10, 10); layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = (RectTransform)viewport.transform; scroll.content = rt; scroll.horizontal = false; scroll.scrollSensitivity = 35; scroll.movementType = ScrollRect.MovementType.Clamped; return content.transform;
        }
        Transform Column(Transform parent, Vector2 min, Vector2 max) { var g = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup)); g.transform.SetParent(parent, false); Rect(g, min, max); var l = g.GetComponent<VerticalLayoutGroup>(); l.spacing = 12; l.childForceExpandHeight = false; return g.transform; }
        GameObject Box(Transform parent, string name, Color color) { var g = new GameObject(name, typeof(RectTransform), typeof(Image)); g.transform.SetParent(parent, false); g.GetComponent<Image>().color = color; return g; }
        TMP_Text Text(Transform parent, string value, float size, Color color) { var g = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); g.transform.SetParent(parent, false); var t = g.GetComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = size; var sizing = g.AddComponent<AtlasTextSize>(); sizing.baseSize = size; sizing.Apply(Host.Preferences.textScale); t.color = color; t.raycastTarget = false; t.richText = false; t.overflowMode = TextOverflowModes.Ellipsis; return t; }
        Button Button(Transform parent, string label, Action action)
        {
            var g = Box(parent, label, Teal); var b = g.AddComponent<Button>(); b.targetGraphic = g.GetComponent<Image>(); var colors = b.colors; colors.highlightedColor = new Color(.72f, .86f, .85f); colors.selectedColor = colors.highlightedColor; b.colors = colors;
            var l = g.AddComponent<LayoutElement>(); l.preferredHeight = 56 * Host.Preferences.textScale; l.minHeight = 48;
            var t = Text(g.transform, label, 23, Color.white); t.alignment = TextAlignmentOptions.Center; Rect(t.gameObject, new Vector2(.025f, .08f), new Vector2(.975f, .92f)); b.onClick.AddListener(() => action()); return b;
        }
        static void Rect(GameObject g, Vector2 min, Vector2 max) { var r = (RectTransform)g.transform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; }
    }
}
