#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ro3Ios.Editor
{
    public static class BuildIos
    {
        public static void Build()
        {
            Debug.Log("[Ro3Ios.Build] Starting iOS project build...");

            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Minimal);

            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "14.0";
            PlayerSettings.bundleVersion = GetArg("-ro3Version", "1.0.0");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.ro3emu.client");
            PlayerSettings.productName = "Lagnarok RO3";

            string[] scenes = { "Assets/Shell/Startup.unity" };
            if (!File.Exists(scenes[0]))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                var go = new GameObject("Ro3Startup");
                go.AddComponent<Ro3Ios.Shell.GameInit>();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenes[0]);
            }

            string outDir = GetArg("-ro3Out", "ios/out/xcode");
            Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outDir,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Ro3Ios.Build] Build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
                EditorApplication.Exit(1);
            }

            Debug.Log($"[Ro3Ios.Build] Xcode project generated at {outDir}");
        }

        static string GetArg(string name, string defaultValue)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return defaultValue;
        }
    }
}
#endif
