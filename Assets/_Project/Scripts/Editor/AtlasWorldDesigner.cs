using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectAtlas.Domain;
using ProjectAtlas.Presentation;
using ProjectAtlas.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAtlas.Editor
{
    public sealed class AtlasWorldDesigner : EditorWindow
    {
        const string SceneDirectory = "Assets/_Project/Scenes/";
        int _zoneIndex;
        float _grid = 1;
        bool _gizmos = true;
        Vector2 _scroll;
        string _report = "Buka ruang, pilih penanda pada denah, lalu gunakan handles atau Inspector. Perubahan mendukung Undo; simpan scene melalui Unity.";
        [MenuItem("Tools/Project Atlas/World Designer")]
        public static void Open() { var window = GetWindow<AtlasWorldDesigner>("Atlas World Designer"); window.minSize = new Vector2(700, 620); }
        void OnEnable() { SceneView.duringSceneGui += SceneGUI; Selection.selectionChanged += Repaint; EditorApplication.hierarchyChanged += Repaint; }
        void OnDisable() { SceneView.duringSceneGui -= SceneGUI; Selection.selectionChanged -= Repaint; EditorApplication.hierarchyChanged -= Repaint; }
        static ZoneRoot CurrentRoot() => UnityEngine.Object.FindObjectsByType<ZoneRoot>(FindObjectsSortMode.None).FirstOrDefault(r => r.gameObject.scene == SceneManager.GetActiveScene());
        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("ATLAS / WORLD DESIGNER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Denah ini menampilkan objek scene di Editor. Pilih titik untuk memilih objek. Play selalu dapat dimulai dari Bootstrap melalui tombol Siapkan Play.", MessageType.Info);
            _zoneIndex = GUILayout.Toolbar(_zoneIndex, MuseumMap.Zones.Select(z => z.label).ToArray());
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Buka ruang terpilih")) OpenZone(MuseumMap.Zones[_zoneIndex].id);
            if (GUILayout.Button("Lihat dari atas")) TopView();
            if (GUILayout.Button("Siapkan Play dari Bootstrap")) { EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(AtlasBuilder.PreferredScenePath("Bootstrap")); _report = "Play akan mulai dari Bootstrap. Scene yang sedang diedit tetap menjadi workspace Editor."; }
            EditorGUILayout.EndHorizontal();
            var root = CurrentRoot();
            if (root != null)
            {
                var zone = MuseumMap.Find(root.zoneId);
                if (zone != null) DrawPlan(root, zone);
                _grid = Mathf.Clamp(EditorGUILayout.FloatField("Grid snap (meter)", _grid), .1f, 4);
                _gizmos = EditorGUILayout.Toggle("Gizmos spawn / interaksi", _gizmos);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Snap pilihan ke grid")) SnapSelection(root);
                if (GUILayout.Button("Ratakan X")) Align(root, true);
                if (GUILayout.Button("Ratakan Z")) Align(root, false);
                if (GUILayout.Button("Validasi penempatan")) _report = string.Join("\n", Inspect(root));
                EditorGUILayout.EndHorizontal();
                if (Selection.activeTransform != null && Selection.activeTransform.IsChildOf(root.transform))
                {
                    var selected = Selection.activeTransform;
                    EditorGUILayout.LabelField("Pilihan: " + selected.name, EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck(); var position = EditorGUILayout.Vector3Field("Posisi lokal", selected.localPosition); var rotation = EditorGUILayout.Vector3Field("Rotasi", selected.localEulerAngles);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(selected, "Atlas placement"); selected.localPosition = position; selected.localEulerAngles = rotation; Record(selected); }
                    if (GUILayout.Button("Fokus objek di Scene View")) SceneView.lastActiveSceneView?.FrameSelected();
                }
            }
            else EditorGUILayout.HelpBox("Belum ada ZoneRoot di scene aktif. Buka Lobby/Galeri melalui tombol di atas. SampleScene bukan ruang museum.", MessageType.Warning);
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("LAYOUT YANG DIRAPIKAN", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Buat salinan scene untuk jalur masuk dan penanda yang lebih jelas. Scene asli tidak ditimpa. Salinan hasil dipilih untuk Play/build; objek dan penempatannya tetap dapat Anda edit.", MessageType.None);
            if (GUILayout.Button("Buat & gunakan salinan layout rapi"))
            {
                try { _report = AtlasLayoutRevision.Create(); OpenZone("Lobby"); }
                catch (Exception e) { _report = e.Message; Debug.LogError("ATLAS layout: " + e.Message); }
            }
            EditorGUILayout.HelpBox(_report, MessageType.Info); EditorGUILayout.EndScrollView();
        }
        static void OpenZone(string name)
        {
            string path = AtlasBuilder.PreferredScenePath(name);
            if (!File.Exists(path)) { Debug.LogError("Scene belum tersedia: " + path); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single); TopView();
        }
        static void TopView()
        {
            var view = SceneView.lastActiveSceneView ?? GetWindow<SceneView>();
            view.LookAt(new Vector3(0, 0, 0), Quaternion.Euler(90, 0, 0), 23, true); view.Repaint();
        }
        void DrawPlan(ZoneRoot root, ZoneMap zone)
        {
            var outer = GUILayoutUtility.GetRect(400, 360, GUILayout.ExpandWidth(true));
            float scale = Mathf.Min((outer.width - 34) / zone.width, (outer.height - 34) / zone.depth);
            var map = new Rect(outer.center.x - zone.width * scale / 2, outer.center.y - zone.depth * scale / 2, zone.width * scale, zone.depth * scale);
            EditorGUI.DrawRect(map, new Color(.89f, .92f, .90f));
            for (float x = -zone.width / 2; x <= zone.width / 2; x += _grid) { float px = map.x + zone.MapX(x) * map.width; EditorGUI.DrawRect(new Rect(px, map.y, 1, map.height), new Color(.72f, .78f, .75f)); }
            for (float z = -zone.depth / 2; z <= zone.depth / 2; z += _grid) { float py = map.yMax - zone.MapY(z) * map.height; EditorGUI.DrawRect(new Rect(map.x, py, map.width, 1), new Color(.72f, .78f, .75f)); }
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger) continue;
                var b = collider.bounds; if (b.size.y < .3f || b.min.y > 2.2f) continue;
                var pos = root.transform.InverseTransformPoint(b.center);
                EditorGUI.DrawRect(new Rect(map.x + zone.MapX(pos.x) * map.width - b.size.x * scale / 2, map.yMax - zone.MapY(pos.z) * map.height - b.size.z * scale / 2, b.size.x * scale, b.size.z * scale), new Color(.38f, .45f, .47f, .55f));
            }
            foreach (var target in root.GetComponentsInChildren<InteractionTarget>(true))
            {
                var pos = root.transform.InverseTransformPoint(target.transform.position); var rect = new Rect(map.x + zone.MapX(pos.x) * map.width - 7, map.yMax - zone.MapY(pos.z) * map.height - 7, 14, 14);
                var color = GUI.backgroundColor; GUI.backgroundColor = target.action == "zone" ? new Color(1, .65f, .25f) : new Color(.2f, .85f, .65f);
                if (GUI.Button(rect, new GUIContent("", target.label))) { Selection.activeGameObject = target.gameObject; EditorGUIUtility.PingObject(target.gameObject); }
                GUI.backgroundColor = color;
            }
            if (root.spawn != null)
            {
                var pos = root.transform.InverseTransformPoint(root.spawn.position); var rect = new Rect(map.x + zone.MapX(pos.x) * map.width - 27, map.yMax - zone.MapY(pos.z) * map.height - 10, 54, 20);
                if (GUI.Button(rect, "Spawn")) Selection.activeTransform = root.spawn;
            }
            GUI.Label(new Rect(map.x, map.y - 20, map.width, 20), zone.label + " / " + zone.width + " × " + zone.depth + " m / N ↑", EditorStyles.boldLabel);
        }
        void SnapSelection(ZoneRoot root)
        {
            foreach (var t in Selection.transforms.Where(t => t != root.transform && t.IsChildOf(root.transform)))
            {
                Undo.RecordObject(t, "Atlas grid snap"); var p = t.localPosition; p.x = Mathf.Round(p.x / _grid) * _grid; p.z = Mathf.Round(p.z / _grid) * _grid; t.localPosition = p; Record(t);
            }
        }
        static void Align(ZoneRoot root, bool x)
        {
            var transforms = Selection.transforms.Where(t => t != root.transform && t.IsChildOf(root.transform)).ToArray(); if (transforms.Length < 2) return;
            var reference = transforms[0].position;
            foreach (var t in transforms.Skip(1)) { Undo.RecordObject(t, "Atlas align"); var p = t.position; if (x) p.x = reference.x; else p.z = reference.z; t.position = p; Record(t); }
        }
        static void Record(Transform t) { PrefabUtility.RecordPrefabInstancePropertyModifications(t); EditorSceneManager.MarkSceneDirty(t.gameObject.scene); SceneView.RepaintAll(); }
        void SceneGUI(SceneView view)
        {
            if (!_gizmos) return; var root = CurrentRoot(); if (root == null) return;
            var zone = MuseumMap.Find(root.zoneId); if (zone == null) return;
            Handles.color = new Color(.2f, .8f, .7f); Handles.DrawWireCube(root.transform.position + Vector3.up * .03f, new Vector3(zone.width, .05f, zone.depth));
            if (root.spawn != null) { Handles.color = Color.cyan; Handles.DrawWireDisc(root.spawn.position, Vector3.up, 1.5f); Handles.Label(root.spawn.position + Vector3.up, "SPAWN / area bebas 1,5 m"); }
            var t = Selection.activeTransform; if (t == null || !t.IsChildOf(root.transform)) return;
            Handles.color = Color.yellow; if (t.GetComponent<InteractionTarget>() != null) { Handles.DrawWireDisc(t.position, Vector3.up, 2.5f); Handles.Label(t.position + Vector3.up * 2, t.name + " / interaksi 2,5 m"); }
            EditorGUI.BeginChangeCheck(); var position = Handles.PositionHandle(t.position, t.rotation);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(t, "Atlas move object"); t.position = position; Record(t); }
        }
        internal static List<string> Inspect(ZoneRoot root)
        {
            var messages = new List<string>(); var zone = MuseumMap.Find(root.zoneId);
            if (zone == null) return new List<string> { "ERROR: Zone ID tidak dikenal." };
            if (root.spawn == null) messages.Add("ERROR: spawn belum ditentukan.");
            else
            {
                var p = root.transform.InverseTransformPoint(root.spawn.position);
                if (!zone.Contains(p.x, p.z, .5f)) messages.Add("ERROR: spawn di luar batas aman ruang.");
                var capsule = new Bounds(root.spawn.position + Vector3.up * .9f, new Vector3(.6f, 1.7f, .6f));
                foreach (var c in root.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && c.enabled && c.bounds.size.y > .3f)) if (c.bounds.Intersects(capsule)) messages.Add("ERROR: spawn bertabrakan dengan " + c.name);
            }
            var targets = root.GetComponentsInChildren<InteractionTarget>(true);
            foreach (var target in targets)
            {
                var p = root.transform.InverseTransformPoint(target.transform.position);
                if (!zone.Contains(p.x, p.z)) messages.Add("ERROR: target di luar ruangan: " + target.name);
                if (target.action == "zone" && MuseumMap.Find(target.destination) == null) messages.Add("ERROR: tujuan portal tidak valid: " + target.name);
            }
            for (int i = 0; i < targets.Length; i++) for (int j = i + 1; j < targets.Length; j++) if (Vector3.Distance(targets[i].transform.position, targets[j].transform.position) < 1.2f) messages.Add("REVIEW: target terlalu dekat: " + targets[i].name + " / " + targets[j].name);
            if (messages.Count == 0) messages.Add("PASS: spawn, batas ruang, jarak target dan tujuan portal. Lebar jalur/readability tetap perlu diuji berjalan.");
            return messages;
        }
    }
}
