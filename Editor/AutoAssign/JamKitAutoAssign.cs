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
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        static void OnComponentAdded(Component c)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (c == null || !IsJamKitType(c.GetType())) return;
            FillComponent(c, log: true);
        }

        /// <summary>
        /// Safety net for prefab drag-ins (componentWasAdded doesn't fire for those): when a
        /// hierarchy is created — dragging a package/sample prefab into a scene — fill any null
        /// JamKit refs as instance overrides. If this ever misses a path, [Required] shows red
        /// and the Doctor's Auto-Assign button catches it: three nets, none load-bearing.
        /// </summary>
        static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy) continue;
                stream.GetCreateGameObjectHierarchyEvent(i, out var evt);
                if (EditorUtility.InstanceIDToObject(evt.instanceId) is not GameObject go) continue;
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && IsJamKitType(mb.GetType()))
                        FillComponent(mb, log: false);
            }
        }

        /// <summary>
        /// Fill null JamKit refs across a prefab ASSET (sample setup wires imported sample
        /// prefabs to the project's services this way — every instance everywhere inherits it).
        /// </summary>
        public static int FillPrefabAsset(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int filled = 0;
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && IsJamKitType(mb.GetType()))
                        filled += FillComponent(mb, log: false);
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
            // Prefab stage AND LoadPrefabContents previews are isolated: scene references can't be
            // saved into them, so component lookups are skipped there (asset refs still fill).
            bool inPrefabStage = PrefabStageUtility.GetPrefabStage(c.gameObject) != null
                || EditorSceneManager.IsPreviewSceneObject(c.gameObject);

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
