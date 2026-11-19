using UnityEngine;

namespace Metz.JamKit
{
    public static class LayerMaskExtensions
    {
        public static bool Contains(this LayerMask mask, int layer) => (mask.value & (1 << layer)) != 0;
    }
}
