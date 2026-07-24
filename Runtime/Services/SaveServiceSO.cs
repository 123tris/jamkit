using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Stateless JSON persistence under <see cref="Application.persistentDataPath"/>/<see cref="Folder"/>.
    /// No runner needed — file I/O is direct.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Save Service", fileName = "SaveService")]
    public sealed class SaveServiceSO : ServiceSO
    {
        [Tooltip("Subfolder under persistentDataPath where saves live. Defaults to 'saves'.")]
        public string Folder = "saves";

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        string Root => Path.Combine(Application.persistentDataPath, Folder);
        string PathFor(string key) => Path.Combine(Root, key + ".json");

        // A key becomes a file name under Root, so a key with a separator or ".." would write
        // outside the save folder. Reject it up front (§10.5 fail fast) — a silent escape is worse
        // than a refused save.
        static bool IsValidKey(string key)
            => !string.IsNullOrEmpty(key)
               && key.IndexOf('/') < 0 && key.IndexOf('\\') < 0
               && !key.Contains("..")
               && key.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        bool ValidKeyOrWarn(string key, string op)
        {
            if (IsValidKey(key)) return true;
            Debug.LogError($"[JamKit] Save.{op} '{key}' failed — invalid key. A save key becomes a file name, so it can't be empty or contain '/', '\\', or '..'.");
            return false;
        }

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
            if (!ValidKeyOrWarn(key, "Write")) return;
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

        /// <summary>
        /// Read a saved value, distinguishing "no save exists" (returns false, no log) from "the save
        /// is unreadable or corrupt" (returns false, logs the cause). The old <see cref="Read{T}"/>
        /// collapsed both into the fallback, so a corrupt save looked identical to a fresh start.
        /// </summary>
        public bool TryRead<T>(string key, out T value)
        {
            value = default;
            if (!ValidKeyOrWarn(key, "Read")) return false;

            var path = PathFor(key);
            if (!File.Exists(path)) return false;   // no save yet — not an error

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JamKit] Save.Read '{key}' failed — could not read the file: {e.Message}");
                return false;
            }

            try
            {
                var wrap = JsonUtility.FromJson<Wrapper<T>>(json);
                if (wrap == null)
                {
                    Debug.LogError($"[JamKit] Save.Read '{key}' failed — the file is corrupt (empty or invalid JSON).");
                    return false;
                }
                value = wrap.Value;
                return true;
            }
            catch (Exception e)   // JsonUtility.FromJson throws on malformed JSON
            {
                Debug.LogError($"[JamKit] Save.Read '{key}' failed — the file is corrupt: {e.Message}");
                return false;
            }
        }

        public T Read<T>(string key, T fallback = default)
            => TryRead<T>(key, out var value) ? value : fallback;

        public bool Has(string key) => IsValidKey(key) && File.Exists(PathFor(key));

        public void Delete(string key)
        {
            if (!ValidKeyOrWarn(key, "Delete")) return;
            var path = PathFor(key);
            if (File.Exists(path)) { File.Delete(path); FlushToDisk(); }
        }

        [Button("Delete All Saves"), FoldoutGroup("Debug")]
        public void DeleteAll()
        {
            if (!Directory.Exists(Root)) return;
            foreach (var f in Directory.GetFiles(Root, "*.json")) File.Delete(f);
            FlushToDisk();
        }

        [Serializable] class Wrapper<T> { public T Value; }
    }
}
