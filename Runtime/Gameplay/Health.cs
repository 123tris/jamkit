using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Bare-bones hit-point tracker. Two broadcast layers:
    ///   - Per-instance C# events (<see cref="Damaged"/> / <see cref="Healed"/> / <see cref="Died"/>) —
    ///     juice components on the same object subscribe automatically, so only *this* instance reacts.
    ///   - Optional Ripple assets for global reactions (HUD, camera shake, any-enemy-died counters):
    ///     <see cref="CurrentVariable"/> mirrors current HP, <see cref="OnDamaged"/> / <see cref="OnHealed"/>
    ///     fire with the amount, <see cref="OnDied"/> fires when HP reaches zero.
    /// Pool-aware: respawning through a <see cref="GameObjectPool"/> refills HP via <see cref="IPoolable"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IPoolable
    {
        [Header("Stats")]
        [Min(0)] public float Max = 10f;
        [Min(0)] public float Current = 10f;

        [Header("On Death")]
        public bool DestroyOnDeath = false;
        [Tooltip("Return to this pool on death instead of destroying — for pooled enemies/projectiles. Takes precedence over DestroyOnDeath.")]
        public PoolServiceSO DespawnPool;

        [Header("Broadcast (Ripple)")]
        public FloatVariableSO CurrentVariable;
        public FloatEvent OnDamaged;
        public FloatEvent OnHealed;
        public VoidEventSO OnDied;

        /// <summary>Per-instance events (unlike the shared Ripple assets above). Fired with the damage/heal amount.</summary>
        public event System.Action<float> Damaged;
        public event System.Action<float> Healed;
        public event System.Action Died;

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
            Damaged?.Invoke(amount);
            if (OnDamaged != null) OnDamaged.Invoke(amount);
            if (Current <= 0f)
            {
                Died?.Invoke();
                if (OnDied != null) OnDied.Invoke();
                if (DespawnPool != null) DespawnPool.Despawn(gameObject);
                else if (DestroyOnDeath) Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Min(Max, Current + amount);
            PushCurrent();
            Healed?.Invoke(amount);
            if (OnHealed != null) OnHealed.Invoke(amount);
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
    }
}
