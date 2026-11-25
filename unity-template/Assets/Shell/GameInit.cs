using System;
using UnityEngine;

namespace Ro3Ios.Shell
{
    public class GameInit : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            RemoteLogger.Init();
            var updater = Type.GetType("Ro3Ios.Updater.UpdateScreen, Ro3Ios.Shell")
                       ?? Type.GetType("Ro3Ios.Updater.UpdateScreen");
            if (updater != null)
            {
                var screen = new GameObject("Updater").AddComponent(updater);
                updater.GetField("OnReady")?.SetValue(screen, (Action)StartGame);
            }
            else
            {
                StartGame();
            }
        }

        public static void StartGame()
        {
            Debug.Log("[Ro3Ios.GameInit] Initializing iOS subsystems...");

            ShaderFixer.Install();

            var eventSystem = Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
            var inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                ?? Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");

            if (eventSystem != null && UnityEngine.Object.FindObjectOfType(eventSystem) == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent(eventSystem);
                if (inputModule != null) es.AddComponent(inputModule);
                UnityEngine.Object.DontDestroyOnLoad(es);
            }

            Type startup = null;
            try { System.Reflection.Assembly.Load("Assembly-CSharp"); } catch { }
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                startup = asm.GetType("Ro3Startup");
                if (startup != null)
                {
                    Debug.Log("[Ro3Ios] Found Ro3Startup in assembly: " + asm.FullName);
                    break;
                }
            }

            if (startup != null)
            {
                Debug.Log("[Ro3Ios] Starting game via Type (Ro3Startup)");
                new GameObject("Startup").AddComponent(startup);
                return;
            }

            // Fallback: try Unity string-based AddComponent which searches engine type registry
            Debug.LogWarning("[Ro3Ios] Searching Ro3Startup via Unity engine component registry...");
            var go = new GameObject("Startup");
            var comp = go.AddComponent("Ro3Startup");
            if (comp != null)
            {
                Debug.Log("[Ro3Ios] Starting game via engine component registry (Ro3Startup)");
                return;
            }

            var sb = new System.Text.StringBuilder("[Ro3Ios] Loaded assemblies: ");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) sb.Append(asm.GetName().Name).Append(", ");
            Debug.LogError(sb.ToString());
            Debug.LogError("[Ro3Ios] Ro3Startup not found: game assemblies missing from build");
        }
    }
}
