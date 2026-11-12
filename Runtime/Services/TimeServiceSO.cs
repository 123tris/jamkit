using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class TimeModifier
    {
        readonly TimeServiceSO service;
        float scale;

        internal TimeModifier(TimeServiceSO service, float scale)
        {
            this.service = service;
            this.scale = scale;
        }

        public float Scale
        {
            get => scale;
            set
            {
                scale = value;
                service.Apply();
            }
        }

        public void Release() => service.Release(this);
    }

    [CreateAssetMenu(menuName = "JamKit/Services/Time Service", fileName = "TimeService")]
    public sealed class TimeServiceSO : ServiceSO
    {
        readonly List<TimeModifier> modifiers = new();
        TimeModifier pause;
        float baseScale = 1f;

        public float BaseScale
        {
            get => baseScale;
            set
            {
                baseScale = value;
                Apply();
            }
        }

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public float EffectiveScale
        {
            get
            {
                float scale = baseScale;
                foreach (TimeModifier modifier in modifiers)
                    scale = Mathf.Min(scale, baseScale * modifier.Scale);
                return Mathf.Max(0f, scale);
            }
        }

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public int ActiveModifiers => modifiers.Count;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public bool IsPaused => pause != null;

        public TimeModifier Push(float scale)
        {
            var modifier = new TimeModifier(this, scale);
            modifiers.Add(modifier);
            Apply();
            return modifier;
        }

        public TimeModifier PushSmooth(float scale, float seconds, float from = 1f)
        {
            TimeModifier modifier = Push(from);
            JamKitHost.Run(EaseTo(modifier, scale, seconds));
            return modifier;
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Pause() => pause ??= Push(0f);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Resume()
        {
            pause?.Release();
            pause = null;
        }

        public void FreezeForSeconds(float seconds) => FreezeAsync(seconds);

        public Awaitable FreezeAsync(float seconds, float scale = 0f) => JamKitHost.Run(Freeze(seconds, scale));

        public override void ResetState()
        {
            modifiers.Clear();
            pause = null;
            Apply();
        }

        protected override void OnDisable()
        {
            ResetState();
            base.OnDisable();
        }

        internal void Release(TimeModifier modifier)
        {
            modifiers.Remove(modifier);
            Apply();
        }

        internal void Apply()
        {
            if (Application.isPlaying) Time.timeScale = EffectiveScale;
        }

        IEnumerator Freeze(float seconds, float scale)
        {
            TimeModifier modifier = Push(scale);
            yield return new WaitForSecondsRealtime(seconds);
            modifier.Release();
        }

        static IEnumerator EaseTo(TimeModifier modifier, float scale, float seconds)
        {
            float from = modifier.Scale;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                modifier.Scale = Mathf.Lerp(from, scale, t / seconds);
                yield return null;
            }
            modifier.Scale = scale;
        }
    }
}
