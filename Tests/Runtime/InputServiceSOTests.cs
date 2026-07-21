using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit.Tests
{
    /// <summary>
    /// Characterization tests for the input service's cache + map switching, using a
    /// programmatically-built InputActionAsset (no devices, no bindings needed).
    /// </summary>
    public class InputServiceSOTests
    {
        InputServiceSO _svc;
        InputActionAsset _asset;

        static InputActionAsset MakeAsset(string uiName = "UI", string gameplayName = "Gameplay")
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var ui = asset.AddActionMap(uiName);
            ui.AddAction("Submit");
            ui.AddAction("Cancel");
            ui.AddAction("Navigate");
            var gameplay = asset.AddActionMap(gameplayName);
            gameplay.AddAction("Move");
            gameplay.AddAction("Look");
            gameplay.AddAction("Jump");
            gameplay.AddAction("Attack");
            gameplay.AddAction("Interact");
            gameplay.AddAction("Pause");
            return asset;
        }

        [SetUp]
        public void SetUp()
        {
            _svc = ScriptableObject.CreateInstance<InputServiceSO>();
            _asset = MakeAsset();
            _svc.Actions = _asset;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_svc); // OnDisable disables the current map
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void GettersResolveMapsAndActions()
        {
            Assert.IsNotNull(_svc.UI);
            Assert.IsNotNull(_svc.Gameplay);
            Assert.AreEqual("Move", _svc.Move.name);
            Assert.AreEqual("Pause", _svc.Pause.name);
            Assert.AreEqual("Submit", _svc.UI_Submit.name);
            Assert.AreEqual("Navigate", _svc.UI_Navigate.name);
        }

        [Test]
        public void CacheInvalidatesWhenAssetSwapped()
        {
            var before = _svc.Gameplay;
            var replacement = MakeAsset();
            _svc.Actions = replacement;

            Assert.AreNotSame(before, _svc.Gameplay);
            Object.DestroyImmediate(replacement);
        }

        [Test]
        public void CacheInvalidatesWhenMapNameChanged()
        {
            Assert.IsNotNull(_svc.Gameplay);
            _svc.GameplayMapName = "DoesNotExist";
            Assert.IsNull(_svc.Gameplay);
            Assert.IsNull(_svc.Move);
        }

        [Test]
        public void SwitchToEnablesNextAndDisablesPrevious()
        {
            _svc.SwitchToGameplay();
            Assert.AreSame(_svc.Gameplay, _svc.CurrentMap);
            Assert.IsTrue(_svc.Gameplay.enabled);

            _svc.SwitchToUI();
            Assert.AreSame(_svc.UI, _svc.CurrentMap);
            Assert.IsTrue(_svc.UI.enabled);
            Assert.IsFalse(_svc.Gameplay.enabled);
        }

        [Test]
        public void SwitchToNullIsIgnored()
        {
            _svc.SwitchToGameplay();
            _svc.SwitchTo(null);
            Assert.AreSame(_svc.Gameplay, _svc.CurrentMap);
            Assert.IsTrue(_svc.Gameplay.enabled);
        }

        [Test]
        public void ResetStateClearsCurrentMap()
        {
            _svc.SwitchToGameplay();
            _svc.ResetState();
            Assert.IsNull(_svc.CurrentMap);
        }

        // Current behavior, changes in 0.10 (see plan): the AutoEnableGameplay flag makes a
        // property GETTER enable an input map as a side effect. The flag is being removed;
        // this test is deleted with it.
        [Test]
        public void AutoEnableGameplayEnablesMapOnFirstGetterAccess_CurrentBehavior()
        {
            _svc.AutoEnableGameplay = true;
            _ = _svc.Move; // getter triggers EnsureCache -> SwitchTo(gameplay)
            Assert.AreSame(_svc.Gameplay, _svc.CurrentMap);
            Assert.IsTrue(_svc.Gameplay.enabled);
        }
    }
}
