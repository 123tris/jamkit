using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    public sealed class JamKitDoctorWindow : EditorWindow
    {
        const string CleanClass = "jk-doctor--clean";
        const string FixableClass = "jk-issue--fixable";
        const string SelectableClass = "jk-issue--selectable";

        [SerializeField] VisualTreeAsset layout;
        [SerializeField] VisualTreeAsset issueRow;

        List<JamKitDoctor.Issue> _issues = new();
        VisualElement _root;
        ScrollView _list;

        [MenuItem("JamKit/Doctor", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<JamKitDoctorWindow>("JamKit Doctor");
            window.minSize = new Vector2(420f, 240f);
            Scan();
        }

        void CreateGUI()
        {
            layout.CloneTree(rootVisualElement);
            _root = rootVisualElement.Q(className: "jk-doctor");
            _list = _root.Q<ScrollView>("issues");
            _root.Q<ToolbarButton>("rescan").clicked += Scan;
            _root.Q<ToolbarButton>("auto-assign").clicked += () =>
            {
                JamKitAutoAssign.FillOpenScenes();
                Scan();
            };
            Scan();
        }

        void OnEnable() => JamKitDoctor.Scanned += OnScanned;

        void OnDisable() => JamKitDoctor.Scanned -= OnScanned;

        static void Scan() => JamKitDoctor.Scan();

        void OnScanned()
        {
            _issues = JamKitDoctor.LastIssues.ToList();
            ShowIssues();
        }

        void ShowIssues()
        {
            if (_root == null) return;
            _list.Clear();
            _root.EnableInClassList(CleanClass, _issues.Count == 0);
            foreach (var issue in _issues) _list.Add(Row(issue));
        }

        VisualElement Row(JamKitDoctor.Issue issue)
        {
            VisualElement row = issueRow.CloneTree();
            var message = row.Q<HelpBox>("message");
            message.text = issue.Message;
            message.messageType = (HelpBoxMessageType)issue.Severity;

            VisualElement body = row.Q(className: "jk-issue");
            body.EnableInClassList(FixableClass, issue.Fix != null);
            body.EnableInClassList(SelectableClass, issue.Context != null);

            var fix = row.Q<Button>("fix");
            fix.text = issue.FixLabel ?? "Fix";
            fix.clicked += () =>
            {
                issue.Fix();
                Scan();
            };
            row.Q<Button>("select").clicked += () =>
            {
                Selection.activeObject = issue.Context;
                EditorGUIUtility.PingObject(issue.Context);
            };
            return row;
        }
    }
}
