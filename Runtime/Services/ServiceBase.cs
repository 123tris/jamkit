using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Base for every JamKit service asset. A service exists only where behavior must wrap
    /// engine/native machinery (scene loads, the timescale stack, input maps, pooling, file IO,
    /// FMOD instances) — plain data belongs in Ripple variables instead (see PILLARS.md).
    /// Owns the play-session reset contract: <see cref="ResetState"/> runs right before the
    /// editor enters play mode (ExitingEditMode), which is correct with Domain Reload disabled.
    /// Builds get a fresh process, so no hook is needed there.
    /// </summary>
    public abstract class ServiceSO : ScriptableObject
    {
        /// <summary>Drop all play-session state so nothing leaks between sessions.</summary>
        public virtual void ResetState() { }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
        }

#if UNITY_EDITOR
        void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode) ResetState();
        }
#endif
    }

    /// <summary>
    /// Base for services that need a scene-side host. With no runner registered every
    /// routed method is a null-safe no-op (tests, editor preview, menu-only scenes).
    /// </summary>
    public abstract class ServiceSO<TRunner> : ServiceSO where TRunner : class
    {
        protected TRunner Runner { get; private set; }

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug"), PropertyOrder(1000)]
        public bool HasRunner => Runner != null;

        public void RegisterRunner(TRunner runner)
        {
            Runner = runner;
            OnRunnerRegistered();
        }

        public void UnregisterRunner(TRunner runner)
        {
            if (!ReferenceEquals(Runner, runner)) return;
            Runner = null;
            OnRunnerUnregistered();
        }

        /// <summary>
        /// Called when a runner (re)enables — i.e. at every scene load that contains one.
        /// Defaults to <see cref="ServiceSO.ResetState"/>: a leaked timescale push or stale pool
        /// is a worse jam-day bug than a restarted service. Override empty to keep state across
        /// scene loads (the FMOD music instance does).
        /// </summary>
        protected virtual void OnRunnerRegistered() => ResetState();

        protected virtual void OnRunnerUnregistered() { }
    }

    /// <summary>
    /// Scene-side host for a <see cref="ServiceSO{TRunner}"/>. Registers itself on enable so the
    /// asset's methods have a live MonoBehaviour to work through; per-scene and disposable — the
    /// service asset is the persistent half. CRTP keeps registration type-safe:
    /// <c>class AudioServiceRunner : ServiceRunner&lt;AudioServiceSO, AudioServiceRunner&gt;</c>.
    /// </summary>
    public abstract class ServiceRunner<TService, TRunner> : MonoBehaviour
        where TService : ServiceSO<TRunner>
        where TRunner : ServiceRunner<TService, TRunner>
    {
        [Required, Tooltip("The service asset this runner hosts. Auto-assigned when exactly one exists.")]
        public TService Service;

        protected bool IsRegistered { get; private set; }

        protected virtual void OnEnable()
        {
            if (Service == null)
            {
                Debug.LogWarning($"[JamKit] {name}: {GetType().Name} has no Service assigned.", this);
                return;
            }
            Service.RegisterRunner((TRunner)this);
            IsRegistered = true;
        }

        protected virtual void OnDisable()
        {
            if (Service != null) Service.UnregisterRunner((TRunner)this);
            IsRegistered = false;
        }
    }
}
