using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorperLife
{
    public enum LifeStage
    {
        CharacterCreation = 0,
        Camp = 1,
        Posting = 2,
        ServiceYear = 3,
        PassingOut = 4,
        Ended = 5
    }

    [Serializable]
    public class Needs
    {
        public float energy = 90f;
        public float hunger = 80f;   // 100 = full, 0 = starving
        public float health = 95f;
        public float mood = 75f;
        public float hygiene = 85f;

        public void ClampAll()
        {
            energy = Mathf.Clamp(energy, 0f, 100f);
            hunger = Mathf.Clamp(hunger, 0f, 100f);
            health = Mathf.Clamp(health, 0f, 100f);
            mood = Mathf.Clamp(mood, 0f, 100f);
            hygiene = Mathf.Clamp(hygiene, 0f, 100f);
        }
    }

    [Serializable]
    public class Reputation
    {
        public float nysc = 50f;       // NYSC record: attendance, clearance, CDS
        public float ppa = 50f;        // PPA performance
        public float community = 50f;  // community standing
    }

    [Serializable]
    public class PlayerProfile
    {
        public string firstName = "Tunde";
        public string lastName = "Adeyemi";
        public bool isFemale = false;
        public int skinTone = 2;          // index into Palette.SkinTones
        public int hairStyle = 0;
        public string course = "Computer Science";
        public string homeState = "Lagos";
        public string background = "Average";   // Rich, Average, Struggling
        public string deployedState = "Oyo";
        public string callUpNumber = "";

        public string FullName => firstName + " " + lastName;
    }

    [Serializable]
    public class Transaction
    {
        public int day;
        public int minute;
        public int amount;      // positive = income, negative = spend
        public string label;
    }

    [Serializable]
    public class GameState
    {
        public int saveVersion = 1;
        public PlayerProfile profile = new PlayerProfile();
        public LifeStage stage = LifeStage.CharacterCreation;

        // Time: day 1 is the first day of camp. minuteOfDay 0..1439
        public int day = 1;
        public float minuteOfDay = 4 * 60 + 30;

        public int money = 25000;
        public Needs needs = new Needs();
        public Reputation rep = new Reputation();
        public int platoon = 1;

        public List<string> flags = new List<string>();
        public List<string> inventory = new List<string>();
        public List<string> seenEvents = new List<string>();
        public List<Transaction> transactions = new List<Transaction>();

        // Camp bookkeeping
        public int drillsAttended;
        public int drillsMissed;
        public int lecturesPassed;
        public int timesLate;
        public string skill = "";            // SAED skill picked at camp
        public string campRole = "";          // platoon leader, OBS, band...

        public bool HasFlag(string f) => flags.Contains(f);

        public void AddFlag(string f)
        {
            if (!string.IsNullOrEmpty(f) && !flags.Contains(f)) flags.Add(f);
        }

        public void RemoveFlag(string f)
        {
            if (!string.IsNullOrEmpty(f)) flags.Remove(f);
        }

        public int Hour => Mathf.FloorToInt(minuteOfDay / 60f);
        public int Minute => Mathf.FloorToInt(minuteOfDay % 60f);

        public string ClockText()
        {
            int h = Hour;
            int m = Minute;
            string ampm = h >= 12 ? "pm" : "am";
            int h12 = h % 12;
            if (h12 == 0) h12 = 12;
            return h12 + ":" + m.ToString("00") + " " + ampm;
        }
    }
}
