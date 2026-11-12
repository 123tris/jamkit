using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    [InitializeOnLoad]
    public static class JamKitAutoAssign
    {
        static JamKitAutoAssign()
        {
            ObjectFactory.componentWasAdded += OnComponentAdded;
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        static void OnComponentAdded(Component component)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !IsJamKitType(component.GetType())) return;
            FillComponent(component, log: true);
        }

        static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy) continue;
                stream.GetCreateGameObjectHierarchyEvent(i, out var created);
                if (EditorUtility.InstanceIDToObject(created.instanceId) is GameObject root) FillHierarchy(root, log: false);
            }
        }

        public static int FillPrefabAsset(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int filled = FillHierarchy(root, log: false);
                if (filled > 0) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return filled;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("JamKit/Auto-Assign References In Open Scenes", priority = 20)]
        public static void FillOpenScenes()
        {
            int total = 0;
            foreach (MonoBehaviour component in AllJamKitComponentsInOpenScenes())
                total += FillComponent(component, log: true);
            Debug.Log($"[JamKit] Auto-assign filled {total} reference(s).");
        }

        public static bool IsJamKitType(Type type) => type.Namespace != null && type.Namespace.StartsWith("Metz.JamKit");

        public static IEnumerable<MonoBehaviour> AllJamKitComponentsInOpenScenes()
        {
            foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (component != null && IsJamKitType(component.GetType()))
                    yield return component;
        }

        static int FillHierarchy(GameObject root, bool log)
        {
            int filled = 0;
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && IsJamKitType(component.GetType()))
                    filled += FillComponent(component, log);
            return filled;
        }

        public static int FillComponent(Component component, bool log = false)
        {
            var serialized = new SerializedObject(component);
            bool isolated = PrefabStageUtility.GetPrefabStage(component.gameObject) != null
                            || EditorSceneManager.IsPreviewSceneObject(component.gameObject);
            var report = new StringBuilder();
            int filled = 0;

            foreach (FieldInfo field in component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                SerializedProperty property = serialized.FindProperty(field.Name);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue != null) continue;

                Object match = FindCandidate(field, isolated);
                if (match == null) continue;

                property.objectReferenceValue = match;
                report.Append($"\n  {field.Name} = {match.name}");
                filled++;
            }

            if (filled == 0) return 0;
            serialized.ApplyModifiedProperties();
            if (log) Debug.Log($"[JamKit] Auto-assigned on {component.GetType().Name} ({component.name}):{report}", component);
            return filled;
        }

        static Object FindCandidate(FieldInfo field, bool isolated)
        {
            Type type = field.FieldType;

            if (type == typeof(InputActionReference))
                return field.IsDefined(typeof(RequiredAttribute)) ? InputReferences.Find(field.Name) : null;

            if (!IsJamKitType(type)) return null;

            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                string[] guids = AssetDatabase.FindAssets("t:" + type.Name);
                return guids.Length == 1 ? AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), type) : null;
            }

            if (isolated || !typeof(Component).IsAssignableFrom(type)) return null;
            Object[] inScene = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
            return inScene.Length == 1 ? inScene[0] : null;
        }
    }
}
