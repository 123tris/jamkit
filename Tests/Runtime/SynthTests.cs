using System;
using System.Linq;
using NUnit.Framework;

namespace Metz.JamKit.Tests
{
    public class SynthTests
    {
        [Test]
        public void TheSameSeedRendersTheSameSound([Values] SynthPreset preset)
        {
            CollectionAssert.AreEqual(Synth.Render(preset, 7), Synth.Render(preset, 7));
        }

        [Test]
        public void AnotherSeedRendersAnotherSound([Values] SynthPreset preset)
        {
            CollectionAssert.AreNotEqual(Synth.Render(preset, 7), Synth.Render(preset, 8));
        }

        [Test]
        public void EverySoundLastsItsEnvelopeAndStaysInRange([Values] SynthPreset preset)
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                Synth.Voice voice = Synth.For(preset, seed);
                float[] samples = Synth.Render(preset, seed);

                Assert.AreEqual((int)(voice.Seconds * Synth.SampleRate), samples.Length, $"{preset} {seed}");
                Assert.IsTrue(samples.All(sample => !float.IsNaN(sample) && Math.Abs(sample) <= 1f), $"{preset} {seed}");
                Assert.IsTrue(samples.Any(sample => Math.Abs(sample) > 0.05f), $"{preset} {seed} is silent");
            }
        }

        [Test]
        public void ASoundEndsInSilence([Values] SynthPreset preset)
        {
            float[] samples = Synth.Render(preset, 3);

            Assert.Less(Math.Abs(samples[^1]), 0.01f);
        }
    }
}
