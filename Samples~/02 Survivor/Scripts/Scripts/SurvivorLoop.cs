using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// The survivor loop's glue — the "graduate to a small script" example. Listens for enemy
    /// deaths (sample-local Ripple event the Enemy prefab broadcasts), awards score, and ends
    /// the run when the arena's GameTimer completes (wired via the timer's Completed UltEvent —
    /// look at the SurvivorArena prefab to see the whole loop in the inspector).
    /// </summary>
    public sealed class SurvivorLoop : MonoBehaviour
    {
        [Required, Tooltip("Auto-assigned at sample setup.")]
        public SceneServiceSO SceneService;
        [Tooltip("Sample-local event the Enemy prefab broadcasts on death.")]
        public VoidEventSO EnemyDied;
        [Tooltip("OPTIONAL: drag Assets/_Project/Variables/Score here so kills feed the project score and the GameOver screen.")]
        public FloatVariableSO Score;
        [Min(0f)] public float ScorePerKill = 10f;
        public string GameOverScene = "GameOver";

        void OnEnable() { if (EnemyDied != null) EnemyDied.AddListener(OnEnemyDied); }
        void OnDisable() { if (EnemyDied != null) EnemyDied.RemoveListener(OnEnemyDied); }

        void OnEnemyDied()
        {
            if (Score != null) Score.Add(ScorePerKill);
        }

        /// <summary>Wired from the arena GameTimer's Completed event.</summary>
        public void OnTimerDone() => SceneService?.LoadAsync(GameOverScene);
    }
}
