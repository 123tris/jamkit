using System.Collections.Generic;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// ScriptableObject definition of the time service: a stack-based wrapper for
    /// <see cref="Time.timeScale"/>. Push/pop modifiers compose without stomping —
    /// pause + hit-freeze + slow-mo nest properly.
    /// Scene-side <see cref="TimeServiceRunner"/> is required for <see cref="FreezeForSeconds"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Time Service", fileName = "TimeService")]
    public sealed class TimeServiceSO : ScriptableObject
    {
        readonly Stack<float> _stack = new();
        float _baseScale = 1f;
        TimeServiceRunner _runner;

        public float BaseScale
        {
            get => _baseScale;
            set { _baseScale = value; Apply(); }
        }

        internal void RegisterRunner(TimeServiceRunner r) => _runner = r;
        internal void UnregisterRunner(TimeServiceRunner r) { if (_runner == r) _runner = null; }

        void OnDisable()
        {
            // Restore timescale when the SO unloads (e.g. domain reload exits play mode).
            _stack.Clear();
            if (Application.isPlaying) Time.timeScale = 1f;
        }

        public void Push(float scale) { _stack.Push(scale); Apply(); }
        public void Pop() { if (_stack.Count > 0) _stack.Pop(); Apply(); }
        public void Clear() { _stack.Clear(); Apply(); }

        /// <summary>
        /// Drop all pushed modifiers and restore base timescale. The runner calls this each play
        /// session so a leftover push (e.g. quitting Play while paused with Domain Reload disabled)
        /// can't leak a frozen timescale into the next session.
        /// </summary>
        public void ResetState() { _stack.Clear(); Apply(); }

        public void Pause() => Push(0f);
        public void Resume() => Pop();

        /// <summary>Push then auto-pop after <paramref name="seconds"/> real time. Requires a Runner.</summary>
        public Coroutine FreezeForSeconds(float seconds, float scale = 0f)
            => _runner == null ? null : _runner.StartFreeze(seconds, scale);

        void Apply()
        {
            if (!Application.isPlaying) return;
            Time.timeScale = _stack.Count == 0
                ? _baseScale
                : Mathf.Max(0f, _stack.Peek());
        }
    }
}
