using System;
using System.Collections;
using System.IO;
using ProjectAtlas.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    public sealed class ImageViewer : MonoBehaviour, IScrollHandler, IDragHandler
    {
        public RawImage image;
        public TMP_Text status;
        Texture2D _texture;
        UnityWebRequest _request;
        float _zoom = 1;
        Media _media;
        public void Open(Media media) { _media = media; Cleanup(); ResetView(); StartCoroutine(Load(media)); }
        void OnEnable() { if (_media != null) Open(_media); }
        IEnumerator Load(Media media)
        {
            status.text = "Memuat gambar...";
            if (media?.locator == null || !ContentValidator.SafeBundlePath(media.locator.path) || !media.offlineAllowed) { status.text = "Gambar tidak tersedia dalam paket offline."; yield break; }
            string path = Path.Combine(UnityEngine.Application.streamingAssetsPath, media.locator.path);
            if (!File.Exists(path)) { status.text = "Gambar tidak ditemukan dalam paket offline."; yield break; }
            if (new FileInfo(path).Length > 16 * 1024 * 1024) { status.text = "Gambar melebihi batas 16 MB."; yield break; }
            _request = UnityWebRequestTexture.GetTexture(new Uri(path).AbsoluteUri, true);
            yield return _request.SendWebRequest();
            if (_request.result != UnityWebRequest.Result.Success) { status.text = "Gambar rusak atau format tidak didukung."; _request.Dispose(); _request = null; yield break; }
            _texture = DownloadHandlerTexture.GetContent(_request);
            _request.Dispose(); _request = null;
            if (_texture.width > 4096 || _texture.height > 4096) { Destroy(_texture); _texture = null; status.text = "Resolusi melebihi batas 4096 px."; yield break; }
            image.texture = _texture;
            image.color = Color.white;
            image.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;
            status.text = media.id + "  /  Scroll: zoom  /  Tarik: geser";
        }
        public void ResetView() { _zoom = 1; image.rectTransform.localScale = Vector3.one; image.rectTransform.anchoredPosition = Vector2.zero; }
        public void OnScroll(PointerEventData e) { _zoom = Mathf.Clamp(_zoom + e.scrollDelta.y * .15f, 1, 4); image.rectTransform.localScale = Vector3.one * _zoom; if (_zoom == 1) ResetView(); }
        public void OnDrag(PointerEventData e) { if (_zoom <= 1) return; var p = image.rectTransform.anchoredPosition + e.delta / GetComponentInParent<Canvas>().scaleFactor; var limit = image.rectTransform.rect.size * (_zoom - 1) * .5f; image.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -limit.x, limit.x), Mathf.Clamp(p.y, -limit.y, limit.y)); }
        void OnDisable() => Cleanup();
        void Cleanup() { StopAllCoroutines(); if (_request != null) { _request.Abort(); _request.Dispose(); _request = null; } if (image != null) { image.texture = null; image.color = Color.clear; } if (_texture != null) { Destroy(_texture); _texture = null; } }
    }
}
