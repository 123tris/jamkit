using System.Collections.Generic;
using System.Linq;
using Metz.JamKit.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    [CustomPropertyDrawer(typeof(UxmlNameAttribute))]
    public sealed class UxmlNameDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            List<string> names = Names(property, (UxmlNameAttribute)attribute);
            BaseField<string> field;
            if (names == null)
            {
                field = new TextField(preferredLabel);
            }
            else
            {
                if (!string.IsNullOrEmpty(property.stringValue) && !names.Contains(property.stringValue)) names.Insert(0, property.stringValue);
                field = new DropdownField(preferredLabel) { choices = names };
            }

            field.tooltip = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true).OfType<TooltipAttribute>().FirstOrDefault()?.tooltip;
            field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            field.BindProperty(property);
            return field;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            => property.stringValue = EditorGUI.TextField(position, label, property.stringValue);

        public static List<string> Names(SerializedProperty property, UxmlNameAttribute uxmlName)
        {
            var component = property.serializedObject.targetObject as Component;
            PanelRenderer panel = component == null ? null : component.GetComponentInParent<PanelRenderer>(true);
            if (panel == null || panel.visualTreeAsset == null) return null;

            VisualElement scope = panel.visualTreeAsset.CloneTree();
            if (uxmlName.Scope != null)
            {
                string scopeName = property.serializedObject.FindProperty(uxmlName.Scope)?.stringValue;
                if (!string.IsNullOrEmpty(scopeName)) scope = scope.Q(scopeName) ?? scope;
            }

            return scope.Query<VisualElement>().ToList()
                .Where(element => !string.IsNullOrEmpty(element.name))
                .Where(element => uxmlName.ElementType == null || uxmlName.ElementType.IsInstanceOfType(element))
                .Select(element => element.name)
                .Distinct()
                .ToList();
        }
    }
}
