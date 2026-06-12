using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host that advances a <see cref="TimerServiceSO"/> each frame. Drop one in your
    /// scene (typically on JamKitCore) and assign the service. Start/stop the clock from the SO.
    /// </summary>
    public sealed class TimerServiceRunner : MonoBehaviour
    {
        public TimerServiceSO Service;

        void OnEnable()
        {
            if (Service != null) Service.ResetState();   // clear any state left from a previous play session
        }

        void Update()
        {
            if (Service == null) return;
            Service.Tick(Service.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime);
        }
    }
}
