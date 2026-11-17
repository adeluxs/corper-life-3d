using System;
using UnityEngine;

namespace CorperLife
{
    /// <summary>Scripted camp moments: arrival, registration, kit, swearing-in, punishments, end of camp.</summary>
    public class StoryController
    {
        readonly GameManager gm;
        GameState S => gm.State;
        UIManager UI => gm.UI;

        static readonly string[] Skills =
            { "Tailoring", "Baking and catering", "ICT and graphics", "Agro-farming", "Cosmetology", "Photography" };

        static readonly string[] Roles =
            { "Platoon leader", "OBS camp media", "Red Cross team", "Camp band", "Sanitation team" };

        public StoryController(GameManager gm) { this.gm = gm; }

        public string CurrentObjective()
        {
            if (S == null) return "";
            if (S.stage == LifeStage.ServiceYear) return gm.Year.CurrentObjective();
            if (S.stage == LifeStage.PassingOut) return "Service year complete";
            if (S.stage != LifeStage.Camp) return "";
            if (!S.HasFlag("registered")) return "Go to the camp office and register";
            if (!S.HasFlag("found_bunk")) return "Find your bunk in the hostel";
            if (S.day >= 2 && !S.HasFlag("kit_collected")) return "Collect your kit at the camp office";
            if (S.day == 3 && !S.HasFlag("sworn_in")) return "Swearing-in: be at the parade ground by 9am";
            var slot = CampSchedule.Current(S.minuteOfDay);
            if (slot != null && slot.location != null && slot.mandatory &&
                !S.HasFlag(LocationActions.AttendKey(S, slot.id)))
                return slot.label + " - " + Locations.NameOf(slot.location);
            if (S.Hour >= 21 || S.Hour < 4) return "Go and sleep in the hostel";
            var next = CampSchedule.Next(S.minuteOfDay);
            return "Free time. Next: " + next.label;
        }

        // ---------- Day 1 ----------

        public void OnArrival()
        {
            var p = S.profile;
            UI.Dialog("Welcome to camp",
                "After a long bus ride from " + p.homeState + ", you finally reach the orientation camp in " +
                p.deployedState + " State. Soldiers are at the gate and the queue is already long.\n\n" +
                "You have " + Palette.Naira(S.money) + " on you. Make it last: allawee only comes at the end of the month.",
                new DialogButton("Let's go", () => UI.Toast("Objective: register at the camp office", Palette.Accent)));
        }

        public void Register()
        {
            gm.AdvanceTime(90);
            S.AddFlag("registered");
            S.profile.callUpNumber = S.profile.deployedState.Substring(0, 2).ToUpper() + "/26C/" +
                                     UnityEngine.Random.Range(1000, 9999);
            S.rep.nysc += 2;
            UI.Dialog("Registration done",
                "Documents checked, medical test passed, and your state code is " + S.profile.callUpNumber + ".\n\n" +
                "You are in Platoon " + S.platoon + ". Go and find your bunk in the hostel.",
                new DialogButton("OK", null));
        }

        public void OnEnterLocation(string loc)
        {
            if (S.stage != LifeStage.Camp) return;

            if (loc == Locations.Hostel && S.HasFlag("registered") && !S.HasFlag("found_bunk"))
            {
                S.AddFlag("found_bunk");
                UI.Dialog("Your bunk",
                    "You get the top bunk near the window. Your bunkmate, Chidi from Enugu, is already " +
                    "spreading his mosquito net.\n\n\"Corper! You go need net o, mosquito here no dey play.\"",
                    new DialogButton("Buy a net from Mammy Market later", () => S.AddFlag("needs_net")),
                    new DialogButton("I brought my own net", () =>
                    {
                        if (S.profile.background == "Struggling") { S.AddFlag("needs_net"); UI.Toast("You didn't actually pack one..."); }
                        else S.AddFlag("has_net");
                    }));
            }

            if (loc == Locations.Parade && S.day == 3 && !S.HasFlag("sworn_in") && S.Hour >= 8 && S.Hour < 13)
                SwearingIn();
        }

