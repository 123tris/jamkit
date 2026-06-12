using System;
using System.Collections.Generic;

namespace Metz.JamKit
{
    /// <summary>
    /// Generic finite state machine keyed by an enum. Register OnEnter / OnUpdate / OnExit per state.
    /// </summary>
    public sealed class FSM<TState> where TState : struct, Enum
    {
        struct StateData
        {
            public Action OnEnter;
            public Action OnUpdate;
            public Action OnExit;
        }

        readonly Dictionary<TState, StateData> _states = new();
        TState _current;
        bool _hasState;

        public TState Current => _current;
        public bool HasState => _hasState;

        public event Action<TState, TState> Transitioned;

        public FSM<TState> On(TState state, Action onEnter = null, Action onUpdate = null, Action onExit = null)
        {
            _states[state] = new StateData { OnEnter = onEnter, OnUpdate = onUpdate, OnExit = onExit };
            return this;
        }

        public void Change(TState next)
        {
            if (_hasState && EqualityComparer<TState>.Default.Equals(_current, next)) return;

            TState prev = _current;
            if (_hasState && _states.TryGetValue(_current, out var oldData))
                oldData.OnExit?.Invoke();

            _current = next;
            _hasState = true;

            if (_states.TryGetValue(next, out var newData))
                newData.OnEnter?.Invoke();

            Transitioned?.Invoke(prev, next);
        }

        public void Tick()
        {
            if (_hasState && _states.TryGetValue(_current, out var data))
                data.OnUpdate?.Invoke();
        }

        public bool Is(TState state) => _hasState && EqualityComparer<TState>.Default.Equals(_current, state);
    }
}
