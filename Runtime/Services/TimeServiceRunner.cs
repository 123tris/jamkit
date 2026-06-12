using System.Collections;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="TimeServiceSO.FreezeForSeconds"/>.
    /// You only need this if you call FreezeForSeconds — Push/Pop/Pause/Resume work on the SO alone.
    /// </summary>
    public sealed class TimeServiceRunner : MonoBehaviour
    {
        public TimeServiceSO Service;

        void OnEnable()
        {
            if (Service == null) return;
            Service.ResetState();   // clear any timescale push left over from a previous play session
            Service.RegisterRunner(this);
        }

        void OnDisable() { if (Service != null) Service.UnregisterRunner(this); }

        internal Coroutine StartFreeze(float seconds, float scale)
            => StartCoroutine(FreezeRoutine(seconds, scale));

        IEnumerator FreezeRoutine(float seconds, float scale)
        {
            Service.Push(scale);
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            Service.Pop();
        }
    }
}
