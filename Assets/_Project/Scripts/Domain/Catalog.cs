using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectAtlas.Domain
{
    [Serializable] public sealed class Catalog
    {
        public int schemaVersion;
        public string catalogVersion, publishedAt;
        public bool isSample;
        public Category[] categories = Array.Empty<Category>();
        public Project[] projects = Array.Empty<Project>();
        public Media[] media = Array.Empty<Media>();
        public Document[] documents = Array.Empty<Document>();
        public DocumentVersion[] documentVersions = Array.Empty<DocumentVersion>();
        public Milestone[] milestones = Array.Empty<Milestone>();
        public Exhibition[] exhibitions = Array.Empty<Exhibition>();
    }
    [Serializable] public sealed class Category { public string id, label; public int sortOrder; }
    [Serializable] public sealed class TeamMember { public string displayName, role; }
    [Serializable] public sealed class Project
    {
        public string id, slug, title, summary, developmentStatus, publicationState, visibility;
        public int year, sortOrder;
        public string problem, solution, coverMediaId, displayType, updatedAt;
        public bool isSample;
        public string[] categoryIds = Array.Empty<string>(), tags = Array.Empty<string>(), outcomes = Array.Empty<string>(), features = Array.Empty<string>(), techStack = Array.Empty<string>();
        public string[] galleryMediaIds = Array.Empty<string>(), demoMediaIds = Array.Empty<string>(), documentIds = Array.Empty<string>(), relatedProjectIds = Array.Empty<string>();
        public TeamMember[] team = Array.Empty<TeamMember>();
    }
    [Serializable] public sealed class Locator { public string type, path; }
    [Serializable] public sealed class Media { public string id, kind, contentType, checksum; public Locator locator; public int width, height; public bool offlineAllowed; }
    [Serializable] public sealed class Document { public string id, title, currentVersionId; }
    [Serializable] public sealed class SourceRef { public string provider, fileId, webUrl; }
    [Serializable] public sealed class DocumentVersion { public string id, documentId, versionLabel, mimeType, modifiedAt, previewState; public SourceRef sourceRef; public string[] previewMediaIds = Array.Empty<string>(); }
    [Serializable] public sealed class Milestone { public string id, projectId, date, title, description; public string[] evidenceDocumentIds = Array.Empty<string>(); }
    [Serializable] public sealed class Exhibition { public string id, title; public string[] projectIds = Array.Empty<string>(), presentationMediaIds = Array.Empty<string>(); }

    public static class ContentValidator
    {
        public static List<string> Validate(Catalog c)
        {
            var errors = new List<string>();
            if (c == null) { errors.Add("catalog: JSON kosong."); return errors; }
            if (c.schemaVersion != 1) errors.Add("schemaVersion: hanya versi 1 didukung.");
            var projects = Index(c.projects, x => x.id, "projects", errors);
            var categories = Index(c.categories, x => x.id, "categories", errors);
            var media = Index(c.media, x => x.id, "media", errors);
            var documents = Index(c.documents, x => x.id, "documents", errors);
            var versions = Index(c.documentVersions, x => x.id, "documentVersions", errors);
            Index(c.milestones, x => x.id, "milestones", errors);
            Index(c.exhibitions, x => x.id, "exhibitions", errors);
            foreach (var p in Safe(c.projects).Where(x => x != null))
            {
                var at = "projects/" + p.id;
                if (string.IsNullOrWhiteSpace(p.title) || string.IsNullOrWhiteSpace(p.summary)) errors.Add(at + ": judul/ringkasan wajib.");
                if ((p.title?.Length ?? 0) > 200 || (p.summary?.Length ?? 0) > 4000 || (p.problem?.Length ?? 0) > 16000 || (p.solution?.Length ?? 0) > 16000) errors.Add(at + ": teks terlalu panjang.");
                if (p.publicationState != "published" || p.visibility != "public") errors.Add(at + ": paket offline publik hanya menerima published/public.");
                if (!new[] { "planned", "in_progress", "completed", "archived" }.Contains(p.developmentStatus)) errors.Add(at + ": developmentStatus tidak dikenal.");
                if (Safe(p.categoryIds).Length == 0) errors.Add(at + ": kategori wajib.");
                if (c.isSample && (!p.isSample || !(p.id ?? "").StartsWith("sample-"))) errors.Add(at + ": fixture harus berlabel sample.");
                Refs(p.categoryIds, categories, at + "/categoryIds", errors);
                Refs(p.documentIds, documents, at + "/documentIds", errors);
                Refs(p.relatedProjectIds, projects, at + "/relatedProjectIds", errors);
                Refs(p.galleryMediaIds, media, at + "/galleryMediaIds", errors);
                Refs(p.demoMediaIds, media, at + "/demoMediaIds", errors);
                if (!string.IsNullOrEmpty(p.coverMediaId)) Refs(new[] { p.coverMediaId }, media, at + "/coverMediaId", errors);
            }
            foreach (var m in Safe(c.media).Where(x => x != null))
            {
                if (m.locator == null || m.locator.type != "bundle" || !SafeBundlePath(m.locator.path)) errors.Add("media/" + m.id + ": path bundle tidak aman.");
                if (!m.offlineAllowed) errors.Add("media/" + m.id + ": izin offline diperlukan.");
                if (m.width < 1 || m.height < 1 || m.width > 4096 || m.height > 4096) errors.Add("media/" + m.id + ": dimensi di luar batas 1..4096.");
            }
            foreach (var d in Safe(c.documents).Where(x => x != null))
            {
                Refs(new[] { d.currentVersionId }, versions, "documents/" + d.id, errors);
                var v = Safe(c.documentVersions).FirstOrDefault(x => x != null && x.id == d.currentVersionId);
                if (v != null && v.documentId != d.id) errors.Add("documents/" + d.id + ": currentVersion milik dokumen lain.");
            }
            foreach (var v in Safe(c.documentVersions).Where(x => x != null))
            {
                Refs(new[] { v.documentId }, documents, "documentVersions/" + v.id, errors);
                Refs(v.previewMediaIds, media, "documentVersions/" + v.id + "/previewMediaIds", errors);
                if (Safe(v.previewMediaIds).Length > 500) errors.Add("documentVersions/" + v.id + ": maksimum 500 halaman.");
                if (!string.IsNullOrEmpty(v.sourceRef?.webUrl) && !ApprovedUrl(v.sourceRef.webUrl)) errors.Add("documentVersions/" + v.id + ": URL tidak diizinkan.");
            }
            foreach (var m in Safe(c.milestones).Where(x => x != null))
            {
                Refs(new[] { m.projectId }, projects, "milestones/" + m.id, errors);
                Refs(m.evidenceDocumentIds, documents, "milestones/" + m.id, errors);
                if (!DateTime.TryParseExact(m.date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)) errors.Add("milestones/" + m.id + ": tanggal ISO tidak valid.");
            }
            foreach (var e in Safe(c.exhibitions).Where(x => x != null)) { Refs(e.projectIds, projects, "exhibitions/" + e.id, errors); Refs(e.presentationMediaIds, media, "exhibitions/" + e.id, errors); }
            return errors;
        }
        public static T[] Safe<T>(T[] value) => value ?? Array.Empty<T>();
        public static bool SafeBundlePath(string path) => !string.IsNullOrWhiteSpace(path) && !path.StartsWith("/") && !path.Contains("\\") && !path.Contains(":") && !path.Contains("%") && path.Split('/').All(x => x != ".." && x != "." && x.Length > 0);
        public static bool ApprovedUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo) && uri.IsDefaultPort && new[] { "drive.google.com", "docs.google.com" }.Contains(uri.Host);
        static HashSet<string> Index<T>(T[] items, Func<T, string> id, string at, List<string> errors) where T : class
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (items == null) { errors.Add(at + ": array tidak boleh null."); return set; }
            if (items.Length > 10000) errors.Add(at + ": maksimum 10000 record.");
            foreach (var item in items) if (item == null || string.IsNullOrWhiteSpace(id(item)) || !set.Add(id(item))) errors.Add(at + ": ID kosong/duplikat.");
            return set;
        }
        static void Refs(string[] ids, HashSet<string> target, string at, List<string> errors) { foreach (var id in Safe(ids)) if (id == null || !target.Contains(id)) errors.Add(at + ": referensi tidak ditemukan: " + id); }
    }

    public static class CatalogQuery
    {
        public static string Normalize(string value) => string.Join(" ", (value ?? "").Normalize(NormalizationForm.FormKC).ToLowerInvariant().Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        public static Project[] Search(Catalog c, string text = "", string category = "", int year = 0, string status = "")
        {
            string q = Normalize(text);
            return ContentValidator.Safe(c.projects).Where(p => p.publicationState == "published" && p.visibility == "public")
                .Where(p => string.IsNullOrEmpty(category) || ContentValidator.Safe(p.categoryIds).Contains(category))
                .Where(p => year == 0 || p.year == year).Where(p => string.IsNullOrEmpty(status) || p.developmentStatus == status)
                .Where(p => Normalize(p.title + " " + p.summary + " " + string.Join(" ", ContentValidator.Safe(p.tags)) + " " + string.Join(" ", ContentValidator.Safe(p.team).Select(t => t.displayName)) + " " + string.Join(" ", ContentValidator.Safe(c.categories).Where(x => ContentValidator.Safe(p.categoryIds).Contains(x.id)).Select(x => x.label))).Contains(q))
                .OrderBy(p => q.Length == 0 ? 0 : Normalize(p.title) == q ? 0 : Normalize(p.title).StartsWith(q) ? 1 : Normalize(p.title).Contains(q) ? 2 : 3)
                .ThenBy(p => p.sortOrder).ThenBy(p => Normalize(p.title), StringComparer.Ordinal).ThenBy(p => p.id, StringComparer.Ordinal).ToArray();
        }
        public static Project[] Page(Project[] items, int page, int size = 8) => items.Skip(Math.Max(0, page) * Math.Max(1, size)).Take(Math.Max(1, size)).ToArray();
        public static Project[] SearchCategories(Catalog catalog, string text, string[] categories, int year = 0, string status = "")
        {
            var selected = new HashSet<string>(ContentValidator.Safe(categories));
            return Search(catalog, text, "", year, status).Where(p => selected.Count == 0 || ContentValidator.Safe(p.categoryIds).Any(selected.Contains)).ToArray();
        }
    }
}
