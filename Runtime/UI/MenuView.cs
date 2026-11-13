using System;
using System.Collections.Generic;
using System.Linq;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class MenuView : MonoBehaviour
    {
        const string HiddenClass = "jk-view--hidden";
        const string ShownClass = "jk-view--shown";

        [Serializable]
        public sealed class ButtonBinding
        {
            [ValueDropdown("@$root.ButtonNames()")]
            public string ElementName;

            public Signal OnClicked = new();
        }

        [ValueDropdown(nameof(ElementNames)), Tooltip("Name of this screen's root element in the menu UXML.")]
        public string RootElementName;

        [Tooltip("Refuses to open while false. Point at a Ripple bool to gate on game state.")]
        public BoolReference CanOpen = new(true);

        [BoxGroup("While on top")] public bool PausesTime;
        [BoxGroup("While on top")] public bool SwitchesToUIInput = true;

        [FoldoutGroup("Events")] public Signal OnShown = new();
        [FoldoutGroup("Events")] public Signal OnHidden = new();

        public List<ButtonBinding> Buttons = new();

        VisualElement root;

        [ShowInInspector, ReadOnly, HideInEditorMode, FoldoutGroup("Debug")]
        public bool IsVisible => root != null && root.ClassListContains(ShownClass);

        internal void Bind(VisualElement documentRoot)
        {
            root = documentRoot.Q(RootElementName);
            root.AddToClassList(HiddenClass);
            root.RemoveFromClassList(ShownClass);
            foreach (ButtonBinding button in Buttons)
                root.Q<Button>(button.ElementName).clicked += button.OnClicked.Invoke;
        }

        internal void SetVisible(bool visible)
        {
            if (root == null || visible == IsVisible) return;
            root.EnableInClassList(HiddenClass, !visible);
            root.EnableInClassList(ShownClass, visible);
            if (visible) OnShown.Invoke();
            else OnHidden.Invoke();
        }

        internal void Focus()
        {
            VisualElement first = root?.Query().Where(element => element.focusable).First();
            first?.schedule.Execute(first.Focus);
        }

#if UNITY_EDITOR
        IEnumerable<string> ElementNames() => Named<VisualElement>(MenuTree());

        IEnumerable<string> ButtonNames() => Named<Button>(MenuTree()?.Q(RootElementName));

        VisualElement MenuTree()
        {
            var panel = GetComponentInParent<PanelRenderer>();
            return panel == null || panel.visualTreeAsset == null ? null : panel.visualTreeAsset.CloneTree();
        }

        static IEnumerable<string> Named<T>(VisualElement scope) where T : VisualElement
            => scope == null
                ? Enumerable.Empty<string>()
                : scope.Query<T>().ToList().Select(element => element.name).Where(elementName => !string.IsNullOrEmpty(elementName));
#endif
    }
}
