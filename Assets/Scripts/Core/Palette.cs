using UnityEngine;

namespace CorperLife
{
    /// <summary>Shared colours: skin tones, NYSC kit, UI.</summary>
    public static class Palette
    {
        public static readonly Color[] SkinTones =
        {
            new Color32(0x3B, 0x24, 0x19, 255), // deep
            new Color32(0x4E, 0x31, 0x20, 255),
            new Color32(0x63, 0x40, 0x2A, 255),
            new Color32(0x7A, 0x52, 0x37, 255),
            new Color32(0x96, 0x68, 0x47, 255),
            new Color32(0xB5, 0x84, 0x5E, 255), // light
        };

        public static readonly Color Khaki = new Color32(0xB8, 0xA0, 0x6A, 255);
        public static readonly Color KitWhite = new Color32(0xF2, 0xF2, 0xEE, 255);
        public static readonly Color NigeriaGreen = new Color32(0x00, 0x87, 0x51, 255);
        public static readonly Color Boot = new Color32(0x1E, 0x1A, 0x16, 255);

        // UI
        public static readonly Color PanelBg = new Color(0.07f, 0.09f, 0.08f, 0.88f);
        public static readonly Color PanelLight = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Accent = new Color32(0x1F, 0xB2, 0x6B, 255);
        public static readonly Color Warn = new Color32(0xE8, 0xA3, 0x3D, 255);
        public static readonly Color Danger = new Color32(0xD9, 0x4A, 0x3F, 255);
        public static readonly Color TextMain = new Color(0.96f, 0.96f, 0.94f, 1f);
        public static readonly Color TextDim = new Color(0.75f, 0.78f, 0.76f, 1f);

        public static string Naira(int amount)
        {
            string sign = amount < 0 ? "-" : "";
            // Plain "N" because the built-in UI font may not have the naira glyph.
            return sign + "N" + Mathf.Abs(amount).ToString("N0");
        }
    }
}
