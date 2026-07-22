using Ripple;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Drives a UI Toolkit <see cref="Label"/>'s text from a Ripple <see cref="FloatVariableSO"/>.
    /// Point it at a HUD UIDocument, name the label, and pick a format — no code needed to show
    /// score / HP / timer. Example formats: <c>"Score: {0:0}"</c>, <c>"{0:0.0}s"</c>, <c>"x{0:0}"</c>.
    /// </summary>
    public sealed class LabelBinding : UIBindingBase
    {
        [Tooltip("Name of the Label element in the document.")]
        public string ElementName;
        public FloatVariableSO Variable;
        [Tooltip("Composite format string. {0} is the value.")]
        public string Format = "{0:0}";

        Label _label;

        protected override void Subscribe()   { if (Variable != null) Variable.OnValueChanged += OnChanged; }
        protected override void Unsubscribe()  { if (Variable != null) Variable.OnValueChanged -= OnChanged; }
        void OnChanged(float _) => Apply();

        protected override void Resolve()
        {
            if (Root == null || string.IsNullOrEmpty(ElementName)) return;
            _label = Root.Q<Label>(ElementName);
            if (_label == null) WarnUnresolved(ElementName);
        }

        protected override void Apply()
        {
            if (_label == null) Resolve();
            if (_label == null) return;
            float v = Variable != null ? Variable.CurrentValue : 0f;
            _label.text = string.Format(Format ?? "{0}", v);
        }
    }
}
