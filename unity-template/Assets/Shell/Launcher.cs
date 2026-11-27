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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Launcher] Scene '{scene.name}' loaded! Performing Metal shader sweep...");
        Shader fallback = Shader.Find("Hidden/Ro3Ios/Fallback") ?? Shader.Find("UI/Default");
        foreach (var root in scene.GetRootGameObjects())
        {
            FixShadersRecursive(root, fallback);
        }
    }

    private static void FixShadersRecursive(GameObject go, Shader fallback)
    {
        if (go == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterials == null) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (m != null && m.shader != null && (!m.shader.isSupported || m.shader.name == "Hidden/InternalErrorShader"))
                {
                    m.shader = fallback;
                }
            }
        }
        foreach (var g in go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            if (g.material != null && g.material.shader != null && (!g.material.shader.isSupported || g.material.shader.name == "Hidden/InternalErrorShader"))
            {
                g.material.shader = fallback;
            }
        }
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
