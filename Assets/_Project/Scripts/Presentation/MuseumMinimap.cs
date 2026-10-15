using System.Collections.Generic;
using System.Linq;
using ProjectAtlas.Application;
using ProjectAtlas.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    public sealed class MuseumMinimap : MonoBehaviour
    {
        RectTransform _map;
        RectTransform _playerMarker;
        TMP_Text _caption;
        Transform _player, _zoneRoot;
        ZoneMap _zone;
        IAtlasHost _host;
        MuseumUI _ui;
        TMP_FontAsset _font;
        readonly List<GameObject> _geometry = new List<GameObject>();
        readonly Dictionary<int, RectTransform> _visitors = new Dictionary<int, RectTransform>();
        readonly List<KeyValuePair<InteractionTarget, RectTransform>> _targets = new List<KeyValuePair<InteractionTarget, RectTransform>>();
        float _nextTick;
        public void Initialize(MuseumUI ui, IAtlasHost host, Transform player)
        {
            _host = host; _ui = ui; _player = player; _font = ui.font;
            var card = gameObject.AddComponent<Image>(); card.color = new Color32(245, 242, 234, 245); card.raycastTarget = false;
            var rt = (RectTransform)transform; rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = Vector2.one; rt.anchoredPosition = new Vector2(-24, -84); rt.sizeDelta = new Vector2(280, 278);
            _caption = Label(transform, "DENAH RUANG / N", 18); _caption.rectTransform.anchorMin = new Vector2(0, .87f); _caption.rectTransform.anchorMax = Vector2.one; _caption.rectTransform.offsetMin = new Vector2(12, 0); _caption.rectTransform.offsetMax = new Vector2(-12, 0);
            var area = new GameObject("Floor plan", typeof(RectTransform), typeof(Image)); area.transform.SetParent(transform, false); _map = (RectTransform)area.transform;
            _map.anchorMin = _map.anchorMax = new Vector2(.5f, .49f); _map.sizeDelta = new Vector2(252, 196); area.GetComponent<Image>().color = new Color32(226, 232, 228, 255); area.GetComponent<Image>().raycastTarget = false;
            _playerMarker = Dot(_map, "You", new Color32(22, 91, 160, 255), 13);
            var arrow = Label(_playerMarker, "▲", 24); arrow.color = new Color32(22, 91, 160, 255); arrow.alignment = TextAlignmentOptions.Center; Stretch(arrow.rectTransform); _playerMarker.GetComponent<Image>().enabled = false;
            var legend = Label(transform, "Biru: Anda  •  Ungu: pengunjung\nM: peta museum", 14); legend.rectTransform.anchorMin = Vector2.zero; legend.rectTransform.anchorMax = new Vector2(1, .15f); legend.rectTransform.offsetMin = new Vector2(12, 0); legend.rectTransform.offsetMax = new Vector2(-8, 0);
            gameObject.SetActive(false);
        }
        public void Bind(string zone, Transform root)
        {
            _zone = MuseumMap.Find(zone); _zoneRoot = root;
            foreach (var g in _geometry) Destroy(g); _geometry.Clear(); _targets.Clear();
            foreach (var marker in _visitors.Values) Destroy(marker.gameObject); _visitors.Clear();
            if (_zone == null || root == null) { gameObject.SetActive(false); return; }
            float scale = Mathf.Min(252 / _zone.width, 196 / _zone.depth); _map.sizeDelta = new Vector2(_zone.width, _zone.depth) * scale;
            _caption.text = _zone.label.ToUpperInvariant() + " / N ↑";
            foreach (var collider in root.GetComponentsInChildren<Collider>(true).Take(300))
            {
                if (collider.isTrigger || collider.GetComponentInParent<InteractionTarget>() != null) continue;
                var bounds = collider.bounds; var size = bounds.size;
                if (size.y < .3f || bounds.center.y - bounds.extents.y > 2.2f || (size.x > _zone.width * .8f && size.z > _zone.depth * .8f)) continue;
                var block = Dot(_map, "Obstacle", new Color32(100, 116, 119, 140), 1);
                Place(block, root.InverseTransformPoint(bounds.center)); block.sizeDelta = new Vector2(Mathf.Max(2, size.x * scale), Mathf.Max(2, size.z * scale)); _geometry.Add(block.gameObject);
            }
            foreach (var target in root.GetComponentsInChildren<InteractionTarget>(true))
            {
                var dot = Dot(_map, target.label, target.action == "zone" ? new Color32(196, 110, 24, 255) : new Color32(35, 139, 126, 255), target.action == "zone" ? 9 : 6);
                _geometry.Add(dot.gameObject); _targets.Add(new KeyValuePair<InteractionTarget, RectTransform>(target, dot));
            }
            _playerMarker.SetAsLastSibling(); gameObject.SetActive(true);
        }
        public void RefreshVisibility(bool exploring) { gameObject.SetActive(exploring && _zone != null && !_ui.HasModal); }
        void Update()
        {
            if (_zoneRoot == null || _player == null) return;
            Place(_playerMarker, _zoneRoot.InverseTransformPoint(_player.position)); _playerMarker.localRotation = Quaternion.Euler(0, 0, -(_player.eulerAngles.y - _zoneRoot.eulerAngles.y));
            if (Time.unscaledTime < _nextTick) return; _nextTick = Time.unscaledTime + .1f;
            foreach (var target in _targets) { target.Value.gameObject.SetActive(target.Key.gameObject.activeInHierarchy); Place(target.Value, _zoneRoot.InverseTransformPoint(target.Key.transform.position)); }
            var remote = _host.Session.Visitors.Where(p => p.id != _host.Session.LocalId && p.zone == _host.Zone).ToArray();
            foreach (int id in _visitors.Keys.Where(id => remote.All(p => p.id != id)).ToArray()) { Destroy(_visitors[id].gameObject); _visitors.Remove(id); }
            foreach (var p in remote)
            {
                if (!_visitors.TryGetValue(p.id, out var dot)) { dot = Dot(_map, p.name, new Color32(129, 83, 180, 255), 9); _visitors.Add(p.id, dot); }
                Place(dot, _zoneRoot.InverseTransformPoint(new Vector3(p.x, p.y, p.z)));
            }
            _playerMarker.SetAsLastSibling();
        }
        void Place(RectTransform rt, Vector3 position) { rt.anchorMin = rt.anchorMax = new Vector2(_zone.MapX(position.x), _zone.MapY(position.z)); rt.anchoredPosition = Vector2.zero; }
        RectTransform Dot(Transform parent, string name, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; go.GetComponent<Image>().raycastTarget = false;
            var rect = (RectTransform)go.transform; rect.sizeDelta = Vector2.one * size; return rect;
        }
        TMP_Text Label(Transform parent, string text, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); var label = go.GetComponent<TMP_Text>(); label.font = _font; label.fontSize = size; label.text = text; label.color = new Color32(34, 53, 73, 255); label.raycastTarget = false; label.richText = false; return label;
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(-8, -8); rt.offsetMax = new Vector2(8, 8); }
    }
}
