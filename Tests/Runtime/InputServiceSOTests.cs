using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Tests
{
    public class InputServiceSOTests
    {
        InputServiceSO input;
        InputActionAsset actions;

        [SetUp]
        public void SetUp()
        {
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.AddActionMap("UI").AddAction("Cancel");
            actions.AddActionMap("Gameplay").AddAction("Move");

            input = ScriptableObject.CreateInstance<InputServiceSO>();
            input.Actions = actions;
            input.ManageCursor = false;
        }

        [TearDown]
        public void TearDown()
        {
            actions.Disable();
            Object.DestroyImmediate(input);
            Object.DestroyImmediate(actions);
        }

        [Test]
        public void SwitchToGameplay_EnablesGameplayAndDisablesUI()
        {
            input.SwitchToUI();

            input.SwitchToGameplay();

            Assert.IsTrue(actions.FindActionMap("Gameplay").enabled);
            Assert.IsFalse(actions.FindActionMap("UI").enabled);
            Assert.IsTrue(input.InGameplay);
        }

        [Test]
        public void SwitchToUI_EnablesUIAndDisablesGameplay()
        {
            input.SwitchToGameplay();

            input.SwitchToUI();

            Assert.IsTrue(actions.FindActionMap("UI").enabled);
            Assert.IsFalse(actions.FindActionMap("Gameplay").enabled);
            Assert.IsFalse(input.InGameplay);
        }

        [Test]
        public void SwitchToUI_SilencesEveryGameplayMap()
        {
            actions.AddActionMap("Gameplay1").AddAction("Move");
            actions.AddActionMap("Gameplay2").AddAction("Move");

            input.SwitchToGameplay();
            Assert.IsTrue(actions.FindActionMap("Gameplay1").enabled && actions.FindActionMap("Gameplay2").enabled);

            input.SwitchToUI();
            Assert.IsFalse(actions.FindActionMap("Gameplay1").enabled || actions.FindActionMap("Gameplay2").enabled);
        }

        [Test]
        public void UnknownUIMapNameThrows()
        {
            input.UIMapName = "Missing";

            Assert.Throws<ArgumentException>(() => input.SwitchToGameplay());
        }
    }
}
