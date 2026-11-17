using System;
using System.IO;
using PuzzleKingdoms.Data;
using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// Synthesizes the placeholder sounds (PB-8, D-036) as .wav files, fills
    /// <c>Data/SoundBank.asset</c> with them and links it from GameContent. The
    /// output is the same every run, so it can be regenerated at any time:
    /// <c>Puzzle Kingdoms → Generate Placeholder Sounds</c>, or in batch mode
    /// <c>-executeMethod PuzzleKingdoms.EditorTools.PlaceholderSounds.Generate</c>.
    /// </summary>
    public static class PlaceholderSounds
    {
        public const string Folder = "Assets/_Project/Audio/Placeholder";
        public const string BankPath = "Assets/_Project/Data/SoundBank.asset";
        private const string ContentPath = "Assets/_Project/Data/GameContent.asset";
        private const int Rate = 44100;
        private const int MusicRate = 22050;

        [MenuItem("Puzzle Kingdoms/Generate Placeholder Sounds")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            Write("match", Rate, Pop());
            Write("blast-created", Rate, Whoosh());
            Write("blast-fired", Rate, Thump());
            Write("prism-created", Rate, Chime());
            Write("prism-fired", Rate, Sweep());
            Write("stone-crack", Rate, Crack());
            Write("invalid-swap", Rate, Nope());
            Write("unit-recruited", Rate, Ding());
            Write("rally-charge", Rate, Horn());
            Write("music-loop", MusicRate, MusicLoop(), normalize: 0.6f);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            var serialized = new SerializedObject(bank);
            Link(serialized, "match", "match");
            Link(serialized, "blastCreated", "blast-created");
            Link(serialized, "blastFired", "blast-fired");
            Link(serialized, "prismCreated", "prism-created");
            Link(serialized, "prismFired", "prism-fired");
            Link(serialized, "stoneCrack", "stone-crack");
            Link(serialized, "invalidSwap", "invalid-swap");
            Link(serialized, "unitRecruited", "unit-recruited");
            Link(serialized, "rallyCharge", "rally-charge");
            Link(serialized, "music", "music-loop");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bank);

            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            if (content != null)
            {
                var contentObject = new SerializedObject(content);
                contentObject.FindProperty("sounds").objectReferenceValue = bank;
                contentObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(content);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlaceholderSounds] wrote 10 clips to {Folder} and {BankPath}");
        }

        private static void Link(SerializedObject bank, string field, string file)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{file}.wav");
            if (clip == null)
            {
                throw new InvalidOperationException($"clip {file} was not imported");
            }
            bank.FindProperty(field).objectReferenceValue = clip;
        }

        // --- Sounds --------------------------------------------------------

        /// <summary>Short bubbly pop: a falling sine with a fast decay.</summary>
        private static float[] Pop() => Render(0.11f, Rate, (t, n) =>
            Sine(Sweep(t, 900f, 420f, 0.11f)) * Decay(t, 28f) * Attack(t, 0.003f));

        /// <summary>Breathy rising whoosh: noise through a lowpass that opens and closes.</summary>
        private static float[] Whoosh()
        {
            var lowpass = new OnePole();
            return Render(0.35f, Rate, (t, n) =>
            {
                float shape = Mathf.Sin(Mathf.PI * t / 0.35f);
                return lowpass.Low(n.Next(), 0.02f + 0.25f * shape) * shape * 2.2f;
            });
        }

        /// <summary>Deep thump: a sine dropping from 140 to 45 Hz plus a short noise hit.</summary>
        private static float[] Thump() => Render(0.32f, Rate, (t, n) =>
            Sine(Sweep(t, 140f, 45f, 0.25f)) * Decay(t, 9f) + n.Next() * 0.5f * Decay(t, 60f));

        /// <summary>Bright bell chord (E6, A6, E7) ringing out.</summary>
        private static float[] Chime() => Render(0.6f, Rate, (t, n) =>
            (Sine(1318.5f * t) + 0.7f * Sine(1760f * t) + 0.4f * Sine(2637f * t)) * Decay(t, 6f) * Attack(t, 0.004f));

        /// <summary>Glassy upward sweep for the Prism going off.</summary>
        private static float[] Sweep() => Render(0.38f, Rate, (t, n) =>
        {
            float phase = Sweep(t, 400f, 2400f, 0.38f);
            return (Sine(phase) + 0.35f * Sine(2f * phase)) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.38f)) * 0.8f;
        });

        /// <summary>Three dry noise crackles, like rock breaking.</summary>
        private static float[] Crack()
        {
            var highpass = new OnePole();
            return Render(0.22f, Rate, (t, n) =>
            {
                float hits = Decay(t, 45f) + 0.7f * Decay(Mathf.Max(0f, t - 0.05f), 50f) * Step(t, 0.05f)
                             + 0.5f * Decay(Mathf.Max(0f, t - 0.11f), 55f) * Step(t, 0.11f);
                return highpass.High(n.Next(), 0.35f) * hits * 1.6f;
            });
        }

        /// <summary>Soft low "uh-uh": two short muted tones going down.</summary>
        private static float[] Nope() => Render(0.24f, Rate, (t, n) =>
        {
            float first = (t <= 0.1f ? 1f : 0f) * Sine(240f * t) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.1f));
            float u = t - 0.12f;
            float second = Step(t, 0.12f) * Sine(190f * u) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.12f));
            return (first + second) * 0.7f;
        });

        /// <summary>Clear "ding" (G6 with an octave).</summary>
        private static float[] Ding() => Render(0.4f, Rate, (t, n) =>
            (Sine(1568f * t) + 0.3f * Sine(3136f * t)) * Decay(t, 9f) * Attack(t, 0.002f));

        /// <summary>Short brass-like fanfare: two horn notes (A3 then E4) built from harmonics.</summary>
        private static float[] Horn() => Render(0.7f, Rate, (t, n) =>
        {
            float note = t < 0.22f ? 220f : 329.6f;
            float local = t < 0.22f ? t : t - 0.22f;
            float length = t < 0.22f ? 0.22f : 0.48f;
            float tone = 0f;
            for (int h = 1; h <= 6; h++)
            {
                tone += Sine(note * h * t) / h;
            }
            float envelope = Mathf.Clamp01(local / 0.03f) * Mathf.Clamp01((length - local) / 0.06f);
            return tone * envelope * 0.6f;
        });

        /// <summary>
        /// 8-second loop at 120 bpm: Am, F, C, G arpeggios in soft triangle tones
        /// over a sine bass. Every note ends inside the loop, so it repeats cleanly.
        /// </summary>
        private static float[] MusicLoop()
        {
            int[][] chords =
            {
                new[] { 57, 60, 64, 69 }, // A minor
                new[] { 53, 57, 60, 65 }, // F major
                new[] { 48, 52, 55, 60 }, // C major
                new[] { 55, 59, 62, 67 }, // G major
            };
            int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3 };
            const float eighth = 0.25f;
            const float length = 8f;
            var samples = new float[(int)(length * MusicRate)];
            for (int chord = 0; chord < chords.Length; chord++)
            {
                float chordStart = chord * 2f;
                AddNote(samples, Midi(chords[chord][0] - 12), chordStart, 1.9f, 0.5f, bass: true);
                for (int i = 0; i < pattern.Length; i++)
                {
                    int midi = chords[chord][pattern[i]] + 12;
                    AddNote(samples, Midi(midi), chordStart + i * eighth, eighth * 0.95f, 0.22f, bass: false);
                }
            }
            return samples;
        }

        private static void AddNote(float[] samples, float frequency, float start, float seconds, float volume, bool bass)
        {
            int first = (int)(start * MusicRate);
            int count = (int)(seconds * MusicRate);
            for (int i = 0; i < count && first + i < samples.Length; i++)
            {
                float t = (float)i / MusicRate;
                float envelope = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((seconds - t) / 0.03f)
                                 * (bass ? 1f : Mathf.Exp(-t * 6f));
                float phase = frequency * t;
                float wave = bass ? Sine(phase) : Triangle(phase);
                samples[first + i] += wave * envelope * volume;
            }
        }

        // --- Building blocks -----------------------------------------------

        private delegate float Voice(float t, Noise noise);

        private static float[] Render(float seconds, int rate, Voice voice)
        {
            var noise = new Noise(12345u);
            var samples = new float[(int)(seconds * rate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                // A 5 ms fade at the end keeps every clip click-free.
                float tail = Mathf.Clamp01((seconds - t) / 0.005f);
                samples[i] = voice(t, noise) * tail;
            }
            return samples;
        }

        /// <summary>Sine of a phase given in cycles (frequency × time).</summary>
        private static float Sine(float cycles) => Mathf.Sin(2f * Mathf.PI * cycles);

        private static float Triangle(float cycles)
        {
            float x = cycles - Mathf.Floor(cycles);
            return 4f * Mathf.Abs(x - 0.5f) - 1f;
        }

        /// <summary>Phase in cycles of a tone gliding exponentially from f0 to f1 over <paramref name="seconds"/>.</summary>
        private static float Sweep(float t, float f0, float f1, float seconds)
        {
            float k = Mathf.Log(f1 / f0) / seconds;
            return f0 * (Mathf.Exp(k * t) - 1f) / k;
        }

        private static float Decay(float t, float rate) => Mathf.Exp(-t * rate);

        private static float Attack(float t, float seconds) => Mathf.Clamp01(t / seconds);

        /// <summary>1 once <paramref name="t"/> reached <paramref name="at"/>, else 0.</summary>
        private static float Step(float t, float at) => t >= at ? 1f : 0f;

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        /// <summary>Deterministic white noise (xorshift), so every run writes the same files.</summary>
        private sealed class Noise
        {
            private uint _state;

            public Noise(uint seed) => _state = seed;

            public float Next()
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return _state / (float)uint.MaxValue * 2f - 1f;
            }
        }

        /// <summary>One-pole filter: smooth (lowpass) or what smoothing removes (highpass).</summary>
        private sealed class OnePole
        {
            private float _low;

            public float Low(float input, float amount)
            {
                _low += (input - _low) * amount;
                return _low;
            }

            public float High(float input, float amount) => input - Low(input, amount);
        }

        /// <summary>Writes 16-bit mono PCM, scaled so the loudest sample is at <paramref name="normalize"/>.</summary>
        private static void Write(string name, int rate, float[] samples, float normalize = 0.85f)
        {
            float peak = 0f;
            foreach (float s in samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(s));
            }
            float gain = peak > 0f ? normalize / peak : 0f;
            using var stream = new FileStream($"{Folder}/{name}.wav", FileMode.Create);
            using var writer = new BinaryWriter(stream);
            int dataBytes = samples.Length * 2;
            writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(rate);
            writer.Write(rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            writer.Write(dataBytes);
            foreach (float s in samples)
            {
                writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * gain * short.MaxValue), short.MinValue, short.MaxValue));
            }
        }
    }
}
