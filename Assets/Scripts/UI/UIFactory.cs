using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CorperLife
{
    /// <summary>Small helpers that build uGUI elements from code.</summary>
    public static class UIFactory
    {
        static Font font;
        static Sprite rounded;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>A white rounded-rectangle sprite generated at runtime (9-sliced).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64, r = 18;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(r - x, x - (size - 1 - r)));
                    float dy = Mathf.Max(0, Mathf.Max(r - y, y - (size - 1 - r)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)(255 * Mathf.Clamp01(r - d + 0.5f));
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(px);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return rounded;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max,
            Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float pad = 0f) =>
            rt.Anchor(Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));

        public static Image Panel(string name, Transform parent, Color color, bool roundedCorners = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (roundedCorners)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size,
            Color color, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, string text, Color bg, UnityAction onClick,
            int fontSize = 30)
        {
            var img = Panel(name, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var label = Label("Text", img.transform, text, fontSize, Palette.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Stretch(12f);
            return btn;
        }

        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing, int pad = 0,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static LayoutElement Height(this Component c, float h)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            return le;
        }

        public static void FitHeight(Component c)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    public class DialogButton
    {
        public string label;
        public Action onClick;
        public bool enabled;

        public DialogButton(string label, Action onClick, bool enabled = true)
        {
            this.label = label;
            this.onClick = onClick;
            this.enabled = enabled;
        }
    }

    /// <summary>A labelled 0-100 bar.</summary>
    public class StatBar
    {
        public Image fill;
        public Text label;

        public StatBar(Transform parent, string name)
        {
            var row = UIFactory.Rect(name, parent);
            row.Height(34);
            label = UIFactory.Label("Label", row, name, 22, Palette.TextMain);
            label.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0.38f, 1));
            var bg = UIFactory.Panel("Bg", row, new Color(1, 1, 1, 0.15f));
            bg.rectTransform.Anchor(new Vector2(0.4f, 0.22f), new Vector2(1, 0.78f));
            fill = UIFactory.Panel("Fill", bg.transform, Palette.Accent);
            fill.rectTransform.Anchor(Vector2.zero, Vector2.one);
        }

        public void Set(float value)
        {
            float v = Mathf.Clamp01(value / 100f);
            fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, v), 1f);
            fill.color = v < 0.2f ? Palette.Danger : v < 0.4f ? Palette.Warn : Palette.Accent;
        }
    }
}
