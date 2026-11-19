using System;
using System.Collections;
using UnityEngine;

namespace Metz.JamKit
{
    public static class Tween
    {
        public static Awaitable Run(float seconds, Action<float> step, Ease ease = Ease.OutCubic, bool unscaled = false)
            => Run(JamKitHost.Get<JamKitHost>(), seconds, step, ease, unscaled);

        public static Awaitable Run(UnityEngine.Object owner, float seconds, Action<float> step,
            Ease ease = Ease.OutCubic, bool unscaled = false)
            => JamKitHost.Run(Routine(owner, seconds, step, ease, unscaled));

        public static Awaitable MoveTo(this Transform transform, Vector3 position, float seconds, Ease ease = Ease.OutCubic)
        {
            Vector3 from = transform.position;
            return Run(transform, seconds, t => transform.position = Vector3.LerpUnclamped(from, position, t), ease);
        }

        public static Awaitable ScaleTo(this Transform transform, Vector3 scale, float seconds, Ease ease = Ease.OutCubic)
        {
            Vector3 from = transform.localScale;
            return Run(transform, seconds, t => transform.localScale = Vector3.LerpUnclamped(from, scale, t), ease);
        }

        public static Awaitable FadeTo(this SpriteRenderer sprite, float alpha, float seconds, Ease ease = Ease.Linear)
        {
            float from = sprite.color.a;
            return Run(sprite, seconds, t => sprite.color = sprite.color.WithA(Mathf.Lerp(from, alpha, t)), ease);
        }

        public static Awaitable FadeTo(this CanvasGroup group, float alpha, float seconds, Ease ease = Ease.Linear)
        {
            float from = group.alpha;
            return Run(group, seconds, t => group.alpha = Mathf.Lerp(from, alpha, t), ease);
        }

        static IEnumerator Routine(UnityEngine.Object owner, float seconds, Action<float> step, Ease ease, bool unscaled)
        {
            for (float t = 0f; t < seconds && owner != null; t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                step(ease.Evaluate(t / seconds));
                yield return null;
            }
            if (owner != null) step(1f);
        }
    }
}