        public void CollectKit()
        {
            gm.AdvanceTime(60);
            S.AddFlag("kit_collected");
            gm.World.ApplyProfile(S.profile, true);
            UI.Dialog("Kit collected",
                "White tees, white shorts, white socks and canvas shoes for camp, plus khaki, jungle boots and a face cap for ceremonies.\n\n" +
                "You change into your whites straight away. But the khaki is two sizes too big.",
                new DialogButton("Pay a camp tailor to adjust it (" + Palette.Naira(1500) + ")", () =>
                {
                    if (EconomySystem.TrySpend(S, 1500, "Tailor adjusted khaki"))
                    {
                        S.AddFlag("khaki_fitted");
                        NeedsSystem.Apply(S, mood: 8);
                        UI.Toast("Your khaki now fits perfectly.");
                    }
                    else UI.Toast("Not enough money.", Palette.Danger);
                }, S.money >= 1500),
                new DialogButton("Swap with someone in your platoon", () =>
                {
                    if (UnityEngine.Random.value < 0.55f) { S.AddFlag("khaki_fitted"); UI.Toast("Lucky! You found a swap."); }
                    else { NeedsSystem.Apply(S, mood: -4); UI.Toast("No one has your size. Baggy khaki it is."); }
                }),
                new DialogButton("Wear it like that", () => NeedsSystem.Apply(S, mood: -3)));
        }

        void SwearingIn()
        {
            S.AddFlag("sworn_in");
            gm.AdvanceTime(120);
            S.rep.nysc += 5;
            NeedsSystem.Apply(S, mood: 12, energy: -10);
            UI.Dialog("Swearing-in ceremony",
                "Thousands of corps members in khaki stand under the sun. The governor's representative arrives, " +
                "and with your right hand raised you take the oath.\n\nYou are now a serving corps member.",
                new DialogButton("Take a photo for your status (" + Palette.Naira(500) + ")", () =>
                {
                    if (EconomySystem.TrySpend(S, 500, "Swearing-in photo")) { NeedsSystem.Apply(S, mood: 6); S.AddFlag("sworn_photo"); }
                }, S.money >= 500),
                new DialogButton("Continue", null));
        }

        // ---------- Camp life ----------

        public void AttendSaed()
        {
            if (string.IsNullOrEmpty(S.skill))
            {
                var buttons = new DialogButton[Skills.Length];
                for (int i = 0; i < Skills.Length; i++)
                {
                    string skill = Skills[i];
                    buttons[i] = new DialogButton(skill, () =>
                    {
                        S.skill = skill;
                        UI.Toast("You chose " + skill + ". It will unlock a side hustle later.");
                        AttendSaed();
                    });
                }
                UI.Dialog("Choose your SAED skill",
                    "SAED teaches a trade you can make money from during and after service. Choose carefully.",
                    buttons);
                return;
            }

            S.AddFlag(LocationActions.AttendKey(S, "saed"));
            gm.AdvanceTime(90);
            S.AddFlag("saed_" + S.day);
            int sessions = 0;
            foreach (var f in S.flags) if (f.StartsWith("saed_")) sessions++;
            NeedsSystem.Apply(S, energy: -5, mood: 3);
            UI.Toast(S.skill + " training done (" + sessions + " sessions).");
        }

        public void ChooseCampRole()
        {
            var buttons = new DialogButton[Roles.Length + 1];
            for (int i = 0; i < Roles.Length; i++)
            {
                string role = Roles[i];
                buttons[i] = new DialogButton(role, () =>
                {
                    S.campRole = role;
                    S.rep.nysc += 3;
                    UI.Toast("You are now in the " + role + ".");
                });
            }
            buttons[Roles.Length] = new DialogButton("Not now", null);
            UI.Dialog("Camp roles", "Roles give you more responsibility, new friends and different events.", buttons);
        }

        public void CallHome()
        {
            gm.AdvanceTime(15);
            float chance = S.profile.background == "Rich" ? 0.75f : S.profile.background == "Average" ? 0.45f : 0.15f;
            if (S.money < 3000 && UnityEngine.Random.value < chance)
            {
                int amount = S.profile.background == "Rich" ? 20000 : S.profile.background == "Average" ? 7000 : 2000;
                EconomySystem.Earn(S, amount, "Money from home");
                NeedsSystem.Apply(S, mood: 10);
                UI.Dialog("Call home", "\"How is camp? Are you eating well?\"\n\nYour family sent you " +
                                       Palette.Naira(amount) + ".", new DialogButton("Thank them", null));
            }
            else if (S.profile.background == "Struggling" && UnityEngine.Random.value < 0.35f)
            {
                UI.Dialog("Call home", "Your younger brother needs school fees. Can you help with " +
                                       Palette.Naira(3000) + "?",
                    new DialogButton("Send it", () =>
                    {
                        if (EconomySystem.TrySpend(S, 3000, "Sent to family")) { NeedsSystem.Apply(S, mood: 4); S.rep.community += 2; }
                    }, S.money >= 3000),
                    new DialogButton("Promise to send later", () => NeedsSystem.Apply(S, mood: -6)));
            }
            else
            {
                NeedsSystem.Apply(S, mood: 8);
                UI.Dialog("Call home", "Everyone is fine and they are proud of you. \"Don't let the soldiers break you!\"",
                    new DialogButton("Smile", null));
            }
        }

