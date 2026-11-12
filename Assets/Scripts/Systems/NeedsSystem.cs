using UnityEngine;

namespace CorperLife
{
    /// <summary>How the body and mind change minute by minute.</summary>
    public static class NeedsSystem
    {
        // Per in-game minute while awake and idle. Activity multiplies energy loss.
        const float EnergyDecay = 0.055f;
        const float HungerDecay = 0.075f;
        const float HygieneDecay = 0.035f;

        public static void Tick(GameState s, float minutes, float exertion = 1f)
        {
            var n = s.needs;
            n.energy -= EnergyDecay * exertion * minutes * (n.hunger < 20f ? 1.6f : 1f);
            n.hunger -= HungerDecay * minutes;
            n.hygiene -= HygieneDecay * minutes * (exertion > 1.2f ? 1.8f : 1f);

            // Mood drifts with how the other needs are doing.
            float comfort = (n.energy + n.hunger + n.hygiene + n.health) / 4f;
            float target = Mathf.Lerp(20f, 85f, comfort / 100f);
            n.mood = Mathf.MoveTowards(n.mood, target, 0.02f * minutes);

            // Health suffers when other needs are neglected.
            float harm = 0f;
            if (n.hunger < 15f) harm += 0.03f;
            if (n.energy < 10f) harm += 0.02f;
            if (n.hygiene < 15f) harm += 0.015f;
            if (harm > 0f) n.health -= harm * minutes;
            else n.health += 0.004f * minutes; // slow natural recovery

            n.ClampAll();
        }

        /// <summary>Sleeping restores energy; the rest keeps decaying slowly.</summary>
        public static void Sleep(GameState s, float minutes)
        {
            var n = s.needs;
            n.energy += 0.16f * minutes;
            n.hunger -= 0.03f * minutes;
            n.hygiene -= 0.015f * minutes;
            n.health += 0.01f * minutes;
            n.ClampAll();
        }

        public static void Apply(GameState s, float energy = 0, float hunger = 0, float health = 0,
            float mood = 0, float hygiene = 0)
        {
            var n = s.needs;
            n.energy += energy;
            n.hunger += hunger;
            n.health += health;
            n.mood += mood;
            n.hygiene += hygiene;
            n.ClampAll();
        }

        public static string WorstNeedWarning(GameState s)
        {
            var n = s.needs;
            if (n.health < 25f) return "You're feeling sick. Visit the clinic.";
            if (n.energy < 15f) return "You're exhausted. Rest or you may faint.";
            if (n.hunger < 15f) return "You're very hungry. Go and eat.";
            if (n.hygiene < 20f) return "You need a bath. People are noticing.";
            if (n.mood < 20f) return "You're feeling low. Talk to friends or rest.";
            return null;
        }
    }
}
