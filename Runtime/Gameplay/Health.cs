using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [BoxGroup("Stats")] public FloatReference Max = new(10f);

        [BoxGroup("Stats"), Min(0), ProgressBar(0f, "$MaxValue")]
        public float Current = 10f;

        [BoxGroup("Stats"), Tooltip("Mirrors current HP for HUD binding. Leave empty when several objects share this prefab.")]
        public FloatVariableSO CurrentVariable;

        public bool DestroyOnDeath;

        [FoldoutGroup("Events")] public Signal<float> OnDamaged = new();
        [FoldoutGroup("Events")] public Signal<float> OnHealed = new();
        [FoldoutGroup("Events")] public Signal OnDied = new();

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

        [Button, DisableInEditorMode, FoldoutGroup("Debug"), HorizontalGroup("Debug/Row")]
        public void Kill() => Damage(Current);

        public void SetCurrent(float value)
        {
            bool wasDead = IsDead;
            Current = Mathf.Clamp(value, 0f, MaxValue);
            Publish();
            if (IsDead && !wasDead) Die();
        }

        [Button("Reset"), DisableInEditorMode, HorizontalGroup("Debug/Row")]
        public void ResetFull() => SetCurrent(MaxValue);

        [Button("Damage 1"), DisableInEditorMode, HorizontalGroup("Debug/Row")]
        void DebugDamage() => Damage(1f);

        [Button("Heal 1"), DisableInEditorMode, HorizontalGroup("Debug/Row")]
        void DebugHeal() => Heal(1f);

        void Die()
        {
            OnDied.Invoke();
            if (DestroyOnDeath) Destroy(gameObject);
        }

        void Publish()
        {
            if (CurrentVariable != null) CurrentVariable.SetCurrentValue(Current, this);
        }
    }
}
