using System;
using System.Collections;
using System.IO;
using Ro3Ios.Hooks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ro3Ios.Shell
{
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

    private static readonly Type GraphicType = Type.GetType("UnityEngine.UI.Graphic, UnityEngine.UI");
    private static readonly System.Reflection.PropertyInfo GraphicMatProp = GraphicType?.GetProperty("material");

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
        if (GraphicType != null && GraphicMatProp != null)
        {
            foreach (var g in go.GetComponentsInChildren(GraphicType, true))
            {
                if (GraphicMatProp.GetValue(g) is Material mat && mat.shader != null && (!mat.shader.isSupported || mat.shader.name == "Hidden/InternalErrorShader"))
                {
                    mat.shader = fallback;
                }
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
}
