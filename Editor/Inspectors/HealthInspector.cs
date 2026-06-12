using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    [CustomEditor(typeof(Health))]
    public sealed class HealthInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            var h = (Health)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Ratio: {h.Ratio01:P0}");
            var rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, h.Ratio01, $"{h.Current:0.0} / {h.Max:0.0}");
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Damage 1")) h.Damage(1f);
                if (GUILayout.Button("Heal 1")) h.Heal(1f);
                if (GUILayout.Button("Kill")) h.Kill();
                if (GUILayout.Button("Reset")) h.ResetFull();
            }
        }
    }
}
