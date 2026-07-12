using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// ScriptableObject definition of the time service: a stack-based wrapper for
    /// <see cref="Time.timeScale"/> and the ONLY thing allowed to touch it (see PILLARS.md).
    /// Push/pop modifiers compose without stomping — pause + hit-freeze + slow-mo nest properly.
    /// Scene-side <see cref="TimeServiceRunner"/> is required for <see cref="FreezeForSeconds"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Time Service", fileName = "TimeService")]
    public sealed class TimeServiceSO : ServiceSO<TimeServiceRunner>
    {
        readonly Stack<float> _stack = new();
        float _baseScale = 1f;

        public float BaseScale
        {
            get => _baseScale;
            set { _baseScale = value; Apply(); }
        }

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public float CurrentScale => Application.isPlaying ? Time.timeScale : 1f;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public int StackDepth => _stack.Count;

        protected override void OnDisable()
        {
            // Restore timescale when the SO unloads (e.g. domain reload exits play mode).
            _stack.Clear();
            if (Application.isPlaying) Time.timeScale = 1f;
            base.OnDisable();
        }

        public void Push(float scale) { _stack.Push(scale); Apply(); }
        public void Pop() { if (_stack.Count > 0) _stack.Pop(); Apply(); }
        public void Clear() { _stack.Clear(); Apply(); }

        /// <summary>
        /// Drop all pushed modifiers and restore base timescale. Runs each play session and on
        /// every runner (re)enable, so a leftover push (e.g. quitting Play while paused with
        /// Domain Reload disabled) can't leak a frozen timescale into the next session/scene.
        /// </summary>
        public override void ResetState() { _stack.Clear(); Apply(); }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Pause() => Push(0f);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Resume() => Pop();

        /// <summary>Push then auto-pop after <paramref name="seconds"/> real time. Requires a Runner.</summary>
        public Coroutine FreezeForSeconds(float seconds, float scale = 0f)
            => Runner == null ? null : Runner.StartFreeze(seconds, scale);

        void Apply()
        {
            if (!Application.isPlaying) return;
            Time.timeScale = _stack.Count == 0
                ? _baseScale
                : Mathf.Max(0f, _stack.Peek());
        }
    }
}
