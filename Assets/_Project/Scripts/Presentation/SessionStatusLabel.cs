using ProjectAtlas.Application;
using TMPro;
using UnityEngine;

namespace ProjectAtlas.Presentation
{
    public sealed class SessionStatusLabel : MonoBehaviour
    {
        IAtlasHost _host; TMP_Text _label; float _next;
        public void Initialize(IAtlasHost host, TMP_Text label) { _host = host; _label = label; }
        void Update() { if (_host == null || Time.unscaledTime < _next) return; _next = Time.unscaledTime + .25f; _label.text = _host.Session.Status + " / " + _host.Session.Visitors.Length + " pengunjung" + (_host.Session.Connected ? "\nKode ruang: " + _host.Session.RoomCode : ""); }
    }
}
