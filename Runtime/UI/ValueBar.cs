using Unity.Properties;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [UxmlElement]
    public partial class ValueBar : VisualElement
    {
        public const string ussClassName = "jk-bar";
        public const string fillUssClassName = ussClassName + "__fill";
        public const string verticalUssClassName = ussClassName + "--vertical";

        readonly VisualElement fill = new() { name = "fill", pickingMode = PickingMode.Ignore };
        float low;
        float high = 1f;
        float current = 1f;
        bool upright;

        public ValueBar()
        {
            AddToClassList(ussClassName);
            fill.AddToClassList(fillUssClassName);
            Add(fill);
            Refresh();
        }

        [UxmlAttribute]
        public float min
        {
            get => low;
            set
            {
                low = value;
                Refresh();
            }
        }

        [UxmlAttribute]
        public float max
        {
            get => high;
            set
            {
                high = value;
                Refresh();
            }
        }

        [UxmlAttribute]
        public bool vertical
        {
            get => upright;
            set
            {
                upright = value;
                EnableInClassList(verticalUssClassName, value);
                Refresh();
            }
        }

        [UxmlAttribute, CreateProperty]
        public float value
        {
            get => current;
            set
            {
                current = value;
                Refresh();
            }
        }

        void Refresh()
        {
            Length size = Length.Percent(MathX.Remap01(current, low, high) * 100f);
            fill.style.width = upright ? StyleKeyword.Null : size;
            fill.style.height = upright ? size : StyleKeyword.Null;
        }
    }
}
