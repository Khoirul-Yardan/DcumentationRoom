using System.Collections.Generic;
using System.Linq;
using ProjectAtlas.Application;
using TMPro;
using UnityEngine;

namespace ProjectAtlas.Runtime
{
    public sealed class VisitorPresence : MonoBehaviour
    {
        AppBootstrapper _app;
        readonly Dictionary<int, GameObject> _avatars = new Dictionary<int, GameObject>();
        readonly Dictionary<int, Vector3> _positions = new Dictionary<int, Vector3>();
        readonly Dictionary<int, float> _angles = new Dictionary<int, float>();
        float _nextPublish;
        Material _material;
        public void Initialize(AppBootstrapper app)
        {
            _app = app;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) _material = new Material(shader) { color = new Color32(129, 114, 194, 255) };
        }
        void Update()
        {
            if (_app == null) return;
            if (Time.unscaledTime >= _nextPublish)
            {
                _nextPublish = Time.unscaledTime + .1f;
                if (!_app.IsLoading)
                {
                    var position = _app.player.transform.position;
                    _app.Session.Publish(new VisitorPose { zone = _app.Zone, x = position.x, y = position.y, z = position.z, yaw = _app.player.transform.eulerAngles.y });
                }
                Refresh();
            }
            foreach (var pair in _avatars)
            {
                var avatar = pair.Value; if (avatar == null) continue;
                avatar.transform.position = Vector3.Lerp(avatar.transform.position, _positions[pair.Key], 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
                avatar.transform.rotation = Quaternion.Slerp(avatar.transform.rotation, Quaternion.Euler(0, _angles[pair.Key], 0), 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
                var label = avatar.GetComponentInChildren<TMP_Text>(); label.transform.rotation = _app.player.view.transform.rotation;
            }
        }
        void Refresh()
        {
            var peers = _app.Session.Visitors.Where(p => p.id != _app.Session.LocalId && p.zone == _app.Zone && p.zone != "Frontend").ToArray();
            foreach (int id in _avatars.Keys.Where(id => peers.All(p => p.id != id)).ToArray()) { Destroy(_avatars[id]); _avatars.Remove(id); _positions.Remove(id); _angles.Remove(id); }
            foreach (var peer in peers)
            {
                var position = new Vector3(peer.x, peer.y, peer.z);
                if (!_avatars.TryGetValue(peer.id, out var avatar))
                {
                    avatar = new GameObject("Visitor " + peer.id); avatar.transform.SetParent(transform, false); avatar.transform.position = position;
                    var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Visitor silhouette"; body.transform.SetParent(avatar.transform, false); body.transform.localPosition = Vector3.up * .9f; body.transform.localScale = new Vector3(.55f, .85f, .55f); Destroy(body.GetComponent<Collider>()); if (_material != null) body.GetComponent<Renderer>().sharedMaterial = _material;
                    var label = new GameObject("Name", typeof(TextMeshPro)).GetComponent<TextMeshPro>(); label.transform.SetParent(avatar.transform, false); label.transform.localPosition = Vector3.up * 2; label.font = _app.ui.font; label.fontSize = 2.5f; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(2.5f, .5f); label.richText = false; label.text = peer.name;
                    _avatars.Add(peer.id, avatar);
                }
                _positions[peer.id] = position; _angles[peer.id] = peer.yaw;
            }
        }
        void OnDestroy() { if (_material != null) Destroy(_material); }
    }
}
