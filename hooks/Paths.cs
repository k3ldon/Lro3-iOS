using System.IO;
using UnityEngine;

namespace Ro3Ios.Hooks;

public static class Paths
{
    static string _streamingAssets;

    public static string StreamingAssets()
    {
        if (_streamingAssets != null) return _streamingAssets;

        string ro3DocPath = Path.Combine(Application.persistentDataPath, "ro3_Data", "StreamingAssets");
        if (Directory.Exists(ro3DocPath) && File.Exists(Path.Combine(ro3DocPath, "Ro3BuildSetting.json")))
        {
            return _streamingAssets = ro3DocPath;
        }

        string docPath = Path.Combine(Application.persistentDataPath, "StreamingAssets");
        if (Directory.Exists(docPath) && File.Exists(Path.Combine(docPath, "Ro3BuildSetting.json")))
        {
            return _streamingAssets = docPath;
        }

        if (Directory.Exists(ro3DocPath)) return _streamingAssets = ro3DocPath;
        if (Directory.Exists(docPath)) return _streamingAssets = docPath;

        return _streamingAssets = Application.streamingAssetsPath;
    }

    public static string Documents() => Application.persistentDataPath;
}
