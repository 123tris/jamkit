using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    public sealed class StarterPicker : EditorWindow
    {
        static readonly Vector2 Size = new(300f, 380f);

        readonly List<GameObject> shown = new();
        List<GameObject> starters;
        GameObject parent;
        ListView list;

        [MenuItem("GameObject/JamKit/Starter…", false, 10)]
        static void OpenFromMenu(MenuCommand command) => Open(command.context as GameObject);

        public static void Open(GameObject parent = null)
        {
            EditorWindow anchor = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : focusedWindow;
            Rect area = anchor != null ? anchor.position : EditorGUIUtility.GetMainWindowPosition();
            var picker = CreateInstance<StarterPicker>();
            picker.parent = parent;
            picker.ShowAsDropDown(new Rect(area.center.x - Size.x / 2f, area.y + 30f, Size.x, 0f), Size);
        }

        public static List<GameObject> Starters()
        {
            if (!AssetDatabase.IsValidFolder(ProjectTemplate.StartersFolder)) return new List<GameObject>();
            return AssetDatabase.FindAssets("t:Prefab", new[] { ProjectTemplate.StartersFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(starter => starter != null)
                .ToList();
        }

        public static string DisplayName(GameObject starter) => ObjectNames.NicifyVariableName(starter.name.Replace('_', ' '));

        public static GameObject Place(GameObject starter, GameObject parent = null)
        {
            Scene scene = parent != null ? parent.scene : SceneManager.GetActiveScene();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(starter, scene);
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(instance, parent);
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                Vector3 pivot = SceneView.lastActiveSceneView.pivot;
                instance.transform.position = SceneView.lastActiveSceneView.in2DMode ? new Vector3(pivot.x, pivot.y, 0f) : pivot;
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Place {starter.name}");
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            return instance;
        }

        void CreateGUI()
        {
            starters = Starters();
            if (starters.Count == 0)
            {
                rootVisualElement.Add(new HelpBox("This project has no starters yet. JamKit > New Jam Project copies them in.", HelpBoxMessageType.Info));
                rootVisualElement.Add(new Button(() =>
                {
                    Close();
                    JamProjectWizard.Run();
                }) { text = "New Jam Project" });
                return;
            }

            var search = new ToolbarSearchField { style = { width = StyleKeyword.Auto } };
            list = new ListView(shown, 22f, MakeRow, BindRow) { selectionType = SelectionType.Single, style = { flexGrow = 1f } };
            list.itemsChosen += chosen => Choose(chosen.OfType<GameObject>().FirstOrDefault());
            search.RegisterValueChangedCallback(change => Filter(change.newValue));
            search.RegisterCallback<KeyDownEvent>(key =>
            {
                if (key.keyCode is KeyCode.Return or KeyCode.KeypadEnter) Choose(shown.FirstOrDefault());
                else if (key.keyCode == KeyCode.Escape) Close();
            }, TrickleDown.TrickleDown);

            rootVisualElement.Add(search);
            rootVisualElement.Add(list);
            Filter(string.Empty);
            search.schedule.Execute(() => search.Q<TextField>()?.Focus());
        }

        void Filter(string text)
        {
            shown.Clear();
            shown.AddRange(starters.Where(starter => DisplayName(starter).IndexOf(text ?? "", StringComparison.OrdinalIgnoreCase) >= 0));
            list.RefreshItems();
        }

        void Choose(GameObject starter)
        {
            if (starter == null) return;
            GameObject under = parent;
            Close();
            Place(starter, under);
        }

        VisualElement MakeRow()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new Image { style = { width = 16f, height = 16f, marginRight = 4f } });
            row.Add(new Label());
            row.RegisterCallback<ClickEvent>(_ => Choose(row.userData as GameObject));
            return row;
        }

        void BindRow(VisualElement row, int index)
        {
            GameObject starter = shown[index];
            row.userData = starter;
            row.tooltip = AssetDatabase.GetAssetPath(starter);
            row.Q<Image>().image = AssetPreview.GetMiniThumbnail(starter);
            row.Q<Label>().text = DisplayName(starter);
        }
    }
}
