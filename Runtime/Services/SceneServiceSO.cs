using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Defines async scene-load operations. <see cref="SceneServiceRunner"/> drives the
    /// fade overlay + load coroutine. Optional Ripple <see cref="VoidEventSO"/>s announce
    /// load start/finish for designer wiring (audio cues, analytics, achievements).
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Scene Service", fileName = "SceneService")]
    public sealed class SceneServiceSO : ServiceSO<SceneServiceRunner>
    {
        [Header("Defaults")]
        [Min(0f)] public float DefaultFadeSeconds = 0.35f;
        public Color DefaultFadeColor = Color.black;

        [Header("Events")]
        [Tooltip("Optional VoidEventSO raised when a scene load begins.")]
        public VoidEventSO OnSceneLoadStarted;
        [Tooltip("Optional VoidEventSO raised when a scene load finishes.")]
        public VoidEventSO OnSceneLoadCompleted;

        bool _warnedNoRunner;

        // Reset the once-only warning each play session so a missing runner is flagged every run,
        // not just the first after a domain reload.
        public override void ResetState() => _warnedNoRunner = false;

        // Takes a scene-name string; a SceneRef converts implicitly, so both call sites work.
        public Coroutine LoadAsync(string sceneName)
            => Runner != null ? Runner.Load(sceneName, DefaultFadeSeconds, DefaultFadeColor) : WarnNoRunner();

        public Coroutine LoadAsync(string sceneName, float fadeSeconds, Color fadeColor)
            => Runner != null ? Runner.Load(sceneName, fadeSeconds, fadeColor) : WarnNoRunner();

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void ReloadCurrentScene() => ReloadCurrent();

        public Coroutine ReloadCurrent()
            => Runner != null ? Runner.ReloadCurrent(DefaultFadeSeconds, DefaultFadeColor) : WarnNoRunner();
        public Coroutine ReloadCurrent(float fadeSeconds, Color fadeColor)
            => Runner != null ? Runner.ReloadCurrent(fadeSeconds, fadeColor) : WarnNoRunner();

        // A load with no runner registered is a silent no-op otherwise — the button appears to do
        // nothing. Warn once, naming the fix.
        Coroutine WarnNoRunner()
        {
            if (!_warnedNoRunner)
            {
                _warnedNoRunner = true;
                Debug.LogWarning("[JamKit] SceneService has no runner — scene loads do nothing. " +
                                 "Add JamKitCore (or a SceneServiceRunner) to the scene.", this);
            }
            return null;
        }

        internal void RaiseStarted() => OnSceneLoadStarted?.Invoke();
        internal void RaiseCompleted() => OnSceneLoadCompleted?.Invoke();
    }
}
