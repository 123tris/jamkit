using NUnit.Framework;
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
    }
}
