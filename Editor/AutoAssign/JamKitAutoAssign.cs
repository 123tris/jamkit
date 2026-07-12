using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Kills the per-component drag work: whenever a JamKit component is added in the editor,
    /// its null JamKit-typed references are filled automatically — service SOs from the project
    /// (InputService, PoolService, …) and scene components (MenuController, Toast, …)
    /// — but only when there is exactly one candidate. Zero or many = left null, so the
    /// assignment is never a guess; the Validate window surfaces what remains.
    /// Runtime stays fully explicit: this writes ordinary serialized references you can see
    /// and change in the inspector.
    /// </summary>
    [InitializeOnLoad]
    public static class JamKitAutoAssign
    {
        static JamKitAutoAssign()
        {
            ObjectFactory.componentWasAdded += OnComponentAdded;
        }

        static void OnComponentAdded(Component c)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (c == null || !IsJamKitType(c.GetType())) return;
            FillComponent(c, log: true);
        }

        [MenuItem("JamKit/Auto-Assign References In Open Scenes", priority = 20)]
        public static void FillOpenScenes()
        {
            int total = 0;
            foreach (var c in AllJamKitComponentsInOpenScenes())
                total += FillComponent(c, log: true);
            Debug.Log($"[JamKit] Auto-assign pass complete — {total} reference(s) filled.");
        }

        public static bool IsJamKitType(Type t)
            => t.Namespace != null && t.Namespace.StartsWith("Metz.JamKit");

        public static IEnumerable<MonoBehaviour> AllJamKitComponentsInOpenScenes()
        {
            var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in all)
                if (mb != null && IsJamKitType(mb.GetType()))
                    yield return mb;
        }

        /// <summary>Fill this component's null JamKit references where exactly one candidate exists. Returns fills made.</summary>
        public static int FillComponent(Component c, bool log = false)
        {
            int filled = 0;
            StringBuilder report = null;
            var so = new SerializedObject(c);
            bool inPrefabStage = PrefabStageUtility.GetPrefabStage(c.gameObject) != null;

            foreach (var field in CandidateFields(c.GetType()))
            {
                var prop = so.FindProperty(field.Name);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (prop.objectReferenceValue != null) continue;

                var (match, count) = FindCandidate(field.FieldType, inPrefabStage);
                if (count != 1 || match == null) continue;

                prop.objectReferenceValue = match;
                filled++;
                if (log)
                {
                    report ??= new StringBuilder($"[JamKit] Auto-assigned on {c.GetType().Name} ({c.gameObject.name}):");
                    report.Append($"\n  {field.Name} → {match.name}");
                }
            }

            if (filled > 0) so.ApplyModifiedProperties();
            if (report != null) Debug.Log(report.ToString(), c);
            return filled;
        }

        /// <summary>
        /// For the Validate window: every null JamKit reference on the component, with how many
        /// candidates exist (1 = auto-fillable, 0 = missing, 2+ = ambiguous).
        /// </summary>
        public static List<(string field, int candidates)> AnalyzeNulls(Component c)
        {
            var result = new List<(string, int)>();
            var so = new SerializedObject(c);
            bool inPrefabStage = PrefabStageUtility.GetPrefabStage(c.gameObject) != null;
            foreach (var field in CandidateFields(c.GetType()))
            {
                var prop = so.FindProperty(field.Name);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (prop.objectReferenceValue != null) continue;
                var (_, count) = FindCandidate(field.FieldType, inPrefabStage);
                result.Add((field.Name, count));
            }
            return result;
        }

        static IEnumerable<FieldInfo> CandidateFields(Type componentType)
        {
            foreach (var f in componentType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var ft = f.FieldType;
                if (!typeof(Object).IsAssignableFrom(ft)) continue;
                // Only fields whose *type* lives in JamKit: services and scene singleton-ish
                // components. Ripple events/variables are deliberately excluded — which event an
                // effect listens to is a design choice, not plumbing.
                if (!IsJamKitType(ft)) continue;
                yield return f;
            }
        }

        static (Object match, int count) FindCandidate(Type fieldType, bool inPrefabStage)
        {
            if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
            {
                var guids = AssetDatabase.FindAssets("t:" + fieldType.Name);
                Object single = null;
                int count = 0;
                foreach (var guid in guids)
                {
                    var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), fieldType);
                    if (asset == null) continue;
                    count++;
                    single = asset;
                }
                return (count == 1 ? single : null, count);
            }

            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                // Scene references can't be saved into a prefab being edited in isolation.
                if (inPrefabStage) return (null, 0);
                var objs = Object.FindObjectsByType(fieldType, FindObjectsInactive.Include, FindObjectsSortMode.None);
                return (objs.Length == 1 ? objs[0] : null, objs.Length);
            }

            return (null, 0);
        }
    }
}
