using System;
using System.IO;
using System.Linq;
using ProjectAtlas.Presentation;
using ProjectAtlas.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAtlas.Editor
{
    public static class AtlasLayoutRevision
    {
        [MenuItem("Tools/Project Atlas/Create Organized Layout Copies")]
        public static void CreateFromMenu() { Debug.Log(Create()); }
        public static string Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Hentikan Play sebelum mengubah layout.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("Simpan perubahan scene Anda dahulu. Generator tidak akan menyimpan/menimpa perubahan tersebut.");
            AtlasBuilder.BuildStarterScenes();
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Scenes/LayoutOrganized");
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            foreach (string name in AtlasBuilder.Scenes)
            {
                string source = AtlasBuilder.PreferredScenePath(name); string output = folder + "/" + name + ".unity";
                if (!AssetDatabase.CopyAsset(source, output)) throw new IOException("Gagal menyalin scene: " + source);
                var scene = EditorSceneManager.OpenScene(output, OpenSceneMode.Additive); SceneManager.SetActiveScene(scene);
                try
                {
                    var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ZoneRoot>()).FirstOrDefault();
                    if (root != null) Refine(root, name);
                    EditorSceneManager.SaveScene(scene);
                }
                finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
            }
            var selected = AtlasBuilder.Scenes.Select(n => new EditorBuildSettingsScene(folder + "/" + n + ".unity", true)).ToArray();
            EditorBuildSettings.scenes = selected.Concat(EditorBuildSettings.scenes.Select(s => new EditorBuildSettingsScene(s.path, s.enabled && !AtlasBuilder.Scenes.Contains(Path.GetFileNameWithoutExtension(s.path))))).ToArray();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(folder + "/Bootstrap.unity");
            AssetDatabase.SaveAssets();
            return "Salinan layout dibuat dan dipilih untuk Play/build: " + folder + ". Scene asli dipertahankan. Buka World Designer untuk review dan Undo saat mengedit.";
        }
        static void Refine(ZoneRoot root, string name)
        {
            var targets = root.GetComponentsInChildren<InteractionTarget>(true);
            if (name == "Lobby")
            {
                root.spawn.position = new Vector3(0, .05f, -8); root.spawn.rotation = Quaternion.identity;
                foreach (var target in targets.Where(t => t.action == "zone"))
                {
                    if (target.destination == "Showcase") target.transform.position = new Vector3(0, 0, 10.8f);
                    if (target.destination == "Courtyard") target.transform.position = new Vector3(0, 0, -10.8f);
                }
                foreach (Transform child in root.transform)
                {
                    if (child.name == "Reception") child.position = new Vector3(-5.5f, 0, 2);
                    if (child.name == "Sculpture") child.position = new Vector3(5.5f, 0, 7);
                    var text = child.GetComponent<TMP_Text>(); if (text != null && text.text == "SELAMAT DATANG") child.position += Vector3.left * 5.5f;
                }
                Sign(root.transform, "LURUS / AUDITORIUM    KIRI / GALERI    KANAN / ARSIP", new Vector3(0, 3.1f, 6), 0, 12);
                Sign(root.transform, "M: PETA MUSEUM    TAB: KATALOG    ESC: MENU", new Vector3(0, 3, -.5f), 0, 11);
            }
            if (name == "Gallery")
            {
                root.spawn.position = new Vector3(13.5f, .05f, 0); root.spawn.rotation = Quaternion.Euler(0, -90, 0);
                foreach (var portal in targets.Where(t => t.action == "zone")) portal.transform.position = new Vector3(15.2f, 0, 0);
                Sign(root.transform, "GALERI / JALUR TENGAH BEBAS    E: BACA PROYEK", new Vector3(0, 3.2f, 10.5f), 0, 13);
            }
            if (name == "Archive")
            {
                foreach (var target in targets.Where(t => t.action == "catalog" || t.action == "archive")) target.action = target.label.Contains("ARSIP") ? "archive" : "catalog";
                Sign(root.transform, "DOKUMENTASI / PILIH PROYEK DI TERMINAL", new Vector3(0, 3.3f, 8), 0, 12);
            }
            if (name == "Showcase")
            {
                root.spawn.position = new Vector3(0, .05f, -6.5f);
                foreach (var target in targets.Where(t => t.action == "catalog")) target.action = "exhibition";
                foreach (var text in root.GetComponentsInChildren<TMP_Text>()) if (text.text.Contains("Video belum tersedia")) text.text = "PROJECT ATLAS\nDEMO SINTETIS\n\nE pada podium: pilih presentasi";
                Sign(root.transform, "MENU > MODE PAMERAN / VIDEO & KOLEKSI", new Vector3(0, 3.2f, 3), 0, 12);
            }
            if (name == "Courtyard") root.spawn.position = new Vector3(0, .05f, -5);
            foreach (var error in AtlasWorldDesigner.Inspect(root).Where(m => m.StartsWith("ERROR"))) throw new InvalidDataException(name + ": " + error);
        }
        static void Sign(Transform parent, string caption, Vector3 position, float yaw, float width)
        {
            var existing = parent.GetComponentsInChildren<TMP_Text>().FirstOrDefault(); if (existing == null) return;
            var sign = new GameObject("Atlas wayfinding", typeof(TextMeshPro)).GetComponent<TextMeshPro>(); sign.transform.SetParent(parent, false); sign.transform.position = position; sign.transform.rotation = Quaternion.Euler(0, yaw, 0); sign.font = existing.font; sign.fontSize = 3; sign.color = new Color32(34, 53, 73, 255); sign.alignment = TextAlignmentOptions.Center; sign.rectTransform.sizeDelta = new Vector2(width, .6f); sign.text = caption; sign.richText = false;
        }
    }
}
