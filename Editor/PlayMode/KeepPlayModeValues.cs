using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    [InitializeOnLoad]
    public static class KeepPlayModeValues
    {
        const string MenuPath = "CONTEXT/Component/Keep These Values After Play";
        const string ArraySize = ".Array.size";

        sealed class Snapshot
        {
            public string Name;
            public readonly List<(string path, object value)> Values = new();
        }

        static readonly Dictionary<GlobalObjectId, Snapshot> kept = new();

        static KeepPlayModeValues()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Apply();
            };
        }

        public static int Count => kept.Count;

        [MenuItem(MenuPath, true)]
        static bool CanKeep(MenuCommand command)
            => EditorApplication.isPlaying && command.context is Component component && IsSavedInAFile(component);

        [MenuItem(MenuPath, false, 1000)]
        static void KeepFromMenu(MenuCommand command) => Keep((Component)command.context);

        public static void Keep(Component component)
        {
            kept[GlobalObjectId.GetGlobalObjectIdSlow(component)] = Take(component);
            Debug.Log($"[JamKit] {Describe(component)}: these values will be written back when Play mode ends.", component);
        }

        static bool IsSavedInAFile(Component component) => GlobalObjectId.GetGlobalObjectIdSlow(component).targetObjectId != 0;

        static string Describe(Component component) => $"{component.GetType().Name} on '{component.name}'";

        static Snapshot Take(Component component)
        {
            var snapshot = new Snapshot { Name = Describe(component) };
            using var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            bool enter = true;
            while (property.NextVisible(enter))
            {
                enter = property.propertyType == SerializedPropertyType.Generic;
                if (property.propertyPath == "m_Script") continue;

                switch (property.propertyType)
                {
                    case SerializedPropertyType.Generic:
                        if (property.isArray) snapshot.Values.Add((property.propertyPath + ArraySize, property.arraySize));
                        break;
                    case SerializedPropertyType.ManagedReference:
                        break;
                    case SerializedPropertyType.ObjectReference:
                        Object reference = property.objectReferenceValue;
                        if (reference != null && EditorUtility.IsPersistent(reference)) snapshot.Values.Add((property.propertyPath, reference));
                        break;
                    case SerializedPropertyType.ArraySize:
                        snapshot.Values.Add((property.propertyPath, property.intValue));
                        break;
                    default:
                        try
                        {
                            snapshot.Values.Add((property.propertyPath, property.boxedValue));
                        }
                        catch (Exception)
                        {
                        }
                        break;
                }
            }
            return snapshot;
        }

        static void Apply()
        {
            if (kept.Count == 0) return;

            var written = new List<string>();
            foreach (KeyValuePair<GlobalObjectId, Snapshot> entry in kept)
            {
                if (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(entry.Key) is not Component target)
                {
                    Debug.LogWarning($"[JamKit] {entry.Value.Name} was not found after Play mode, so its values were not kept.");
                    continue;
                }

                using var serialized = new SerializedObject(target);
                foreach ((string path, object value) in entry.Value.Values)
                {
                    SerializedProperty property = serialized.FindProperty(path);
                    if (property == null) continue;
                    try
                    {
                        if (path.EndsWith(ArraySize)) property.intValue = (int)value;
                        else if (property.propertyType == SerializedPropertyType.ObjectReference) property.objectReferenceValue = (Object)value;
                        else property.boxedValue = value;
                    }
                    catch (Exception)
                    {
                    }
                }
                serialized.ApplyModifiedProperties();
                written.Add(entry.Value.Name);
            }

            kept.Clear();
            if (written.Count > 0) Debug.Log($"[JamKit] Kept the Play mode values of {string.Join(", ", written)}. Undo reverts them.");
        }
    }
}
