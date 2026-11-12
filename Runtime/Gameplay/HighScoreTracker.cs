using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class HighScoreTracker : MonoBehaviour
    {
        [Required] public FloatVariableSO Score;

        [Required, Tooltip("Tick Persist on this variable so the record survives restarts.")]
        public FloatVariableSO HighScore;

        [FoldoutGroup("Events"), Tooltip("Raised the first time the record breaks each run.")]
        public Signal OnNewHighScore = new();

        bool announced;

        void OnEnable()
        {
            announced = false;
            Score.AddListener(OnScoreChanged);
        }

        void OnDisable() => Score.RemoveListener(OnScoreChanged);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void ResetScore()
        {
            Score.CurrentValue = 0f;
            announced = false;
        }

        internal void OnScoreChanged(float score)
        {
            if (score <= HighScore.CurrentValue) return;
            HighScore.CurrentValue = score;
            if (announced) return;
            announced = true;
            OnNewHighScore.Invoke();
        }
    }
}
