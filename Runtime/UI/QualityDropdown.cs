using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [UxmlElement]
    public partial class QualityDropdown : PopupField<int>
    {
        public QualityDropdown()
        {
            choices = Enumerable.Range(0, QualitySettings.names.Length).ToList();
            formatSelectedValueCallback = Name;
            formatListItemCallback = Name;
            SetValueWithoutNotify(QualitySettings.GetQualityLevel());
        }

        public override void SetValueWithoutNotify(int newValue)
            => base.SetValueWithoutNotify(choices.Contains(newValue) ? newValue : QualitySettings.GetQualityLevel());

        static string Name(int level) => level >= 0 && level < QualitySettings.names.Length ? QualitySettings.names[level] : string.Empty;
    }
}
