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
    ///   - Optional global Ripple assets: <see cref="Max"/> is a constant-or-variable
    ///     <see cref="FloatReference"/> (point it at a shared asset so an upgrade/difficulty knob
    ///     raises max HP everywhere at once); <see cref="CurrentVariable"/> is a *two-way* mirror of
    ///     current HP — bind a HUD to it, and any external write (checkpoint, cheat, debug panel)
    ///     drives this Health back; Broadcast* fan damage/death out globally.
    /// Pool-aware: respawning through a <see cref="GameObjectPool"/> refills HP via <see cref="IPoolable"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IPoolable
    {
        [BoxGroup("Stats")]
        [Tooltip("Max HP. A plain constant, or point at a shared Ripple variable so upgrades/difficulty raise it everywhere at once.")]
        public FloatReference Max = new(10f);
        [BoxGroup("Stats"), Min(0), ProgressBar(0f, "$MaxValue")]
        [Tooltip("Live HP for THIS instance. Change it through Damage/Heal/SetCurrent (or the CurrentVariable link), not by writing the field.")]
        public float Current = 10f;

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
        [Tooltip("Optional — TWO-WAY mirror of current HP. Bind a HUD (BarBinding/LabelBinding), and any external write (checkpoint, cheat, debug panel) drives this Health back. Leave null for pooled/multiple instances so each keeps its own HP — a shared asset would pool their health together.")]
        public FloatVariableSO CurrentVariable;
        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — fires for damage to ANY Health sharing this event (global shake, vignette).")]
        public FloatEvent BroadcastDamaged;
        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — fires for ANY death sharing this event (kill counters, wave logic).")]
        public VoidEventSO BroadcastDied;

        // Guards the two-way CurrentVariable link so our own writes don't echo back as "external edits".
        bool _syncing;

        /// <summary>Resolved max HP (the <see cref="Max"/> reference's current value).</summary>
        public float MaxValue => Max != null ? Max.Value : 0f;
        public bool IsDead => Current <= 0f;
        public float Ratio01 => MaxValue <= 0f ? 0f : Mathf.Clamp01(Current / MaxValue);

        void OnEnable()
        {
            if (CurrentVariable != null) CurrentVariable.OnValueChanged += OnCurrentVariableChanged;
            PushCurrent();
        }

        void OnDisable()
        {
            if (CurrentVariable != null) CurrentVariable.OnValueChanged -= OnCurrentVariableChanged;
        }

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
            if (Current <= 0f) HandleDeath();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Min(MaxValue, Current + amount);
            PushCurrent();
            OnHealed?.Invoke(amount);
        }

        public void Kill() => Damage(Current);

        /// <summary>Set current HP directly (checkpoints, cheats, revives). Clamps to [0, Max]; dying this way still fires OnDied.</summary>
        public void SetCurrent(float value)
        {
            bool wasDead = IsDead;
            Current = Mathf.Clamp(value, 0f, MaxValue);
            PushCurrent();
            if (Current <= 0f && !wasDead) HandleDeath();
        }

        public void ResetFull()
        {
            Current = MaxValue;
            PushCurrent();
        }

        void HandleDeath()
        {
            OnDied?.Invoke();
            if (BroadcastDied != null) BroadcastDied.Invoke();
            if (DespawnPool != null) DespawnPool.Despawn(gameObject);
            else if (DestroyOnDeath) Destroy(gameObject);
        }

        // Mirror Current out to the shared variable (guarded so the echo isn't read back as an edit).
        void PushCurrent()
        {
            if (CurrentVariable == null) return;
            _syncing = true;
            CurrentVariable.SetCurrentValue(Current, this);
            _syncing = false;
        }

        // External write to the shared variable (checkpoint, cheat, debug panel, another system) → adopt it.
        // Internal so tests can drive the two-way link without a play-mode OnValueChanged round-trip.
        internal void OnCurrentVariableChanged(float value)
        {
            if (_syncing) return;
            bool wasDead = IsDead;
            Current = Mathf.Clamp(value, 0f, MaxValue);
            // Out-of-range external value → reflect the clamp back so the variable and HP agree.
            if (!Mathf.Approximately(Current, value)) PushCurrent();
            if (Current <= 0f && !wasDead) HandleDeath();
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
