using System.Collections.Generic;
using UnityEngine.UIElements;
using View = Metz.JamKit.MenuController.View;

namespace Metz.JamKit
{
    public sealed class MenuNavigator
    {
        readonly Dictionary<View, VisualElement> _views;
        readonly PauseCoordinator _pause;
        readonly MenuSettingsBinder _settings;
        readonly Stack<View> _stack = new();

        public MenuNavigator(Dictionary<View, VisualElement> views, PauseCoordinator pause, MenuSettingsBinder settings)
        {
            _views = views;
            _pause = pause;
            _settings = settings;
        }

        public View Current => _stack.Count > 0 ? _stack.Peek() : View.None;
        public int Depth => _stack.Count;

        public void Push(View v)
        {
            if (_stack.Count > 0 && _stack.Peek() == v) return;
            _stack.Push(v);
            Apply();
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            if (_stack.Pop() == View.Pause) _pause?.Resume();
            Apply();
        }

        public void Reset(View v)
        {
            _stack.Clear();
            if (v != View.None) _stack.Push(v);
            _pause?.Resume();
            Apply();
        }

        void Apply()
        {
            var top = Current;
            foreach (var kv in _views)
                if (kv.Value != null)
                    kv.Value.style.display = kv.Key == top ? DisplayStyle.Flex : DisplayStyle.None;

            if (top == View.Settings) _settings?.SyncToUI();

            var focus = _views.TryGetValue(top, out var view) ? FirstFocusable(view) : null;
            focus?.schedule.Execute(() => focus.Focus()); // schedule so the view is laid out first
        }

        static VisualElement FirstFocusable(VisualElement view)
        {
            if (view == null) return null;
            foreach (var el in view.Query<VisualElement>().ToList())
                if (el.focusable) return el;
            return null;
        }
    }
}
