using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [UxmlElement]
    public partial class ResolutionDropdown : PopupField<int>
    {
        public ResolutionDropdown()
        {
            choices = Enumerable.Range(0, DisplaySettings.Resolutions.Count).ToList();
            formatSelectedValueCallback = Name;
            formatListItemCallback = Name;
            SetValueWithoutNotify(choices.Count - 1);
        }

        public override void SetValueWithoutNotify(int newValue)
            => base.SetValueWithoutNotify(choices.Contains(newValue) ? newValue : choices.Count - 1);

        static string Name(int index)
        {
            if (index < 0 || index >= DisplaySettings.Resolutions.Count) return string.Empty;
            Resolution resolution = DisplaySettings.Resolutions[index];
            return $"{resolution.width} x {resolution.height}";
        }
    }
}
