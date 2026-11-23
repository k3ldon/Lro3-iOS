using System;
using UnityEngine;

namespace Ro3Ios.Hooks;

public static class Materials
{
    public static Func<Shader, Shader> Remap { get; set; }
    public static Action<Material> Fix { get; set; }

    private static readonly Type Graphic = Type.GetType("UnityEngine.UI.Graphic, UnityEngine.UI");
    private static readonly System.Reflection.PropertyInfo GraphicMaterial = Graphic?.GetProperty("material");

    public static void FixLoaded(object asset)
    {
        if (Fix == null || asset == null) return;
        try
        {
            if (asset is Material material) { Fix(material); return; }
            if (!(asset is GameObject prefab)) return;
            Clicks.EnableDeadClickTargets(prefab);
            FixObject(prefab);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ro3Ios] material fix on load failed: {e.Message}");
        }
    }

    public static void FixInstance(object instance)
    {
        if (Fix == null) return;
        try
        {
            if (instance is Material material) Fix(material);
            else if (instance is GameObject go) FixObject(go);
            else if (instance is Component component && component != null) FixObject(component.gameObject);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ro3Ios] material fix on instantiate failed: {e.Message}");
        }
    }

    private static void FixObject(GameObject root)
    {
        if (root == null) return;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            foreach (var m in renderer.sharedMaterials) Fix(m);
        if (Graphic == null || GraphicMaterial == null) return;
        foreach (var graphic in root.GetComponentsInChildren(Graphic, true))
            Fix(GraphicMaterial.GetValue(graphic) as Material);
    }

    public static Material Create(Shader shader)
    {
        var replacement = Remap?.Invoke(shader);
        return new Material(replacement != null ? replacement : shader);
    }

    public static Material Copy(Material source)
    {
        var copy = new Material(source);
        var replacement = Remap?.Invoke(copy.shader);
        if (replacement != null)
        {
            copy.shader = replacement;
        }
        return copy;
    }
}
