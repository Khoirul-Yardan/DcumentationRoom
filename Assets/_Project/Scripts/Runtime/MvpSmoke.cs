using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectAtlas.Domain;
using ProjectAtlas.Infrastructure;
using ProjectAtlas.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectAtlas.Runtime
{
    // Explicit opt-in build validation. Uses an isolated preferences directory.
    public sealed class MvpSmoke : MonoBehaviour
    {
        readonly List<string> _results = new List<string>();
        string _output;
        AppBootstrapper _app;
        float _start;
        bool _failed, _done;
        void Check(bool value, string label)
        {
            _results.Add((value ? "PASS " : "FAIL ") + label); _failed |= !value;
            File.WriteAllLines(Path.Combine(_output, "mvp-runtime.txt"), _results);
        }
        void Update() { if (!_done && Time.realtimeSinceStartup - _start > 420) { Check(false, "Smoke watchdog timeout"); _done = true; UnityEngine.Application.Quit(1); } }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(_output, name + ".png"));
            yield return new WaitForSeconds(.2f);
        }
        IEnumerator Navigate(string zone)
        {
            _app.Navigate(zone); yield return new WaitUntil(() => !_app.IsLoading);
            Check(_app.Zone == zone, "Navigate " + zone);
        }
        Button FindButton(string prefix) => _app.ui.GetComponentsInChildren<Button>().FirstOrDefault(b => b.GetComponentInChildren<TMP_Text>()?.text.StartsWith(prefix, StringComparison.Ordinal) == true);
        void Click(string prefix) { var b = FindButton(prefix); Check(b != null && b.interactable, "UI action " + prefix); if (b != null && b.interactable) b.onClick.Invoke(); }
        IEnumerator Start()
        {
            _start = Time.realtimeSinceStartup; _app = GetComponent<AppBootstrapper>();
            _output = Path.Combine(Environment.CurrentDirectory, "Docs/Evidence/MVP"); Directory.CreateDirectory(_output);
            _results.Add("Unity " + UnityEngine.Application.unityVersion + " / " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType);
            yield return new WaitForSeconds(1);
            Check(_app.Catalog.projects.Length >= 9, "Data-only fixture > 8 projects");
            yield return Capture("01-menu");
            Click("Katalog proyek"); yield return null; Click("Berikutnya");
            Check(_app.ui.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("Contoh Dokumentasi Aksesibel")), "Catalog second page visible");
            yield return Capture("02-catalog-page2");
            _app.ui.Back(); _app.ui.Settings();
            _app.Preferences.fieldOfView = 80; _app.Preferences.sensitivity = .15f; _app.Preferences.volume = .3f; _app.Preferences.textScale = 1.2f;
            _app.SavePreferences();
            var saved = new LocalPreferences(Path.Combine(UnityEngine.Application.persistentDataPath, "MvpSmoke")).Load();
            Check(saved.fieldOfView == 80 && Mathf.Approximately(saved.sensitivity, .15f) && _app.player.view.fieldOfView == 80, "Settings save/reload and applied camera");
            yield return Capture("03-settings"); Click("Reset pengaturan"); _app.ui.Clear();
            _app.ui.ToggleBookmark("sample-project-009");
            if (!_app.ui.HasBookmark("sample-project-009")) _app.ui.ToggleBookmark("sample-project-009");
            saved = new LocalPreferences(Path.Combine(UnityEngine.Application.persistentDataPath, "MvpSmoke")).Load(); Check(saved.bookmarks.Contains("sample-project-009"), "Bookmark persistence");
            _app.ShowInGallery("sample-project-009"); yield return new WaitUntil(() => !_app.IsLoading);
            var gallery = FindFirstObjectByType<GalleryPresenter>();
            Check(gallery.PageIndex == 1 && gallery.slots.Count(s => s.gameObject.activeSelf) == 1 && gallery.slots[0].projectId == "sample-project-009", "Show in gallery binds page 2 and hides 7 empty slots");
            yield return Capture("04-gallery-page2");
            _app.NextGalleryPage(-1); Check(gallery.slots.Count(s => s.gameObject.activeSelf) == 8, "Gallery rebind 8 slots without duplication");
            _app.OpenProject("sample-project-001"); yield return null;
            Check(!_app.player.Active && _app.ui.HasModal, "Project modal blocks movement"); yield return Capture("05-detail");
            Click("Timeline"); yield return Capture("06-timeline");
            Click("Dokumentasi"); yield return null; Click("Contoh Laporan"); yield return new WaitForSeconds(.5f);
            var image = FindFirstObjectByType<ImageViewer>(); Check(image != null && image.image.texture != null, "Document preview first page decoded");
            Click("Berikutnya"); yield return new WaitForSeconds(.5f);
            Check(FindFirstObjectByType<ImageViewer>()?.image.texture != null, "Document second page decoded"); yield return Capture("07-document-page2");
            _app.ui.Back(); Click("Contoh Dokumen Tanpa"); yield return null;
            Check(_app.ui.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("Preview belum tersedia offline")), "Document without preview/source has explicit fallback");
            _app.ui.Back(); _app.ui.Back(); _app.ui.Video("sample-mvp-demo");
            var video = FindFirstObjectByType<VideoViewer>(); float deadline = Time.realtimeSinceStartup + 22;
            while (!video.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Check(video.Ready, "MP4 H264/AAC prepared on target Windows");
            if (video.Ready)
            {
                yield return new WaitForSeconds(.8f); Check(video.Player.isPlaying && video.Player.time > 0, "Video playback clock advances");
                video.TogglePlayback(); Check(!video.Player.isPlaying, "Video pause");
                video.seek.value = .5f; yield return new WaitForSeconds(.5f); Check(Math.Abs(video.Player.time - video.Player.length / 2) < 1, "Video seek");
                video.volume.value = .25f; Check(Mathf.Approximately(video.GetComponent<AudioSource>().volume, .25f), "Video volume");
                video.TogglePlayback(); yield return Capture("08-video");
            }
            _app.ui.Back(); yield return null;
            Check(FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsSortMode.None).Length == 0 && FindObjectsByType<AudioSource>(FindObjectsSortMode.None).All(a => !a.isPlaying), "Closing video releases decoder and stops audio");
            _app.ui.Viewer("missing-media"); yield return null;
            Check(FindFirstObjectByType<ImageViewer>().status.text.Contains("tidak tersedia"), "Unavailable image fallback"); _app.ui.Back();
            _app.ui.Video(_app.Catalog.projects[0].coverMediaId); yield return null;
            Check(FindFirstObjectByType<VideoViewer>().status.text.Contains("belum didukung"), "Unsupported video fallback"); _app.ui.Back();
            int before = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            for (int i = 0; i < 50; i++) { _app.ui.Viewer(_app.Catalog.projects[0].coverMediaId); yield return new WaitForSeconds(.04f); _app.ui.Back(); yield return null; }
            yield return Resources.UnloadUnusedAssets();
            Check(Resources.FindObjectsOfTypeAll<Texture2D>().Length <= before + 2, "50 image open/close cycles release textures");
            foreach (var zone in new[] { "Lobby", "Gallery", "Archive", "Showcase", "Courtyard" })
            {
                yield return Navigate(zone); yield return Capture("zone-" + zone);
                if (zone == "Archive") { var target = FindObjectsByType<InteractionTarget>(FindObjectsSortMode.None).First(t => t.action == "archive"); target.Interact(_app); yield return null; Check(_app.ui.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "Arsip proyek"), "Archive terminal opens archive preset"); _app.ui.Clear(); }
                if (zone == "Showcase") { var target = FindObjectsByType<InteractionTarget>(FindObjectsSortMode.None).First(t => t.action == "exhibition"); target.Interact(_app); yield return null; Check(FindButton("Putar: sample-mvp-demo") != null, "Auditorium terminal opens curated video collection"); _app.ui.Clear(); }
            }
            for (int i = 0; i < 20; i++) foreach (var zone in new[] { "Lobby", "Gallery", "Archive", "Showcase", "Courtyard" }) { _app.Navigate(zone); yield return new WaitUntil(() => !_app.IsLoading); }
            Check(FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1 && SceneManager.sceneCount == 2, "20 rounds of all 5 zones retain single rig/UI and two loaded scenes");
            yield return Navigate("Lobby");
            Check(_app.player.GetComponent<CharacterController>().enabled, "Controller restored after travel");
            if (Environment.GetCommandLineArgs().Contains("-atlasProfile")) yield return Profile();
            _results.Add("Manual keyboard/mouse, perceptual audio and usability not covered by this automated route.");
            File.WriteAllLines(Path.Combine(_output, "mvp-runtime.txt"), _results); _done = true;
            UnityEngine.Application.Quit(_failed ? 1 : 0);
        }
        IEnumerator Profile()
        {
            var times = new List<float>(); var transitions = new List<float>(); long peak = 0;
            var zoneNames = new[] { "Lobby", "Gallery", "Archive", "Showcase", "Courtyard" };
            foreach (var zone in zoneNames)
            {
                float travel = Time.realtimeSinceStartup; _app.Navigate(zone); yield return new WaitUntil(() => !_app.IsLoading); transitions.Add(Time.realtimeSinceStartup - travel);
                yield return new WaitForSeconds(2); float until = Time.realtimeSinceStartup + 24;
                while (Time.realtimeSinceStartup < until)
                {
                    _app.player.transform.Rotate(0, Time.unscaledDeltaTime * 15, 0);
                    times.Add(Time.unscaledDeltaTime * 1000);
                    using (var process = System.Diagnostics.Process.GetCurrentProcess()) peak = Math.Max(peak, process.WorkingSet64);
                    yield return null;
                }
            }
            times.Sort();
            File.WriteAllLines(Path.Combine(_output, "profile.txt"), new[] {
                "Development Windows build / automated rotating viewpoints / 120 seconds sampled, 2 seconds warmup per zone",
                "CPU: " + SystemInfo.processorType + " / RAM MB: " + SystemInfo.systemMemorySize,
                "GPU: " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceVersion,
                "Resolution: " + Screen.width + "x" + Screen.height + " / Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()],
                "Median FPS: " + (1000 / times[times.Count / 2]).ToString("F1"),
                "p95 frame ms: " + times[(int)(times.Count * .95f)].ToString("F2"),
                "Peak working set MB: " + (peak / 1048576f).ToString("F1"),
                "Transitions seconds: " + string.Join(", ", transitions.Select(x => x.ToString("F3"))),
                "Not a manual walk-through or release-build performance certification." });
        }
    }
}
