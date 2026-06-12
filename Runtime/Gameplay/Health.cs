using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Bare-bones hit-point tracker. Optional Ripple variables / events let HUDs and feedbacks
    /// react without holding a reference to this component:
    ///   - <see cref="CurrentVariable"/> mirrors current HP, ideal for a binding target.
    ///   - <see cref="OnDamaged"/> fires with the damage amount.
    ///   - <see cref="OnHealed"/> fires with the heal amount.
    ///   - <see cref="OnDied"/> fires when HP reaches zero.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [Header("Stats")]
        [Min(0)] public float Max = 10f;
        [Min(0)] public float Current = 10f;
        public bool DestroyOnDeath = false;

        [Header("Broadcast (Ripple)")]
        public FloatVariableSO CurrentVariable;
        public FloatEvent OnDamaged;
        public FloatEvent OnHealed;
        public VoidEventSO OnDied;

        public bool IsDead => Current <= 0f;
        public float Ratio01 => Max <= 0f ? 0f : Mathf.Clamp01(Current / Max);

        void Awake() => PushCurrent();

        public void Damage(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Max(0f, Current - amount);
            PushCurrent();
            if (OnDamaged != null) OnDamaged.Invoke(amount);
            if (Current <= 0f)
            {
                if (OnDied != null) OnDied.Invoke();
                if (DestroyOnDeath) Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Min(Max, Current + amount);
            PushCurrent();
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
