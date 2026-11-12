using UnityEngine;

namespace CorperLife
{
    /// <summary>
    /// Original sounds generated in code (no copied NYSC audio).
    /// Proper recorded audio can replace these later.
    /// </summary>
    public static class ProceduralAudio
    {
        const int Rate = 22050;

        /// <summary>A short bugle-style call: G4 C5 E5 G5, played with a brassy tone.</summary>
        public static AudioClip Bugle()
        {
            float[] notes = { 392f, 523.25f, 659.25f, 783.99f, 659.25f, 783.99f };
            float[] lens = { 0.22f, 0.22f, 0.22f, 0.45f, 0.18f, 0.7f };
            float total = 0f;
            foreach (var l in lens) total += l;
            int count = Mathf.CeilToInt(total * Rate);
            var data = new float[count];

            int i = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                int len = Mathf.CeilToInt(lens[n] * Rate);
                for (int j = 0; j < len && i < count; j++, i++)
                {
                    float t = (float)j / Rate;
                    float env = Mathf.Min(1f, t / 0.03f) * Mathf.Min(1f, (lens[n] - t) / 0.05f);
                    float ph = 2f * Mathf.PI * notes[n] * t;
                    // Odd and even harmonics give a brass-like timbre.
                    float s = Mathf.Sin(ph) * 0.6f + Mathf.Sin(2 * ph) * 0.25f + Mathf.Sin(3 * ph) * 0.12f +
                              Mathf.Sin(4 * ph) * 0.05f;
                    data[i] = s * env * 0.5f;
                }
            }

            var clip = AudioClip.Create("bugle", count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A short drum hit used for the drill beat.</summary>
        public static AudioClip Drum()
        {
            int count = Mathf.CeilToInt(0.18f * Rate);
            var data = new float[count];
            var rng = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 28f);
                float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 60f, t / 0.18f) * t);
                float noise = (float)(rng.NextDouble() * 2 - 1) * 0.25f;
                data[i] = (tone + noise) * env * 0.8f;
            }
            var clip = AudioClip.Create("drum", count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
