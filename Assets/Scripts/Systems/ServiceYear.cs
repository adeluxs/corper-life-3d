using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorperLife
{
    /// <summary>
    /// The service year after camp: PPA work, weekly CDS, monthly clearance and allawee,
    /// rent, side hustles, a CDS project, retention, passing out and the ending.
    ///
    /// Calendar (compressed so a full year is playable): 12 months x 6 days.
    ///   Day 1: rent is due, PPA work      Day 2: PPA work
    ///   Day 3: CDS day (10am-1pm)          Day 4: PPA work
    ///   Day 5: clearance at the LGI office (9am-3pm)
    ///   Day 6: allawee day, free day.  Month 12 day 6 is passing out.
    /// </summary>
    public class ServiceYearController
    {
        public const int Months = 12;
        public const int DaysPerMonth = 6;
        public const int WakeMinute = 6 * 60;

        readonly GameManager gm;
        GameState S => gm.State;
        UIManager UI => gm.UI;

        public ServiceYearController(GameManager gm) { this.gm = gm; }

        // ---------------- Calendar ----------------

        public int Month => (S.day - 1) / DaysPerMonth + 1;
        public int DayOfMonth => (S.day - 1) % DaysPerMonth + 1;
        public bool IsWorkDay => DayOfMonth == 1 || DayOfMonth == 2 || DayOfMonth == 4;
        public bool IsCdsDay => DayOfMonth == 3;
        public bool IsClearanceDay => DayOfMonth == 5;
        public bool IsAllaweeDay => DayOfMonth == 6;
        public bool IsPassingOutDay => Month >= Months && DayOfMonth == 6;

        public string PpaLocation => "ppa_" + (string.IsNullOrEmpty(S.ppaType) ? "school" : S.ppaType);

        public string DayLabel()
        {
            string kind = IsPassingOutDay ? "Passing out" : IsCdsDay ? "CDS day" : IsClearanceDay ? "Clearance day" :
                          IsAllaweeDay ? "Allawee day" : "PPA day";
            return "Month " + Mathf.Min(Month, Months) + ", day " + DayOfMonth + " - " + kind;
        }

        public static string PpaName(string type)
        {
            switch (type)
            {
                case "office": return "TechHub Limited (tech company)";
                case "hospital": return "General Hospital";
                case "lga": return "Local Government Secretariat";
                default: return "Government Secondary School";
            }
        }

        // ---------------- Money rules (in-game amounts) ----------------

        public int StateTopUp()
        {
            // Some states add to the federal allawee; each state is different.
            int h = Mathf.Abs((S.profile.deployedState ?? "").GetHashCode()) % 5;
            return h * 5000;
        }

        public int PpaStipend()
        {
            switch (S.ppaType)
            {
                case "office": return 30000;
                case "hospital": return 15000;
                case "lga": return 5000;
                default: return 0;
            }
        }

        public int Rent()
        {
            switch (S.housing)
            {
                case "selfcon": return 35000;
                case "shared": return 15000;
                default: return 0;   // corpers' lodge
            }
        }

        // ---------------- Starting the service year ----------------

        public void AssignPpa()
        {
            string[] types = { "school", "school", "office", "hospital", "lga" };
            // Medical and tech courses lean towards matching PPAs, like real postings sometimes do.
            string c = S.profile.course ?? "";
            if (c.Contains("Medicine") || c.Contains("Nursing")) S.ppaType = "hospital";
            else if (c.Contains("Computer") && UnityEngine.Random.value < 0.5f) S.ppaType = "office";
            else S.ppaType = types[UnityEngine.Random.Range(0, types.Length)];
        }

        public void Begin()
        {
            if (string.IsNullOrEmpty(S.ppaType)) AssignPpa();
            S.stage = LifeStage.ServiceYear;
            S.day = 1;
            S.minuteOfDay = 9 * 60;
            S.AddFlag("service_started");
            gm.World.LoadArea("town");
            gm.World.ApplyProfile(S.profile, false);
            gm.World.PlacePlayerAt("SPAWN_player");
            gm.ResetScheduleTracking();
            gm.Save();

            UI.Dialog("Welcome to " + S.profile.deployedState,
                "You arrive at the motor park with your bags. Your PPA is " + PpaName(S.ppaType) + ".\n\n" +
                "First, find somewhere to stay. The corpers' lodge is down the road to the south.\n\n" +
                "Each month: work at your PPA, attend CDS, sign clearance at the LGI office, then collect your allawee.",
                new DialogButton("Let's go", () => UI.Toast("Objective: go to the corpers' lodge", Palette.Accent)));
        }

        public string CurrentObjective()
        {
            if (!S.HasFlag("housed")) return "Find accommodation at the corpers' lodge";
            if (!S.HasFlag("ppa_reported")) return "Report to your PPA: " + PpaName(S.ppaType);
            int h = S.Hour;
            if (IsPassingOutDay) return h < 16 ? "Passing-out parade at the community hall" : "Your service year is over";
            if (IsCdsDay && !S.HasFlag(Key("cds")) && h < 13) return "CDS meeting at the community hall, 10am to 1pm";
            if (IsClearanceDay && !S.HasFlag(Key("cleared")) && h < 15) return "Sign clearance at the LGI office before 3pm";
            if (IsAllaweeDay && S.HasFlag(Key("cleared"))) return "Allawee has landed. Free day";
            if (IsWorkDay && !S.HasFlag(Key("worked")) && h < 15) return "Work at your PPA (8am to 3pm)";
            if (h >= 20 || h < 5) return "Go home and sleep";
            return "Free time: side hustle, eat, rest or explore";
        }

        string Key(string what) => "sy_" + S.day + "_" + what;

        // ---------------- Daily rhythm ----------------

        public void OnNewDay()
        {
            S.flags.RemoveAll(f => f.StartsWith("sy_" + (S.day - 2) + "_"));

            if (S.hasPos)
            {
                int take = UnityEngine.Random.Range(1500, 4500);
                EconomySystem.Earn(S, take, "POS stand profit");
                S.hustleEarnings += take;
            }

            if (DayOfMonth == 1 && Month > 1 && Month <= Months)
            {
                int rent = Rent();
                if (rent > 0)
                {
                    if (EconomySystem.TrySpend(S, rent, "Rent")) gm.UI.Toast("Rent paid: " + Palette.Naira(rent));
                    else
                    {
                        S.AddFlag("owes_rent");
                        NeedsSystem.Apply(S, mood: -10);
                        gm.UI.Toast("You couldn't pay rent. The landlord is not happy.", Palette.Danger);
                    }
                }
            }
        }

        /// <summary>Called every frame while the clock runs: checks attendance and pays allawee.</summary>
        public void Tick()
        {
            if (!S.HasFlag("housed")) return;
            float m = S.minuteOfDay;

            // Allawee lands at 9am on allawee day if you were cleared.
            if (IsAllaweeDay && m >= 9 * 60 && !S.HasFlag(Key("paid")))
            {
                S.AddFlag(Key("paid"));
                if (S.HasFlag("sy_" + (S.day - 1) + "_cleared"))
                {
                    int total = EconomySystem.FederalAllawee + StateTopUp();
                    EconomySystem.Earn(S, EconomySystem.FederalAllawee, "Federal allawee");
                    if (StateTopUp() > 0) EconomySystem.Earn(S, StateTopUp(), "State allowance");
                    if (PpaStipend() > 0) { EconomySystem.Earn(S, PpaStipend(), "PPA stipend"); total += PpaStipend(); }
                    NeedsSystem.Apply(S, mood: 15);
                    UI.Dialog("Bank alert!", "Credit: " + Palette.Naira(total) + "\n\nAllawee for month " + Month +
                                             " has landed. Spend it wisely.",
                        new DialogButton("Alhamdulillah / Thank God!", null));
                }
                else
                {
                    NeedsSystem.Apply(S, mood: -15);
                    UI.Dialog("No allawee", "You did not sign clearance this month, so your allawee was not paid.",
                        new DialogButton("Painful", null));
                }
            }

            if (IsPassingOutDay)
            {
                if (m >= 16 * 60 && !S.HasFlag("passed_out")) PassingOut(false);
                return;
            }

            // PPA attendance: the boss checks at 10am.
            if (IsWorkDay && S.HasFlag("ppa_reported") && m >= 10 * 60 && !S.HasFlag(Key("ppa_checked")))
            {
                S.AddFlag(Key("ppa_checked"));
                if (!S.HasFlag(Key("worked")) && gm.CurrentLocation != PpaLocation)
                {
                    S.ppaDaysMissed++;
                    S.rep.ppa = Mathf.Max(0, S.rep.ppa - 5);
                    UI.Toast("Your PPA boss marked you absent today.", Palette.Danger);
                }
            }

            // CDS is checked at 11am.
            if (IsCdsDay && m >= 11 * 60 && !S.HasFlag(Key("cds_checked")))
            {
                S.AddFlag(Key("cds_checked"));
                if (!S.HasFlag(Key("cds")) && gm.CurrentLocation != "cds")
                {
                    S.missedCds++;
                    S.rep.nysc = Mathf.Max(0, S.rep.nysc - 5);
                    UI.Toast("You missed CDS. Your LGI officer noted it.", Palette.Danger);
                }
            }

            // Clearance closes at 3pm.
            if (IsClearanceDay && m >= 15 * 60 && !S.HasFlag(Key("clear_checked")))
            {
                S.AddFlag(Key("clear_checked"));
                if (!S.HasFlag(Key("cleared")))
                {
                    S.missedClearances++;
                    S.rep.nysc = Mathf.Max(0, S.rep.nysc - 8);
                    UI.Dialog("Clearance missed",
                        "The LGI office has closed and you did not sign. Your allawee for this month is withheld." +
                        (S.missedClearances >= 2 ? "\n\nWarning: missing clearance again can get your service extended." : ""),
                        new DialogButton("OK", null));
                }
            }
        }

        // ---------------- Actions by location ----------------

        public List<LocationAction> ActionsFor(string loc)
        {
            var list = new List<LocationAction>();
            int h = S.Hour;

            if (loc == "lodge")
            {
                if (!S.HasFlag("housed"))
                    list.Add(Act("house", "Find a place to stay", gm2 => ChooseHousing()));
                else
                {
                    list.Add(Simple("rest", "Rest for an hour", 60, energy: 12, mood: 3, done: "You feel better."));
                    list.Add(Simple("bath", "Bath and wash clothes", 40, hygiene: 50, mood: 4, energy: -3, done: "Fresh and clean."));
                    list.Add(Simple("cook", "Cook noodles and egg (N900)", 30, hunger: 35, mood: 3, cost: 900, done: "Corper's special."));
                    if (!S.HasFlag(Key("hustle")))
                        list.Add(Act("hustle", "Side hustle: " + HustleName() + " (3 hours)", gm2 => DoHustle()));
                    if (h >= 20 || h < 5)
                        list.Add(Act("sleep", "Sleep till morning", gm2 => gm2.SleepUntilMorning()));
                    if (S.HasFlag("owes_rent"))
                        list.Add(Act("payrent", "Pay rent you owe (" + Palette.Naira(Rent()) + ")", gm2 => PayRentOwed()));
                }
            }
            else if (loc == PpaLocation)
            {
                if (!S.HasFlag("ppa_reported"))
                    list.Add(Act("report", "Report to your PPA", gm2 => ReportToPpa()));
                else if (IsWorkDay && h >= 8 && h < 15 && !S.HasFlag(Key("worked")))
                    list.Add(Act("work", "Start your work shift (" + WorkName() + ")", gm2 => WorkShift()));
                else if (S.HasFlag(Key("worked")))
                    list.Add(Act("extra", "Help out with extra work (1 hour)", gm2 => ExtraWork()));
            }
            else if (loc.StartsWith("ppa_"))
            {
                // Someone else's PPA: you can visit, nothing to do.
            }
            else if (loc == "cds")
            {
                if (IsPassingOutDay && h >= 8 && !S.HasFlag("passed_out"))
                    list.Add(Act("passout", "Join the passing-out parade", gm2 => PassingOut(true)));
                else if (IsCdsDay && h >= 10 && h < 13 && !S.HasFlag(Key("cds")))
                    list.Add(Act("cds", "Attend your CDS group meeting", gm2 => AttendCds()));
                if (Month >= 3 && !S.HasFlag("cds_project_done"))
                {
                    if (!S.HasFlag("cds_project")) list.Add(Act("project", "Start a personal CDS project", gm2 => StartProject()));
                    else list.Add(Act("projectwork", "Work on your CDS project (" + S.projectProgress + "%)", gm2 => WorkOnProject()));
                }
            }
            else if (loc == "lgi")
            {
                if (IsClearanceDay && h >= 9 && h < 15 && !S.HasFlag(Key("cleared")))
                    list.Add(Act("clear", "Sign the clearance register", gm2 => SignClearance()));
                list.Add(Act("lgi_talk", "Talk to your LGI officer", gm2 => TalkLgi()));
            }
            else if (loc == "buka")
            {
                list.Add(Simple("amala", "Amala, ewedu and meat", 30, hunger: 45, mood: 8, cost: 1800, done: "Mama T's food is something else."));
                list.Add(Simple("rice", "Jollof rice and chicken", 30, hunger: 50, mood: 9, cost: 2500, done: "Party jollof on a normal day."));
                list.Add(Simple("beans", "Beans and dodo", 25, hunger: 40, mood: 5, cost: 1200, done: "Filling and cheap."));
            }
            else if (loc == "market")
            {
                list.Add(Simple("foodstuff", "Buy foodstuff for the week", 40, hunger: 20, mood: 3, cost: 6000, done: "Rice, beans, garri, oil. You're stocked."));
                list.Add(Simple("clothes", "Buy a new outfit", 40, mood: 14, cost: 8000, done: "You look sharp."));
                if (!S.hasPos)
                    list.Add(Act("pos", "Start a POS stand (" + Palette.Naira(60000) + ")", gm2 => StartPos()));
                list.Add(Simple("charge", "Charge phone at a shop", 40, mood: 6, cost: 200, done: "Fully charged."));
            }
            else if (loc == "bank")
            {
                list.Add(Act("balance", "Check your balance and history", gm2 => ShowBank()));
            }
            else if (loc == "worship")
            {
                list.Add(Simple("pray", "Pray and reflect", 30, mood: 9, done: "You feel at peace."));
            }
            else if (loc == "park")
            {
                list.Add(Act("travel", "Travel home for a visit (" + Palette.Naira(15000) + ")", gm2 => TravelHome()));
            }
            else if (loc == "street")
            {
                list.Add(Simple("suya", "Buy suya from the mallam", 15, hunger: 20, mood: 8, cost: 1500, done: "Spicy and perfect."));
            }
            return list;
        }

        LocationAction Act(string id, string label, Action<GameManager> run) =>
            new LocationAction { id = id, label = label, run = run };

        LocationAction Simple(string id, string label, int minutes, float energy = 0, float hunger = 0, float mood = 0,
            float hygiene = 0, int cost = 0, string done = null)
        {
            return new LocationAction
            {
                id = id, label = label, minutes = minutes, cost = cost, doneText = done,
                effects = new EventEffects { energy = energy, hunger = hunger, mood = mood, hygiene = hygiene }
            };
        }

        // ---------------- Housing ----------------

        void ChooseHousing()
        {
            UI.Dialog("Where will you stay?",
                "The corpers' lodge has rooms. You can share a free room, rent a shared room, or rent your own self-contain.",
                new DialogButton("Free lodge room (4 corpers per room)", () => SetHousing("lodge")),
                new DialogButton("Shared room, " + Palette.Naira(15000) + " a month", () => SetHousing("shared"), S.money >= 15000),
                new DialogButton("Self-contain, " + Palette.Naira(35000) + " a month", () => SetHousing("selfcon"), S.money >= 35000));
        }

        void SetHousing(string type)
        {
            S.housing = type;
            S.AddFlag("housed");
            int rent = Rent();
            if (rent > 0) EconomySystem.TrySpend(S, rent, "First month's rent");
            string text = type == "lodge"
                ? "You share a room with three other corpers. It's free, but it's crowded and the generator noise is real."
                : type == "shared"
                    ? "You share with one other corper. Quieter, and you can actually sleep."
                    : "Your own self-contain. Peace and quiet, but rent is heavy.";
            UI.Dialog("Settled in", text + "\n\nNext: report to your PPA, " + PpaName(S.ppaType) + ".",
                new DialogButton("OK", null));
        }

        void PayRentOwed()
        {
            if (EconomySystem.TrySpend(S, Rent(), "Rent arrears"))
            {
                S.RemoveFlag("owes_rent");
                UI.Toast("Rent cleared. The landlord is smiling again.");
            }
            else UI.Toast("Not enough money yet.", Palette.Danger);
        }

        /// <summary>Sleep quality depends on where you live.</summary>
        public float SleepQuality() => S.housing == "selfcon" ? 1.15f : S.housing == "shared" ? 1.0f : 0.85f;

        // ---------------- PPA ----------------

        string WorkName()
        {
            switch (S.ppaType)
            {
                case "office": return "coding and support";
                case "hospital": return "ward duty";
                case "lga": return "files and errands";
                default: return "teaching";
            }
        }

        void ReportToPpa()
        {
            S.AddFlag("ppa_reported");
            gm.AdvanceTime(60);
            string text;
            switch (S.ppaType)
            {
                case "office":
                    text = "The HR manager looks at your CV. \"We need someone on the support team. You start tomorrow, 8am sharp.\"";
                    break;
                case "hospital":
                    text = "The Chief Medical Director welcomes you. \"We are short-staffed. You will rotate through the wards.\"";
                    break;
                case "lga":
                    text = "The secretary yawns. \"Corper, sit there. If there is work we will call you.\"";
                    break;
                default:
                    text = "The principal is happy. \"We have no teacher for SS2 " + TeachSubject() + ". The class is yours.\"";
                    break;
            }
            UI.Dialog("Reporting at your PPA", text + "\n\nWork days: day 1, 2 and 4 of each month, 8am to 3pm.",
                new DialogButton("OK", null));
        }

        string TeachSubject()
        {
            string c = S.profile.course ?? "";
            if (c.Contains("Computer")) return "Computer Studies";
            if (c.Contains("Account") || c.Contains("Econ") || c.Contains("Business") || c.Contains("Actuarial")) return "Economics";
            if (c.Contains("English") || c.Contains("Mass")) return "English";
            if (c.Contains("Agric")) return "Agricultural Science";
            return "Mathematics";
        }

        void WorkShift()
        {
            S.AddFlag(Key("worked"));
            S.ppaDaysWorked++;
            float quality = (S.needs.energy + S.needs.mood + S.needs.health) / 300f;   // 0..1
            gm.AdvanceTime(240, 1.3f);
            float gain = Mathf.Lerp(1f, 5f, quality);
            S.rep.ppa = Mathf.Clamp(S.rep.ppa + gain, 0, 100);
            NeedsSystem.Apply(S, energy: -14, mood: quality > 0.6f ? 2 : -4);
            string[] good =
            {
                "Productive day. Your boss nodded at your work.",
                "You solved a problem nobody else could. People noticed.",
                "Busy but satisfying shift.",
            };
            string[] bad =
            {
                "You were tired and slow today. Get some rest.",
                "Hard day. You made a mistake your boss had to fix.",
            };
            UI.Toast(quality > 0.55f ? good[UnityEngine.Random.Range(0, good.Length)] : bad[UnityEngine.Random.Range(0, bad.Length)],
                quality > 0.55f ? Palette.Accent : Palette.Warn);

            // Late in the year, good PPAs may offer to retain you.
            if (Month >= 9 && !S.HasFlag("retention_offered") && S.rep.ppa >= 70 && (S.ppaType == "office" || S.ppaType == "hospital"))
            {
                S.AddFlag("retention_offered");
                int salary = S.ppaType == "office" ? 250000 : 180000;
                UI.Dialog("Retention offer!",
                    "Your boss calls you in. \"We like your work. After passing out, we want to retain you at " +
                    Palette.Naira(salary) + " a month.\"",
                    new DialogButton("Accept the offer", () => { S.AddFlag("accepted_retention"); NeedsSystem.Apply(S, mood: 25); }),
                    new DialogButton("Ask for more", () =>
                    {
                        if (UnityEngine.Random.value < 0.5f) { S.AddFlag("accepted_retention"); S.AddFlag("negotiated"); UI.Toast("They agreed to a higher salary!"); }
                        else UI.Toast("\"That's our best offer. Think about it.\"");
                    }),
                    new DialogButton("Decline, I have other plans", () => S.AddFlag("declined_retention")));
            }
        }

        void ExtraWork()
        {
            gm.AdvanceTime(60, 1.2f);
            S.rep.ppa = Mathf.Clamp(S.rep.ppa + 1f, 0, 100);
            NeedsSystem.Apply(S, energy: -5);
            UI.Toast("Extra effort noted.");
        }

        // ---------------- CDS ----------------

        void AttendCds()
        {
            S.AddFlag(Key("cds"));
            S.cdsAttended++;
            gm.AdvanceTime(150);
            S.rep.nysc = Mathf.Clamp(S.rep.nysc + 3, 0, 100);
            S.rep.community = Mathf.Clamp(S.rep.community + 2, 0, 100);
            NeedsSystem.Apply(S, mood: 4, energy: -6);
            string[] what =
            {
                "Your group cleaned the market drainage. The traders cheered.",
                "You ran a free health awareness talk at the community hall.",
                "Your group taught primary pupils basic computer skills.",
                "Anti-corruption group: you visited schools to talk about integrity.",
            };
            UI.Toast(what[UnityEngine.Random.Range(0, what.Length)], Palette.Accent);
        }

        void StartProject()
        {
            UI.Dialog("Personal CDS project",
                "A personal project can earn you a commendation, or even a state honours award. Pick one:",
                new DialogButton("Borehole for the community", () => BeginProject("borehole")),
                new DialogButton("Library for the school", () => BeginProject("library")),
                new DialogButton("Free health outreach", () => BeginProject("outreach")),
                new DialogButton("Not now", null));
        }

        void BeginProject(string kind)
        {
            S.AddFlag("cds_project");
            S.projectKind = kind;
            S.projectProgress = 0;
            UI.Toast("Project started. Work on it at the community hall and raise funds.");
        }

        void WorkOnProject()
        {
            UI.Dialog("Work on your project",
                "Progress: " + S.projectProgress + "%. How do you want to push it forward?",
                new DialogButton("Visit sponsors and raise funds (3 hours)", () =>
                {
                    gm.AdvanceTime(180, 1.2f);
                    int add = UnityEngine.Random.Range(8, 20) + Mathf.RoundToInt(S.rep.community / 10f);
                    AddProgress(add);
                    NeedsSystem.Apply(S, energy: -10);
                }),
                new DialogButton("Put in your own money (" + Palette.Naira(10000) + ")", () =>
                {
                    if (EconomySystem.TrySpend(S, 10000, "CDS project")) AddProgress(15);
                }, S.money >= 10000),
                new DialogButton("Later", null));
        }

        void AddProgress(int add)
        {
            S.projectProgress = Mathf.Min(100, S.projectProgress + add);
            if (S.projectProgress >= 100 && !S.HasFlag("cds_project_done"))
            {
                S.AddFlag("cds_project_done");
                S.rep.community = Mathf.Clamp(S.rep.community + 25, 0, 100);
                S.rep.nysc = Mathf.Clamp(S.rep.nysc + 10, 0, 100);
                string what = S.projectKind == "borehole" ? "borehole" : S.projectKind == "library" ? "library" : "health outreach";
                UI.Dialog("Project commissioned!",
                    "The community gathers as your " + what + " is commissioned. The chairman thanks you publicly.\n\nYour name is on the plaque.",
                    new DialogButton("Proud moment", null));
            }
            else UI.Toast("Project progress: " + S.projectProgress + "%");
        }

        // ---------------- Clearance ----------------

        void SignClearance()
        {
            gm.AdvanceTime(45);
            if (S.rep.ppa < 25)
            {
                UI.Dialog("Clearance refused",
                    "Your LGI officer looks at your PPA letter. \"Your PPA says you are always absent. Go and settle with them first.\"",
                    new DialogButton("Beg the officer", () =>
                    {
                        if (UnityEngine.Random.value < 0.4f) { Clear(); UI.Toast("\"Last warning, corper.\""); }
                        else UI.Toast("\"Go and do your work.\"", Palette.Danger);
                    }),
                    new DialogButton("Leave", null));
                return;
            }
            Clear();
            UI.Toast("Clearance signed. Allawee tomorrow!", Palette.Accent);
        }

        void Clear()
        {
            S.AddFlag(Key("cleared"));
            S.clearancesSigned++;
        }

        void TalkLgi()
        {
            gm.AdvanceTime(15);
            string msg = S.missedClearances > 0 || S.missedCds > 2
                ? "\"Corper, your record is not good. Missed clearance: " + S.missedClearances + ", missed CDS: " + S.missedCds + ". Be careful.\""
                : "\"You are doing well. Keep attending CDS and clearance, and your certificate is sure.\"";
            UI.Dialog("LGI officer", msg + "\n\nPPA: " + PpaName(S.ppaType) + "\nMonth " + Month + " of " + Months,
                new DialogButton("OK", null));
        }

        // ---------------- Money makers ----------------

        string HustleName()
        {
            switch (S.skill)
            {
                case "Tailoring": return "sewing clothes";
                case "Baking and catering": return "baking small chops";
                case "ICT and graphics": return "graphics and website jobs";
                case "Agro-farming": return "selling farm produce";
                case "Cosmetology": return "make-up and hair";
                case "Photography": return "event photography";
                default: return "tutoring students";
            }
        }

        void DoHustle()
        {
            S.AddFlag(Key("hustle"));
            gm.AdvanceTime(180, 1.1f);
            int baseIncome;
            switch (S.skill)
            {
                case "ICT and graphics": baseIncome = 7000; break;
                case "Photography": baseIncome = 6000; break;
                case "Tailoring": case "Cosmetology": baseIncome = 5000; break;
                case "Baking and catering": baseIncome = 4500; break;
                case "Agro-farming": baseIncome = 3500; break;
                default: baseIncome = 3000; break;
            }
            S.hustleLevel++;
            float growth = 1f + Mathf.Min(S.hustleLevel, 30) * 0.05f;   // customers grow as you keep at it
            int income = Mathf.RoundToInt(baseIncome * growth * UnityEngine.Random.Range(0.6f, 1.4f) / 100f) * 100;
            EconomySystem.Earn(S, income, "Side hustle");
            S.hustleEarnings += income;
            NeedsSystem.Apply(S, energy: -10, mood: 5);
            UI.Toast("Side hustle: +" + Palette.Naira(income) + " (" + HustleName() + ")", Palette.Accent);
        }

        void StartPos()
        {
            UI.Dialog("POS stand",
                "Rent a spot, buy a POS machine and float cash. It earns every day, but fake alerts and robbers are real risks.",
                new DialogButton("Invest " + Palette.Naira(60000), () =>
                {
                    if (EconomySystem.TrySpend(S, 60000, "POS stand setup"))
                    {
                        S.hasPos = true;
                        UI.Toast("Your POS stand is open. It earns daily.");
                    }
                }, S.money >= 60000),
                new DialogButton("Not now", null));
        }

        void ShowBank()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Balance: ").Append(Palette.Naira(S.money)).Append("\n\nRecent:\n");
            int shown = 0;
            for (int i = S.transactions.Count - 1; i >= 0 && shown < 6; i--, shown++)
            {
                var t = S.transactions[i];
                sb.Append(t.amount >= 0 ? "+" : "").Append(Palette.Naira(t.amount)).Append("  ").Append(t.label).Append('\n');
            }
            UI.Dialog("Your bank app", sb.ToString(), new DialogButton("Close", null));
        }

        void TravelHome()
        {
            UI.Dialog("Travel home?",
                "A trip home to " + S.profile.homeState + " takes a full day each way. You will miss a day at your PPA.",
                new DialogButton("Go (" + Palette.Naira(15000) + ")", () =>
                {
                    if (!EconomySystem.TrySpend(S, 15000, "Transport home")) return;
                    gm.AdvanceTime(600, 1f);
                    NeedsSystem.Apply(S, mood: 30, hunger: 40, energy: -10);
                    int gift = S.profile.background == "Rich" ? 30000 : S.profile.background == "Average" ? 10000 : 0;
                    if (gift > 0) EconomySystem.Earn(S, gift, "Gift from family");
                    UI.Dialog("Home sweet home", "Mum cooked your favourite. Everyone wants to hear about your PPA." +
                                                 (gift > 0 ? "\n\nThey gave you " + Palette.Naira(gift) + " for the road." : ""),
                        new DialogButton("Back to service", null));
                }, S.money >= 15000),
                new DialogButton("Not now", null));
        }

        // ---------------- Passing out and endings ----------------

        public void PassingOut(bool attended)
        {
            if (S.HasFlag("passed_out")) return;
            S.AddFlag("passed_out");
            if (attended) gm.AdvanceTime(180, 1.2f);
            S.stage = LifeStage.PassingOut;

            var ending = ComputeEnding(attended);
            S.ending = ending.id;
            gm.Save();

            string stats =
                "PPA days worked: " + S.ppaDaysWorked + "   Missed: " + S.ppaDaysMissed + "\n" +
                "CDS attended: " + S.cdsAttended + "   Missed: " + S.missedCds + "\n" +
                "Clearances: " + S.clearancesSigned + "   Missed: " + S.missedClearances + "\n" +
                "Side hustle earnings: " + Palette.Naira(S.hustleEarnings) + "\n" +
                "Money at the end: " + Palette.Naira(S.money);

            UI.Dialog(attended ? "Passing out!" : "Passing out (you missed the parade)",
                (attended ? "The band plays and your platoon cheers as you collect your certificate.\n\n" : "") +
                "YOUR ENDING: " + ending.title + "\n" + ending.text + "\n\n" + stats,
                new DialogButton("Share my ending on WhatsApp", () => ShareEnding(ending)),
                new DialogButton("Start a new service year", () => { SaveSystem.Delete(); gm.RestartToMenu(); }),
                new DialogButton("Keep exploring", null));
            AdService.MaybeShowInterstitial("passing_out");
        }

        public struct Ending
        {
            public string id, title, text;
        }

        public Ending ComputeEnding(bool attendedParade)
        {
            if (S.missedClearances >= 3 || S.missedCds >= 6 || S.ppaDaysMissed >= 14)
                return new Ending { id = "extended", title = "Service extended", text = "Too many missed clearances and absences. Your certificate is withheld and your service is extended by some months." };
            if (S.HasFlag("accepted_retention"))
                return new Ending { id = "retained", title = "Retained by your PPA", text = "You walk straight into a job at " + PpaName(S.ppaType) + ". No unemployment for you." };
            if (S.HasFlag("cds_project_done") && S.rep.community >= 85 && S.rep.nysc >= 80)
                return new Ending { id = "award", title = "State Honours Award", text = "Your CDS project earned you a state award and a federal job offer." };
            if (S.hustleEarnings >= 250000 || (S.hasPos && S.money >= 200000))
                return new Ending { id = "business", title = "Business owner", text = "Your side hustle grew into a real business. You are now your own boss." };
            if (S.money >= 400000)
                return new Ending { id = "masters", title = "Master's abroad", text = "You saved enough to apply for a master's degree abroad. Japa loading!" };
            if (S.rep.nysc >= 80)
                return new Ending { id = "govjob", title = "Government job", text = "Your excellent NYSC record helped you land a government job." };
            return new Ending { id = "hunting", title = "Job hunting", text = "Certificate in hand, CV ready. The job hunt begins. You've got this." };
        }

        void ShareEnding(Ending e)
        {
            string text = "I just finished my service year in Corper Wee! My ending: " + e.title +
                          ". Play it free: https://play.google.com/store/apps/details?id=com.swiftmynd.corperwee";
            Application.OpenURL("https://wa.me/?text=" + Uri.EscapeDataString(text));
        }
    }
}
