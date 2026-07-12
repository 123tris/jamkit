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

        public Coroutine LoadAsync(string sceneName)
            => Runner?.Load(sceneName, DefaultFadeSeconds, DefaultFadeColor);

        public Coroutine LoadAsync(string sceneName, float fadeSeconds, Color fadeColor)
            => Runner?.Load(sceneName, fadeSeconds, fadeColor);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void ReloadCurrentScene() => ReloadCurrent();

        public Coroutine ReloadCurrent() => Runner?.ReloadCurrent(DefaultFadeSeconds, DefaultFadeColor);
        public Coroutine ReloadCurrent(float fadeSeconds, Color fadeColor) => Runner?.ReloadCurrent(fadeSeconds, fadeColor);

        internal void RaiseStarted() => OnSceneLoadStarted?.Invoke();
        internal void RaiseCompleted() => OnSceneLoadCompleted?.Invoke();
    }
}
