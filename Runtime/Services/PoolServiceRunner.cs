using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="PoolServiceSO"/>. Provides a parent transform for
    /// pooled instances so the scene hierarchy stays tidy and idle objects survive
    /// scene loads if this runner is marked DontDestroyOnLoad.
    /// </summary>
    public sealed class PoolServiceRunner : MonoBehaviour
    {
        public PoolServiceSO Service;

        void OnEnable()
        {
            if (Service == null) return;
            Service.ResetState();   // drop pools left over from a previous play session
            Service.SetRoot(transform);
        }

        void OnDisable()
        {
            if (Service != null) Service.ClearRoot(transform);
        }
    }
}
