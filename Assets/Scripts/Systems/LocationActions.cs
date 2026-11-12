using System;
using System.Collections.Generic;

namespace CorperLife
{
    public class LocationAction
    {
        public string id;
        public string label;
        public int cost;
        public int minutes;
        public float exertion = 1f;
        public Action<GameManager> run;     // custom behaviour (mini-games, menus)
        public EventEffects effects;        // simple effects when run is null
        public string doneText;
    }

    /// <summary>What the player can do at each camp location, depending on time and progress.</summary>
    public static class LocationActions
    {
        static LocationAction Simple(string id, string label, int cost, int minutes, EventEffects fx,
            string done, float exertion = 1f)
        {
            return new LocationAction
            {
                id = id, label = label, cost = cost, minutes = minutes, effects = fx,
                doneText = done, exertion = exertion
            };
        }

        static EventEffects Fx(float energy = 0, float hunger = 0, float health = 0, float mood = 0,
            float hygiene = 0, float repNysc = 0, float repCommunity = 0)
        {
            return new EventEffects
            {
                energy = energy, hunger = hunger, health = health, mood = mood, hygiene = hygiene,
                repNysc = repNysc, repCommunity = repCommunity
            };
        }

        public static List<LocationAction> For(string loc, GameState s)
        {
            var list = new List<LocationAction>();
            var slot = CampSchedule.Current(s.minuteOfDay);
            string slotId = slot?.id;
            int hour = s.Hour;

            switch (loc)
            {
                case Locations.Office:
                    if (!s.HasFlag("registered"))
                        list.Add(new LocationAction
                        {
                            id = "register", label = "Register (documents and medical check)", minutes = 90,
                            run = gm => gm.Story.Register()
                        });
                    else if (s.day >= 2 && !s.HasFlag("kit_collected"))
                        list.Add(new LocationAction
                        {
                            id = "kit", label = "Collect your NYSC kit", minutes = 60,
                            run = gm => gm.Story.CollectKit()
                        });
                    if (s.HasFlag("registered") && string.IsNullOrEmpty(s.campRole) && s.day >= 4)
                        list.Add(new LocationAction
                        {
                            id = "role", label = "Volunteer for a camp role", minutes = 20,
                            run = gm => gm.Story.ChooseCampRole()
                        });
                    list.Add(new LocationAction
                    {
                        id = "callhome", label = "Call home", minutes = 15,
                        run = gm => gm.Story.CallHome()
                    });
                    break;

                case Locations.Market:
                    list.Add(Simple("rice", "Rice, stew and plantain", 1500, 25,
                        Fx(hunger: 45, mood: 6), "That food hit the spot."));
                    list.Add(Simple("bread", "Bread, egg and tea", 800, 15,
                        Fx(hunger: 25, energy: 4), "Quick breakfast done."));
                    list.Add(Simple("suya", "Suya and a cold drink", 2000, 20,
                        Fx(hunger: 30, mood: 12), "Mallam's suya is the best in camp."));
                    list.Add(Simple("energy", "Energy drink", 600, 5,
                        Fx(energy: 15, health: -1), "You feel wide awake."));
                    list.Add(Simple("charge", "Charge your phone", 200, 40,
                        Fx(mood: 7), "Phone at 100%. You checked in with everyone."));
                    list.Add(Simple("barber", "Barber or hair stylist", 1000, 40,
                        Fx(mood: 10, hygiene: 6), "Fresh look."));
                    list.Add(Simple("laundry", "Pay for laundry", 700, 10,
                        Fx(hygiene: 12, mood: 3), "Your whites are white again."));
                    break;

                case Locations.Kitchen:
                    bool mealTime = slotId == "breakfast" || slotId == "lunch" || (hour >= 18 && hour < 20);
                    if (mealTime)
                        list.Add(Simple("campfood", "Queue for camp food (free)", 0, 35,
                            Fx(hunger: 35, mood: -2), "It wasn't sweet, but you're full."));
                    break;

                case Locations.Hostel:
                    if (s.HasFlag("registered"))
                    {
                        list.Add(Simple("rest", "Lie down for an hour", 0, 60,
                            Fx(energy: 12, mood: 3), "You feel a bit better."));
                        list.Add(Simple("bath", "Fetch water and bathe", 0, 30,
                            Fx(hygiene: 45, mood: 4, energy: -3), "Clean and fresh."));
                        if (hour >= 21 || hour < 4)
                            list.Add(new LocationAction
                            {
                                id = "sleep", label = "Sleep until the bugle",
                                run = gm => gm.SleepUntilMorning()
                            });
                    }
                    break;

                case Locations.Clinic:
                    list.Add(Simple("nurse", "See the camp nurse (free)", 0, 60,
                        Fx(health: 18, mood: 2), "The nurse checked you and gave you paracetamol."));
                    list.Add(Simple("malaria", "Buy malaria treatment", 2500, 20,
                        Fx(health: 35), "Course of drugs started."));
                    break;

                case Locations.Worship:
                    list.Add(Simple("pray", "Pray and reflect", 0, 25,
                        Fx(mood: 9, repCommunity: 0.5f), "You feel calmer."));
                    break;

                case Locations.Parade:
                    if (slotId == "drill" && !s.HasFlag(AttendKey(s, "drill")))
                        list.Add(new LocationAction
                        {
                            id = "drill", label = "Fall in with your platoon",
                            run = gm => gm.StartDrill()
                        });
                    break;

                case Locations.Hall:
                    if (slotId == "lectures" && !s.HasFlag(AttendKey(s, "lectures")))
                        list.Add(new LocationAction
                        {
                            id = "lecture", label = "Sit for today's lecture",
                            run = gm => gm.StartLecture()
                        });
                    if (slotId == "social")
                        list.Add(Simple("social", "Join the social night", 0, 90,
                            Fx(mood: 16, energy: -6, repCommunity: 1), "Great night with your platoon."));
                    break;

                case Locations.Saed:
                    if (slotId == "saed" && !s.HasFlag(AttendKey(s, "saed")))
                        list.Add(new LocationAction
                        {
                            id = "saed", label = string.IsNullOrEmpty(s.skill) ? "Choose a SAED skill" : "Attend " + s.skill + " training",
                            run = gm => gm.Story.AttendSaed()
                        });
                    break;

                case Locations.Field:
                    list.Add(Simple("football", "Play football", 0, 60,
                        Fx(energy: -15, mood: 14, hygiene: -12, repCommunity: 1), "You scored one! The platoon is hyped.", 2f));
                    if (slotId == "sports")
                        list.Add(Simple("manowar", "Man O' War obstacle course", 0, 75,
                            Fx(energy: -22, mood: 8, hygiene: -18, repNysc: 2), "Crawled, climbed, survived.", 2.5f));
                    break;

                case Locations.Gate:
                    list.Add(new LocationAction
                    {
                        id = "sneak", label = "Try to go out of camp",
                        run = gm => gm.Story.TryLeaveCamp()
                    });
                    break;
            }
            return list;
        }

        public static string AttendKey(GameState s, string slotId) => "att_" + s.day + "_" + slotId;
    }
}
