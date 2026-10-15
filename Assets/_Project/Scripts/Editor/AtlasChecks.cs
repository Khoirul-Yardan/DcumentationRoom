using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectAtlas.Domain;
using ProjectAtlas.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ProjectAtlas.Editor
{
    public static class AtlasChecks
    {
        [MenuItem("Tools/Project Atlas/Run Data Checks")]
        public static void Run()
        {
            var lines = new List<string>();
            var original = new LocalCatalogRepository(UnityEngine.Application.streamingAssetsPath).Load();
            Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("FAIL: " + name); lines.Add("PASS: " + name); };
            Func<Catalog> copy = () => JsonUtility.FromJson<Catalog>(JsonUtility.ToJson(original));
            check(original.projects.Length >= 9 && original.projects.All(p => p.isSample), "9 or more labeled synthetic projects");
            var duplicate = copy(); duplicate.projects[1].id = duplicate.projects[0].id; check(ContentValidator.Validate(duplicate).Any(e => e.Contains("duplikat")), "duplicate IDs rejected");
            var missing = copy(); missing.projects[0].coverMediaId = "missing"; check(ContentValidator.Validate(missing).Any(e => e.Contains("coverMediaId")), "broken foreign key rejected");
            var schema = copy(); schema.schemaVersion = 99; check(ContentValidator.Validate(schema).Any(e => e.Contains("schemaVersion")), "unknown schema rejected");
            foreach (string path in new[] { "../secret", "/etc/file", "C:/secret", "Media/../../secret", "Media\\secret", "Media/%2e%2e/file" }) check(!ContentValidator.SafeBundlePath(path), "unsafe path rejected: " + path);
            var unpublished = copy(); unpublished.projects[0].visibility = "private"; check(ContentValidator.Validate(unpublished).Any(e => e.Contains("published/public")), "private content rejected from public package");
            check(!ContentValidator.ApprovedUrl("javascript:alert(1)") && !ContentValidator.ApprovedUrl("https://drive.google.com.evil.test/x") && ContentValidator.ApprovedUrl("https://drive.google.com/file/d/sample"), "source URL allowlist");
            check(CatalogQuery.Search(original, "  ATLAS   CATATAN ").Length == 1, "normalized title search");
            check(CatalogQuery.Search(original, "", "sample-digital", 2026, "archived").Length == 1, "category/year/status intersection");
            foreach (int count in new[] { 0, 1, 8, 9, 100 })
            {
                var items = Enumerable.Range(0, count).Select(i => new Project { id = "sample-" + i }).ToArray();
                var seen = Enumerable.Range(0, Math.Max(1, (count + 7) / 8)).SelectMany(i => CatalogQuery.Page(items, i)).ToArray();
                check(seen.Length == count && seen.Select(p => p.id).Distinct().Count() == count, "paging without loss/duplicate: " + count);
            }
            var nulls = copy(); nulls.projects = null; check(ContentValidator.Validate(nulls).Count > 0, "null collection rejected safely");
            Directory.CreateDirectory("Docs/Evidence"); File.WriteAllLines("Docs/Evidence/data-checks.txt", lines); Debug.Log("ATLAS data checks passed: " + lines.Count);
        }
    }
}
