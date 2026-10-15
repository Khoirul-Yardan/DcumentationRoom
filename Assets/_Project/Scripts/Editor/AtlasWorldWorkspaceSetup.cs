using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAtlas.Editor
{
    // First-import authoring setup. Only creates new scene copies; never saves a dirty user scene.
    [InitializeOnLoad]
    public static class AtlasWorldWorkspaceSetup
    {
        static double _next;
        static bool _busy;
        static AtlasWorldWorkspaceSetup() { _next = EditorApplication.timeSinceStartup + 8; EditorApplication.update += Tick; }
        static void Tick()
        {
            if (_busy || EditorApplication.timeSinceStartup < _next || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.Application.isBatchMode) return;
            const string root = "Assets/_Project/Scenes";
            if (!Directory.Exists(root) || !AtlasBuilder.Scenes.All(n => File.Exists(root + "/" + n + ".unity"))) { _next = EditorApplication.timeSinceStartup + 5; return; }
            if (Directory.GetDirectories(root, "LayoutOrganized*").Any(d => AtlasBuilder.Scenes.All(n => File.Exists(d + "/" + n + ".unity")))) { EditorApplication.update -= Tick; return; }
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) { _next = EditorApplication.timeSinceStartup + 5; return; }
            _busy = true;
            try
            {
                string result = AtlasLayoutRevision.Create(); AtlasBuilder.ValidateScenes();
                Directory.CreateDirectory("Docs/Evidence/WorldMultiplayer");
                File.WriteAllText("Docs/Evidence/WorldMultiplayer/layout-editor-generation.txt", "Unity " + UnityEngine.Application.unityVersion + "\n" + result + "\nEditor scene validation completed. Runtime/visual checks still required.\n");
                Debug.Log("ATLAS: " + result); AtlasWorldDesigner.Open();
            }
            catch (Exception e)
            {
                Debug.LogError("ATLAS World Designer setup: " + e.Message + ". Gunakan Tools > Project Atlas > World Designer untuk mencoba lagi.");
                Directory.CreateDirectory("Docs/Evidence/WorldMultiplayer"); File.WriteAllText("Docs/Evidence/WorldMultiplayer/layout-editor-error.txt", e.ToString());
            }
            finally { _busy = false; EditorApplication.update -= Tick; }
        }
    }
}
