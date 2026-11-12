using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class GameTimer : MonoBehaviour
    {
        public enum Mode
        {
            CountDown,
            CountUp,
        }

        public Mode CountMode = Mode.CountDown;

        [Tooltip("Countdown start, or count-up cap. 0 is no cap.")]
        public FloatReference Duration = new(60f);

        public bool Unscaled;
        public bool AutoStart = true;

        [Tooltip("Mirrors the current time for HUD binding.")]
        public FloatVariableSO TimeVariable;

        [FoldoutGroup("Events")] public Signal OnCompleted = new();

        public float CurrentTime { get; private set; }

        public bool IsRunning { get; private set; }

        [ShowInInspector, ReadOnly, ProgressBar(0f, 1f), FoldoutGroup("Debug")]
        public float Progress01
        {
            get
            {
                float progress = MathX.Remap01(CurrentTime, 0f, Duration.Value);
                return CountMode == Mode.CountDown ? 1f - progress : progress;
            }
        }

        void OnEnable()
        {
            ResetTimer();
            IsRunning = AutoStart;
        }

        void Update() => Tick(Unscaled ? Time.unscaledDeltaTime : Time.deltaTime);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void StartTimer()
        {
            ResetTimer();
            IsRunning = true;
        }

        public void StartTimer(float duration)
        {
            Duration = new FloatReference(duration);
            StartTimer();
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Pause() => IsRunning = false;

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Resume() => IsRunning = true;

        public void ResetTimer()
        {
            IsRunning = false;
            SetTime(CountMode == Mode.CountDown ? Duration.Value : 0f);
        }

        internal void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            if (CountMode == Mode.CountDown)
            {
                SetTime(Mathf.Max(0f, CurrentTime - deltaTime));
                if (CurrentTime <= 0f) Complete();
                return;
            }

            bool capped = Duration.Value > 0f;
            SetTime(capped ? Mathf.Min(Duration.Value, CurrentTime + deltaTime) : CurrentTime + deltaTime);
            if (capped && CurrentTime >= Duration.Value) Complete();
        }

        void Complete()
        {
            IsRunning = false;
            OnCompleted.Invoke();
        }

        void SetTime(float time)
        {
            CurrentTime = time;
            if (TimeVariable != null) TimeVariable.CurrentValue = time;
        }
    }
}
