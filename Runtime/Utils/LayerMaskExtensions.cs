using UnityEngine;

namespace Metz.JamKit.Utils
{
    public static class LayerMaskExtensions
    {
        public static bool IsInLayerMask(this LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        public static bool IsInLayerMask(this LayerMask mask, GameObject go)
        {
            return (mask.value & (1 << go.layer)) != 0;
        }
    }
}