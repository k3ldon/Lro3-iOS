using System.Collections.Generic;
using UnityEngine;

namespace Ro3Ios.Shell
{
    public static class ShaderFixer
    {
        private static bool _installed;
        private static Shader _fallbackShader;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            _fallbackShader = Shader.Find("Hidden/Ro3Ios/Fallback");
            if (_fallbackShader == null)
            {
                _fallbackShader = Shader.Find("UI/Default");
            }

            Ro3Ios.Hooks.Materials.Remap = RemapShader;
            Ro3Ios.Hooks.Materials.Fix = FixMaterial;

            Debug.Log("[Ro3Ios.ShaderFixer] Installed Metal shader fixer hook");
        }

        private static Shader RemapShader(Shader original)
        {
            if (original == null) return _fallbackShader;
            if (original.isSupported) return original;
            return _fallbackShader;
        }

        private static void FixMaterial(Material mat)
        {
            if (mat == null) return;
            if (!mat.shader.isSupported)
            {
                mat.shader = _fallbackShader;
            }
        }
    }
}
