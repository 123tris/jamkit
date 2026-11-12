using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
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

            int current = -1;
            if (!string.IsNullOrEmpty(guidProp.stringValue)) current = guids.IndexOf(guidProp.stringValue);
            if (current < 0 && !string.IsNullOrEmpty(nameProp.stringValue)) current = names.IndexOf(nameProp.stringValue);

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
            }
            else if (current >= 0)
            {
                if (nameProp.stringValue != names[current]) nameProp.stringValue = names[current];
                if (guidProp.stringValue != guids[current]) guidProp.stringValue = guids[current];
            }

            EditorGUI.EndProperty();
        }
    }
}
