using System;
using UnityEngine;
using UnityEngine.UI;

namespace CorperLife
{
    /// <summary>Create your corper: name, look, course, home state and family background.</summary>
    public class CharacterCreatorUI : MonoBehaviour
    {
        static readonly string[] Courses =
        {
            "Computer Science", "Medicine and Surgery", "Accounting", "Mass Communication", "Law",
            "Civil Engineering", "Microbiology", "Economics", "Education (English)", "Agriculture",
            "Business Administration", "Nursing Science", "Actuarial Science"
        };

        static readonly string[] States =
        {
            "Abia", "Adamawa", "Akwa Ibom", "Anambra", "Bauchi", "Bayelsa", "Benue", "Borno", "Cross River",
            "Delta", "Ebonyi", "Edo", "Ekiti", "Enugu", "FCT", "Gombe", "Imo", "Jigawa", "Kaduna", "Kano",
            "Katsina", "Kebbi", "Kogi", "Kwara", "Lagos", "Nasarawa", "Niger", "Ogun", "Ondo", "Osun", "Oyo",
            "Plateau", "Rivers", "Sokoto", "Taraba", "Yobe", "Zamfara"
        };

        static readonly string[] Backgrounds = { "Rich", "Average", "Struggling" };
        static readonly string[] BackgroundInfo =
        {
            "Family sends money often. Less street sense.",
            "Some help from home. Balanced start.",
            "Little help from home. Family may ask you for money.",
        };

        PlayerProfile p = new PlayerProfile();
        Action<PlayerProfile> done;
        Text courseText, stateText, bgText, bgInfo;
        Image[] skinSwatches;
        Button maleBtn, femaleBtn;
        InputField firstField, lastField;
        int courseIdx, stateIdx = 24, bgIdx = 1;

        public void Build(UIManager ui, Action<PlayerProfile> onDone)
        {
            done = onDone;
            p.skinTone = 2;

            var panel = UIFactory.Panel("Panel", transform, Palette.PanelBg);
            panel.rectTransform.Anchor(new Vector2(0.03f, 0.05f), new Vector2(0.48f, 0.95f));
            var stack = UIFactory.Rect("Stack", panel.transform).Stretch(36);
            UIFactory.VStack(stack, 14);

            UIFactory.Label("Title", stack, "Create your corper", 46, Palette.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold).Height(64);

            firstField = Field(stack, "First name", p.firstName);
            lastField = Field(stack, "Surname", p.lastName);

            // Gender
            var gRow = Row(stack, 76);
            maleBtn = UIFactory.Button("Male", gRow, "Male", Palette.Accent, () => SetFemale(false), 28);
            femaleBtn = UIFactory.Button("Female", gRow, "Female", new Color(1, 1, 1, 0.12f), () => SetFemale(true), 28);

            // Skin tone
            UIFactory.Label("SkinLabel", stack, "Skin tone", 24, Palette.TextDim).Height(32);
            var sRow = Row(stack, 70);
            skinSwatches = new Image[Palette.SkinTones.Length];
            for (int i = 0; i < Palette.SkinTones.Length; i++)
            {
                int idx = i;
                var b = UIFactory.Button("Skin" + i, sRow, "", Palette.SkinTones[i], () => SetSkin(idx));
                skinSwatches[i] = b.GetComponent<Image>();
            }

            courseText = Cycler(stack, "Course", () => { courseIdx = (courseIdx + Courses.Length - 1) % Courses.Length; Refresh(); },
                () => { courseIdx = (courseIdx + 1) % Courses.Length; Refresh(); });
            stateText = Cycler(stack, "Home state", () => { stateIdx = (stateIdx + States.Length - 1) % States.Length; Refresh(); },
                () => { stateIdx = (stateIdx + 1) % States.Length; Refresh(); });
            bgText = Cycler(stack, "Family", () => { bgIdx = (bgIdx + 2) % 3; Refresh(); },
                () => { bgIdx = (bgIdx + 1) % 3; Refresh(); });
            bgInfo = UIFactory.Label("BgInfo", stack, "", 22, Palette.TextDim);
            bgInfo.Height(34);

            UIFactory.Button("Start", stack, "Print call-up letter and go to camp", Palette.Accent, Finish, 30).Height(92);

            Refresh();
        }

