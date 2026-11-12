using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CorperLife
{
    [Serializable]
    public class QuizQuestion
    {
        public string topic;
        public string question;
        public List<string> options = new List<string>();
        public int answer;
        public string explain;
    }

    [Serializable]
    class QuizFile
    {
        public List<QuizQuestion> questions = new List<QuizQuestion>();
    }

    /// <summary>Camp lecture: three questions on safety, health, money and service-year life.</summary>
    public class LectureQuiz : MonoBehaviour
    {
        const int PerLecture = 3;

        Action<int> done;
        List<QuizQuestion> picked = new List<QuizQuestion>();
        int index, correct;
        RectTransform card;

        public void Build(Action<int> onDone)
        {
            done = onDone;
            var asset = Resources.Load<TextAsset>("Data/quiz");
            var all = asset != null ? JsonUtility.FromJson<QuizFile>(asset.text).questions : new List<QuizQuestion>();
            for (int i = 0; i < all.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, all.Count);
                (all[i], all[j]) = (all[j], all[i]);
            }
            for (int i = 0; i < Mathf.Min(PerLecture, all.Count); i++) picked.Add(all[i]);

            var bg = UIFactory.Panel("Bg", transform, new Color(0.04f, 0.05f, 0.05f, 0.9f), false);
            bg.rectTransform.Stretch();
            if (picked.Count == 0) { done?.Invoke(0); return; }
            ShowQuestion();
        }

        void ShowQuestion()
        {
            if (card != null) Destroy(card.gameObject);
            var q = picked[index];

            var img = UIFactory.Panel("Card", transform, Palette.PanelBg);
            card = img.rectTransform;
            card.Anchor(new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f));
            var stack = UIFactory.Rect("Stack", card).Stretch(40);
            UIFactory.VStack(stack, 16);

            UIFactory.Label("Topic", stack, "LECTURE: " + q.topic.ToUpper() + "   (" + (index + 1) + "/" + picked.Count + ")",
                24, Palette.Accent, TextAnchor.MiddleLeft, FontStyle.Bold).Height(36);
            UIFactory.Label("Q", stack, q.question, 36, Palette.TextMain, TextAnchor.UpperLeft, FontStyle.Bold).Height(130);

            for (int i = 0; i < q.options.Count; i++)
            {
                int choice = i;
                var b = UIFactory.Button("Opt" + i, stack, q.options[i], new Color(1, 1, 1, 0.12f), () => Answer(choice), 28);
                b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                b.Height(86);
            }
        }

        void Answer(int choice)
        {
            var q = picked[index];
            bool right = choice == q.answer;
            if (right) correct++;
            if (card != null) Destroy(card.gameObject);

            var img = UIFactory.Panel("Result", transform, Palette.PanelBg);
            card = img.rectTransform;
            card.Anchor(new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));
            var stack = UIFactory.Rect("Stack", card).Stretch(40);
            UIFactory.VStack(stack, 18, 0, TextAnchor.MiddleCenter);
            UIFactory.Label("Verdict", stack, right ? "Correct!" : "Not quite.", 44, right ? Palette.Accent : Palette.Warn,
                TextAnchor.MiddleCenter, FontStyle.Bold).Height(70);
            UIFactory.Label("Explain", stack, q.explain, 28, Palette.TextDim, TextAnchor.MiddleCenter).Height(150);
            UIFactory.Button("Next", stack, index + 1 < picked.Count ? "Next question" : "Finish lecture", Palette.Accent, () =>
            {
                index++;
                if (index < picked.Count) ShowQuestion();
                else done?.Invoke(correct);
            }, 30).Height(86);
        }
    }
}
