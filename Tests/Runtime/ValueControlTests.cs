using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Tests
{
    public class ValueControlTests
    {
        [Test]
        public void ValueLabelShowsItsValueInItsFormat()
        {
            var label = new ValueLabel { format = "Score: {0:0}", value = 41.6f };
            Label text = label.Q<Label>(className: ValueLabel.textUssClassName);
            Assert.AreEqual("Score: 42", text.text);

            label.format = "{0:0} HP";
            Assert.AreEqual("42 HP", text.text);
        }

        [Test]
        public void ValueLabelInheritsNoStringValueToClashWithItsFloat()
        {
            Assert.IsFalse(typeof(INotifyValueChanged<string>).IsAssignableFrom(typeof(ValueLabel)));
        }

        [Test]
        public void ValueBarFillsItsShareOfTheRange()
        {
            var bar = new ValueBar { min = 0f, max = 200f, value = 50f };
            VisualElement fill = bar.Q(className: ValueBar.fillUssClassName);
            Assert.AreEqual(Length.Percent(25f), fill.style.width.value);

            bar.vertical = true;
            Assert.AreEqual(Length.Percent(25f), fill.style.height.value);
            Assert.AreEqual(StyleKeyword.Null, fill.style.width.keyword);
            Assert.IsTrue(bar.ClassListContains(ValueBar.verticalUssClassName));

            bar.value = 500f;
            Assert.AreEqual(Length.Percent(100f), fill.style.height.value);
        }

        [Test]
        public void QualityDropdownOffersEveryLevelAndShowsTheCurrentOneForAnUnsetValue()
        {
            var dropdown = new QualityDropdown();
            CollectionAssert.AreEqual(Enumerable.Range(0, QualitySettings.names.Length), dropdown.choices);
            Assert.AreEqual(QualitySettings.GetQualityLevel(), dropdown.value);

            dropdown.SetValueWithoutNotify(-1);
            Assert.AreEqual(QualitySettings.GetQualityLevel(), dropdown.value);
        }

        [Test]
        public void ResolutionDropdownOffersEveryDistinctResolution()
        {
            var dropdown = new ResolutionDropdown();
            Assert.AreEqual(DisplaySettings.Resolutions.Count, dropdown.choices.Count);
            Assert.AreEqual(DisplaySettings.Resolutions.Count,
                DisplaySettings.Resolutions.Select(resolution => (resolution.width, resolution.height)).Distinct().Count());
        }
    }
}
