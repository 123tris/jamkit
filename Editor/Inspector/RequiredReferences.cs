using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Metz.JamKit.Inspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public static class RequiredReferences
    {
        const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        const int MaxDepth = 4;

        static readonly string[] SkippedNamespaces = { "System", "UnityEngine", "UnityEditor", "Unity.", "Ripple", "TMPro", "Sirenix" };
        static readonly Dictionary<Type, FieldInfo[]> fieldsByType = new();

        public static List<string> Missing(Object owner)
        {
            var missing = new List<string>();
            if (owner != null) Collect(owner, "", 0, missing);
            return missing;
        }

        static void Collect(object owner, string prefix, int depth, List<string> missing)
        {
            foreach (FieldInfo field in SerializedFields(owner.GetType()))
            {
                object value = field.GetValue(owner);
                string name = prefix + ObjectNames.NicifyVariableName(field.Name);
                if (field.IsDefined(typeof(RequiredAttribute), true))
                {
                    if (!IsHidden(field, owner) && (value is Object reference ? reference == null : value == null)) missing.Add(name);
                    continue;
                }

                if (depth >= MaxDepth || value == null || value is Object) continue;
                if (value is IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                        if (list[i] != null && !(list[i] is Object) && IsScanned(list[i].GetType()))
                            Collect(list[i], $"{name} {i + 1} > ", depth + 1, missing);
                }
                else if (IsScanned(value.GetType()))
                {
                    Collect(value, name + " > ", depth + 1, missing);
                }
            }
        }

        static bool IsHidden(FieldInfo field, object owner)
        {
            var show = field.GetCustomAttribute<ShowIfAttribute>();
            var hide = field.GetCustomAttribute<HideIfAttribute>();
            string conditionName = show?.Condition ?? hide?.Condition;
            if (conditionName == null) return false;

            FieldInfo condition = SerializedFields(owner.GetType()).FirstOrDefault(each => each.Name == conditionName);
            if (condition == null || condition.FieldType != typeof(bool)) return false;
            return (bool)condition.GetValue(owner) != (show != null);
        }

        static FieldInfo[] SerializedFields(Type type)
        {
            if (fieldsByType.TryGetValue(type, out FieldInfo[] fields)) return fields;

            var found = new List<FieldInfo>();
            for (Type each = type; each != null && IsScanned(each); each = each.BaseType)
                found.AddRange(each.GetFields(Declared).Where(IsSerialized));
            fields = found.ToArray();
            fieldsByType[type] = fields;
            return fields;
        }

        static bool IsSerialized(FieldInfo field)
            => !field.IsNotSerialized
               && (field.IsPublic || field.IsDefined(typeof(SerializeField), true) || field.IsDefined(typeof(SerializeReference), true));

        static bool IsScanned(Type type)
        {
            if (type == typeof(object) || typeof(Object) == type || type == typeof(Component) || type == typeof(Behaviour)
                || type == typeof(MonoBehaviour) || type == typeof(ScriptableObject))
                return false;
            string space = type.Namespace ?? "";
            return !SkippedNamespaces.Any(skipped => space.StartsWith(skipped, StringComparison.Ordinal));
        }
    }
}
