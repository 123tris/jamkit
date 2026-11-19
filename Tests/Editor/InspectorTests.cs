using System;
using System.Collections.Generic;
using System.Linq;
using Metz.JamKit.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Tests.Editor
{
    public class InspectorTests
    {
        static IEnumerable<Type> JamKitTypes()
            => typeof(Health).Assembly.GetTypes()
                .Where(type => type.IsPublic && !type.IsAbstract && !type.IsGenericTypeDefinition)
                .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type) || typeof(ScriptableObject).IsAssignableFrom(type))
                .OrderBy(type => type.Name);

        [TestCaseSource(nameof(JamKitTypes))]
        public void EveryTypeBuildsItsInspector(Type type)
        {
            GameObject host = typeof(MonoBehaviour).IsAssignableFrom(type) ? new GameObject(type.Name) : null;
            Object target = host != null ? Add(host, type) : ScriptableObject.CreateInstance(type);
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(target);
            try
            {
                Assert.IsInstanceOf<JamKitEditor>(editor, $"Register {type.Name} with a one-line JamKitEditor subclass.");
                VisualElement inspector = editor.CreateInspectorGUI();
                Assert.IsNotNull(inspector);
                bool hasDebug = JamKitEditor.Buttons(type).Any() || JamKitEditor.LiveValues(type).Any();
                Assert.AreEqual(hasDebug, inspector.Q<Foldout>(JamKitEditor.DebugName) != null);
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(host != null ? host : target);
            }
        }

        [Test]
        public void RequiredReferencesSkipAFieldItsConditionHides()
        {
            var host = new GameObject("Mover");
            try
            {
                var mover = (Mover2D)Add(host, typeof(Mover2D));
                CollectionAssert.AreEquivalent(new[] { "Move", "Jump" }, RequiredReferences.Missing(mover));

                mover.TopDown = true;
                CollectionAssert.AreEquivalent(new[] { "Move" }, RequiredReferences.Missing(mover));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void RequiredReferencesLookInsideListsOfStructs()
        {
            var flow = ScriptableObject.CreateInstance<GameFlow>();
            try
            {
                flow.Transitions.Add(new GameFlow.Transition());
                CollectionAssert.AreEqual(new[] { "Transitions 1 > On" }, RequiredReferences.Missing(flow));
            }
            finally
            {
                Object.DestroyImmediate(flow);
            }
        }

        static Component Add(GameObject host, Type type)
        {
            foreach (RequireComponent requirement in type.GetCustomAttributes(typeof(RequireComponent), true))
                foreach (Type required in new[] { requirement.m_Type0, requirement.m_Type1, requirement.m_Type2 })
                    if (required != null && required.IsAbstract && host.GetComponent(required) == null)
                        host.AddComponent(required == typeof(Collider2D) ? typeof(BoxCollider2D) : typeof(BoxCollider));
            return host.AddComponent(type);
        }
    }
}
