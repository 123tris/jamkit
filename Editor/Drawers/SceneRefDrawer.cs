using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Draws a <see cref="SceneRef"/> as a dropdown of the scenes in Build Settings, so a scene
    /// reference is a pick, not a typed string. Prefers the stored GUID (survives renames) and
    /// falls back to the name; a value that is no longer in Build Settings is still shown, tagged,
    /// so nothing is silently lost.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneRef))]
    public sealed class SceneRefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var nameProp = property.FindPropertyRelative("_name");
            var guidProp = property.FindPropertyRelative("_guid");

            var names = new List<string>();
            var guids = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.enabled) continue;
                names.Add(Path.GetFileNameWithoutExtension(s.path));
                guids.Add(s.guid.ToString());
            }

            // Resolve the current pick: GUID first (rename-proof), then name.
            int current = -1;
            if (!string.IsNullOrEmpty(guidProp.stringValue)) current = guids.IndexOf(guidProp.stringValue);
            if (current < 0 && !string.IsNullOrEmpty(nameProp.stringValue)) current = names.IndexOf(nameProp.stringValue);

            // Option 0 is "(none)"; scenes follow. A value not in Build Settings is appended, tagged.
            var options = new List<string> { "(none)" };
            options.AddRange(names);

            int popupIndex = current >= 0 ? current + 1 : 0;
            bool hasMissing = current < 0 && !string.IsNullOrEmpty(nameProp.stringValue);
            if (hasMissing)
            {
                options.Add($"{nameProp.stringValue}  (not in Build Settings)");
                popupIndex = options.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);

            int chosen = EditorGUI.Popup(position, label.text, popupIndex, options.ToArray());
            if (chosen != popupIndex)
            {
                if (chosen == 0)
                {
                    nameProp.stringValue = string.Empty;
                    guidProp.stringValue = string.Empty;
                }
                else if (!hasMissing || chosen != options.Count - 1)
                {
                    int idx = chosen - 1;
                    nameProp.stringValue = names[idx];
                    guidProp.stringValue = guids[idx];
                }
                // (Re-selecting the tagged "missing" entry leaves the value untouched.)
            }
            else if (current >= 0)
            {
                // Heal name/GUID against the current asset — picks up renames since last edit.
                if (nameProp.stringValue != names[current]) nameProp.stringValue = names[current];
                if (guidProp.stringValue != guids[current]) guidProp.stringValue = guids[current];
            }

            EditorGUI.EndProperty();
        }
    }
}
