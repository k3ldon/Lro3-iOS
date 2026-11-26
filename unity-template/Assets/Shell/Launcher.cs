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

        AsyncOperation op = null;
        try
        {
            Debug.Log("[Launcher] Attempting SceneManager.LoadSceneAsync('GameMain')...");
            op = SceneManager.LoadSceneAsync("GameMain");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Launcher] SceneManager.LoadSceneAsync('GameMain') exception: " + ex.Message);
        }

        if (op != null)
        {
            while (!op.isDone)
            {
                Debug.Log($"[Launcher] Loading GameMain progress: {op.progress:P0}");
                yield return new WaitForSeconds(0.5f);
            }
            Debug.Log("[Launcher] GameMain scene loaded successfully!");
            yield break;
        }

        // Search for GameMain bundle in VFS
        string vfs = Path.Combine(Paths.StreamingAssets(), "VFS");
        Debug.Log("[Launcher] Searching VFS for GameMain bundles in: " + vfs);
        
        string[] candidates = new[] { "gamemain.unity", "Assets/Scenes/GameMain.unity", "GameMain" };
        foreach (var c in candidates)
        {
            ulong h = Ro3Ios.Shell.Ro3Startup.HashString(c);
            string rel = h.ToString("x16").Substring(0, 2) + "/" + h.ToString("x") + ".bundle";
            string p = Path.Combine(vfs, rel);
            if (File.Exists(p))
            {
                Debug.Log($"[Launcher] Found GameMain bundle for '{c}' at: {p}");
                var b = Bundles.LoadFromFile(p);
                if (b != null)
                {
                    Debug.Log("[Launcher] Loaded GameMain bundle! Scenes in bundle: " + string.Join(", ", b.GetAllScenePaths()));
                    SceneManager.LoadScene(Path.GetFileNameWithoutExtension(b.GetAllScenePaths()[0]));
                    yield break;
                }
            }
        }

        Debug.LogWarning("[Launcher] GameMain bundle not found by simple hash, inspecting VFS bundles...");
    }
}
