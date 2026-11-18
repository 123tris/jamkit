using Metz.JamKit.Inspector;
using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [Header("Stats")] public FloatReference Max = new(10f);

        [Min(0)]
        public float Current = 10f;

        [Tooltip("Mirrors current HP for HUD binding. When several objects share this prefab, a LocalVariables listing it gives each one its own copy.")]
        public FloatVariableSO CurrentVariable;

        public bool DestroyOnDeath;

        public Signal<float> OnDamaged = new();
        public Signal<float> OnHealed = new();
        public Signal OnDied = new();

        public float MaxValue => Max.Value;

        public bool IsDead => Current <= 0f;

        public float Ratio01 => MathX.Remap01(Current, 0f, MaxValue);

        void OnEnable() => Publish();

        public void Damage(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Max(0f, Current - amount);
            Publish();
            OnDamaged.Invoke(amount);
            if (IsDead) Die();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Min(MaxValue, Current + amount);
            Publish();
            OnHealed.Invoke(amount);
        }

        [Button]
        public void Kill() => Damage(Current);

        public void SetCurrent(float value)
        {
            bool wasDead = IsDead;
            Current = Mathf.Clamp(value, 0f, MaxValue);
            Publish();
            if (IsDead && !wasDead) Die();
        }

        [Button("Reset")]
        public void ResetFull() => SetCurrent(MaxValue);

        [Button("Damage 1")]
        void DebugDamage() => Damage(1f);

        [Button("Heal 1")]
        void DebugHeal() => Heal(1f);

        void Die()
        {
            OnDied.Invoke();
            if (DestroyOnDeath) Destroy(gameObject);
        }

        void Publish()
        {
            if (CurrentVariable != null) CurrentVariable.For(this).SetCurrentValue(Current, this);
        }
    }
}
