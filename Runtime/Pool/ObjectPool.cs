using System;
using System.Collections.Generic;

namespace Metz.JamKit
{
    /// <summary>
    /// Generic non-Unity object pool. For pooling GameObjects use <see cref="GameObjectPool"/>.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        readonly Stack<T> _items = new();
        readonly Func<T> _factory;
        readonly Action<T> _onGet;
        readonly Action<T> _onReturn;
        readonly int _maxIdle;

        public int CountIdle => _items.Count;

        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onReturn = null, int prewarm = 0, int maxIdle = 256)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onReturn = onReturn;
            _maxIdle = maxIdle;
            for (int i = 0; i < prewarm; i++) _items.Push(_factory());
        }

        public T Get()
        {
            var item = _items.Count > 0 ? _items.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Return(T item)
        {
            if (item == null) return;
            _onReturn?.Invoke(item);
            if (_items.Count < _maxIdle) _items.Push(item);
        }

        public void Clear() => _items.Clear();
    }
}
