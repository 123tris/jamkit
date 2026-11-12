using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    public sealed class LabelBinding : UIBindingBase
    {
        [Tooltip("Name of the Label element in the document.")]
        public string ElementName;

        [Required] public FloatVariableSO Variable;

        [Tooltip("{0} is the value.")]
        public string Format = "{0:0}";

        Label label;

        protected override void Bind(VisualElement root)
        {
            label = root.Q<Label>(ElementName);
            Variable.AddListener(Apply);
            Apply(Variable.CurrentValue);
        }

        void OnDisable() => Variable.RemoveListener(Apply);

        void Apply(float value) => label.text = string.Format(Format, value);
    }
}
