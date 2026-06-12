using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Defines async scene-load operations. <see cref="SceneServiceRunner"/> drives the
    /// fade overlay + load coroutine. Optional Ripple <see cref="VoidEventSO"/>s announce
    /// load start/finish for designer wiring (audio cues, analytics, achievements).
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Scene Service", fileName = "SceneService")]
    public sealed class SceneServiceSO : ScriptableObject
    {
        [Header("Defaults")]
        [Min(0f)] public float DefaultFadeSeconds = 0.35f;
        public Color DefaultFadeColor = Color.black;

        [Header("Events")]
        [Tooltip("Optional VoidEventSO raised when a scene load begins.")]
        public VoidEventSO OnSceneLoadStarted;
        [Tooltip("Optional VoidEventSO raised when a scene load finishes.")]
        public VoidEventSO OnSceneLoadCompleted;

        SceneServiceRunner _runner;

        internal void RegisterRunner(SceneServiceRunner r) => _runner = r;
        internal void UnregisterRunner(SceneServiceRunner r) { if (_runner == r) _runner = null; }
        public bool HasRunner => _runner != null;

        public Coroutine LoadAsync(string sceneName)
            => _runner?.Load(sceneName, DefaultFadeSeconds, DefaultFadeColor);

        public Coroutine LoadAsync(string sceneName, float fadeSeconds, Color fadeColor)
            => _runner?.Load(sceneName, fadeSeconds, fadeColor);

        public Coroutine ReloadCurrent() => _runner?.ReloadCurrent(DefaultFadeSeconds, DefaultFadeColor);
        public Coroutine ReloadCurrent(float fadeSeconds, Color fadeColor) => _runner?.ReloadCurrent(fadeSeconds, fadeColor);

        internal void RaiseStarted() => OnSceneLoadStarted?.Invoke();
        internal void RaiseCompleted() => OnSceneLoadCompleted?.Invoke();
    }
}
