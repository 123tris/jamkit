using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    [CustomPropertyDrawer(typeof(SceneRef))]
    public sealed class SceneRefDrawer : PropertyDrawer
    {
        const string None = "(none)";
        const string NotInBuild = "  (not in Build Settings)";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty sceneName = property.FindPropertyRelative("_name");
            SerializedProperty sceneGuid = property.FindPropertyRelative("_guid");
            var field = new DropdownField(preferredLabel);
            field.AddToClassList(BaseField<string>.alignedFieldUssClassName);

            void Refresh()
            {
                BuildScenes(out List<string> names, out List<string> guids);
                int current = string.IsNullOrEmpty(sceneGuid.stringValue) ? -1 : guids.IndexOf(sceneGuid.stringValue);
                if (current < 0 && !string.IsNullOrEmpty(sceneName.stringValue)) current = names.IndexOf(sceneName.stringValue);

                var choices = new List<string> { None };
                choices.AddRange(names);
                string shown = None;
                if (current >= 0)
                {
                    shown = names[current];
                    if (sceneName.stringValue != names[current] || sceneGuid.stringValue != guids[current])
                    {
                        sceneName.stringValue = names[current];
                        sceneGuid.stringValue = guids[current];
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                else if (!string.IsNullOrEmpty(sceneName.stringValue))
                {
                    shown = sceneName.stringValue + NotInBuild;
                    choices.Add(shown);
                }

                field.choices = choices;
                field.SetValueWithoutNotify(shown);
            }

            field.RegisterValueChangedCallback(change =>
            {
                if (change.newValue.EndsWith(NotInBuild)) return;
                BuildScenes(out List<string> names, out List<string> guids);
                int index = names.IndexOf(change.newValue);
                sceneName.stringValue = index < 0 ? string.Empty : names[index];
                sceneGuid.stringValue = index < 0 ? string.Empty : guids[index];
                property.serializedObject.ApplyModifiedProperties();
            });
            field.TrackPropertyValue(property, _ => Refresh());
            field.RegisterCallback<AttachToPanelEvent>(_ => EditorBuildSettings.sceneListChanged += Refresh);
            field.RegisterCallback<DetachFromPanelEvent>(_ => EditorBuildSettings.sceneListChanged -= Refresh);
            Refresh();
            return field;
        }

        static void BuildScenes(out List<string> names, out List<string> guids)
        {
            names = new List<string>();
            guids = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                names.Add(Path.GetFileNameWithoutExtension(scene.path));
                guids.Add(scene.guid.ToString());
            }
        }
    }
}
