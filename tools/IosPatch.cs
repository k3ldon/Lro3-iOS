#:package Mono.Cecil@0.11.6
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Text.RegularExpressions;

var managed = args[0];
var hooksPath = args[1];

var skip = new Regex(@"^(UnityEngine(\..+)?|Unity\..+|mscorlib|netstandard|System(\..+)?|Mono\..+|Microsoft\..+|Ro3Ios\.Hooks)\.dll$");
var noInit = new Regex(@"^(RecoveryLaunchGate|HL\.Windows\..+|DeNA\.Anjin\.Launcher)$");

var windowsDll = new Regex(@"^(kernel32|user32|imm32|winmm|advapi32|gdi32|shell32|shcore|dwmapi|ntdll|ole32|psapi|version|dbghelp|setupapi|iphlpapi|ws2_32|winhttp|wininet|crypt32|comdlg32"
    + @"|AkUnitySoundEngine|ImSDK|gmesdk|AVProVideo|AVProVideoWinRT|libvlc|CrashSight64|httpdnsplugin|NetworkDiagnosis|windowsplugin|native-lib)(\.dll)?$", RegexOptions.IgnoreCase);

var iosStaticLibs = new Regex(@"^(fsni|libfsni|xlua|libxlua|sqlite3|libsqlite3|cjson|ktx|RecastDll)(\.dll|\.so)?$", RegexOptions.IgnoreCase);

using var hooks = AssemblyDefinition.ReadAssembly(hooksPath);
var streamingAssets = hooks.MainModule.GetType("Ro3Ios.Hooks.Paths").Methods.Single(m => m.Name == "StreamingAssets");
var bundleHooks = hooks.MainModule.GetType("Ro3Ios.Hooks.Bundles").Methods.Where(m => m.Name.StartsWith("LoadFromFile")).ToList();
var shaderFind = hooks.MainModule.GetType("Ro3Ios.Hooks.Shaders").Methods.Single(m => m.Name == "Find");
var materialCreate = hooks.MainModule.GetType("Ro3Ios.Hooks.Materials").Methods.Single(m => m.Name == "Create");
var materialCopy = hooks.MainModule.GetType("Ro3Ios.Hooks.Materials").Methods.Single(m => m.Name == "Copy");
var unityPc = hooks.MainModule.GetType("Ro3Ios.Hooks.Platform").Methods.Single(m => m.Name == "UnityPc");
var luaLog = hooks.MainModule.GetType("Ro3Ios.Hooks.Platform").Methods.Single(m => m.Name == "LuaLog");
var fixLoaded = hooks.MainModule.GetType("Ro3Ios.Hooks.Materials").Methods.Single(m => m.Name == "FixLoaded");
var fixInstance = hooks.MainModule.GetType("Ro3Ios.Hooks.Materials").Methods.Single(m => m.Name == "FixInstance");

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(managed);
var rewritten = new List<(string File, byte[] Bytes)>();

