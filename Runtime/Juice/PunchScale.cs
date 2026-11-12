using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class PunchScale : MonoBehaviour
    {
        [Tooltip("Scale added at the start of the punch, at strength 1.")]
        public Vector3 Punch = new(0.3f, 0.3f, 0.3f);

        [Min(0.01f)] public float Seconds = 0.3f;
        public bool Unscaled;

        Vector3 rest;

        void Awake() => rest = transform.localScale;

        void OnDisable() => transform.localScale = rest;

        [Button, DisableInEditorMode]
        public void Play() => Play(1f);

        public void Play(float strength)
        {
            StopAllCoroutines();
            StartCoroutine(Routine(strength));
        }

        IEnumerator Routine(float strength)
        {
            for (float t = 0f; t < Seconds; t += Unscaled ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                transform.localScale = rest + Punch * (strength * (1f - Ease.OutElastic.Evaluate(t / Seconds)));
                yield return null;
            }
            transform.localScale = rest;
        }
    }
}