        InputField Field(Transform parent, string placeholder, string value)
        {
            var bg = UIFactory.Panel(placeholder, parent, new Color(1, 1, 1, 0.1f));
            bg.Height(72);
            var text = UIFactory.Label("Text", bg.transform, value, 30, Palette.TextMain);
            text.rectTransform.Stretch(18);
            text.raycastTarget = true;
            text.supportRichText = false;
            var ph = UIFactory.Label("Placeholder", bg.transform, placeholder, 30, Palette.TextDim, TextAnchor.MiddleLeft, FontStyle.Italic);
            ph.rectTransform.Stretch(18);
            var f = bg.gameObject.AddComponent<InputField>();
            f.textComponent = text;
            f.placeholder = ph;
            f.characterLimit = 18;
            f.text = value;
            f.onValueChanged.AddListener(_ => ReadNames());
            return f;
        }

        RectTransform Row(Transform parent, float h)
        {
            var row = UIFactory.Rect("Row", parent);
            row.Height(h);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;
            return row;
        }

        Text Cycler(Transform parent, string label, UnityEngine.Events.UnityAction prev, UnityEngine.Events.UnityAction next)
        {
            UIFactory.Label(label + "Label", parent, label, 24, Palette.TextDim).Height(30);
            var row = Row(parent, 70);
            var l = UIFactory.Button("Prev", row, "<", new Color(1, 1, 1, 0.12f), prev, 34);
            l.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0.2f;
            var t = UIFactory.Label("Value", row, "", 28, Palette.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var r = UIFactory.Button("Next", row, ">", new Color(1, 1, 1, 0.12f), next, 34);
            r.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0.2f;
            return t;
        }

        void SetFemale(bool female)
        {
            p.isFemale = female;
            if (female && p.firstName == "Tunde") { p.firstName = "Amina"; firstField.text = "Amina"; }
            if (!female && p.firstName == "Amina") { p.firstName = "Tunde"; firstField.text = "Tunde"; }
            Refresh();
        }

        void SetSkin(int i)
        {
            p.skinTone = i;
            Refresh();
        }

        void ReadNames()
        {
            p.firstName = string.IsNullOrWhiteSpace(firstField.text) ? "Corper" : firstField.text.Trim();
            p.lastName = string.IsNullOrWhiteSpace(lastField.text) ? "" : lastField.text.Trim();
        }

        void Refresh()
        {
            p.course = Courses[courseIdx];
            p.homeState = States[stateIdx];
            p.background = Backgrounds[bgIdx];
            courseText.text = p.course;
            stateText.text = p.homeState;
            bgText.text = p.background;
            bgInfo.text = BackgroundInfo[bgIdx] + "  Start with " + Palette.Naira(EconomySystem.StartingCash(p.background)) + ".";

            maleBtn.GetComponent<Image>().color = p.isFemale ? new Color(1, 1, 1, 0.12f) : Palette.Accent;
            femaleBtn.GetComponent<Image>().color = p.isFemale ? Palette.Accent : new Color(1, 1, 1, 0.12f);
            for (int i = 0; i < skinSwatches.Length; i++)
                skinSwatches[i].transform.localScale = i == p.skinTone ? Vector3.one * 1.12f : Vector3.one * 0.86f;

            GameManager.I?.World?.ApplyProfile(p, false);
        }

        void Finish()
        {
            ReadNames();
            // Deployment is random and never your home state, like real life.
            string deployed;
            do deployed = States[UnityEngine.Random.Range(0, States.Length)];
            while (deployed == p.homeState);
            p.deployedState = deployed;
            done?.Invoke(p);
        }
    }
}
