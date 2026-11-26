using System;
using System.Collections;
using System.IO;
using Ro3Ios.Hooks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Launcher : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[Launcher] Awake executed!");
    }

    private void Start()
    {
        Debug.Log("[Launcher] Start executed! Initializing GameMain loading sequence...");
        StartCoroutine(LoadGameSequence());
    }

    private IEnumerator LoadGameSequence()
    {
        yield return null;
        Debug.Log("[Launcher] Beginning scene transition to GameMain...");

        string vfs = Path.Combine(Paths.StreamingAssets(), "VFS");
        string gameMainBundlePath = Path.Combine(vfs, "ce70de479620a022.bundle");
        if (!File.Exists(gameMainBundlePath))
        {
            gameMainBundlePath = Path.Combine(vfs, "ce", "ce70de479620a022.bundle");
        }

        Debug.Log("[Launcher] Loading GameMain scene bundle: " + gameMainBundlePath);
        var sceneBundle = Bundles.LoadFromFile(gameMainBundlePath);
        if (sceneBundle != null)
        {
            var scenePaths = sceneBundle.GetAllScenePaths();
            string sceneToLoad = scenePaths.Length > 0 ? scenePaths[0] : "GameMainLancher";
            Debug.Log($"[Launcher] Scene bundle loaded! Loading scene: '{sceneToLoad}'...");
            SceneManager.LoadScene(sceneToLoad);
            yield break;
        }

        Debug.LogError("[Launcher] Failed to load GameMain scene bundle at: " + gameMainBundlePath);
    }
}
