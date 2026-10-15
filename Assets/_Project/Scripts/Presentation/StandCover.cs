using System;
using System.Collections;
using System.IO;
using ProjectAtlas.Domain;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ProjectAtlas.Presentation
{
    // Runtime-only exhibit surface. Does not change a saved scene, material or prefab.
    public sealed class StandCover : MonoBehaviour
    {
        RawImage _image;
        Texture2D _texture;
        UnityWebRequest _request;
        public void Initialize()
        {
            if (_image != null) return;
            var canvas = new GameObject("Cover / synthetic fixture", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(transform, false); canvas.transform.localPosition = new Vector3(0, 1.93f, .76f); canvas.transform.localScale = Vector3.one * .007f;
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(480, 230);
            var raw = new GameObject("Cover", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)); raw.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)raw.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            _image = raw.GetComponent<RawImage>(); _image.raycastTarget = false; _image.color = Color.clear;
            raw.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }
        public IEnumerator Load(Media media)
        {
            Cleanup(); Initialize();
            if (media?.locator == null || !media.offlineAllowed || !ContentValidator.SafeBundlePath(media.locator.path)) yield break;
            var path = Path.Combine(UnityEngine.Application.streamingAssetsPath, media.locator.path);
            if (!File.Exists(path) || new FileInfo(path).Length > 16 * 1024 * 1024) yield break;
            _request = UnityWebRequestTexture.GetTexture(new Uri(path).AbsoluteUri, true);
            yield return _request.SendWebRequest();
            if (_request == null) yield break;
            if (_request.result == UnityWebRequest.Result.Success)
            {
                _texture = DownloadHandlerTexture.GetContent(_request);
                if (_texture.width > 4096 || _texture.height > 4096) { Destroy(_texture); _texture = null; }
                else { _image.texture = _texture; _image.color = Color.white; _image.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height; }
            }
            _request.Dispose(); _request = null;
        }
        public void Cleanup()
        {
            if (_request != null) { _request.Abort(); _request.Dispose(); _request = null; }
            if (_image != null) { _image.texture = null; _image.color = Color.clear; }
            if (_texture != null) { Destroy(_texture); _texture = null; }
        }
        void OnDisable() => Cleanup();
    }
}
