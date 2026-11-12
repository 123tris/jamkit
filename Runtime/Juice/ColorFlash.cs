using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class ColorFlash : MonoBehaviour
    {
        [Required] public SpriteRenderer Target;
        public Color Color = new(1f, 0.3f, 0.3f);
        [Min(0.01f)] public float Seconds = 0.15f;
        public bool Unscaled;

        Color rest;

        void Reset() => Target = GetComponentInChildren<SpriteRenderer>();

        void Awake() => rest = Target.color;

        void OnDisable() => Target.color = rest;

        [Button, DisableInEditorMode]
        public void Play()
        {
            StopAllCoroutines();
            StartCoroutine(Routine());
        }

        IEnumerator Routine()
        {
            for (float t = 0f; t < Seconds; t += Unscaled ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                Target.color = Color.Lerp(Color, rest, t / Seconds);
                yield return null;
            }
            Target.color = rest;
        }
    }
}
