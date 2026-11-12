using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorperLife
{
    [Serializable]
    public class EventEffects
    {
        public int money;
        public float energy, hunger, health, mood, hygiene;
        public float repNysc, repPpa, repCommunity;
        public string addFlag;
        public string removeFlag;
        public int skipMinutes;       // time lost (e.g. sick bay)
        public string result;         // text shown after the choice
    }

    [Serializable]
    public class EventChoice
    {
        public string label;
        public int cost;              // money required to pick this choice (0 = free)
        public string requiresFlag;
        public EventEffects effects = new EventEffects();
        // Optional second outcome picked with this chance instead of "effects".
        public float riskChance;
        public EventEffects riskEffects;
    }

    [Serializable]
    public class LifeEvent
    {
        public string id;
        public string title;
        public string text;
        public string stage = "Camp";     // Camp, ServiceYear, Any
        public string location = "any";   // location id or "any"
        public int minDay = 1;
        public int maxDay = 999;
        public int minHour = 0;
        public int maxHour = 24;
        public int weight = 10;
        public bool once = true;
        public string requiresFlag;
        public string blockedByFlag;
        public List<EventChoice> choices = new List<EventChoice>();
    }

    [Serializable]
    class LifeEventFile
    {
        public List<LifeEvent> events = new List<LifeEvent>();
    }

    /// <summary>
    /// Random life events loaded from Resources/Data/events_*.json.
    /// New events can be added by dropping JSON in, no code change needed.
    /// </summary>
    public class LifeEventSystem
    {
        readonly List<LifeEvent> all = new List<LifeEvent>();

        public int Count => all.Count;

        public void LoadAll()
        {
            all.Clear();
            foreach (var asset in Resources.LoadAll<TextAsset>("Data/Events"))
            {
                try
                {
                    var file = JsonUtility.FromJson<LifeEventFile>(asset.text);
                    if (file?.events != null) all.AddRange(file.events);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Bad events file " + asset.name + ": " + e.Message);
                }
            }
            Debug.Log("Loaded " + all.Count + " life events");
        }

        public LifeEvent Get(string id) => all.Find(e => e.id == id);

        public LifeEvent PickRandom(GameState s, string currentLocation)
        {
            var pool = new List<LifeEvent>();
            int total = 0;
            foreach (var e in all)
            {
                if (!IsEligible(e, s, currentLocation)) continue;
                pool.Add(e);
                total += Mathf.Max(1, e.weight);
            }
            if (pool.Count == 0) return null;

            int roll = UnityEngine.Random.Range(0, total);
            foreach (var e in pool)
            {
                roll -= Mathf.Max(1, e.weight);
                if (roll < 0) return e;
            }
            return pool[pool.Count - 1];
        }

        static bool IsEligible(LifeEvent e, GameState s, string loc)
        {
            if (e.once && s.seenEvents.Contains(e.id)) return false;
            if (e.stage != "Any" && e.stage != s.stage.ToString()) return false;
            if (e.location != "any" && e.location != loc) return false;
            if (s.day < e.minDay || s.day > e.maxDay) return false;
            if (s.Hour < e.minHour || s.Hour >= e.maxHour) return false;
            if (!string.IsNullOrEmpty(e.requiresFlag) && !s.HasFlag(e.requiresFlag)) return false;
            if (!string.IsNullOrEmpty(e.blockedByFlag) && s.HasFlag(e.blockedByFlag)) return false;
            return true;
        }

        /// <summary>Applies a choice. Returns the result text to show the player.</summary>
        public static string ApplyChoice(GameState s, LifeEvent e, EventChoice c)
        {
            if (!s.seenEvents.Contains(e.id)) s.seenEvents.Add(e.id);
            if (c.cost > 0) EconomySystem.TrySpend(s, c.cost, e.title);

            var fx = c.effects;
            if (c.riskEffects != null && c.riskChance > 0f && UnityEngine.Random.value < c.riskChance)
                fx = c.riskEffects;

            ApplyEffects(s, fx, e.title);
            return string.IsNullOrEmpty(fx.result) ? "" : fx.result;
        }

        public static void ApplyEffects(GameState s, EventEffects fx, string label)
        {
            if (fx == null) return;
            if (fx.money > 0) EconomySystem.Earn(s, fx.money, label);
            else if (fx.money < 0) EconomySystem.TrySpend(s, Mathf.Min(-fx.money, s.money), label);

            NeedsSystem.Apply(s, fx.energy, fx.hunger, fx.health, fx.mood, fx.hygiene);
            s.rep.nysc = Mathf.Clamp(s.rep.nysc + fx.repNysc, 0f, 100f);
            s.rep.ppa = Mathf.Clamp(s.rep.ppa + fx.repPpa, 0f, 100f);
            s.rep.community = Mathf.Clamp(s.rep.community + fx.repCommunity, 0f, 100f);
            s.AddFlag(fx.addFlag);
            s.RemoveFlag(fx.removeFlag);
            if (fx.skipMinutes > 0) s.minuteOfDay = Mathf.Min(s.minuteOfDay + fx.skipMinutes, 1439f);
        }
    }
}
