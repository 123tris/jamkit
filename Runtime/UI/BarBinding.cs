using Ripple;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Drives the fill of a UI Toolkit bar from a Ripple <see cref="FloatVariableSO"/> — bind an
    /// HP/stamina/charge bar with no code. Point at a HUD UIDocument and name the <b>fill</b> element
    /// (a child whose width/height should track the value); this sets that element's size in percent.
    /// Normalizes against <see cref="MaxValue"/>, or <see cref="MaxVariable"/> if assigned
    /// (e.g. mirror Health.Max so the bar scales when max HP changes).
    /// </summary>
    public sealed class BarBinding : UIBindingBase
    {
        [Tooltip("Name of the fill VisualElement whose size tracks the value.")]
        public string FillElementName;
        public FloatVariableSO Variable;

        [Header("Range")]
        public float MinValue = 0f;
        public float MaxValue = 1f;
        [Tooltip("Optional — take the max from a variable instead of MaxValue.")]
        public FloatVariableSO MaxVariable;
        [Tooltip("Fill grows vertically (height) instead of horizontally (width).")]
        public bool Vertical = false;

        VisualElement _fill;

        protected override void Subscribe()
        {
            if (Variable != null) Variable.OnValueChanged += OnChanged;
            if (MaxVariable != null) MaxVariable.OnValueChanged += OnChanged;
        }

        protected override void Unsubscribe()
        {
            if (Variable != null) Variable.OnValueChanged -= OnChanged;
            if (MaxVariable != null) MaxVariable.OnValueChanged -= OnChanged;
        }

        void OnChanged(float _) => Apply();

        protected override void Resolve()
        {
            if (Root == null || string.IsNullOrEmpty(FillElementName)) return;
            _fill = Root.Q<VisualElement>(FillElementName);
            if (_fill == null) WarnUnresolved(FillElementName);
        }

        protected override void Apply()
        {
            if (_fill == null) Resolve();
            if (_fill == null) return;

            float max = MaxVariable != null ? MaxVariable.CurrentValue : MaxValue;
            float denom = max - MinValue;
            float value = Variable != null ? Variable.CurrentValue : 0f;
            float n = denom <= 0f ? 0f : Mathf.Clamp01((value - MinValue) / denom);

            var pct = Length.Percent(n * 100f);
            if (Vertical) _fill.style.height = pct;
            else _fill.style.width = pct;
        }
    }
}
