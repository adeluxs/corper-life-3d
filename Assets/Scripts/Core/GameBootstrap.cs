using UnityEngine;

namespace CorperLife
{
    /// <summary>The only object in the scene. Builds the world, UI and game manager, then shows the main menu.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            var uiGo = new GameObject("UI");
            var ui = uiGo.AddComponent<UIManager>();
            ui.Build();

            var worldGo = new GameObject("World");
            var world = worldGo.AddComponent<WorldBuilder>();

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.Init(ui, world);

            world.Build(ui.Joystick);
            world.SetPreview(true);

            ShowMenu(gm, ui, world);
        }

        static void ShowMenu(GameManager gm, UIManager ui, WorldBuilder world)
        {
            var saved = SaveSystem.Load();
            ui.ShowMainMenu(saved != null,
                onContinue: () =>
                {
                    world.SetPreview(false);
                    gm.ContinueGame(saved);
                },
                onNew: () =>
                {
                    ui.ShowCharacterCreator(profile =>
                    {
                        world.SetPreview(false);
                        gm.NewGame(profile);
                    });
                });
        }
    }
}
