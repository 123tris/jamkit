using System;
using UnityEngine;

namespace Metz.JamKit.Inspector
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : PropertyAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : Attribute
    {
        public ButtonAttribute(string label = null) => Label = label;

        public string Label { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ShowInInspectorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowIfAttribute : Attribute
    {
        public ShowIfAttribute(string condition) => Condition = condition;

        public string Condition { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HideIfAttribute : Attribute
    {
        public HideIfAttribute(string condition) => Condition = condition;

        public string Condition { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UxmlNameAttribute : PropertyAttribute
    {
        public UxmlNameAttribute(Type elementType = null, string scope = null)
        {
            ElementType = elementType;
            Scope = scope;
        }

        public Type ElementType { get; }

        public string Scope { get; }
    }
}
