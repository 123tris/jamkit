using Unity.Properties;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [UxmlElement]
    public partial class ValueLabel : Label
    {
        public new const string ussClassName = "jk-value-label";

        string pattern = "{0:0}";
        float shown;

        public ValueLabel()
        {
            AddToClassList(ussClassName);
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

        void Refresh() => text = string.Format(pattern, shown);
    }
}
