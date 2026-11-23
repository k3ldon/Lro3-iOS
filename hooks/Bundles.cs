using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ro3Ios.Hooks;

public static class Bundles
{
    private const int TargetPlatformIos = 9;
    private static readonly HashSet<string> Untouched = new HashSet<string>();
    private static string _cacheDir;

    public static AssetBundle LoadFromFile(string path) => LoadFromFile(path, 0u, 0ul);
    public static AssetBundle LoadFromFile(string path, uint crc) => LoadFromFile(path, crc, 0ul);

    public static AssetBundle LoadFromFile(string path, uint crc, ulong offset)
    {
        var cached = Retargeted(path, offset);
        return cached != null ? AssetBundle.LoadFromFile(cached) : AssetBundle.LoadFromFile(path, crc, offset);
    }

    public static AssetBundleCreateRequest LoadFromFileAsync(string path) => LoadFromFileAsync(path, 0u, 0ul);
    public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc) => LoadFromFileAsync(path, crc, 0ul);

    public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc, ulong offset)
    {
        var cached = Retargeted(path, offset);
        return cached != null ? AssetBundle.LoadFromFileAsync(cached) : AssetBundle.LoadFromFileAsync(path, crc, offset);
    }

    private static string CacheDir()
    {
        if (_cacheDir != null) return _cacheDir;
        _cacheDir = Path.Combine(Application.temporaryCachePath, "bundle-cache");
        Directory.CreateDirectory(_cacheDir);
        return _cacheDir;
    }

    private static string Retargeted(string path, ulong offset)
    {
        string key = $"{path}:{offset}";
        lock (Untouched)
        {
            if (Untouched.Contains(key)) return null;
        }

        string hashName = $"{path.GetHashCode():x8}_{offset:x8}.bundle";
        string targetPath = Path.Combine(CacheDir(), hashName);
        if (File.Exists(targetPath)) return targetPath;

        try
        {
            if (!RetargetBundle(path, offset, targetPath))
            {
                lock (Untouched) { Untouched.Add(key); }
                return null;
            }
            return targetPath;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ro3Ios.Bundles] Failed to retarget {path} at {offset}: {e.Message}");
            lock (Untouched) { Untouched.Add(key); }
            return null;
        }
    }

    private static bool RetargetBundle(string srcPath, ulong offset, string dstPath)
    {
        if (!File.Exists(srcPath)) return false;
        using var fs = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (offset > 0) fs.Seek((long)offset, SeekOrigin.Begin);

        byte[] header = new byte[128];
        int read = fs.Read(header, 0, header.Length);
        if (read < 16) return false;

        string signature = ReadNullTerminatedAscii(header, 0);
        if (signature != "UnityFS") return false;

        fs.Seek((long)offset, SeekOrigin.Begin);
        using var outFs = new FileStream(dstPath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.CopyTo(outFs);
        return true;
    }

    private static string ReadNullTerminatedAscii(byte[] data, int start)
    {
        int end = start;
        while (end < data.Length && data[end] != 0) end++;
        return Encoding.ASCII.GetString(data, start, end - start);
    }
}
