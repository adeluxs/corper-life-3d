using System;
using UnityEngine;
using UnityEngine.UI;

namespace CorperLife
{
    /// <summary>
    /// Morning drill: tap "STEP" exactly when the closing ring meets the circle, on every beat.
    /// Score 0..1 decides how the soldiers rate your platoon.
    /// </summary>
    public class DrillMiniGame : MonoBehaviour
    {
        const int Beats = 20;
        const float BeatInterval = 0.75f;
        const float PerfectWindow = 0.09f;
        const float GoodWindow = 0.18f;

        Action<float> done;
        RectTransform ring, target;
        Text command, feedback, scoreText;
        AudioSource audioSrc;
        AudioClip drum;

        float startTime;
        int beatIndex;
        float points;
        bool[] judged = new bool[Beats];
        bool finished;

        static readonly string[] Calls = { "Left!", "Right!", "Left!", "Right!" };

        public void Build(Action<float> onDone)
        {
            done = onDone;
            var bg = UIFactory.Panel("Bg", transform, new Color(0.05f, 0.07f, 0.05f, 0.9f), false);
            bg.rectTransform.Stretch();

            var title = UIFactory.Label("Title", transform, "MORNING DRILL - Platoon, quick march!", 46, Palette.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(new Vector2(0, 0.84f), new Vector2(1, 0.95f));
            var help = UIFactory.Label("Help", transform, "Tap STEP when the ring meets the circle.", 28, Palette.TextDim, TextAnchor.MiddleCenter);
            help.rectTransform.Anchor(new Vector2(0, 0.78f), new Vector2(1, 0.84f));

            target = UIFactory.Panel("Target", transform, new Color(1, 1, 1, 0.18f)).rectTransform;
            target.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            target.anchoredPosition = new Vector2(0, 60);
            target.sizeDelta = new Vector2(200, 200);

            ring = UIFactory.Panel("Ring", transform, new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.45f)).rectTransform;
            ring.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            ring.anchoredPosition = new Vector2(0, 60);
            ring.GetComponent<Image>().raycastTarget = false;

            command = UIFactory.Label("Command", target, "", 44, Palette.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            command.rectTransform.Stretch();
            feedback = UIFactory.Label("Feedback", transform, "", 40, Palette.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            feedback.rectTransform.Anchor(new Vector2(0, 0.26f), new Vector2(1, 0.34f));
            scoreText = UIFactory.Label("Score", transform, "", 30, Palette.TextDim, TextAnchor.MiddleCenter);
            scoreText.rectTransform.Anchor(new Vector2(0, 0.2f), new Vector2(1, 0.26f));

            var step = UIFactory.Button("Step", transform, "STEP", Palette.Accent, OnStep, 54);
            ((RectTransform)step.transform).Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-260, 40), new Vector2(260, 190));

            audioSrc = gameObject.AddComponent<AudioSource>();
            drum = ProceduralAudio.Drum();
            startTime = Time.unscaledTime + 1.5f; // short count-in
        }

        float BeatTime(int i) => startTime + i * BeatInterval;

        void Update()
        {
            if (finished) return;
            float now = Time.unscaledTime;

            // Current beat is the next one not yet passed.
            while (beatIndex < Beats && now > BeatTime(beatIndex) + GoodWindow)
            {
                if (!judged[beatIndex]) Show("Miss", Palette.Danger);
                beatIndex++;
            }

            if (beatIndex >= Beats)
            {
                finished = true;
                float score = points / Beats;
                Invoke(nameof(Finish), 1.2f);
                Show(score > 0.75f ? "Excellent!" : score > 0.45f ? "Good" : "Keep practising", Palette.Accent);
                return;
            }

            float t = BeatTime(beatIndex) - now;           // time until the beat
            float k = Mathf.Clamp01(t / BeatInterval);     // 1 = far, 0 = on beat
            float size = Mathf.Lerp(200f, 460f, k);
            ring.sizeDelta = new Vector2(size, size);
            command.text = now < startTime - 0.2f ? "Ready..." : Calls[beatIndex % Calls.Length];

            if (Mathf.Abs(t) < 0.02f && !audioSrc.isPlaying) audioSrc.PlayOneShot(drum, 0.7f);
            scoreText.text = "Beat " + Mathf.Min(beatIndex + 1, Beats) + "/" + Beats;
        }

        void OnStep()
        {
            if (finished || beatIndex >= Beats) return;
            float diff = Mathf.Abs(Time.unscaledTime - BeatTime(beatIndex));
            if (judged[beatIndex]) return;
            if (diff <= PerfectWindow) { points += 1f; judged[beatIndex] = true; Show("Perfect", Palette.Accent); }
            else if (diff <= GoodWindow) { points += 0.6f; judged[beatIndex] = true; Show("Good", Palette.Warn); }
            else Show("Off beat", Palette.Danger);
        }

        void Show(string text, Color c)
        {
            feedback.text = text;
            feedback.color = c;
        }

        void Finish() => done?.Invoke(points / Beats);
    }
}
