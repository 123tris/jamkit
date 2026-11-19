using System;
using System.Collections.Generic;
using Metz.JamKit.Inspector;
using Ripple;
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
            [UxmlName(typeof(Button), nameof(MenuView.RootElementName))]
            public string ElementName;

            public Signal OnClicked = new();
        }

        [UxmlName, Tooltip("Name of this screen's root element in the menu UXML.")]
        public string RootElementName;

        [Tooltip("Refuses to open while false. Point at a Ripple bool to gate on game state.")]
        public BoolReference CanOpen = new(true);

        [Header("While on top")] public bool PausesTime;
        public bool SwitchesToUIInput = true;

        public Signal OnShown = new();
        public Signal OnHidden = new();

        public List<ButtonBinding> Buttons = new();

        VisualElement root;

        [ShowInInspector]
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
    }
}
