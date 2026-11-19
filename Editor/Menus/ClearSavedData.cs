using System.Collections.Generic;
using System.Linq;
using Ripple;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    public static class ClearSavedData
    {
        [MenuItem("JamKit/Clear Saved Data", priority = 40)]
        public static void ClearFromMenu()
        {
            List<BaseVariable> persisted = PersistedVariables();
            if (persisted.Count == 0)
            {
                EditorUtility.DisplayDialog("JamKit: Clear Saved Data", "No Ripple variable in this project has Persist ticked, so nothing is saved.", "OK");
                return;
            }

            string names = string.Join(", ", persisted.Select(variable => variable.name));
            if (!EditorUtility.DisplayDialog("JamKit: Clear Saved Data",
                    $"Clear the saved value of {persisted.Count} persistent variable(s) on this computer: {names}?\n\n" +
                    "Each starts from its initial value again. This can't be undone.",
                    "Clear", "Cancel"))
                return;

            Clear(persisted);
            Debug.Log($"[JamKit] Cleared the saved values of {names}.");
        }

        public static List<BaseVariable> PersistedVariables()
            => AssetDatabase.FindAssets("t:" + nameof(BaseVariable))
                .Select(guid => AssetDatabase.LoadAssetAtPath<BaseVariable>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(variable => variable != null && IsPersisted(variable))
                .OrderBy(variable => variable.name)
                .ToList();

        public static void Clear(IEnumerable<BaseVariable> variables)
        {
            foreach (BaseVariable variable in variables) variable.ClearSavedValue();
            PlayerPrefs.Save();
        }

        static bool IsPersisted(BaseVariable variable)
        {
            using var serialized = new SerializedObject(variable);
            SerializedProperty persist = serialized.FindProperty("_persist");
            return persist != null && persist.boolValue;
        }
    }
}
