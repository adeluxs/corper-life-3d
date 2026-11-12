using UnityEngine;

namespace CorperLife
{
    /// <summary>Moves the sun with the in-game clock and tints the sky, fog and ambient light.</summary>
    public class DayNightCycle : MonoBehaviour
    {
        public Light sun;

        static readonly Color DayAmbient = new Color(0.62f, 0.62f, 0.58f);
        static readonly Color DuskAmbient = new Color(0.55f, 0.38f, 0.28f);
        static readonly Color NightAmbient = new Color(0.08f, 0.1f, 0.16f);
        static readonly Color DayFog = new Color(0.82f, 0.8f, 0.72f);       // light harmattan-style haze
        static readonly Color NightFog = new Color(0.04f, 0.05f, 0.08f);

        void Start()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 260f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            if (sun != null) sun.shadows = LightShadows.Soft;
        }

        void Update()
        {
            var gm = GameManager.I;
            float minute = gm != null && gm.State != null ? gm.State.minuteOfDay : 9 * 60f;

            // Sunrise ~6:30am, sunset ~6:45pm (Nigeria is near the equator).
            float dayT = Mathf.InverseLerp(6.5f * 60, 18.75f * 60, minute);
            bool isDay = minute > 6.5f * 60 && minute < 18.75f * 60;
            float elevation = isDay ? Mathf.Sin(dayT * Mathf.PI) * 70f + 2f : -15f;

            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(elevation, -30f + dayT * 160f, 0);
                float target = isDay ? Mathf.Clamp01(Mathf.Sin(dayT * Mathf.PI) * 1.6f) * 1.15f : 0f;
                sun.intensity = Mathf.Lerp(sun.intensity, target, Time.deltaTime * 2f);
                sun.color = Color.Lerp(new Color(1f, 0.62f, 0.38f), new Color(1f, 0.96f, 0.88f),
                    Mathf.Clamp01(Mathf.Sin(dayT * Mathf.PI) * 2f));
            }

            float light01 = isDay ? Mathf.Clamp01(Mathf.Sin(dayT * Mathf.PI) * 2.5f) : 0f;
            float dusk = isDay ? 1f - Mathf.Clamp01(Mathf.Sin(dayT * Mathf.PI) * 4f) : 0f;
            Color amb = Color.Lerp(NightAmbient, DayAmbient, light01);
            amb = Color.Lerp(amb, DuskAmbient, dusk * 0.5f);
            RenderSettings.ambientLight = amb;
            RenderSettings.fogColor = Color.Lerp(NightFog, DayFog, light01);
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.Lerp(new Color(0.02f, 0.03f, 0.07f),
                    Color.Lerp(new Color(0.55f, 0.72f, 0.88f), new Color(0.95f, 0.6f, 0.4f), dusk), light01);
            }
        }
    }
}
