using System;
using System.IO;
using System.Linq;
using ProjectAtlas.Infrastructure;
using ProjectAtlas.Presentation;
using ProjectAtlas.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectAtlas.Editor
{
    public static partial class AtlasBuilder
    {
        const string Root = "Assets/_Project/";
        static TMP_FontAsset _font;
        static Material _paper, _ink, _teal, _sage, _orange;
        internal static readonly string[] Scenes = { "Bootstrap", "Frontend", "Lobby", "Gallery", "Archive", "Showcase", "Courtyard" };
        internal static string PreferredScenePath(string name) => EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled && s.path.StartsWith(Root + "Scenes/", StringComparison.Ordinal) && Path.GetFileNameWithoutExtension(s.path) == name)?.path ?? Root + "Scenes/" + name + ".unity";

        [MenuItem("Tools/Project Atlas/Build Starter Scenes")]
        public static void BuildStarterScenes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Hentikan Play sebelum menjalankan builder.");
            EnsureBootstrapHostScene();
            ValidateCatalog();
            Directory.CreateDirectory(Root + "Scenes"); Directory.CreateDirectory(Root + "Prefabs"); Directory.CreateDirectory(Root + "Art/Materials");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (_font == null)
            {
                var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                AssetDatabase.ImportPackage(Path.Combine(pkg.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (_font == null) { Debug.Log("ATLAS: menunggu import font. Scene akan dibuat otomatis sesudah import selesai."); return; }
            }
            _paper = Material("Paper", "#F5F2EA"); _ink = Material("Ink", "#223549"); _teal = Material("Teal", "#2F8F91"); _sage = Material("Sage", "#9BB79B"); _orange = Material("Orange", "#ECA35C");
            PrepareMuseumKit();
            var original = SceneManager.GetActiveScene();
            foreach (var name in Scenes)
            {
                string path = Root + "Scenes/" + name + ".unity";
                // Existing scenes are user-owned once saved. A second run never overwrites them.
                if (File.Exists(path)) continue;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
                try
                {
                    if (name == "Bootstrap") Bootstrap();
                    else if (name == "Frontend") Frontend();
                    else BuildMuseumZone(name);
                    EditorSceneManager.SaveScene(scene, path);
                }
                finally { EditorSceneManager.CloseScene(scene, true); if (original.IsValid()) SceneManager.SetActiveScene(original); }
            }
            var atlas = Scenes.Select(n => new EditorBuildSettingsScene(PreferredScenePath(n), true)).ToArray();
            EditorBuildSettings.scenes = atlas.Concat(EditorBuildSettings.scenes.Where(s => !atlas.Any(a => a.path == s.path))).ToArray();
            if (GraphicsSettings.defaultRenderPipeline == null) GraphicsSettings.defaultRenderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            ValidateScenes();
            Directory.CreateDirectory("Docs/Evidence");
            File.WriteAllText("Docs/Evidence/scene-generation.txt", "Generated and validated in Unity " + UnityEngine.Application.unityVersion + "\n" + string.Join("\n", Scenes.Select(PreferredScenePath)));
            Debug.Log("ATLAS: starter scenes ready. Open Assets/_Project/Scenes/Bootstrap.unity and Play.");
        }
        [MenuItem("Tools/Project Atlas/Validate Catalog")]
        public static void ValidateCatalog() { var catalog = new LocalCatalogRepository(UnityEngine.Application.streamingAssetsPath).Load(); foreach (var media in catalog.media) if (!File.Exists(Path.Combine(UnityEngine.Application.streamingAssetsPath, media.locator.path))) throw new InvalidDataException("Media tidak ditemukan: " + media.id); Debug.Log("ATLAS: catalog valid / " + catalog.projects.Length + " projects."); }
        [MenuItem("Tools/Project Atlas/Build Windows")]
        public static void BuildWindows()
        {
            BuildStarterScenes(); Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes.Select(PreferredScenePath).ToArray(), locationPathName = "Builds/Windows/ProjectAtlas.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            Directory.CreateDirectory("Docs/Evidence"); File.WriteAllText("Docs/Evidence/build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nSize: " + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build Windows gagal: " + report.summary.result);
        }
        public static void BatchBuild() { BuildStarterScenes(); BuildStarterScenes(); AtlasChecks.Run(); BuildWindows(); }
        static Material Material(string name, string hex)
        {
            string path = Root + "Art/Materials/MAT_" + name + ".mat"; var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("URP Lit shader tidak tersedia.");
            var material = new Material(shader); ColorUtility.TryParseHtmlString(hex, out var color); material.color = color; material.SetFloat("_Smoothness", .2f); AssetDatabase.CreateAsset(material, path); return material;
        }
        static void Bootstrap()
        {
            var app = new GameObject("Atlas Services").AddComponent<AppBootstrapper>();
            var rig = new GameObject("PF_PlayerRig"); rig.layer = 2;
            var capsule = rig.AddComponent<CharacterController>(); capsule.height = 1.8f; capsule.radius = .3f; capsule.center = new Vector3(0, .9f, 0); capsule.stepOffset = .25f;
            var camera = new GameObject("Player Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera"; camera.transform.SetParent(rig.transform, false); camera.transform.localPosition = new Vector3(0, 1.65f, 0);
            var view = camera.GetComponent<Camera>(); view.fieldOfView = 65; view.nearClipPlane = .05f; view.farClipPlane = 100; view.backgroundColor = new Color(.86f, .91f, .91f); view.clearFlags = CameraClearFlags.SolidColor;
            var motor = rig.AddComponent<PlayerMotor>(); motor.view = view; app.player = motor;
            SavePrefab(rig, "PF_PlayerRig");
            var canvas = new GameObject("PF_UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MuseumUI));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            app.ui = canvas.GetComponent<MuseumUI>(); app.ui.font = _font; SavePrefab(canvas, "PF_UIRoot");
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
        static void Frontend()
        {
            Lighting(); Cube("Menu backdrop", new Vector3(0, -.1f, 6), new Vector3(25, .2f, 25), _paper);
            Prop("SM_Sculpture", new Vector3(0, 0, 7));
            for (int i = -1; i <= 1; i += 2) { Prop("SM_Tree", new Vector3(i * 6, 0, 8)); Prop("SM_Bench", new Vector3(i * 5, 0, 4)); }
        }
        static InteractionTarget Stand()
        {
            var go = Prop("SM_ProjectStand", Vector3.zero, 0, false);
            var baseCollider = go.AddComponent<BoxCollider>(); baseCollider.center = new Vector3(0, .08f, 0); baseCollider.size = new Vector3(5, .16f, 3.7f);
            var panelCollider = go.AddComponent<BoxCollider>(); panelCollider.center = new Vector3(0, 1.5f, 1.15f); panelCollider.size = new Vector3(4.7f, 2.8f, .25f);
            var target = go.AddComponent<InteractionTarget>(); target.action = "project";
            target.titleLabel = WorldText("Proyek contoh", new Vector3(0, 1.9f, .8f), Quaternion.identity, 3.85f, 1.7f, .36f, go.transform);
            var trigger = new GameObject("InteractionCollider", typeof(BoxCollider)); trigger.transform.SetParent(go.transform); trigger.transform.localPosition = new Vector3(0, 1.4f, .55f); var box = trigger.GetComponent<BoxCollider>(); box.size = new Vector3(4.5f, 2.7f, .3f); box.isTrigger = true;
            return target;
        }
        static InteractionTarget Target(string label, Vector3 position, float yaw, string action, string destination)
        {
            var go = new GameObject(label); go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            Cube("Plinth", new Vector3(0, .65f, 0), new Vector3(2.8f, 1.3f, .6f), _teal, true, go.transform);
            Cube("Sign", new Vector3(0, 2, 0), new Vector3(3, 1.1f, .2f), _ink, true, go.transform);
            WorldText(label + "\n[E]", new Vector3(0, 2, -.14f), Quaternion.identity, 2.8f, 1, .32f, go.transform, Color.white);
            var target = go.AddComponent<InteractionTarget>(); target.action = action; target.destination = destination; target.label = label;
            return target;
        }
        static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, bool collision = true, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static TMP_Text WorldText(string text, Vector3 position, Quaternion rotation, float width, float height, float size, Transform parent = null, Color? color = null)
        {
            var go = new GameObject("Signage", typeof(TextMeshPro)); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localRotation = rotation;
            var t = go.GetComponent<TextMeshPro>(); t.font = _font; t.text = text; t.fontSize = size * 10; t.color = color ?? new Color32(34, 53, 73, 255); t.alignment = TextAlignmentOptions.Center; t.rectTransform.sizeDelta = new Vector2(width, height); t.enableAutoSizing = true; t.fontSizeMin = 1; t.fontSizeMax = size * 10; return t;
        }
        static void Plant(Vector3 p)
        {
            Cube("Planter", p + Vector3.up * .35f, new Vector3(.85f, .7f, .85f), _ink);
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "Sage topiary"; crown.transform.position = p + Vector3.up * 1.2f; crown.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f); crown.GetComponent<Renderer>().sharedMaterial = _sage; UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
        }
        static void Lighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.78f, .82f, .84f); RenderSettings.fog = false;
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f; light.color = new Color(1, .95f, .85f); light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(50, -30, 0); RenderSettings.sun = light;
        }
        static void SavePrefab(GameObject go, string name) { string path = Root + "Prefabs/" + name + ".prefab"; if (!File.Exists(path)) PrefabUtility.SaveAsPrefabAsset(go, path); }
        static void EnsureBootstrapHostScene()
        {
            // Creating scenes additively requires the current active scene to be a saved scene,
            // not an untitled/unsaved one. Batchmode always starts with an untitled scene, so
            // save (or reopen) a dedicated host scene once before any additive work happens.
            var active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path)) return;
            if (active.isDirty) throw new InvalidOperationException("Simpan scene untitled yang sedang diedit sebelum menjalankan builder Atlas.");
            Directory.CreateDirectory(Root + "Scenes");
            string hostPath = Root + "Scenes/_BatchHost.unity";
            if (File.Exists(hostPath)) EditorSceneManager.OpenScene(hostPath, OpenSceneMode.Single);
            else EditorSceneManager.SaveScene(active, hostPath);
        }
    }
}
