using System;
using System.IO;
using ProjectAtlas.Application;
using UnityEngine;

namespace ProjectAtlas.Infrastructure
{
    public sealed class LocalPreferences
    {
        readonly string _path;
        public LocalPreferences(string directory) { _path = Path.Combine(directory, "atlas-preferences-v1.json"); }
        public UserPreferences Load()
        {
            if (!File.Exists(_path)) return new UserPreferences();
            try
            {
                var data = JsonUtility.FromJson<UserPreferences>(File.ReadAllText(_path));
                if (data == null || data.version != 1) throw new InvalidDataException("Unsupported preferences");
                data.Sanitize(); return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("ATLAS preferences reset: " + e.GetType().Name);
                try { File.Copy(_path, _path + ".invalid-" + DateTime.UtcNow.Ticks); } catch (Exception copyError) when (copyError is IOException || copyError is UnauthorizedAccessException) { }
                return new UserPreferences();
            }
        }
        public void Save(UserPreferences data)
        {
            data.Sanitize(); Directory.CreateDirectory(Path.GetDirectoryName(_path));
            var temp = _path + ".tmp"; File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(_path)) File.Replace(temp, _path, null); else File.Move(temp, _path);
        }
    }
}
