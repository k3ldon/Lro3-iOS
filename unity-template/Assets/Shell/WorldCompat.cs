using UnityEngine;

namespace Ro3Ios.Shell
{
    public class WorldCompat : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
