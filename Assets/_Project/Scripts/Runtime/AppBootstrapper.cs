using System;
using System.Collections;
using System.Linq;
using ProjectAtlas.Application;
using ProjectAtlas.Domain;
using ProjectAtlas.Infrastructure;
using ProjectAtlas.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAtlas.Runtime
{
    public sealed class AppBootstrapper : MonoBehaviour, IAtlasHost
    {
        public PlayerMotor player;
        public MuseumUI ui;
        public Catalog Catalog { get; private set; }
        public string Zone { get; private set; } = "Frontend";
        public bool IsLoading { get; private set; }
        bool _ready;
        int _galleryPage;
        string _focusProject;
        LocalPreferences _preferencesStore;
        public UserPreferences Preferences { get; private set; }
        public IVisitorSession Session { get; } = new OnlineVisitorSession();
        void Awake()
        {
            string preferencesPath = UnityEngine.Application.persistentDataPath;
            if (Environment.GetCommandLineArgs().Contains("-atlasMvpSmoke")) preferencesPath = System.IO.Path.Combine(preferencesPath, "MvpSmoke");
            _preferencesStore = new LocalPreferences(preferencesPath); Preferences = _preferencesStore.Load();
            player.Initialize(this); ui.Initialize(this);
            ui.AttachMinimap(player.transform);
            gameObject.AddComponent<VisitorPresence>().Initialize(this);
            UnityEngine.Application.runInBackground = true;
            ApplyPreferences();
            player.CatalogRequested = () => { if (_ready && !IsLoading && !ui.HasModal) OpenCatalog(); };
            player.MenuRequested = () => { if (!IsLoading && _ready) ui.Back(); };
            player.MapRequested = () => { if (_ready && !IsLoading && !ui.HasModal) ui.Map(); };
        }
        IEnumerator Start()
        {
            try { Catalog = new LocalCatalogRepository(UnityEngine.Application.streamingAssetsPath).Load(); _ready = true; }
            catch (Exception e) { Debug.LogError("ATLAS catalog: " + e.Message); ui.Error(e.Message); yield break; }
            yield return SceneManager.LoadSceneAsync("Frontend", LoadSceneMode.Additive);
            ui.Menu();
            if (System.Environment.GetCommandLineArgs().Contains("-atlasSmoke")) gameObject.AddComponent<RuntimeSmoke>();
            if (System.Environment.GetCommandLineArgs().Contains("-atlasMvpSmoke")) gameObject.AddComponent<MvpSmoke>();
        }
        void Update() { if (_ready) { ui.SetHud(ZoneLabel(Zone) + " / data contoh" + (Session.Connected ? " / " + Session.Visitors.Length + " pengunjung" : ""), player.Prompt); ui.RefreshMinimap(!IsLoading && Zone != "Frontend"); } }
        void OnDestroy() => Session.Dispose();
        public static string ZoneLabel(string zone) => zone == "Gallery" ? "Galeri" : zone == "Archive" ? "Arsip" : zone == "Showcase" ? "Auditorium" : zone == "Courtyard" ? "Taman" : zone == "Frontend" ? "Menu" : "Lobby";
        public void SetModal(bool open) => player.SetActive(_ready && !open && !IsLoading && Zone != "Frontend");
        public void Resume() => SetModal(false);
        public void OpenCatalog() { if (_ready && !IsLoading) ui.Catalog(); }
        public void OpenProject(string id) { if (_ready && !IsLoading) ui.Detail(id); }
        public void OpenArchive() { if (_ready && !IsLoading) ui.Catalog("archived"); }
        public void OpenExhibitions() { if (_ready && !IsLoading) ui.Exhibitions(); }
        public void SavePreferences()
        {
            ApplyPreferences();
            try { _preferencesStore.Save(Preferences); } catch (Exception) { ui.Error("Pengaturan belum dapat disimpan. Perubahan berlaku selama sesi ini."); }
        }
        void ApplyPreferences()
        {
            Preferences.Sanitize(); AudioListener.volume = Preferences.volume; player.Sensitivity = Preferences.sensitivity;
            player.view.fieldOfView = Preferences.fieldOfView;
            QualitySettings.SetQualityLevel(Mathf.Clamp(Preferences.quality, 0, QualitySettings.names.Length - 1));
            ui.ApplyTextScale();
        }
        public void ShowInGallery(string id)
        {
            if (IsLoading) return;
            int index = Array.FindIndex(CatalogQuery.Search(Catalog), p => p.id == id);
            if (index < 0) return;
            _galleryPage = index / 8; _focusProject = id; Navigate("Gallery");
        }
        void BindZone(ZoneRoot root)
        {
            ui.BindMinimap(root.zoneId, root.transform);
            var gallery = root.GetComponent<GalleryPresenter>();
            if (gallery != null)
            {
                gallery.Bind(Catalog, _galleryPage);
                if (_focusProject != null)
                {
                    var slot = gallery.slots.FirstOrDefault(s => s.gameObject.activeSelf && s.projectId == _focusProject);
                    if (slot != null) player.Teleport(slot.transform.TransformPoint(new Vector3(0, .05f, -1.7f)), slot.transform.rotation);
                    _focusProject = null;
                }
            }
            // Upgrade behavior in memory; saved user scenes and prefabs remain untouched.
            foreach (var target in root.GetComponentsInChildren<InteractionTarget>(true))
            {
                if (root.zoneId == "archive" && target.action == "catalog" && target.label.Contains("ARSIP")) target.action = "archive";
                if (root.zoneId == "showcase" && target.action == "catalog") target.action = "exhibition";
            }
            if (root.zoneId == "showcase")
                foreach (var label in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if (label.text.Contains("Video belum tersedia")) label.text = "PROJECT ATLAS\nRUANG PRESENTASI\n\nKoleksi demonstrasi sintetis\nE di podium / Menu > Mode pameran";
        }
        public void Navigate(string scene) { if (!_ready || IsLoading || !new[] { "Lobby", "Gallery", "Archive", "Showcase", "Courtyard" }.Contains(scene)) return; StartCoroutine(Transition(scene)); }
        IEnumerator Transition(string destination)
        {
            IsLoading = true; player.SetActive(false); ui.Loading(destination);
            string previous = Zone;
            if (destination == previous)
            {
                var same = FindZone(SceneManager.GetSceneByName(destination));
                if (same?.spawn != null) player.Teleport(same.spawn.position, same.spawn.rotation);
                if (same != null) BindZone(same);
                IsLoading = false; ui.Clear(); yield break;
            }
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(destination)) { IsLoading = false; ui.Clear(); ui.Error("Scene tujuan belum masuk build: " + destination); yield break; }
            yield return SceneManager.LoadSceneAsync(destination, LoadSceneMode.Additive);
            var next = SceneManager.GetSceneByName(destination); var root = FindZone(next);
            if (root == null || root.spawn == null)
            {
                yield return SceneManager.UnloadSceneAsync(next); IsLoading = false; ui.Clear(); ui.Error("Spawn tujuan tidak valid. Zona sebelumnya dipertahankan."); yield break;
            }
            SceneManager.SetActiveScene(next); player.Teleport(root.spawn.position, root.spawn.rotation);
            BindZone(root);
            Zone = destination;
            var old = SceneManager.GetSceneByName(previous); if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            IsLoading = false; ui.Clear();
        }
        static ZoneRoot FindZone(Scene scene) => scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ZoneRoot>()).FirstOrDefault();
        public void NextGalleryPage(int direction)
        {
            var gallery = FindFirstObjectByType<GalleryPresenter>(); if (gallery == null) return;
            ui.Clear(); gallery.Bind(Catalog, gallery.PageIndex + direction); _galleryPage = gallery.PageIndex;
        }
    }
}
