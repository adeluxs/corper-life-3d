using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorperLife
{
    /// <summary>Builds and runs every screen: menu, character creator, HUD, dialogs, toasts and mini-games.</summary>
    public class UIManager : MonoBehaviour
    {
        public RectTransform Root { get; private set; }
        public VirtualJoystick Joystick { get; private set; }

        public bool IsModalOpen => dialogOpen || menuOpen || miniGameOpen || fading;

        bool dialogOpen, menuOpen, miniGameOpen, fading;

        // HUD
        RectTransform hud;
        Text dayText, clockText, moneyText, objectiveText, locationText;
        StatBar energyBar, hungerBar, healthBar, moodBar, hygieneBar;
        RectTransform actionsPanel, actionsList;
        Text actionsTitle;

        // Dialog
        RectTransform dialogLayer;
        readonly Queue<Action> pendingDialogs = new Queue<Action>();

        // Toasts
        RectTransform toastStack;

        Image fadeImage;
        AudioSource audioSource;
        AudioClip bugleClip;

        public void Build()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.6f;
            Root = (RectTransform)canvasGo.transform;

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            BuildHUD();

            toastStack = UIFactory.Rect("Toasts", Root).Anchor(new Vector2(0.3f, 0.62f), new Vector2(0.7f, 0.86f));
            var tv = UIFactory.VStack(toastStack, 10, 0, TextAnchor.UpperCenter);
            tv.childControlHeight = true;

            dialogLayer = UIFactory.Rect("Dialogs", Root).Stretch();

            fadeImage = UIFactory.Panel("Fade", Root, new Color(0, 0, 0, 0), false);
            fadeImage.rectTransform.Stretch();
            fadeImage.raycastTarget = false;

            audioSource = gameObject.AddComponent<AudioSource>();
            bugleClip = ProceduralAudio.Bugle();

            ShowHUD(false);
        }

        // ---------------- HUD ----------------

        void BuildHUD()
        {
            hud = UIFactory.Rect("HUD", Root).Stretch();

            // Top-left: day and time
            var timeBox = UIFactory.Panel("TimeBox", hud, Palette.PanelBg);
            timeBox.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -138), new Vector2(404, -24));
            dayText = UIFactory.Label("Day", timeBox.transform, "Camp - Day 1", 28, Palette.TextMain, TextAnchor.UpperLeft, FontStyle.Bold);
            dayText.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(20, 10), new Vector2(-20, -12));
            clockText = UIFactory.Label("Clock", timeBox.transform, "4:30 am", 40, Palette.TextMain, TextAnchor.LowerLeft, FontStyle.Bold);
            clockText.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(20, 10), new Vector2(-20, -10));

            // Top-right: money and menu
            var moneyBox = UIFactory.Panel("MoneyBox", hud, Palette.PanelBg);
            moneyBox.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-404, -104), new Vector2(-124, -24));
            moneyText = UIFactory.Label("Money", moneyBox.transform, "N0", 36, Palette.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            moneyText.rectTransform.Stretch(8);
            var menuBtn = UIFactory.Button("Menu", hud, "II", Palette.PanelBg, OpenPauseMenu, 32);
            ((RectTransform)menuBtn.transform).Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-104, -104), new Vector2(-24, -24));

            // Top-centre: objective and location
            var objBox = UIFactory.Panel("Objective", hud, Palette.PanelBg);
            objBox.rectTransform.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-460, -112), new Vector2(460, -24));
            objectiveText = UIFactory.Label("Text", objBox.transform, "", 26, Palette.TextMain, TextAnchor.UpperCenter, FontStyle.Bold);
            objectiveText.rectTransform.Stretch(10);
            locationText = UIFactory.Label("Loc", objBox.transform, "", 22, Palette.TextDim, TextAnchor.LowerCenter);
            locationText.rectTransform.Stretch(8);

            // Left: needs
            var needs = UIFactory.Panel("Needs", hud, Palette.PanelBg);
            needs.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -380), new Vector2(404, -156));
            var needsStack = UIFactory.Rect("Stack", needs.transform).Stretch(14);
            UIFactory.VStack(needsStack, 6);
            energyBar = new StatBar(needsStack, "Energy");
            hungerBar = new StatBar(needsStack, "Food");
            healthBar = new StatBar(needsStack, "Health");
            moodBar = new StatBar(needsStack, "Mood");
            hygieneBar = new StatBar(needsStack, "Hygiene");

            // Right: actions at current location
            var ap = UIFactory.Panel("Actions", hud, Palette.PanelBg);
            actionsPanel = ap.rectTransform;
            actionsPanel.Anchor(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-584, 40), new Vector2(-24, 640));
            actionsTitle = UIFactory.Label("Title", actionsPanel, "", 30, Palette.TextMain, TextAnchor.UpperLeft, FontStyle.Bold);
            actionsTitle.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -64), new Vector2(-22, -14));
            var scrollRt = UIFactory.Rect("Scroll", actionsPanel).Anchor(Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -72));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scrollRt.gameObject.AddComponent<RectMask2D>();
            actionsList = UIFactory.Rect("List", scrollRt).Anchor(new Vector2(0, 1), new Vector2(1, 1));
            actionsList.pivot = new Vector2(0.5f, 1f);
            UIFactory.VStack(actionsList, 10);
            UIFactory.FitHeight(actionsList);
            scroll.content = actionsList;
            actionsPanel.gameObject.SetActive(false);

            // Bottom-left: joystick
            var joyRt = UIFactory.Rect("Joystick", hud).Anchor(Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(380, 380));
            Joystick = joyRt.gameObject.AddComponent<VirtualJoystick>();
            Joystick.Build();
        }

        public void ShowHUD(bool show) => hud.gameObject.SetActive(show);

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.State == null || !hud.gameObject.activeSelf) return;
            var s = gm.State;

            string stage = s.stage == LifeStage.Camp ? "Orientation camp" : "Service year";
            dayText.text = stage + " - Day " + s.day;
            clockText.text = s.ClockText();
            moneyText.text = Palette.Naira(s.money);
            objectiveText.text = gm.Story.CurrentObjective();
            locationText.text = gm.CurrentLocation != null ? Locations.NameOf(gm.CurrentLocation) : "";

            energyBar.Set(s.needs.energy);
            hungerBar.Set(s.needs.hunger);
            healthBar.Set(s.needs.health);
            moodBar.Set(s.needs.mood);
            hygieneBar.Set(s.needs.hygiene);
        }

        public void ShowActions(string title, List<LocationAction> actions)
        {
            UIFactory.Clear(actionsList);
            actionsTitle.text = title;
            if (actions.Count == 0)
            {
                var t = UIFactory.Label("None", actionsList, "Nothing to do here right now.", 24, Palette.TextDim);
                t.Height(60);
            }
            var gm = GameManager.I;
            foreach (var a in actions)
            {
                var action = a;
                string label = a.label;
                if (a.cost > 0) label += "   " + Palette.Naira(a.cost);
                var b = UIFactory.Button(a.id, actionsList, label, new Color(1, 1, 1, 0.12f), () => gm.RunAction(action), 24);
                b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                b.Height(76);
            }
            actionsPanel.gameObject.SetActive(true);
        }

        public void HideActions() => actionsPanel.gameObject.SetActive(false);

        // ---------------- Dialogs ----------------

        public void Dialog(string title, string body, params DialogButton[] buttons)
        {
            if (dialogOpen)
            {
                pendingDialogs.Enqueue(() => Dialog(title, body, buttons));
                return;
            }
            dialogOpen = true;

            var overlay = UIFactory.Panel("Dialog", dialogLayer, new Color(0, 0, 0, 0.55f), false);
            overlay.rectTransform.Stretch();

            var card = UIFactory.Panel("Card", overlay.transform, new Color(0.1f, 0.12f, 0.11f, 0.98f));
            card.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            card.rectTransform.sizeDelta = new Vector2(1000, 0);
            var stack = UIFactory.VStack(card.rectTransform, 18, 36);
            stack.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.FitHeight(card);

            var t = UIFactory.Label("Title", card.transform, title, 40, Palette.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.Height(56);
            UIFactory.Label("Body", card.transform, body, 28, Palette.TextDim, TextAnchor.UpperLeft);

            if (buttons == null || buttons.Length == 0) buttons = new[] { new DialogButton("OK", null) };
            foreach (var db in buttons)
            {
                var d = db;
                var btn = UIFactory.Button("Choice", card.transform, d.label,
                    d.enabled ? Palette.Accent : new Color(1, 1, 1, 0.1f), () =>
                    {
                        Destroy(overlay.gameObject);
                        dialogOpen = false;
                        d.onClick?.Invoke();
                        if (!dialogOpen && pendingDialogs.Count > 0) pendingDialogs.Dequeue()();
                        GameManager.I?.RefreshActions();
                    }, 26);
                btn.interactable = d.enabled;
                btn.Height(78);
            }
        }

        // ---------------- Toasts ----------------

        public void Toast(string message, Color? accent = null)
        {
            StartCoroutine(ToastRoutine(message, accent ?? Palette.TextMain));
        }

        IEnumerator ToastRoutine(string message, Color color)
        {
            var bg = UIFactory.Panel("Toast", toastStack, Palette.PanelBg);
            bg.Height(68);
            var t = UIFactory.Label("Text", bg.transform, message, 26, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch(10);
            var group = bg.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            if (toastStack.childCount > 4) Destroy(toastStack.GetChild(0).gameObject);
            yield return new WaitForSecondsRealtime(3.2f);
            for (float a = 1f; a > 0f; a -= Time.unscaledDeltaTime * 2f)
            {
                if (group == null) yield break;
                group.alpha = a;
                yield return null;
            }
            if (bg != null) Destroy(bg.gameObject);
        }

        // ---------------- Menus ----------------

        public void ShowMainMenu(bool hasSave, Action onContinue, Action onNew)
        {
            menuOpen = true;
            var screen = UIFactory.Panel("MainMenu", Root, new Color(0.04f, 0.06f, 0.05f, 0.82f), false);
            screen.rectTransform.Stretch();

            var title = UIFactory.Label("Title", screen.transform, "CORPER LIFE 3D", 110, Palette.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Anchor(new Vector2(0, 0.66f), new Vector2(1, 0.88f));
            var sub = UIFactory.Label("Sub", screen.transform, "Camp. PPA. Allawee. Wahala. Your service year.", 34, Palette.TextDim, TextAnchor.MiddleCenter);
            sub.rectTransform.Anchor(new Vector2(0, 0.58f), new Vector2(1, 0.66f));

            var col = UIFactory.Rect("Buttons", screen.transform).Anchor(new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.55f));
            UIFactory.VStack(col, 22, 0, TextAnchor.MiddleCenter);
            if (hasSave)
                UIFactory.Button("Continue", col, "Continue", Palette.Accent, () => { Destroy(screen.gameObject); menuOpen = false; onContinue(); }, 36).Height(100);
            UIFactory.Button("New", col, hasSave ? "New service year" : "Start your service year",
                hasSave ? new Color(1, 1, 1, 0.14f) : Palette.Accent,
                () => { Destroy(screen.gameObject); menuOpen = false; onNew(); }, 36).Height(100);

            var disclaimer = UIFactory.Label("Disclaimer", screen.transform,
                "A fictional life simulation game. Not affiliated with, endorsed by or connected to the National Youth Service Corps (NYSC) or the Federal Government of Nigeria.  |  Swiftmynd Technologies Ltd",
                20, Palette.TextDim, TextAnchor.MiddleCenter);
            disclaimer.rectTransform.Anchor(new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.12f));
        }

        void OpenPauseMenu()
        {
            var gm = GameManager.I;
            Dialog("Paused",
                "Day " + gm.State.day + "  |  NYSC record " + Mathf.RoundToInt(gm.State.rep.nysc) + "/100\n" +
                "State code: " + gm.State.profile.callUpNumber + "   Platoon " + gm.State.platoon,
                new DialogButton("Resume", null),
                new DialogButton("Save", () => { gm.Save(); Toast("Game saved."); }),
                new DialogButton("Remove ads", () => Toast("Purchases arrive in a later build.")));
        }

        public void ShowCharacterCreator(Action<PlayerProfile> onDone)
        {
            menuOpen = true;
            var creator = UIFactory.Rect("Creator", Root).Stretch();
            var c = creator.gameObject.AddComponent<CharacterCreatorUI>();
            c.Build(this, p =>
            {
                Destroy(creator.gameObject);
                menuOpen = false;
                onDone(p);
            });
        }

        // ---------------- Mini-games ----------------

        public void StartDrillGame(Action<float> onDone)
        {
            miniGameOpen = true;
            var rt = UIFactory.Rect("Drill", Root).Stretch();
            var g = rt.gameObject.AddComponent<DrillMiniGame>();
            g.Build(score =>
            {
                Destroy(rt.gameObject);
                miniGameOpen = false;
                onDone(score);
            });
        }

        public void StartQuiz(Action<int> onDone)
        {
            miniGameOpen = true;
            var rt = UIFactory.Rect("Quiz", Root).Stretch();
            var g = rt.gameObject.AddComponent<LectureQuiz>();
            g.Build(correct =>
            {
                Destroy(rt.gameObject);
                miniGameOpen = false;
                onDone(correct);
            });
        }

        // ---------------- Effects ----------------

        public void FadeSleep(Action midpoint) => StartCoroutine(FadeRoutine(midpoint));

        IEnumerator FadeRoutine(Action midpoint)
        {
            fading = true;
            fadeImage.raycastTarget = true;
            for (float a = 0; a < 1f; a += Time.unscaledDeltaTime * 1.5f)
            {
                fadeImage.color = new Color(0, 0, 0, a);
                yield return null;
            }
            fadeImage.color = Color.black;
            midpoint?.Invoke();
            yield return new WaitForSecondsRealtime(0.8f);
            PlayBugle();
            for (float a = 1f; a > 0f; a -= Time.unscaledDeltaTime)
            {
                fadeImage.color = new Color(0, 0, 0, a);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.raycastTarget = false;
            fading = false;
        }

        public void PlayBugle()
        {
            if (bugleClip != null) audioSource.PlayOneShot(bugleClip, 0.6f);
        }
    }
}
