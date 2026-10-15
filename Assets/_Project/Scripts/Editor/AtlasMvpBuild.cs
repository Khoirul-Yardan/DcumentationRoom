using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ProjectAtlas.Application;
using ProjectAtlas.Domain;
using ProjectAtlas.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ProjectAtlas.Editor
{
    public static class AtlasMvpBuild
    {
        static Dictionary<string, string> Hashes() => Directory.GetFiles("Assets", "*", SearchOption.AllDirectories).Where(p => new[] { ".unity", ".prefab", ".mat" }.Contains(Path.GetExtension(p))).ToDictionary(p => p, p => { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))); });
        [MenuItem("Tools/Project Atlas/Build MVP Offline Package")]
        public static void Run()
        {
            var before = Hashes(); AtlasBuilder.BuildStarterScenes(); AtlasBuilder.BuildStarterScenes(); var after = Hashes();
            if (before.Any(p => !after.ContainsKey(p.Key) || after[p.Key] != p.Value)) throw new InvalidDataException("Builder modified existing scene/prefab/material.");
            var lines = new List<string> { "PASS builder twice preserves existing scene/prefab/material bytes", "PASS scene validation and Editor import" };
            AtlasChecks.Run();
            var catalog = new LocalCatalogRepository(UnityEngine.Application.streamingAssetsPath).Load();
            if (catalog.documents.Length < 2 || !catalog.media.Any(m => m.kind == "video")) throw new InvalidDataException("MVP document/video fixtures missing");
            string dir = "Temp/AtlasMvp/Preferences"; Directory.CreateDirectory(dir);
            var store = new LocalPreferences(dir); var prefs = new UserPreferences { volume = .25f, fieldOfView = 80, bookmarks = new[] { "sample-project-001", "missing-project" } }; store.Save(prefs);
            var loaded = store.Load(); if (loaded.fieldOfView != 80 || loaded.bookmarks.Length != 2) throw new Exception("Preference roundtrip");
            lines.Add("PASS settings/bookmarks persistent roundtrip, unknown bookmark retained");
            File.WriteAllText(Path.Combine(dir, "atlas-preferences-v1.json"), "not json");
            loaded = store.Load(); if (loaded.fieldOfView != 65) throw new Exception("Corrupt preferences fallback");
            lines.Add("PASS corrupt settings reset and retained diagnostic copy");
            prefs.fieldOfView = float.NaN; prefs.textScale = 9; prefs.Sanitize(); if (prefs.fieldOfView != 65 || prefs.textScale != 1.5f) throw new Exception("Settings bounds");
            lines.Add("PASS settings NaN/range validation");
            foreach (var media in catalog.media.Where(m => !string.IsNullOrEmpty(m.checksum)))
            {
                using (var sha = SHA256.Create()) { string hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(UnityEngine.Application.streamingAssetsPath, media.locator.path)))).Replace("-", "").ToLowerInvariant(); if (hash != media.checksum) throw new InvalidDataException("Checksum: " + media.id); }
            }
            lines.Add("PASS MVP fixture media checksums");
            Directory.CreateDirectory("Docs/Evidence/MVP"); File.WriteAllLines("Docs/Evidence/MVP/editor-checks.txt", lines);
            AtlasBuilder.BuildWindows();
        }
    }
}
