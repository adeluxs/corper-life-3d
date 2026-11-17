using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorperLife
{
    /// <summary>
    /// Runs the simulation: time, needs, timetable, attendance, random events and saving.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        // 1 real second = this many in-game minutes. A camp day (4:30am-10pm) is about 12 real minutes.
        public const float MinutesPerSecond = 1.5f;

        public GameState State { get; private set; }
        public LifeEventSystem Events { get; private set; }
        public StoryController Story { get; private set; }
        public ServiceYearController Year { get; private set; }
        public UIManager UI { get; private set; }
        public WorldBuilder World { get; private set; }

        public string CurrentLocation { get; private set; }
        public bool Running { get; private set; }
        public bool Paused => !Running || UI == null || UI.IsModalOpen;

        int lastHour = -1;
        string lastSlotId;
        readonly HashSet<string> checkedAttendance = new HashSet<string>();
        float autosaveTimer;
        bool fainting;

        public void Init(UIManager ui, WorldBuilder world)
        {
            I = this;
            UI = ui;
            World = world;
            Events = new LifeEventSystem();
            Events.LoadAll();
            Story = new StoryController(this);
            Year = new ServiceYearController(this);
        }

        // ---------- Starting ----------

        public void NewGame(PlayerProfile profile)
        {
            State = new GameState
            {
                profile = profile,
                stage = LifeStage.Camp,
                day = 1,
                minuteOfDay = 10 * 60,      // arrive at the gate around 10am
                platoon = Random.Range(1, 11),
            };
            State.money = EconomySystem.StartingCash(profile.background);
            State.transactions.Clear();
            EconomySystem.Earn(State, 0, "Opening balance");
            BeginPlay();
            World.PlacePlayerAt("SPAWN_player");
            Story.OnArrival();
        }

        public void ContinueGame(GameState loaded)
        {
            State = loaded;
            if (State.stage == LifeStage.Posting)
            {
                BeginPlay();
                Year.Begin();
                return;
            }
            if (State.stage == LifeStage.ServiceYear || State.stage == LifeStage.PassingOut)
            {
                World.LoadArea("town");
                BeginPlay();
                World.PlacePlayerAt(State.HasFlag("housed") ? "SPAWN_lodge" : "SPAWN_player");
            }
            else
            {
                BeginPlay();
                World.PlacePlayerAt(State.HasFlag("registered") ? "LOC_hostel" : "SPAWN_player");
            }
            UI.Toast("Welcome back, " + State.profile.firstName + ".");
        }

        void BeginPlay()
        {
            World.ApplyProfile(State.profile, State.stage == LifeStage.Camp && State.HasFlag("kit_collected"));
            lastHour = State.Hour;
            lastSlotId = CampSchedule.Current(State.minuteOfDay)?.id;
            checkedAttendance.Clear();
            Running = true;
            UI.ShowHUD(true);
        }

        // ---------- Main loop ----------

        void Update()
        {
            if (Paused || State == null) return;

            float minutes = Time.deltaTime * MinutesPerSecond;
            float exertion = World.PlayerIsMoving ? 1.4f : 1f;
            AdvanceTime(minutes, exertion);

            autosaveTimer += Time.deltaTime;
            if (autosaveTimer > 30f) { autosaveTimer = 0f; Save(); }
        }

        /// <summary>Moves the clock forward and runs everything that depends on time.</summary>
        public void AdvanceTime(float minutes, float exertion = 1f, bool sleeping = false)
        {
            if (sleeping) NeedsSystem.Sleep(State, minutes);
            else NeedsSystem.Tick(State, minutes, exertion);

            State.minuteOfDay += minutes;
            if (State.minuteOfDay >= 1440f)
            {
                State.minuteOfDay -= 1440f;
                NewDay();
            }

            if (State.Hour != lastHour)
            {
                lastHour = State.Hour;
                if (!sleeping) OnNewHour();
            }

            if (!sleeping) CheckSchedule();
            if (!sleeping) CheckCollapse();
        }

        void NewDay()
        {
            State.day++;
            State.flags.RemoveAll(f => f.StartsWith("att_" + (State.day - 2) + "_"));
            checkedAttendance.Clear();
            if (State.stage == LifeStage.Camp && State.day > CampSchedule.CampDays)
            {
                Story.EndOfCamp();
            }
            else if (State.stage == LifeStage.ServiceYear)
            {
                Year.OnNewDay();
                if (State.day > ServiceYearController.Months * ServiceYearController.DaysPerMonth && !State.HasFlag("passed_out"))
                    Year.PassingOut(false);
            }
        }

        void OnNewHour()
        {
            // About one random event every 3 to 4 waking hours.
            if (State.Hour >= 5 && State.Hour < 23 && Random.value < 0.28f)
            {
                var e = Events.PickRandom(State, CurrentLocation);
                if (e != null) ShowLifeEvent(e);
            }

            string warn = NeedsSystem.WorstNeedWarning(State);
            if (warn != null) UI.Toast(warn, Palette.Warn);
        }

        public void ResetScheduleTracking()
        {
            lastHour = State.Hour;
            lastSlotId = null;
            checkedAttendance.Clear();
        }

        /// <summary>Back to the main menu (used after passing out to start a new service year).</summary>
        public void RestartToMenu()
        {
            Running = false;
            SceneManager.LoadScene(0);
        }

        void CheckSchedule()
        {
            if (State.stage == LifeStage.ServiceYear || State.stage == LifeStage.PassingOut)
            {
                if (State.stage == LifeStage.ServiceYear) Year.Tick();
                return;
            }
            if (State.stage != LifeStage.Camp || !State.HasFlag("registered")) return;

            var slot = CampSchedule.Current(State.minuteOfDay);
            string id = slot?.id;
            if (id != lastSlotId)
            {
                lastSlotId = id;
                if (slot != null)
                {
                    string where = slot.location != null ? " at the " + Locations.NameOf(slot.location) : "";
                    UI.Toast(slot.label + where, Palette.Accent);
                    if (id == "wake") UI.PlayBugle();
                }
            }

            if (slot == null || !slot.mandatory) return;
            if (State.day < 3 && slot.id != "wake") return; // registration days are lenient

            string key = LocationActions.AttendKey(State, slot.id);
            if (State.HasFlag(key) || checkedAttendance.Contains(key)) return;

            // Being at the parade ground for the morning bugle counts by itself.
            if (slot.id == "wake" && CurrentLocation == Locations.Parade)
            {
                State.AddFlag(key);
                return;
            }

            if (State.minuteOfDay >= slot.start + CampSchedule.GraceMinutes)
            {
                checkedAttendance.Add(key);
                if (CurrentLocation == slot.location && slot.id != "drill" && slot.id != "lectures" && slot.id != "saed")
                {
                    State.AddFlag(key);
                    return;
                }
                if (CurrentLocation != slot.location) Story.CaughtAbsent(slot);
            }
        }

        void CheckCollapse()
        {
            if (fainting) return;
            if (State.needs.energy <= 0.5f || State.needs.health <= 0.5f)
            {
                fainting = true;
                Story.Collapse(() => fainting = false);
            }
        }

        // ---------- Player actions ----------

        public void SetLocation(string loc)
        {
            if (CurrentLocation == loc) return;
            CurrentLocation = loc;
            RefreshActions();
            if (loc != null && State != null)
            {
                Story.OnEnterLocation(loc);
                if (State.stage == LifeStage.Camp && loc == Locations.Parade)
                {
                    var slot = CampSchedule.Current(State.minuteOfDay);
                    if (slot != null && slot.id == "wake")
                        State.AddFlag(LocationActions.AttendKey(State, "wake"));
                }
            }
        }

        public void ClearLocation(string loc)
        {
            if (CurrentLocation == loc) SetLocation(null);
        }

        public void RefreshActions()
        {
            if (State == null || UI == null) return;
            if (CurrentLocation == null) { UI.HideActions(); return; }
            UI.ShowActions(Locations.NameOf(CurrentLocation), LocationActions.For(CurrentLocation, State));
        }

        public void RunAction(LocationAction a)
        {
            if (a.run != null) { a.run(this); RefreshActions(); return; }

            if (a.cost > 0 && !EconomySystem.TrySpend(State, a.cost, a.label))
            {
                UI.Dialog("Not enough money",
                    "You need " + Palette.Naira(a.cost) + " but you have " + Palette.Naira(State.money) + ".",
                    new DialogButton("Watch an ad for " + Palette.Naira(500), () =>
                        AdService.ShowRewarded("cash_top_up", () =>
                        {
                            EconomySystem.Earn(State, 500, "Ad reward");
                            UI.Toast("+" + Palette.Naira(500));
                        })),
                    new DialogButton("OK", null));
                return;
            }

            if (a.minutes > 0) AdvanceTime(a.minutes, a.exertion);
            LifeEventSystem.ApplyEffects(State, a.effects, a.label);
            if (!string.IsNullOrEmpty(a.doneText)) UI.Toast(a.doneText);
            RefreshActions();
        }

        public void StartDrill()
        {
            UI.StartDrillGame(score =>
            {
                State.AddFlag(LocationActions.AttendKey(State, "drill"));
                State.drillsAttended++;
                AdvanceTime(60, 2.2f);
                float repGain = Mathf.Lerp(-2f, 4f, score);
                State.rep.nysc = Mathf.Clamp(State.rep.nysc + repGain, 0, 100);
                NeedsSystem.Apply(State, mood: score > 0.7f ? 6 : -3);
                UI.Toast(score > 0.7f ? "Sharp drill! The soldiers noticed." :
                         score > 0.4f ? "Drill done. Not bad." : "You kept missing the step. Practise more.");
                RefreshActions();
            });
        }

        public void StartLecture()
        {
            UI.StartQuiz(correct =>
            {
                State.AddFlag(LocationActions.AttendKey(State, "lectures"));
                AdvanceTime(120);
                if (correct >= 2) State.lecturesPassed++;
                State.rep.nysc = Mathf.Clamp(State.rep.nysc + correct - 1, 0, 100);
                NeedsSystem.Apply(State, energy: -6, mood: correct >= 2 ? 4 : -2);
                RefreshActions();
            });
        }

        public void SleepUntilMorning()
        {
            UI.FadeSleep(() =>
            {
                bool camp = State.stage == LifeStage.Camp;
                float wake = camp ? CampSchedule.WakeMinute : ServiceYearController.WakeMinute;
                float minutesToWake = State.minuteOfDay < wake ? wake - State.minuteOfDay : 1440f - State.minuteOfDay + wake;
                float quality = camp ? 1f : Year.SleepQuality();
                // Sleep in hour-sized chunks so day changes run normally.
                while (minutesToWake > 0.01f)
                {
                    float step = Mathf.Min(60f, minutesToWake);
                    AdvanceTime(step, 1f, sleeping: true);
                    minutesToWake -= step;
                }
                if (quality != 1f) NeedsSystem.Apply(State, energy: (quality - 1f) * 40f, mood: (quality - 1f) * 20f);
                lastSlotId = null;
                Save();
                if (camp)
                {
                    UI.Toast("Day " + State.day + ". The bugle is sounding!", Palette.Accent);
                    var special = CampSchedule.SpecialDay(State.day);
                    if (special != null) UI.Toast("Today: " + special);
                }
                else if (State.stage == LifeStage.ServiceYear) UI.Toast(Year.DayLabel(), Palette.Accent);
                AdService.MaybeShowInterstitial("new_day");
                RefreshActions();
            });
        }

        public void ShowLifeEvent(LifeEvent e)
        {
            var buttons = new List<DialogButton>();
            foreach (var c in e.choices)
            {
                var choice = c;
                bool affordable = c.cost <= 0 || State.money >= c.cost;
                bool allowed = string.IsNullOrEmpty(c.requiresFlag) || State.HasFlag(c.requiresFlag);
                string label = c.cost > 0 ? c.label + " (" + Palette.Naira(c.cost) + ")" : c.label;
                buttons.Add(new DialogButton(label, () =>
                {
                    string result = LifeEventSystem.ApplyChoice(State, e, choice);
                    if (!string.IsNullOrEmpty(result))
                        UI.Dialog(e.title, result, new DialogButton("Continue", null));
                    RefreshActions();
                }, affordable && allowed));
            }
            UI.Dialog(e.title, e.text, buttons.ToArray());
        }

        public void Save()
        {
            if (State != null && State.stage != LifeStage.CharacterCreation) SaveSystem.Save(State);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();
    }
}
