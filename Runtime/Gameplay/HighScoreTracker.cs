using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// The only score logic the kit ships: watches a score variable and raises the high-score
    /// variable when the record breaks. Score and HighScore are plain Ripple variables — award
    /// points with <c>ScoreVariable.Add(x)</c> from anything, bind HUDs to the variables, and
    /// tick Persist on HighScore so the record survives restarts. Lives on JamKitCore.
    /// </summary>
    public sealed class HighScoreTracker : MonoBehaviour
    {
        [Required, Tooltip("The run's score. Reset it at run start (e.g. wire a Bootstrap/Play event to ResetScore).")]
        public FloatVariableSO Score;
        [Required, Tooltip("The record. Tick Persist on this variable so it survives restarts.")]
        public FloatVariableSO HighScore;
        [Tooltip("Optional global broadcast, fired the FIRST time the record breaks each run (toast/stinger).")]
        public VoidEventSO OnNewHighScore;

        bool _announced;

        void OnEnable()
        {
            _announced = false;
            if (Score != null) Score.AddListener(OnScoreChanged);
        }

        void OnDisable()
        {
            if (Score != null) Score.RemoveListener(OnScoreChanged);
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void ResetScore()
        {
            if (Score != null) Score.SetCurrentValue(0f);
            _announced = false;
        }

        internal void OnScoreChanged(float value)
        {
            if (HighScore == null || value <= HighScore.CurrentValue) return;
            HighScore.SetCurrentValue(value);
            if (_announced || OnNewHighScore == null) return;
            _announced = true;
            OnNewHighScore.Invoke();
        }
    }
}
