using System;
using System.IO;
using ProjectAtlas.Application;
using ProjectAtlas.Domain;
using UnityEngine;

namespace ProjectAtlas.Infrastructure
{
    public sealed class LocalCatalogRepository : IProjectRepository
    {
        readonly string _root;
        public LocalCatalogRepository(string root) { _root = Path.GetFullPath(root); }
        public Catalog Load()
        {
            string path = Path.Combine(_root, "Catalog/catalog.json");
            if (!File.Exists(path)) throw new InvalidDataException("Katalog lokal belum tersedia. Jalankan Build Starter Scenes.");
            if (new FileInfo(path).Length > 8 * 1024 * 1024) throw new InvalidDataException("Katalog melebihi batas 8 MB.");
            var result = JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
            var errors = ContentValidator.Validate(result);
            if (errors.Count > 0) throw new InvalidDataException(string.Join("\n", errors));
            return result;
        }
    }
}
