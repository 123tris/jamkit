using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    public sealed class SurvivorLoop : MonoBehaviour
    {
        [Required] public SceneServiceSO SceneService;
        [Required] public VoidEventSO EnemyDied;
        [Required] public FloatVariableSO Score;
        [Min(0f)] public float ScorePerKill = 10f;
        public SceneRef GameOverScene = new("GameOver");

        void OnEnable() => EnemyDied.AddListener(OnEnemyDied);

        void OnDisable() => EnemyDied.RemoveListener(OnEnemyDied);

        void OnEnemyDied() => Score.Add(ScorePerKill);

        public void OnTimerDone() => SceneService.Load(GameOverScene);
    }
}
