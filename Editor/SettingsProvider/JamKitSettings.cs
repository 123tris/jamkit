using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    [FilePath("ProjectSettings/JamKitSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class JamKitSettings : ScriptableSingleton<JamKitSettings>
    {
        [Tooltip("Your itch.io page as user/game, for example metz/space-jam. Empty only builds and zips.")]
        public string ItchTarget = "";

        [Tooltip("The butler channel. A name with 'html5' in it makes itch.io offer the build to play in the browser.")]
        public string ItchChannel = "html5";

        [Tooltip("butler's full path, when it is not on your PATH.")]
        public string ButlerPath = "";

        [Tooltip("A WebGL Build Profile to build with. Empty builds WebGL with the scenes in Build Settings.")]
        public BuildProfile BuildProfile;

        public void SaveSettings() => Save(true);

        [SettingsProvider]
        static SettingsProvider Create() => new("Project/JamKit", SettingsScope.Project)
        {
            keywords = new[] { "itch", "butler", "publish", "webgl", "jam" },
            activateHandler = (_, root) =>
            {
                instance.hideFlags &= ~HideFlags.NotEditable;
                var serialized = new SerializedObject(instance);
                var page = new VisualElement { style = { paddingLeft = 10f, paddingTop = 2f, paddingRight = 10f } };
                page.Add(new Label("JamKit") { style = { fontSize = 19f, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8f } });
                page.Add(new Label("Publish to itch.io") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f } });
                foreach (string field in new[] { nameof(ItchTarget), nameof(ItchChannel), nameof(ButlerPath), nameof(BuildProfile) })
                    page.Add(new PropertyField(serialized.FindProperty(field)));
                page.Add(new HelpBox("Log in once with 'butler login' in a terminal; butler keeps the login, JamKit never sees it.",
                    HelpBoxMessageType.Info) { style = { marginTop = 6f } });
                page.Bind(serialized);
                page.TrackSerializedObjectValue(serialized, _ => instance.SaveSettings());
                root.Add(page);
            },
        };
    }
}
