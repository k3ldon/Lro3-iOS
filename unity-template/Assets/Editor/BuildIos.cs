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

            int preloadedCount = 0;
            foreach (var importer in PluginImporter.GetAllImporters())
            {
                if (importer.assetPath.StartsWith("Assets/Plugins/Game/"))
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.iOS, true);
                    importer.isPreloaded = true;
                    preloadedCount++;
                }
            }
            Debug.Log($"[Ro3Ios.Build] Configured {preloadedCount} game plugins with isPreloaded=true for iOS AOT inclusion");

            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "14.0";
            PlayerSettings.iOS.locationUsageDescription = "Location required for game regional servers";
            PlayerSettings.iOS.cameraUsageDescription = "Camera used for profile photos";
            PlayerSettings.iOS.microphoneUsageDescription = "Microphone used for voice chat";
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

            // Copy native iOS C/C++ plugins into generated Xcode project
            string pluginsSrc = "Assets/Plugins/iOS";
            string pluginsDst = Path.Combine(outDir, "Libraries/Plugins/iOS");
            if (Directory.Exists(pluginsSrc))
            {
                Directory.CreateDirectory(pluginsDst);
                foreach (var f in Directory.GetFiles(pluginsSrc))
                {
                    if (f.EndsWith(".c") || f.EndsWith(".cpp") || f.EndsWith(".m") || f.EndsWith(".mm") || f.EndsWith(".h"))
                    {
                        string dstFile = Path.Combine(pluginsDst, Path.GetFileName(f));
                        File.Copy(f, dstFile, true);
                        Debug.Log($"[Ro3Ios.Build] Injected native plugin: {Path.GetFileName(f)} -> {dstFile}");
                    }
                }
            }
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
