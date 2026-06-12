using System.Collections.Generic;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Shuffled-deck random source: draws without replacement until empty, then reshuffles.
    /// Avoids the runs of repeats that pure random produces.
    /// </summary>
    public sealed class RandomBag<T>
    {
        readonly List<T> _source;
        readonly List<T> _bag = new();

        public RandomBag(IEnumerable<T> items)
        {
            _source = new List<T>(items);
        }

        public bool IsEmpty => _source.Count == 0;
        public int Remaining => _bag.Count;

        public T Draw()
        {
            if (IsEmpty) return default;
            if (_bag.Count == 0) Refill();
            int idx = Random.Range(0, _bag.Count);
            var item = _bag[idx];
            _bag.RemoveAt(idx);
            return item;
        }

        public void Refill()
        {
            _bag.Clear();
            _bag.AddRange(_source);
        }

        public void Reset() => _bag.Clear();
    }
}
