using System.Collections.Generic;
using UnityEngine;

namespace Ro3Ios.Hooks;

public static class Shaders
{
    private static readonly HashSet<string> Missing = new HashSet<string>();
    private static Dictionary<string, Shader> _ours;
    private static Shader _fallback;

    public static Shader Find(string name)
    {
        if (Ours().TryGetValue(name ?? "", out var own)) return own;
        if (name != null && name.StartsWith("Soft Mask/") && Ours().TryGetValue(name.Substring("Soft Mask/".Length), out own)) return own;
        var shader = Shader.Find(name);
        if (shader != null) return Materials.Remap?.Invoke(shader) ?? shader;
        if (Missing.Add(name ?? "")) Debug.LogWarning($"[Ro3Ios] shader missing for Metal, using fallback: {name}");
        if (_fallback == null) _fallback = Shader.Find("Hidden/Ro3Ios/Fallback");
        return _fallback;
    }

    private static Dictionary<string, Shader> Ours()
    {
        if (_ours != null) return _ours;
        _ours = new Dictionary<string, Shader>();
        foreach (var s in Resources.LoadAll<Shader>("Ro3Shaders"))
            if (!_ours.ContainsKey(s.name)) _ours[s.name] = s;
        return _ours;
    }
}
