using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Ro3Ios.Hooks;
using UnityEngine;

namespace Ro3Ios.Shell
{
    public class Ro3Startup : MonoBehaviour
    {
        private const string StartupBundleName = "startup_bundle";
        private const string LauncherPrefabPath = "Assets/Scenes/Prefabs/Launcher.prefab";

        public static string VfsRoot => Path.Combine(Paths.StreamingAssets(), "VFS");

        public void Awake()
        {
            StartLauncher();
        }

        private void StartLauncher()
        {
            string relPath = GetBundleRelativePath(HashString(StartupBundleName));
            string fullPath = ResolveBundlePath(relPath);
            Debug.Log("[Ro3Startup] Loading startup bundle: " + fullPath);

            AssetBundle bundle = Bundles.LoadFromFile(fullPath);
            if (bundle == null)
            {
                Debug.LogErrorFormat("[Ro3Startup] Launcher bundle not found at: {0}", fullPath);
                return;
            }

            string assetKey = HashString(Path.GetFileName(LauncherPrefabPath)).ToString("x");
            Debug.Log("[Ro3Startup] Loading launcher prefab key: " + assetKey);

            UnityEngine.Object prefab = bundle.LoadAsset(assetKey);
            if (prefab == null)
            {
                Debug.LogError("[Ro3Startup] Launcher prefab not found in bundle");
                bundle.Unload(false);
                return;
            }

            Debug.Log("[Ro3Startup] Instantiating launcher prefab: " + prefab.name);
            var inst = UnityEngine.Object.Instantiate(prefab);
            bundle.Unload(false);

            var go = inst as GameObject;
            if (go != null)
            {
                if (go.GetComponent<HappyEngine.Base.CoroutineManager>() == null)
                {
                    go.AddComponent<HappyEngine.Base.CoroutineManager>();
                }

                // Check if the official game Launcher (from Assembly-CSharp) is attached
                var components = go.GetComponents<MonoBehaviour>();
                bool hasOfficialLauncher = false;
                foreach (var c in components)
                {
                    if (c != null && c.GetType().Name == "Launcher" && string.IsNullOrEmpty(c.GetType().Namespace))
                    {
                        hasOfficialLauncher = true;
                        Debug.Log("[Ro3Startup] Official Assembly-CSharp Launcher component found on prefab! Letting official URP boot sequence run...");
                        break;
                    }
                }

                if (!hasOfficialLauncher && go.GetComponent<Ro3Ios.Shell.Launcher>() == null)
                {
                    Debug.Log("[Ro3Startup] Official Launcher not found on prefab, attaching Ro3Ios.Shell.Launcher fallback");
                    go.AddComponent<Ro3Ios.Shell.Launcher>();
                }
            }

            Destroy(gameObject);
        }

        private static string ResolveBundlePath(string relPath)
        {
            string[] searchRoots = new[]
            {
                Path.Combine(Application.persistentDataPath, "ro3_Data", "StreamingAssets", "VFS"),
                Path.Combine(Application.persistentDataPath, "StreamingAssets", "VFS"),
                Path.Combine(Application.persistentDataPath, "VFS"),
                Path.Combine(Application.persistentDataPath, "ro3_Data", "StreamingAssets"),
                Path.Combine(Application.persistentDataPath, "StreamingAssets"),
                Path.Combine(Paths.StreamingAssets(), "VFS"),
                Path.Combine(Application.streamingAssetsPath, "VFS")
            };

            foreach (var root in searchRoots)
            {
                string candidate = Path.Combine(root, relPath);
                if (File.Exists(candidate))
                {
                    Debug.Log("[Ro3Startup] Found startup bundle at: " + candidate);
                    return candidate;
                }
            }

            Debug.LogWarning("[Ro3Startup] Searching persistentDataPath for " + Path.GetFileName(relPath) + "...");
            try
            {
                var found = Directory.GetFiles(Application.persistentDataPath, Path.GetFileName(relPath), SearchOption.AllDirectories);
                if (found.Length > 0)
                {
                    Debug.Log("[Ro3Startup] Found startup bundle recursively at: " + found[0]);
                    return found[0];
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Ro3Startup] Recursive search error: " + ex.Message);
            }

            return Path.Combine(Paths.StreamingAssets(), "VFS", relPath);
        }

