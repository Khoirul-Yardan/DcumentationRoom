using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectAtlas.Infrastructure;
using ProjectAtlas.Presentation;
using ProjectAtlas.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAtlas.Editor
{
    public static partial class AtlasBuilder
    {
        static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();
        static void PrepareMuseumKit()
        {
            string[] names = { "Paper", "Ink", "Teal", "Sage", "Orange", "Wood", "Purple", "Leaf", "White", "Screen", "Gold" };
            string[] hex = { "#F5F2EA", "#223549", "#2F8F91", "#9BB79B", "#ECA35C", "#B78658", "#8172C2", "#557B60", "#FFFFFF", "#152B38", "#D6B66D" };
            for (int i = 0; i < names.Length; i++) Palette[names[i]] = Material(names[i], hex[i]);
            var original = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(original.path) && !original.isDirty && SceneManager.sceneCount == 1)
            {
                // Batchmode starts with an untitled unsaved scene; EditorSceneManager cannot add
                // additively on top of it. Replace it with a fresh Single scene first so the
                // subsequent additive NewScene call has a valid base scene to attach to.
                original = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(staging);
            try
            {
                foreach (string path in Directory.GetFiles(Root + "Art/Models", "SM_*.obj"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    string prefab = Root + "Prefabs/PF_" + name.Substring(3) + ".prefab";
                    if (File.Exists(prefab)) continue;
                    var model = ImportedModel(name);
                    PrefabUtility.SaveAsPrefabAsset(model, prefab);
                    UnityEngine.Object.DestroyImmediate(model);
                }
            }
            finally { EditorSceneManager.CloseScene(staging, true); if (original.IsValid()) SceneManager.SetActiveScene(original); }
        }
        static GameObject ImportedModel(string name)
        {
            string path = Root + "Art/Models/" + name + ".obj";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new InvalidOperationException("Model belum diimpor: " + path);
            var model = UnityEngine.Object.Instantiate(asset); model.name = name.Substring(3);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m != null && Palette.TryGetValue(m.name.Replace(" (Instance)", ""), out var mapped) ? mapped : _paper).ToArray();
            }
            return model;
        }
        static GameObject Prop(string model, Vector3 position, float yaw = 0, bool collider = true)
        {
            string path = Root + "Prefabs/PF_" + model.Substring(3) + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : ImportedModel(model);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            if (collider)
            {
                var renderers = go.GetComponentsInChildren<MeshFilter>();
                var bounds = new Bounds(Vector3.zero, Vector3.zero); bool first = true;
                foreach (var r in renderers)
                {
                    if (r.sharedMesh == null) continue;
                    var b = r.sharedMesh.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var pt = b.center + Vector3.Scale(b.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                        pt = go.transform.InverseTransformPoint(r.transform.TransformPoint(pt));
                        if (first) { bounds = new Bounds(pt, Vector3.zero); first = false; } else bounds.Encapsulate(pt);
                    }
                }
                var box = go.AddComponent<BoxCollider>(); box.center = bounds.center; box.size = bounds.size;
            }
            return go;
        }
        static void BuildMuseumZone(string name)
        {
            var root = new GameObject(name + " Zone").AddComponent<ZoneRoot>(); root.zoneId = name.ToLowerInvariant();
            var spawn = new GameObject("Spawn_default").transform; spawn.SetParent(root.transform); spawn.position = new Vector3(0, .05f, -7); root.spawn = spawn;
            if (name == "Gallery") { spawn.position = new Vector3(13.5f, .05f, 0); spawn.rotation = Quaternion.Euler(0, -90, 0); }
            Lighting();
            switch (name)
            {
                case "Lobby": Lobby(); break;
                case "Gallery": Gallery(root); break;
                case "Archive": Archive(); break;
                case "Showcase": Showcase(); break;
                case "Courtyard": Courtyard(); break;
            }
            // Keep every zone editable as a tidy single hierarchy, including lights and props.
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects()) if (go != root.gameObject) go.transform.SetParent(root.transform, true);
        }
        static void Shell(float width, float depth, string title, string subtitle, Material accent, bool openAir = false)
        {
            Cube("Floor / collision", new Vector3(0, -.14f, 0), new Vector3(width, .28f, depth), _paper);
            for (float x = -width / 2 + 4; x < width / 2; x += 4) Cube("Stone joint", new Vector3(x, .005f, 0), new Vector3(.025f, .01f, depth), Palette["Wood"], false);
            for (float z = -depth / 2 + 4; z < depth / 2; z += 4) Cube("Stone joint", new Vector3(0, .005f, z), new Vector3(width, .01f, .025f), Palette["Wood"], false);
            float height = openAir ? 1 : 5.6f;
            Cube("North wall", new Vector3(0, height / 2, depth / 2), new Vector3(width, height, .25f), _paper);
            Cube("South wall", new Vector3(0, height / 2, -depth / 2), new Vector3(width, height, .25f), _paper);
            Cube("East wall", new Vector3(width / 2, height / 2, 0), new Vector3(.25f, height, depth), _paper);
            Cube("West wall", new Vector3(-width / 2, height / 2, 0), new Vector3(.25f, height, depth), _paper);
            // Invisible high colliders stop leaving the garden while retaining low courtyard walls.
            if (openAir)
            {
                foreach (var p in new[] { new Vector3(width / 2, 2, 0), new Vector3(-width / 2, 2, 0), new Vector3(0, 2, depth / 2), new Vector3(0, 2, -depth / 2) })
                { var g = new GameObject("Garden boundary", typeof(BoxCollider)); g.transform.position = p; g.GetComponent<BoxCollider>().size = p.x == 0 ? new Vector3(width, 4, .1f) : new Vector3(.1f, 4, depth); }
            }
            if (!openAir)
            {
                Cube("North frieze", new Vector3(0, 4.85f, depth / 2 - .17f), new Vector3(width - .5f, .65f, .07f), accent, false);
                for (float z = -depth / 2 + 3; z < depth / 2; z += 6)
                {
                    Cube("Skylight beam", new Vector3(0, 5.4f, z), new Vector3(width, .15f, .20f), Palette["Wood"], false);
                    for (int side = -1; side <= 1; side += 2) { Prop("SM_Pillar", new Vector3(side * (width / 2 - .6f), 0, z)); Prop("SM_Pendant", new Vector3(side * width * .22f, 4.0f, z), 0, false); }
                }
                for (int side = -1; side <= 1; side += 2)
                    Cube("Wall base trim", new Vector3(side * (width / 2 - .15f), .18f, 0), new Vector3(.08f, .35f, depth - .3f), Palette["Wood"], false);
            }
            float signY = openAir ? 3.9f : 3.7f;
            if (openAir) Cube("Garden sign wall", new Vector3(0, 2.7f, depth / 2 - .3f), new Vector3(11, 4.4f, .25f), _teal);
            WorldText(title, new Vector3(0, signY, depth / 2 - .5f), Quaternion.identity, 16, .9f, .7f, null, openAir ? Color.white : (Color?)null);
            WorldText(subtitle, new Vector3(0, signY - .9f, depth / 2 - .52f), Quaternion.identity, 16, .7f, .27f, null, openAir ? Color.white : (Color?)null);
        }
        static void Lobby()
        {
            Shell(24, 24, "PROJECT ATLAS", "IDE / PROSES / HASIL     -     MUSEUM PROYEK DIGITAL", _teal);
            var reception = Prop("SM_Reception", new Vector3(0, 0, 2));
            AddTarget(reception, "catalog", "", "Buka katalog proyek");
            WorldText("SELAMAT DATANG", new Vector3(0, .65f, 1.40f), Quaternion.identity, 3.8f, .5f, .32f, null, Color.white);
            Prop("SM_Sculpture", new Vector3(0, 0, 7));
            Portal("01 / GALERI", "Gallery", new Vector3(-11.2f, 0, 0), -90);
            Portal("02 / ARSIP", "Archive", new Vector3(11.2f, 0, 0), 90);
            Portal("03 / AUDITORIUM", "Showcase", new Vector3(6.5f, 0, 10.8f), 0);
            Portal("04 / TAMAN", "Courtyard", new Vector3(6.5f, 0, -10.8f), 180);
            for (int side = -1; side <= 1; side += 2)
            {
                Prop("SM_Bench", new Vector3(side * 7.5f, 0, -5)); Prop("SM_Planter", new Vector3(side * 9.5f, 0, -5));
                Prop("SM_Tree", new Vector3(side * 8.6f, 0, 7.4f));
                Cube("Wayfinding line", new Vector3(side * 6, .02f, 0), new Vector3(9, .025f, .15f), _teal, false);
            }
            var directory = Prop("SM_Wayfinding", new Vector3(-3.2f, 0, -4.5f)); AddTarget(directory, "catalog", "", "Cari proyek");
            WorldText("MULAI DI SINI\nWASD  bergerak\nE  interaksi\nTAB  katalog", new Vector3(-3.2f, 2, -4.59f), Quaternion.identity, 1.8f, 1.2f, .24f);
        }
        static void Gallery(ZoneRoot root)
        {
            Shell(32, 24, "GALERI PROYEK", "KARYA DIGITAL / PENELITIAN / EKSPERIMEN    -    DATA CONTOH", _teal);
            var gallery = root.gameObject.AddComponent<GalleryPresenter>(); gallery.slots = new InteractionTarget[8];
            for (int i = 0; i < 8; i++)
            {
                var stand = Stand(); stand.name = "slot-" + (i + 1).ToString("00"); stand.transform.position = new Vector3(-10.5f + (i % 4) * 7, 0, i < 4 ? -6 : 6); stand.transform.rotation = Quaternion.Euler(0, i < 4 ? 180 : 0, 0); gallery.slots[i] = stand;
            }
            gallery.pageLabel = WorldText("Memuat koleksi...", new Vector3(0, 2.2f, 11.65f), Quaternion.identity, 8, .6f, .3f);
            gallery.Bind(new LocalCatalogRepository(UnityEngine.Application.streamingAssetsPath).Load(), 0);
            Portal("KEMBALI / LOBBY", "Lobby", new Vector3(15.2f, 0, -3.4f), 90);
            Terminal("KOLEKSI BERIKUTNYA", new Vector3(5.7f, 0, 10), "next"); Terminal("KOLEKSI SEBELUMNYA", new Vector3(-5.7f, 0, 10), "previous");
            Prop("SM_Sculpture", new Vector3(-13.5f, 0, 0)); Prop("SM_Planter", new Vector3(13, 0, 9));
            // Empty slots remain available for data; decorative plinths do not masquerade as real projects.
            Prop("SM_Pedestal", new Vector3(-4, 0, 8.8f)); Prop("SM_DocumentStand", new Vector3(3, 0, 9.2f));
        }
        static void Archive()
        {
            Shell(28, 24, "PERPUSTAKAAN ARSIP", "MENYIMPAN PROSES / MERAWAT PENGETAHUAN", Palette["Purple"]);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 3; i++) Prop("SM_Bookshelf", new Vector3(side * 9, 0, -5 + i * 6), side < 0 ? -90 : 90);
            for (int i = 0; i < 2; i++)
            {
                float z = -2 + i * 6; Prop("SM_ReadingTable", new Vector3(0, 0, z));
                Prop("SM_Chair", new Vector3(-.8f, 0, z - 1.2f)); Prop("SM_Chair", new Vector3(.8f, 0, z + 1.2f), 180);
            }
            string[] labels = { "SEMUA PROYEK", "DOKUMENTASI", "MEDIA", "ARSIP PROYEK" };
            for (int i = 0; i < 4; i++) Terminal(labels[i], new Vector3(-6 + i * 4, 0, 9), i == 3 ? "archive" : "catalog");
            Portal("KEMBALI / LOBBY", "Lobby", new Vector3(0, 0, -10.8f), 180);
            Prop("SM_Planter", new Vector3(-4.5f, 0, -7)); Prop("SM_Planter", new Vector3(4.5f, 0, -7));
        }
        static void Showcase()
        {
            Shell(28, 22, "AUDITORIUM", "RUANG BERBAGI CERITA DAN PRESENTASI", _orange);
            Prop("SM_Stage", new Vector3(0, 0, 6));
            var screen = Prop("SM_PresentationScreen", new Vector3(0, .3f, 8.5f)); AddTarget(screen, "exhibition", "", "Lihat koleksi presentasi");
            WorldText("PROJECT ATLAS\nRUANG PRESENTASI\n\nKoleksi demonstrasi sintetis\nE di podium / Menu > Mode pameran", new Vector3(0, 3.8f, 8.25f), Quaternion.identity, 8.9f, 4.8f, .8f);
            var podium = Prop("SM_Podium", new Vector3(-4, .3f, 5)); AddTarget(podium, "exhibition", "", "Buka koleksi presentasi");
            for (int row = 0; row < 5; row++)
                foreach (int column in new[] { -4, -3, -2, 2, 3, 4 }) Prop("SM_AuditoriumSeat", new Vector3(column * 1.5f, 0, 1.3f - row * 1.8f), 180);
            // Low stage step is within the player's 0.25 m step offset.
            Cube("Stage step", new Vector3(-4, .075f, 3.7f), new Vector3(3, .15f, .8f), Palette["Wood"]);
            Portal("KEMBALI / LOBBY", "Lobby", new Vector3(0, 0, -9.8f), 180);
            Prop("SM_Planter", new Vector3(-8.5f, 0, 6)); Prop("SM_Planter", new Vector3(8.5f, 0, 6));
        }
        static void Courtyard()
        {
            Shell(24, 18, "TAMAN ATLAS", "RUANG JEDA / IDE BERIKUTNYA DIMULAI DI SINI", _sage, true);
            Cube("Central path", new Vector3(0, .01f, 0), new Vector3(4, .025f, 16), Palette["Wood"], false);
            for (int side = -1; side <= 1; side += 2)
            {
                Prop("SM_Tree", new Vector3(side * 7, 0, 3.3f)); Prop("SM_Tree", new Vector3(side * 8, 0, -4.5f));
                Prop("SM_GardenBed", new Vector3(side * 6.5f, 0, -.6f));
                Prop("SM_Bench", new Vector3(side * 3.7f, 0, 1), side < 0 ? -90 : 90);
                for (int z = -5; z < 7; z += 4) Prop("SM_Bollard", new Vector3(side * 2.4f, 0, z));
            }
            Prop("SM_Sculpture", new Vector3(0, 0, 5));
            Portal("KEMBALI / LOBBY", "Lobby", new Vector3(0, 0, -7.9f), 180);
            var credits = Prop("SM_Wayfinding", new Vector3(4, 0, -5));
            WorldText("DIBUAT UNTUK ATLAS\nModel 3D orisinal\nKonten proyek: sintetis\nUnity / URP", new Vector3(4, 2, -5.09f), Quaternion.identity, 1.8f, 1.2f, .23f);
        }
        static void Portal(string label, string destination, Vector3 position, float yaw)
        {
            var p = Prop("SM_Portal", position, yaw, false); AddTarget(p, "zone", destination, "Masuk " + label);
            var trigger = p.AddComponent<BoxCollider>(); trigger.center = new Vector3(0, 1.7f, -.2f); trigger.size = new Vector3(3.9f, 3.5f, .65f); trigger.isTrigger = true;
            WorldText(label + "\n[E] MASUK", new Vector3(0, 2, -.04f), Quaternion.identity, 2.8f, 1.8f, .45f, p.transform, Color.white);
        }
        static void Terminal(string label, Vector3 position, string action = "catalog")
        {
            var terminal = Prop("SM_Terminal", position); AddTarget(terminal, action, "", label);
            WorldText(label + "\n[E]", new Vector3(0, 1.45f, -.14f), Quaternion.identity, 1.2f, .6f, .20f, terminal.transform, Color.white);
        }
        static void AddTarget(GameObject go, string action, string destination, string prompt)
        { var target = go.AddComponent<InteractionTarget>(); target.action = action; target.destination = destination; target.label = prompt; }

        [MenuItem("Tools/Project Atlas/Validate Scenes")]
        public static void ValidateScenes()
        {
            var original = SceneManager.GetActiveScene(); var report = new List<string>();
            foreach (string name in Scenes)
            {
                string path = PreferredScenePath(name);
                var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                    int missing = objects.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                    if (missing > 0) throw new InvalidDataException(name + ": missing scripts " + missing);
                    int renderers = objects.Sum(t => t.GetComponents<Renderer>().Length);
                    if (name != "Bootstrap" && renderers == 0) throw new InvalidDataException(name + ": geometry kosong.");
                    if (name != "Bootstrap" && name != "Frontend" && !objects.Any(t => t.TryGetComponent<ZoneRoot>(out var root) && root.spawn != null)) throw new InvalidDataException(name + ": spawn tidak ditemukan.");
                    report.Add(name + ": " + objects.Length + " objects, " + renderers + " renderers, 0 missing scripts");
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid()) SceneManager.SetActiveScene(original);
            Directory.CreateDirectory("Docs/Evidence"); File.WriteAllLines("Docs/Evidence/scene-validation.txt", report);
            Debug.Log("ATLAS: 7 scenes validated. " + string.Join("; ", report));
        }
    }
}
