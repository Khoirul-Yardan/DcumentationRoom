using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectAtlas.Editor
{
    // Completes the user's requested missing scene generation after Unity's asynchronous import.
    // Existing scenes are preserved by the builder. No network command listener or automatic build EXE.
    [InitializeOnLoad]
    public static class AtlasSceneSetup
    {
        static double _nextAttempt;
        static int _attempts;
        static bool _busy;
        static AtlasSceneSetup() { _nextAttempt = EditorApplication.timeSinceStartup + 5; EditorApplication.update += Tick; }
        static void Tick()
        {
            if (_busy || _attempts >= 3 || EditorApplication.timeSinceStartup < _nextAttempt || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AtlasBuilder.Scenes.All(n => File.Exists("Assets/_Project/Scenes/" + n + ".unity"))) { EditorApplication.update -= Tick; return; }
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/SM_Bench.obj") == null) { _nextAttempt = EditorApplication.timeSinceStartup + 3; return; }
            _busy = true; _attempts++;
            try { AtlasBuilder.BuildStarterScenes(); }
            catch (Exception e) { Debug.LogError("ATLAS scene setup: " + e); Directory.CreateDirectory("Docs/Evidence"); File.WriteAllText("Docs/Evidence/scene-setup-error.txt", e.ToString()); }
            finally { _busy = false; _nextAttempt = EditorApplication.timeSinceStartup + 10; }
        }
    }

    public sealed class AtlasModelImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art/Models/SM_", StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1;
            importer.useFileScale = false;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.normalSmoothingAngle = 0;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }
    }
}
