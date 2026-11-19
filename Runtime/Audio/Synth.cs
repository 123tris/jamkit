using System;

namespace Metz.JamKit
{
    public enum SynthPreset
    {
        Pickup,
        Hit,
        Jump,
        Explosion,
        Powerup,
        Blip,
        Laser,
    }

    public static class Synth
    {
        public const int SampleRate = 44100;

        public enum Wave
        {
            Square,
            Saw,
            Sine,
            Noise,
        }

        public readonly struct Voice
        {
            public readonly Wave Wave;
            public readonly double Frequency;
            public readonly double Slide;
            public readonly double Attack;
            public readonly double Sustain;
            public readonly double Decay;
            public readonly double Punch;
            public readonly double Duty;
            public readonly double JumpAt;
            public readonly double JumpRatio;

            public Voice(Wave wave, double frequency, double slide, double attack, double sustain, double decay,
                double punch = 0, double duty = 0.5, double jumpAt = 0, double jumpRatio = 1)
            {
                Wave = wave;
                Frequency = frequency;
                Slide = slide;
                Attack = Math.Max(0.001, attack);
                Sustain = Math.Max(0.001, sustain);
                Decay = Math.Max(0.001, decay);
                Punch = Math.Clamp(punch, 0, 1);
                Duty = Math.Clamp(duty, 0.05, 0.95);
                JumpAt = jumpAt;
                JumpRatio = jumpRatio;
            }

            public double Seconds => Attack + Sustain + Decay;
        }

        public static Voice For(SynthPreset preset, int seed)
        {
            var random = new Random(seed);
            double Between(double low, double high) => low + random.NextDouble() * (high - low);
            Wave Either(Wave first, Wave second) => random.NextDouble() < 0.5 ? first : second;

            return preset switch
            {
                SynthPreset.Pickup => new Voice(Wave.Square, Between(700, 1200), 0, 0.001, Between(0.03, 0.08), Between(0.1, 0.25),
                    Between(0.3, 0.6), Between(0.3, 0.5), Between(0.04, 0.08), Between(1.3, 1.6)),
                SynthPreset.Hit => new Voice(Either(Wave.Noise, Wave.Saw), Between(900, 2500), -Between(1500, 4000), 0.001,
                    Between(0.02, 0.06), Between(0.1, 0.2)),
                SynthPreset.Jump => new Voice(Wave.Square, Between(250, 450), Between(500, 1100), 0.001, Between(0.05, 0.1),
                    Between(0.1, 0.2), 0, Between(0.3, 0.5)),
                SynthPreset.Explosion => new Voice(Wave.Noise, Between(300, 900), -Between(200, 600), 0.001, Between(0.1, 0.25),
                    Between(0.3, 0.6), Between(0.4, 0.8)),
                SynthPreset.Powerup => new Voice(Either(Wave.Square, Wave.Saw), Between(300, 500), Between(300, 700), 0.001,
                    Between(0.15, 0.3), Between(0.2, 0.4), 0, Between(0.4, 0.6), Between(0.1, 0.15), 1.5),
                SynthPreset.Blip => new Voice(Either(Wave.Square, Wave.Sine), Between(600, 1500), 0, 0.001, Between(0.02, 0.06),
                    Between(0.02, 0.08)),
                SynthPreset.Laser => new Voice(Either(Wave.Square, Wave.Saw), Between(800, 1500), -Between(1500, 3000), 0.001,
                    Between(0.05, 0.15), Between(0.05, 0.15), 0, Between(0.2, 0.5)),
                _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null),
            };
        }

        public static float[] Render(SynthPreset preset, int seed, int sampleRate = SampleRate) => Render(For(preset, seed), sampleRate, seed);

        public static float[] Render(Voice voice, int sampleRate, int noiseSeed = 0)
        {
            var samples = new float[Math.Max(1, (int)(voice.Seconds * sampleRate))];
            var noise = new Random(noiseSeed);
            double phase = 0;
            double held = noise.NextDouble() * 2 - 1;

            for (int i = 0; i < samples.Length; i++)
            {
                double time = (double)i / sampleRate;
                double frequency = voice.Frequency + voice.Slide * time;
                if (voice.JumpAt > 0 && time >= voice.JumpAt) frequency *= voice.JumpRatio;
                frequency = Math.Max(20, frequency);

                phase += frequency / sampleRate;
                if (phase >= 1)
                {
                    phase -= Math.Floor(phase);
                    held = noise.NextDouble() * 2 - 1;
                }

                double wave = voice.Wave switch
                {
                    Wave.Square => phase < voice.Duty ? 1 : -1,
                    Wave.Saw => 2 * phase - 1,
                    Wave.Sine => Math.Sin(2 * Math.PI * phase),
                    _ => held,
                };
                samples[i] = (float)(wave * Envelope(voice, time) * 0.5);
            }
            return samples;
        }

        static double Envelope(Voice voice, double time)
        {
            if (time < voice.Attack) return time / voice.Attack;
            time -= voice.Attack;
            if (time < voice.Sustain) return 1 + voice.Punch * (1 - time / voice.Sustain);
            time -= voice.Sustain;
            return Math.Max(0, 1 - time / voice.Decay);
        }
    }
}