        public void TryLeaveCamp()
        {
            UI.Dialog("Camp gate",
                "Soldiers guard the gate. Corps members are not allowed out of camp during orientation.",
                new DialogButton("Beg the soldier to let you out", () =>
                {
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        NeedsSystem.Apply(S, mood: 12);
                        S.rep.nysc -= 4;
                        UI.Toast("He waved you through. Risky, but you bought proper food outside.");
                        NeedsSystem.Apply(S, hunger: 35);
                        EconomySystem.TrySpend(S, Mathf.Min(2000, S.money), "Food outside camp");
                    }
                    else
                    {
                        S.rep.nysc -= 3;
                        NeedsSystem.Apply(S, energy: -12, mood: -8);
                        UI.Dialog("Caught", "\"Oya, frog jump! Twenty times!\"", new DialogButton("Ouch", null));
                    }
                }),
                new DialogButton("Go back inside", null));
        }

        public void CaughtAbsent(ScheduleSlot slot)
        {
            S.timesLate++;
            if (slot.id == "drill") S.drillsMissed++;
            S.rep.nysc = Mathf.Max(0, S.rep.nysc - 4);
            string[] punishments =
            {
                "A soldier finds you and orders you to do 30 frog jumps in front of everyone.",
                "Your name is written down. You will cut grass with your platoon this evening.",
                "\"Corper, where you dey since?\" You stand in the sun for 20 minutes as punishment.",
            };
            string text = "You missed " + slot.label.ToLower() + ".\n\n" + punishments[UnityEngine.Random.Range(0, punishments.Length)];
            UI.Dialog("Absent!", text,
                new DialogButton("Accept the punishment", () => NeedsSystem.Apply(S, energy: -12, mood: -8)),
                new DialogButton("Watch an ad to talk your way out", () =>
                    AdService.ShowRewarded("skip_punishment", () =>
                    {
                        S.rep.nysc += 2;
                        UI.Toast("The soldier let it go this time.");
                    })));
        }

        public void Collapse(Action onDone)
        {
            string cause = S.needs.health <= 0.5f ? "You've been ill for too long" : "You pushed yourself too hard";
            UI.Dialog("You fainted",
                cause + " and collapsed. The Red Cross team carried you to the camp clinic.",
                new DialogButton("Rest in the clinic (lose 4 hours)", () =>
                {
                    gm.AdvanceTime(240, 1f, sleeping: true);
                    NeedsSystem.Apply(S, energy: 35, health: 30, mood: -10);
                    EconomySystem.TrySpend(S, Mathf.Min(1000, S.money), "Clinic drugs");
                    gm.World.PlacePlayerAt("LOC_clinic");
                    onDone();
                }),
                new DialogButton("Watch an ad to recover now", () =>
                    AdService.ShowRewarded("revive", () =>
                    {
                        NeedsSystem.Apply(S, energy: 40, health: 30);
                        onDone();
                    })));
        }

        // ---------- End of camp ----------

        public void EndOfCamp()
        {
            S.stage = LifeStage.Posting;
            gm.Year.AssignPpa();
            string ppa = ServiceYearController.PpaName(S.ppaType);
            S.AddFlag("ppa_assigned");
            gm.Save();
            float score = (S.rep.nysc + S.needs.mood) / 2f;
            string summary =
                "Drills attended: " + S.drillsAttended + "   Missed: " + S.drillsMissed + "\n" +
                "Lectures passed: " + S.lecturesPassed + "   Times late: " + S.timesLate + "\n" +
                "SAED skill: " + (string.IsNullOrEmpty(S.skill) ? "none" : S.skill) + "\n" +
                "NYSC record: " + Mathf.RoundToInt(S.rep.nysc) + "/100";

            UI.Dialog("Posting letter",
                "Camp is over! " + (score > 60 ? "You were one of the standout corpers in Platoon " + S.platoon + ". " : "") +
                "Your posting letter says your PPA is " + ppa + ".\n\n" + summary,
                new DialogButton("Travel to your PPA", () => gm.Year.Begin()));
            AdService.MaybeShowInterstitial("camp_end");
        }
    }
}
