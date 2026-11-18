using System;
using System.Collections.Generic;
using System.Reflection;
using Metz.JamKit.Inspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public sealed class RequiredDrawer : PropertyDrawer
    {
        static readonly Color MissingTint = new(1f, 0.55f, 0.55f);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return new HelpBox($"[Required] only checks object references, and {preferredLabel} is not one.", HelpBoxMessageType.Warning);

            var root = new VisualElement();
            var field = new ObjectField(preferredLabel)
            {
                objectType = ObjectType(fieldInfo.FieldType),
                allowSceneObjects = !EditorUtility.IsPersistent(property.serializedObject.targetObject),
                tooltip = fieldInfo.GetCustomAttribute<TooltipAttribute>()?.tooltip,
            };
            field.AddToClassList(BaseField<UnityEngine.Object>.alignedFieldUssClassName);
            field.BindProperty(property);

            var warning = new HelpBox($"{preferredLabel} is required.", HelpBoxMessageType.Error);
            root.Add(field);
            root.Add(warning);

            void Refresh(SerializedProperty changed) => warning.style.display = IsMissing(changed) ? DisplayStyle.Flex : DisplayStyle.None;
            Refresh(property);
            root.TrackPropertyValue(property, Refresh);
            return root;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Color previous = GUI.color;
            if (IsMissing(property)) GUI.color = MissingTint;
            EditorGUI.ObjectField(position, property, ObjectType(fieldInfo.FieldType), label);
            GUI.color = previous;
        }

        public static bool IsMissing(SerializedProperty property)
            => property.propertyType == SerializedPropertyType.ObjectReference
               && !property.hasMultipleDifferentValues
               && property.objectReferenceValue == null;

        static Type ObjectType(Type fieldType)
        {
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) return fieldType.GetGenericArguments()[0];
            return fieldType;
        }
    }
}
