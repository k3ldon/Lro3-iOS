using System.IO;
using UnityEngine;

namespace Ro3Ios.Hooks;

public static class Paths
{
    static string _streamingAssets;

    public static string StreamingAssets()
    {
        if (_streamingAssets != null) return _streamingAssets;

        string docPath = Path.Combine(Application.persistentDataPath, "StreamingAssets");
        if (Directory.Exists(docPath) && File.Exists(Path.Combine(docPath, "Ro3BuildSetting.json")))
        {
            return _streamingAssets = docPath;
        }

        return _streamingAssets = Application.streamingAssetsPath;
    }

    public static string Documents() => Application.persistentDataPath;
}
