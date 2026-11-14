using Unity.Properties;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [UxmlElement]
    public partial class ValueLabel : VisualElement
    {
        public const string ussClassName = "jk-value-label";
        public const string textUssClassName = ussClassName + "__text";

        readonly Label label = new() { name = "text", pickingMode = PickingMode.Ignore };
        string pattern = "{0:0}";
        float shown;

        public ValueLabel()
        {
            AddToClassList(ussClassName);
            label.AddToClassList(textUssClassName);
            Add(label);
            Refresh();
        }

        [UxmlAttribute]
        public string format
        {
            get => pattern;
            set
            {
                pattern = value;
                Refresh();
            }
        }

        [UxmlAttribute, CreateProperty]
        public float value
        {
            get => shown;
            set
            {
                shown = value;
                Refresh();
            }
        }

        void Refresh() => label.text = string.Format(pattern, shown);
    }
}
