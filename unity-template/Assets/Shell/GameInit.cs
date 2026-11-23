using System;
using UnityEngine;

namespace Ro3Ios.Shell
{
    public class GameInit : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            StartGame();
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

            var startup = Type.GetType("Ro3Startup, Assembly-CSharp");
            if (startup == null)
            {
                Debug.LogError("[Ro3Ios] Ro3Startup not found: game assemblies missing from build");
                return;
            }

            Debug.Log("[Ro3Ios] Starting game (Ro3Startup)");
            new GameObject("Startup").AddComponent(startup);
        }
    }
}
