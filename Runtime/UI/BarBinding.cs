using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    public sealed class BarBinding : UIBindingBase
    {
        [Tooltip("Name of the fill element whose size tracks the value.")]
        public string FillElementName;

        [Required] public FloatVariableSO Variable;
        public float MinValue;
        public FloatReference MaxValue = new(1f);

        [Tooltip("Grow in height instead of width.")]
        public bool Vertical;

        VisualElement fill;

        protected override void Bind(VisualElement root)
        {
            fill = root.Q(FillElementName);
            Variable.AddListener(Apply);
            Apply(Variable.CurrentValue);
        }

        void OnDisable() => Variable.RemoveListener(Apply);

        void Apply(float value)
        {
            Length size = Length.Percent(MathX.Remap01(value, MinValue, MaxValue.Value) * 100f);
            if (Vertical) fill.style.height = size;
            else fill.style.width = size;
        }
    }
}
