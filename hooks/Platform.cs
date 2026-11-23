using System.IO;
using UnityEngine;

namespace Ro3Ios.Hooks;

public static class Platform
{
    private static bool? _isPc;
    private static bool? _showLuaLog;

    public static bool UnityPc()
    {
        if (_isPc.HasValue) return _isPc.Value;
        _isPc = File.Exists(Path.Combine(Application.persistentDataPath, "pc-ui"));
        return _isPc.Value;
    }

    public static bool LuaLog()
    {
        if (_showLuaLog.HasValue) return _showLuaLog.Value;
        _showLuaLog = File.Exists(Path.Combine(Application.persistentDataPath, "lua-debug"));
        return _showLuaLog.Value;
    }

    public static Rect GetSafeArea() => Screen.safeArea;
}
