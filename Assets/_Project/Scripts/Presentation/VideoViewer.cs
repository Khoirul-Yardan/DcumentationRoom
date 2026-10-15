using System;
using System.IO;
using ProjectAtlas.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ProjectAtlas.Presentation
{
    public sealed class VideoViewer : MonoBehaviour
    {
        public RawImage image;
        public TMP_Text status;
        public Slider seek, volume;
        public Button play;
        public VideoPlayer Player { get; private set; }
        RenderTexture _texture;
        AudioSource _audio;
        float _started;
        public bool Ready => Player != null && Player.isPrepared;
        public void Open(Media media)
        {
            if (media?.locator == null || !media.offlineAllowed || !ContentValidator.SafeBundlePath(media.locator.path)) { status.text = "Video tidak tersedia offline."; return; }
            string path = Path.Combine(UnityEngine.Application.streamingAssetsPath, media.locator.path);
            if (!File.Exists(path)) { status.text = "Video tidak ditemukan dalam paket offline."; return; }
            if (media.contentType != "video/mp4") { status.text = "Format video belum didukung. Gunakan MP4 H.264/AAC."; return; }
            if (new FileInfo(path).Length > 256L * 1024 * 1024) { status.text = "Video melebihi batas paket 256 MB."; return; }
            Player = gameObject.AddComponent<VideoPlayer>(); _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false; _audio.volume = volume.value;
            Player.playOnAwake = false; Player.source = VideoSource.Url; Player.url = new Uri(path).AbsoluteUri;
            Player.audioOutputMode = VideoAudioOutputMode.AudioSource; Player.controlledAudioTrackCount = 1; Player.EnableAudioTrack(0, true); Player.SetTargetAudioSource(0, _audio);
            Player.renderMode = VideoRenderMode.RenderTexture; Player.isLooping = true; Player.waitForFirstFrame = true;
            Player.errorReceived += OnError; Player.prepareCompleted += OnPrepared;
            seek.onValueChanged.AddListener(v => { if (Ready && Player.canSetTime) Player.time = v * Player.length; });
            volume.onValueChanged.AddListener(v => { if (_audio != null) _audio.volume = v; });
            play.onClick.AddListener(TogglePlayback); _started = Time.realtimeSinceStartup; status.text = "Menyiapkan video..."; Player.Prepare();
        }
        void OnPrepared(VideoPlayer player)
        {
            if (player.width == 0 || player.height == 0 || player.width > 4096 || player.height > 4096) { OnError(player, "Resolusi tidak didukung."); return; }
            _texture = new RenderTexture((int)player.width, (int)player.height, 0); _texture.Create(); player.targetTexture = _texture; image.texture = _texture; image.color = Color.white;
            image.GetComponent<AspectRatioFitter>().aspectRatio = (float)player.width / player.height;
            play.interactable = true; seek.interactable = player.canSetTime; player.Play();
        }
        public void TogglePlayback() { if (!Ready) return; if (Player.isPlaying) Player.Pause(); else Player.Play(); }
        void Update()
        {
            if (Player == null) return;
            if (!Ready) { if (_started > 0 && Time.realtimeSinceStartup - _started > 20) OnError(Player, "Waktu persiapan habis."); return; }
            if (Player.length > 0) seek.SetValueWithoutNotify((float)(Player.time / Player.length));
            status.text = (Player.isPlaying ? "Memutar" : "Jeda") + " / " + Player.time.ToString("F0") + " / " + Player.length.ToString("F0") + " detik";
            play.GetComponentInChildren<TMP_Text>().text = Player.isPlaying ? "Jeda" : "Putar";
        }
        void OnError(VideoPlayer player, string message) { Cleanup(); status.text = "Video gagal diputar. Codec/berkas tidak didukung pada perangkat ini. " + message; }
        void OnDisable() => Cleanup();
        void Cleanup()
        {
            _started = 0;
            if (Player != null) { Player.errorReceived -= OnError; Player.prepareCompleted -= OnPrepared; Player.Stop(); Player.targetTexture = null; Destroy(Player); Player = null; }
            if (_audio != null) { _audio.Stop(); Destroy(_audio); _audio = null; }
            if (image != null) { image.texture = null; image.color = Color.clear; }
            if (_texture != null) { _texture.Release(); Destroy(_texture); _texture = null; }
            if (play != null) play.interactable = false; if (seek != null) seek.interactable = false;
        }
    }
}
