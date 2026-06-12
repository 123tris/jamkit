using System;
using System.IO;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Stateless JSON persistence under <see cref="Application.persistentDataPath"/>/<see cref="Folder"/>.
    /// No runner needed — file I/O is direct.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Save Service", fileName = "SaveService")]
    public sealed class SaveServiceSO : ScriptableObject
    {
        [Tooltip("Subfolder under persistentDataPath where saves live. Defaults to 'saves'.")]
        public string Folder = "saves";

        string Root => Path.Combine(Application.persistentDataPath, Folder);
        string PathFor(string key) => Path.Combine(Root, key + ".json");

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void JamKitSyncFiles();
#endif

        /// <summary>On WebGL, flush the in-memory FS to IndexedDB so saves survive the tab closing. No-op elsewhere.</summary>
        static void FlushToDisk()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JamKitSyncFiles();
#endif
        }

        public void Write<T>(string key, T data)
        {
            try
            {
                Directory.CreateDirectory(Root);
                var wrap = new Wrapper<T> { Value = data };
                File.WriteAllText(PathFor(key), JsonUtility.ToJson(wrap, prettyPrint: true));
                FlushToDisk();
            }
            catch (Exception e)
            {
                Debug.LogError($"[JamKit] Save.Write '{key}' failed: {e.Message}");
            }
        }

        public T Read<T>(string key, T fallback = default)
        {
            try
            {
                var path = PathFor(key);
                if (!File.Exists(path)) return fallback;
                var wrap = JsonUtility.FromJson<Wrapper<T>>(File.ReadAllText(path));
                return wrap == null ? fallback : wrap.Value;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JamKit] Save.Read '{key}' failed: {e.Message}");
                return fallback;
            }
        }

        public bool Has(string key) => File.Exists(PathFor(key));

        public void Delete(string key)
        {
            var path = PathFor(key);
            if (File.Exists(path)) { File.Delete(path); FlushToDisk(); }
        }

        public void DeleteAll()
        {
            if (!Directory.Exists(Root)) return;
            foreach (var f in Directory.GetFiles(Root, "*.json")) File.Delete(f);
            FlushToDisk();
        }

        [Serializable] class Wrapper<T> { public T Value; }
    }
}
