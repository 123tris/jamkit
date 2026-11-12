using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class AutoDespawn : MonoBehaviour
    {
        public FloatReference Seconds = new(3f);
        public bool Unscaled;

        Cooldown lifetime;

        void OnEnable()
        {
            lifetime = new Cooldown(Unscaled);
            lifetime.Start(Seconds.Value);
        }

        void Update()
        {
            if (lifetime.Ready) Destroy(gameObject);
        }
    }
}
