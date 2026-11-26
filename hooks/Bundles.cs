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
        byte[] raw = File.ReadAllBytes(srcPath);

        int actualOffset = (int)offset;
        if (raw.Length >= 24 && raw[0] == 'R' && raw[1] == 'O' && raw[2] == '3' && raw[3] == 'V')
        {
            actualOffset = 24;
            Debug.Log($"[Ro3Ios.Bundles] Stripped RO3V header from {Path.GetFileName(srcPath)} (24 bytes)");
        }

        byte[] data;
        if (actualOffset == 0)
        {
            data = raw;
        }
        else
        {
            data = new byte[raw.Length - actualOffset];
            Buffer.BlockCopy(raw, actualOffset, data, 0, data.Length);
        }

        if (data.Length < 32) return false;
        string signature = ReadNullTerminatedAscii(data, 0);
        if (signature != "UnityFS") return false;

        // Scan for Unity version string "2022.3.62f3\0" followed by target platform 19 (StandaloneWindows64)
        // and retarget it to 9 (iOS)
        byte[] verBytes = Encoding.ASCII.GetBytes("2022.3.62f3\0");
        bool patched = false;
        for (int i = 0; i < data.Length - verBytes.Length - 8; i++)
        {
            bool match = true;
            for (int j = 0; j < verBytes.Length; j++)
            {
                if (data[i + j] != verBytes[j]) { match = false; break; }
            }
            if (match)
            {
                int platformOffset = i + verBytes.Length;
                int platform = BitConverter.ToInt32(data, platformOffset);
                if (platform == 19) // StandaloneWindows64
                {
                    data[platformOffset] = (byte)TargetPlatformIos; // 9 = iOS
                    data[platformOffset + 1] = 0;
                    data[platformOffset + 2] = 0;
                    data[platformOffset + 3] = 0;
                    patched = true;
                    Debug.Log($"[Ro3Ios.Bundles] Retargeted {Path.GetFileName(srcPath)} from platform {platform} to {TargetPlatformIos} (offset 0x{platformOffset:X})");
                }
            }
        }

        File.WriteAllBytes(dstPath, data);
        return true;
    }

    private static string ReadNullTerminatedAscii(byte[] data, int start)
    {
        int end = start;
        while (end < data.Length && data[end] != 0) end++;
        return Encoding.ASCII.GetString(data, start, end - start);
    }
}
