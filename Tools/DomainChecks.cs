using System;
using System.Linq;
using ProjectAtlas.Domain;

public static class DomainChecks
{
    static int _count;
    static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); _count++; }
    static Catalog Fixture(int count = 3)
    {
        return new Catalog { schemaVersion = 1, isSample = true, categories = new[] { new Category { id = "sample-digital", label = "Contoh Digital" } },
            projects = Enumerable.Range(0, count).Select(i => new Project { id = "sample-" + i, title = "Contoh " + i, summary = "Sebuah prototype", isSample = true,
                publicationState = "published", visibility = "public", developmentStatus = i == 0 ? "archived" : "completed", categoryIds = new[] { "sample-digital" }, year = 2026, sortOrder = i }).ToArray() };
    }
    public static int Main()
    {
        try
        {
            Check(ContentValidator.Validate(Fixture()).Count == 0, "valid fixture");
            Check(ContentValidator.Validate(null).Count == 1, "null catalog rejected");
            var c = Fixture(); c.projects[1].id = c.projects[0].id; Check(ContentValidator.Validate(c).Any(e => e.Contains("duplikat")), "duplicate project ID rejected");
            c = Fixture(); c.projects[0].coverMediaId = "missing"; Check(ContentValidator.Validate(c).Any(e => e.Contains("coverMediaId")), "missing cover rejected");
            c = Fixture(); c.projects[0].categoryIds = new[] { "missing" }; Check(ContentValidator.Validate(c).Any(e => e.Contains("categoryIds")), "missing category rejected");
            c = Fixture(); c.schemaVersion = 2; Check(ContentValidator.Validate(c).Any(e => e.Contains("schemaVersion")), "unknown schema rejected");
            c = Fixture(); c.projects = null; Check(ContentValidator.Validate(c).Count > 0, "null project array rejected");
            c = Fixture(); c.projects[0] = null; Check(ContentValidator.Validate(c).Count > 0, "null record rejected without exception");
            c = Fixture(); c.projects[0].publicationState = "draft"; Check(ContentValidator.Validate(c).Count > 0, "draft rejected from public snapshot");
            c = Fixture(); c.projects[0].visibility = "private"; Check(ContentValidator.Validate(c).Count > 0, "private rejected from public snapshot");
            c = Fixture(); c.projects[0].title = new string('x', 201); Check(ContentValidator.Validate(c).Count > 0, "oversized title rejected");
            c = Fixture(); c.projects[0].isSample = false; Check(ContentValidator.Validate(c).Count > 0, "sample label enforced");
            foreach (var p in new[] { "../file", "/file", "C:/file", "Media/../../file", "Media\\file", "Media/%2e%2e/file", "Media//file", "" }) Check(!ContentValidator.SafeBundlePath(p), "unsafe path: " + p);
            Check(ContentValidator.SafeBundlePath("Media/sample.png"), "relative bundle path accepted");
            foreach (var url in new[] { "javascript:alert(1)", "file:///secret", "http://drive.google.com/x", "https://drive.google.com.evil.test/x", "https://name@drive.google.com/x", "https://drive.google.com:444/x" }) Check(!ContentValidator.ApprovedUrl(url), "unsafe URL rejected: " + url);
            Check(ContentValidator.ApprovedUrl("https://drive.google.com/file/d/sample"), "approved URL accepted");
            Check(CatalogQuery.Search(Fixture(), "  CONTOH   1 ").Single().id == "sample-1", "whitespace/case normalization");
            Check(CatalogQuery.Search(Fixture(), "", "sample-digital", 2026, "archived").Single().id == "sample-0", "filter intersection, archived remains public");
            Check(CatalogQuery.Search(Fixture(), "", "sample-digital", 2025).Length == 0, "year intersection excludes other year");
            Check(CatalogQuery.Normalize("ＡＢＣ") == "abc", "Unicode FormKC normalization");
            foreach (int count in new[] { 0, 1, 8, 9, 100 })
            {
                var all = CatalogQuery.Search(Fixture(count));
                var pages = Enumerable.Range(0, Math.Max(1, (count + 7) / 8)).SelectMany(i => CatalogQuery.Page(all, i)).ToArray();
                Check(pages.Length == count && pages.Select(p => p.id).Distinct().Count() == count, "paging: " + count);
            }
            c = Fixture(); c.projects = c.projects.Reverse().ToArray(); Check(CatalogQuery.Search(c)[0].id == "sample-0", "stable sort independent of manifest order");
            c = Fixture(); c.documents = new[] { new Document { id = "doc", currentVersionId = "version" } }; c.documentVersions = new[] { new DocumentVersion { id = "version", documentId = "other" } }; Check(ContentValidator.Validate(c).Any(e => e.Contains("milik dokumen lain")), "cross-document current version rejected");
            c = Fixture(); c.categories = new[] { new Category { id = "sample-digital" }, new Category { id = "sample-research" } }; c.projects[1].categoryIds = new[] { "sample-research" };
            Check(CatalogQuery.SearchCategories(c, "", new[] { "sample-digital", "sample-research" }).Length == 3, "multi-category OR includes both categories without duplicates");
            Check(CatalogQuery.SearchCategories(c, "", new[] { "sample-digital", "sample-research" }, 2026, "completed").Length == 2, "multi-category OR intersected with year and status");
            Check(CatalogQuery.SearchCategories(c, "", new[] { "sample-digital", "sample-research" }, 2025, "completed").Length == 0, "multi-category AND year excludes wrong year");
            Check(CatalogQuery.SearchCategories(c, "CONTOH 1", new[] { "sample-digital" }).Length == 0, "search intersects category");
            Check(CatalogQuery.SearchCategories(c, "", Array.Empty<string>()).Length == 3, "reset categories restores all projects");
            c = Fixture(100); c.documents = Enumerable.Range(0, 1000).Select(i => new Document { id = "sample-doc-" + i, currentVersionId = "sample-v-" + i }).ToArray();
            c.documentVersions = Enumerable.Range(0, 1000).Select(i => new DocumentVersion { id = "sample-v-" + i, documentId = "sample-doc-" + i }).ToArray();
            Check(ContentValidator.Validate(c).Count == 0, "100 projects and 1000 documents validated");
            var timer = System.Diagnostics.Stopwatch.StartNew(); for (int i = 0; i < 100; i++) CatalogQuery.SearchCategories(c, "Contoh", new[] { "sample-digital" }, 2026, "completed"); timer.Stop();
            Console.WriteLine("Metadata query mean over 100 runs (100 projects / 1000 docs): " + (timer.Elapsed.TotalMilliseconds / 100).ToString("F3") + " ms; not a Unity render benchmark");
            Console.WriteLine("TOTAL PASS " + _count); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
