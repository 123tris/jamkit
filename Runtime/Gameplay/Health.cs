using Ripple;
using Sirenix.OdinInspector;
using UltEvents;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Bare-bones hit-point tracker — the hub gameplay hangs off. Two broadcast layers:
    ///   - Per-instance UltEvents (<see cref="OnDamaged"/> / <see cref="OnHealed"/> /
    ///     <see cref="OnDied"/>): THE wiring surface. Wire feedbacks (a Feel
    ///     MMF_Player.PlayFeedbacks, HitStop.Play), SpawnBurst.Burst, Respawner, or game logic
    ///     right in the inspector — only *this* instance reacts. Code subscribes to the same
    ///     slots (<c>health.OnDied.DynamicCalls += ...</c>).
    ///   - Optional global Ripple assets (Broadcast*): HUD binding, any-enemy-died counters,
    ///     global shake. Shared by every Health that references the same asset.
    /// Pool-aware: respawning through a <see cref="GameObjectPool"/> refills HP via <see cref="IPoolable"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IPoolable
    {
        [BoxGroup("Stats"), Min(0)] public float Max = 10f;
        [BoxGroup("Stats"), Min(0), ProgressBar(0f, "$Max")] public float Current = 10f;

        [BoxGroup("On Death")] public bool DestroyOnDeath = false;
        [BoxGroup("On Death")]
        [Tooltip("Return to this pool on death instead of destroying — for pooled enemies/projectiles. Takes precedence over DestroyOnDeath.")]
        public PoolServiceSO DespawnPool;

        [FoldoutGroup("Events (this instance)")]
        [Tooltip("This instance took damage (amount). Wire feedbacks here: MMF_Player.PlayFeedbacks(), HitStop.Play(float)...")]
        public UltEvent<float> OnDamaged;
        [FoldoutGroup("Events (this instance)")]
        [Tooltip("This instance was healed (amount).")]
        public UltEvent<float> OnHealed;
        [FoldoutGroup("Events (this instance)")]
        [Tooltip("This instance died. Wire SpawnBurst.Burst(), Respawner.RespawnAfterDelay(), score awards...")]
        public UltEvent OnDied;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — mirrors current HP for HUD binding (BarBinding/LabelBinding).")]
        public FloatVariableSO CurrentVariable;
        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — fires for damage to ANY Health sharing this event (global shake, vignette).")]
        public FloatEvent BroadcastDamaged;
        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — fires for ANY death sharing this event (kill counters, wave logic).")]
        public VoidEventSO BroadcastDied;

        public bool IsDead => Current <= 0f;
        public float Ratio01 => Max <= 0f ? 0f : Mathf.Clamp01(Current / Max);

        void Awake() => PushCurrent();

        // Pooled respawn = fresh HP, otherwise a reused enemy comes back dead.
        public void OnSpawn() => ResetFull();
        public void OnDespawn() { }

        public void Damage(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Max(0f, Current - amount);
            PushCurrent();
            OnDamaged?.Invoke(amount);
            if (BroadcastDamaged != null) BroadcastDamaged.Invoke(amount);
            if (Current <= 0f)
            {
                OnDied?.Invoke();
                if (BroadcastDied != null) BroadcastDied.Invoke();
                if (DespawnPool != null) DespawnPool.Despawn(gameObject);
                else if (DestroyOnDeath) Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Min(Max, Current + amount);
            PushCurrent();
            OnHealed?.Invoke(amount);
        }

        public void Kill() => Damage(Current);

        public void ResetFull()
        {
            Current = Max;
            PushCurrent();
        }

        void PushCurrent()
        {
            if (CurrentVariable != null) CurrentVariable.SetCurrentValue(Current);
        }

        // Debug buttons exercise the REAL wiring — clicking Damage fires the whole chain
        // (feedbacks, broadcasts, death). Replaces the old custom HealthInspector.
        [Button("Damage 1"), DisableInEditorMode, FoldoutGroup("Debug"), HorizontalGroup("Debug/Row")]
        void DebugDamage() => Damage(1f);
        [Button("Heal 1"), DisableInEditorMode, HorizontalGroup("Debug/Row")]
        void DebugHeal() => Heal(1f);
        [Button("Kill"), DisableInEditorMode, HorizontalGroup("Debug/Row")]
        void DebugKill() => Kill();
        [Button("Reset"), DisableInEditorMode, HorizontalGroup("Debug/Row")]
        void DebugReset() => ResetFull();
    }
}
