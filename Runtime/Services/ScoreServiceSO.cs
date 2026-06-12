using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Tracks the current run's score and a persisted high score. No runner needed.
    /// Optional Ripple mirrors let a HUD bind without code:
    ///   - <see cref="ScoreVariable"/> / <see cref="HighScoreVariable"/> mirror the values (bind a LabelBinding/BarBinding).
    ///   - <see cref="OnScoreChanged"/> fires with the new total; <see cref="OnNewHighScore"/> fires when the record breaks.
    /// High score persists through an assigned <see cref="SaveServiceSO"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Score Service", fileName = "ScoreService")]
    public sealed class ScoreServiceSO : ScriptableObject
    {
        [Header("Persistence")]
        [Tooltip("Optional. When set, the high score is read on first access and written whenever it improves.")]
        public SaveServiceSO SaveService;
        public string HighScoreKey = "JamKit.HighScore";

        [Header("Broadcast (Ripple)")]
        [Tooltip("Optional — mirrors the current score for HUD binding.")]
        public FloatVariableSO ScoreVariable;
        [Tooltip("Optional — mirrors the high score for HUD binding.")]
        public FloatVariableSO HighScoreVariable;
        public IntEvent OnScoreChanged;
        public VoidEventSO OnNewHighScore;

        int _score;
        int _high;
        bool _loaded;

        public int Score => _score;
        public int HighScore { get { EnsureLoaded(); return _high; } }

        void OnEnable() { _loaded = false; _score = 0; }

        void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _high = SaveService != null ? SaveService.Read(HighScoreKey, 0) : 0;
            if (HighScoreVariable != null) HighScoreVariable.SetCurrentValue(_high);
        }

        /// <summary>Reset the current score to zero (call at the start of a run). Keeps the high score.</summary>
        public void ResetScore() => Set(0);

        public void Add(int amount) => Set(_score + amount);

        public void Set(int value)
        {
            EnsureLoaded();
            _score = Mathf.Max(0, value);
            if (ScoreVariable != null) ScoreVariable.SetCurrentValue(_score);
            if (OnScoreChanged != null) OnScoreChanged.Invoke(_score);

            if (_score > _high)
            {
                _high = _score;
                if (HighScoreVariable != null) HighScoreVariable.SetCurrentValue(_high);
                if (SaveService != null) SaveService.Write(HighScoreKey, _high);
                if (OnNewHighScore != null) OnNewHighScore.Invoke();
            }
        }
    }
}
