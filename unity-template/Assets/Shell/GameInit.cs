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

            Debug.Log("[Ro3Ios] Starting game: creating Startup GameObject with Ro3Startup component");
            new GameObject("Startup").AddComponent<Ro3Startup>();
        }
    }
}