foreach (var file in Directory.GetFiles(managed, "*.dll").OrderBy(f => f))
{
    var name = Path.GetFileName(file);
    if (skip.IsMatch(name)) continue;
    using var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(file)), new ReaderParameters { AssemblyResolver = resolver });
    MethodReference? target = null;
    ModuleReference? internalMod = null;
    int redirected = 0, dead = 0, stubbed = 0, inits = 0, bundleLoads = 0, windowsCalls = 0, staticInternal = 0, shaderFinds = 0, platformChecks = 0, materials = 0, instances = 0;

    foreach (var type in asm.MainModule.GetTypes())
    {
        foreach (var method in type.Methods)
        {
            if (noInit.IsMatch(type.FullName))
            {
                int before = method.CustomAttributes.Count;
                for (int i = method.CustomAttributes.Count - 1; i >= 0; i--)
                    if (method.CustomAttributes[i].AttributeType.FullName == "UnityEngine.RuntimeInitializeOnLoadMethodAttribute")
                        method.CustomAttributes.RemoveAt(i);
                inits += before - method.CustomAttributes.Count;
            }

            if (type.FullName == "HybridCLR.RuntimeApi" && method.IsInternalCall)
            {
                StubReturningZero(method);
                stubbed++;
                continue;
            }

            if (method.HasPInvokeInfo)
            {
                string modName = method.PInvokeInfo.Module.Name;
                if (iosStaticLibs.IsMatch(modName))
                {
                    internalMod ??= GetOrAddModuleReference(asm.MainModule, "__Internal");
                    method.PInvokeInfo.Module = internalMod;
                    staticInternal++;
                    continue;
                }
                if (windowsDll.IsMatch(modName))
                {
                    StubReturningDefault(method);
                    windowsCalls++;
                    continue;
                }
            }

            if (!method.HasBody) continue;

            if (type.FullName == "LA_Utils" && type.Properties.Any(p => p.Name == "Obf_i" && p.GetMethod == method)
                && method.Body.Instructions.Count == 2
                && method.Body.Instructions[0].OpCode == OpCodes.Ldc_I4_1)
            {
                method.Body.Instructions[0].OpCode = OpCodes.Call;
                method.Body.Instructions[0].Operand = asm.MainModule.ImportReference(unityPc);
                platformChecks++;
            }

            if (type.FullName == "HappyEngine.GS_Debug" && type.Properties.Any(p => p.Name == "ShowLog" && p.GetMethod == method)
                && method.Body.Instructions.Count == 2 && method.Body.Instructions[0].OpCode == OpCodes.Ldc_I4_1)
            {
                method.Body.Instructions[0].OpCode = OpCodes.Call;
                method.Body.Instructions[0].Operand = asm.MainModule.ImportReference(luaLog);
                platformChecks++;
            }

            if (type.FullName == "HappyEngine.Runtime.NewRuntimeSystem.RecoveryLuaRuntimeContext"
                || type.FullName == "HappyEngine.WorldStream.WorldStreamTool.TerrainLightmapInfo")
            {
                foreach (var inst in method.Body.Instructions)
                    if (inst.OpCode == OpCodes.Call && inst.Operand is MethodReference pr
                        && pr.DeclaringType.FullName == "UnityEngine.Application" && pr.Name == "get_platform")
                    {
                        inst.OpCode = OpCodes.Ldc_I4;
                        inst.Operand = 8;
                        platformChecks++;
                    }
            }

            if (type.FullName == "HappyEngine.Runtime.NewRuntimeSystem.AssetElement" && method.Name == "DoLoadCompleted")
            {
                var getAsset = type.Properties.Single(p => p.Name == "Asset").GetMethod;
                var il = method.Body.GetILProcessor();
                var first = method.Body.Instructions[0];
                il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(first, il.Create(OpCodes.Call, getAsset));
                il.InsertBefore(first, il.Create(OpCodes.Call, asm.MainModule.ImportReference(fixLoaded)));
                materials++;
            }

            foreach (var inst in method.Body.Instructions)
            {
                if (inst.OpCode == OpCodes.Newobj && inst.Operand is MethodReference ctor && ctor.DeclaringType.FullName == "UnityEngine.Material"
                    && ctor.Parameters.Count == 1)
                {
                    var from = ctor.Parameters[0].ParameterType.FullName;
                    if (from != "UnityEngine.Shader" && from != "UnityEngine.Material") continue;
                    inst.OpCode = OpCodes.Call;
                    inst.Operand = asm.MainModule.ImportReference(from == "UnityEngine.Shader" ? materialCreate : materialCopy);
                    materials++;
                }

                if (inst.OpCode != OpCodes.Call || inst.Operand is not MethodReference mr) continue;

                if (mr.DeclaringType.FullName == "UnityEngine.Application" && mr.Name == "get_streamingAssetsPath")
                {
                    inst.Operand = target ??= asm.MainModule.ImportReference(streamingAssets);
                    redirected++;
                }
                else if (mr.DeclaringType.FullName == "UnityEngine.Shader" && mr.Name == "Find")
                {
                    inst.Operand = asm.MainModule.ImportReference(shaderFind);
                    shaderFinds++;
                }
                else if (mr.DeclaringType.FullName == "UnityEngine.AssetBundle" && mr.Name.StartsWith("LoadFromFile"))
                {
                    var hook = bundleHooks.FirstOrDefault(h => MatchesSignature(h, mr));
                    if (hook != null)
                    {
                        inst.Operand = asm.MainModule.ImportReference(hook);
                        bundleLoads++;
                    }
                }
            }
        }
    }

    var mem = new MemoryStream();
    asm.Write(mem);
    rewritten.Add((file, mem.ToArray()));
    Console.WriteLine($"[IosPatch] {name}: __Internal={staticInternal}, winStubs={windowsCalls}, streamingAssets={redirected}, bundles={bundleLoads}, shaders={shaderFinds}");
}

resolver.Dispose();

foreach (var (path, bytes) in rewritten)
{
    File.WriteAllBytes(path, bytes);
}

Console.WriteLine($"[IosPatch] Complete: {rewritten.Count} assemblies processed for iOS.");

static ModuleReference GetOrAddModuleReference(ModuleDefinition module, string name)
{
    foreach (var mod in module.ModuleReferences)
        if (mod.Name == name) return mod;
    var newMod = new ModuleReference(name);
    module.ModuleReferences.Add(newMod);
    return newMod;
}

static void StubReturningDefault(MethodDefinition method)
{
    method.IsPInvokeImpl = false;
    method.PInvokeInfo = null;
    method.Body = new MethodBody(method);
    var il = method.Body.GetILProcessor();
    var ret = method.ReturnType;
    if (ret.FullName == "System.Void") il.Emit(OpCodes.Ret);
    else if (ret.FullName == "System.Boolean" || ret.FullName == "System.Int32" || ret.FullName == "System.UInt32" || ret.FullName == "System.Int16" || ret.FullName == "System.UInt16" || ret.FullName == "System.Byte")
    {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);
    }
    else if (ret.FullName == "System.Int64" || ret.FullName == "System.UInt64")
    {
        il.Emit(OpCodes.Ldc_I8, 0L);
        il.Emit(OpCodes.Ret);
    }
    else if (ret.FullName == "System.IntPtr")
    {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Conv_I);
        il.Emit(OpCodes.Ret);
    }
    else
    {
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ret);
    }
}

static void StubReturningZero(MethodDefinition method)
{
    method.IsInternalCall = false;
    method.Body = new MethodBody(method);
    var il = method.Body.GetILProcessor();
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Ret);
}

static bool MatchesSignature(MethodReference a, MethodReference b)
{
    if (a.Name != b.Name || a.Parameters.Count != b.Parameters.Count) return false;
    for (int i = 0; i < a.Parameters.Count; i++)
        if (a.Parameters[i].ParameterType.FullName != b.Parameters[i].ParameterType.FullName) return false;
    return true;
}
