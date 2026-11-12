using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CorperLife.EditorTools
{
    /// <summary>
    /// Creates the game scene and builds the Android APK/AAB.
    /// Used by GitHub Actions (GameCI) so nobody has to set up scenes by hand.
    /// Menu: Corper Life > Build Android APK.
    /// </summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Corper Life/Create Main Scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var boot = new GameObject("Bootstrap");
            boot.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Main scene created at " + ScenePath);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Swiftmynd Technologies Ltd";
            PlayerSettings.productName = "Corper Life 3D";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.swiftmynd.corperlife3d");
            PlayerSettings.bundleVersion = Env("APP_VERSION", "0.1.0");
            PlayerSettings.Android.bundleVersionCode = int.TryParse(Env("VERSION_CODE", "1"), out var vc) ? vc : 1;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.colorSpace = ColorSpace.Gamma; // faster on older phones

            // Keep frame rate steady on 3 GB phones.
            QualitySettings.shadowDistance = 40f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = 0;
        }

        static string Env(string key, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(v) ? fallback : v;
        }

        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return fallback;
        }

        [MenuItem("Corper Life/Build Android APK")]
        public static void BuildAndroid()
        {
            CreateScene();
            ConfigurePlayer();

            string path = Arg("-customBuildPath", "Builds/Android/CorperLife3D.apk");
            bool aab = path.EndsWith(".aab");
            EditorUserBuildSettings.buildAppBundle = aab;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Builds");

            // Signing: GameCI passes keystore details as env vars when we add a release keystore.
            string ks = Env("ANDROID_KEYSTORE_PATH", "");
            if (!string.IsNullOrEmpty(ks) && File.Exists(ks))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = Env("ANDROID_KEYSTORE_PASS", "");
                PlayerSettings.Android.keyaliasName = Env("ANDROID_KEYALIAS_NAME", "");
                PlayerSettings.Android.keyaliasPass = Env("ANDROID_KEYALIAS_PASS", "");
            }
            else PlayerSettings.Android.useCustomKeystore = false; // debug key: fine for test APKs

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Build result: " + report.summary.result + ", size " + report.summary.totalSize / (1024 * 1024) + " MB");
            if (report.summary.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw new Exception("Build failed");
            }
        }
    }
}