        private static string GetBundleRelativePath(ulong hash)
        {
            return GetPrefix(hash) + "/" + hash.ToString("x") + ".bundle";
        }

        private static string GetPrefix(ulong hash)
        {
            return hash.ToString("x16").Substring(0, 2);
        }

        public static ulong HashString(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            byte[] bytes = Encoding.UTF8.GetBytes(text.ToLowerInvariant());
            return HashBytes(bytes, 0uL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashBytes(ReadOnlySpan<byte> data, ulong seed = 0uL)
        {
            ulong h1 = seed;
            ulong h2 = seed;
            int length = data.Length;
            int numChunks = length / 16;
            for (int i = 0; i < numChunks; i++)
            {
                int offset = i * 16;
                ulong k1 = BitConverter.ToUInt64(data.Slice(offset, 8));
                ulong k2 = BitConverter.ToUInt64(data.Slice(offset + 8, 8));
                k1 *= 9782798678568883157uL;
                k1 = Rotl64(k1, 31);
                k1 *= 5545529020109919103L;
                h1 ^= k1;
                h1 = Rotl64(h1, 27);
                h1 += h2;
                h1 = h1 * 5 + 1390208809;
                k2 *= 5545529020109919103L;
                k2 = Rotl64(k2, 33);
                k2 *= 9782798678568883157uL;
                h2 ^= k2;
                h2 = Rotl64(h2, 31);
                h2 += h1;
                h2 = h2 * 5 + 944331445;
            }

            int tailOffset = numChunks * 16;
            ulong t1 = 0uL;
            ulong t2 = 0uL;
            int rem = length & 0xF;
            if (rem > 0)
            {
                ReadOnlySpan<byte> tail = data.Slice(tailOffset);
                if (rem >= 15) t2 ^= (ulong)tail[14] << 48;
                if (rem >= 14) t2 ^= (ulong)tail[13] << 40;
                if (rem >= 13) t2 ^= (ulong)tail[12] << 32;
                if (rem >= 12) t2 ^= (ulong)tail[11] << 24;
                if (rem >= 11) t2 ^= (ulong)tail[10] << 16;
                if (rem >= 10) t2 ^= (ulong)tail[9] << 8;
                if (rem >= 9)
                {
                    t2 ^= tail[8];
                    t2 *= 5545529020109919103L;
                    t2 = Rotl64(t2, 33);
                    t2 *= 9782798678568883157uL;
                    h2 ^= t2;
                }
                if (rem >= 8)
                {
                    t1 ^= BitConverter.ToUInt64(tail);
                }
                else
                {
                    if (rem >= 7) t1 ^= (ulong)tail[6] << 48;
                    if (rem >= 6) t1 ^= (ulong)tail[5] << 40;
                    if (rem >= 5) t1 ^= (ulong)tail[4] << 32;
                    if (rem >= 4) t1 ^= (ulong)tail[3] << 24;
                    if (rem >= 3) t1 ^= (ulong)tail[2] << 16;
                    if (rem >= 2) t1 ^= (ulong)tail[1] << 8;
                    if (rem >= 1) t1 ^= tail[0];
                }

                t1 *= 9782798678568883157uL;
                t1 = Rotl64(t1, 31);
                t1 *= 5545529020109919103L;
                h1 ^= t1;
            }

            h1 ^= (ulong)length;
            h2 ^= (ulong)length;
            h1 += h2;
            h2 += h1;
            h1 = Fmix64(h1);
            h2 = Fmix64(h2);
            return h1 + h2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rotl64(ulong val, byte shift)
        {
            return (val << (int)shift) | (val >> (64 - (int)shift));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Fmix64(ulong k)
        {
            k ^= k >> 33;
            k *= 18397679294719823053uL;
            k ^= k >> 33;
            k *= 14181476777654086739uL;
            k ^= k >> 33;
            return k;
        }
    }
}
