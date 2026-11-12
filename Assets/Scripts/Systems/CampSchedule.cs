using System.Collections.Generic;

namespace CorperLife
{
    public class ScheduleSlot
    {
        public int start;           // minute of day
        public int end;
        public string id;
        public string label;
        public string location;     // location id the player should be at, or null if free
        public bool mandatory;
        public float exertion = 1f;

        public ScheduleSlot(int start, int end, string id, string label, string location,
            bool mandatory, float exertion = 1f)
        {
            this.start = start;
            this.end = end;
            this.id = id;
            this.label = label;
            this.location = location;
            this.mandatory = mandatory;
            this.exertion = exertion;
        }
    }

    /// <summary>The fixed daily timetable of orientation camp.</summary>
    public static class CampSchedule
    {
        public const int CampDays = 21;
        public const int WakeMinute = 4 * 60 + 30;
        public const int LightsOutMinute = 22 * 60;
        public const int GraceMinutes = 25;

        public static readonly List<ScheduleSlot> Slots = new List<ScheduleSlot>
        {
            new ScheduleSlot(270, 300, "wake", "Bugle: wake up and fall in", Locations.Parade, true),
            new ScheduleSlot(300, 330, "devotion", "Morning devotion", Locations.Worship, false),
            new ScheduleSlot(330, 450, "drill", "Morning drill and parade", Locations.Parade, true, 2.2f),
            new ScheduleSlot(450, 540, "breakfast", "Breakfast", null, false),
            new ScheduleSlot(540, 780, "lectures", "Lectures in the multipurpose hall", Locations.Hall, true),
            new ScheduleSlot(780, 900, "lunch", "Lunch and rest", null, false),
            new ScheduleSlot(900, 1020, "saed", "SAED skills training", Locations.Saed, true),
            new ScheduleSlot(1020, 1140, "sports", "Sports and Man O' War", Locations.Field, false, 2.0f),
            new ScheduleSlot(1140, 1320, "social", "Social night", Locations.Hall, false),
            new ScheduleSlot(1320, 1440, "lightsout", "Lights out: go to your hostel", Locations.Hostel, false),
        };

        public static ScheduleSlot Current(float minuteOfDay)
        {
            foreach (var s in Slots)
                if (minuteOfDay >= s.start && minuteOfDay < s.end) return s;
            return null; // between midnight and 4:30 = sleeping hours
        }

        public static ScheduleSlot Next(float minuteOfDay)
        {
            foreach (var s in Slots)
                if (s.start > minuteOfDay) return s;
            return Slots[0];
        }

        /// <summary>Special days override the normal timetable label.</summary>
        public static string SpecialDay(int day)
        {
            switch (day)
            {
                case 1: return "Arrival and registration day";
                case 2: return "Kit collection day";
                case 3: return "Swearing-in ceremony";
                case 10: return "Inter-platoon football";
                case 14: return "Cultural night";
                case 18: return "Camp pageant night";
                case 20: return "Award night";
                case 21: return "Closing ceremony and posting letters";
                default: return null;
            }
        }
    }
}
