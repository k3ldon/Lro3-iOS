param(
  [Parameter(Mandatory)][string]$Server,
  [string]$Client = $(if (Test-Path 'D:\Lagnarok\Client') { 'D:\Lagnarok\Client' } elseif (Test-Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'Client\Ragnarok3')) { (Join-Path (Split-Path $PSScriptRoot -Parent) 'Client\Ragnarok3') } else { 'D:\Lagnarok\Client' }),
  [string]$Pem = $(if (Test-Path (Join-Path $PSScriptRoot 'data\gate_rsa_public.pem')) { (Join-Path $PSScriptRoot 'data\gate_rsa_public.pem') } elseif (Test-Path 'C:\Users\Carlos\Downloads\ro3emu-server\data\gate_rsa_public.pem') { 'C:\Users\Carlos\Downloads\ro3emu-server\data\gate_rsa_public.pem' } else { (Join-Path (Split-Path $PSScriptRoot -Parent) 'Client\RO3Emu\data\gate_rsa_public.pem') }),
  [string]$Project = (Join-Path $PSScriptRoot 'Ro3Ios'),
  [string]$Unity = 'C:\Dev\Unity\Editors\2022.3.62f3\Editor\Unity.exe',
  [switch]$VerboseLogs
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dotnet = if (Test-Path "$env:USERPROFILE\.dotnet\dotnet.exe") { "$env:USERPROFILE\.dotnet\dotnet.exe" } else { "dotnet" }

if ($Server -match '://(127\.0\.0\.1|localhost)') {
  Write-Warning "$Server is the iOS device itself; for iPhone testing, use the PC's LAN IP or public domain"
}
foreach ($p in $Client, $Pem) { if (-not (Test-Path $p)) { throw "not found: $p" } }

Write-Host '== [iOS] Locating ClientTools'
$tools = Join-Path $PSScriptRoot 'tools\ClientTools\Ro3Emu.ClientTools.exe'
if (-not (Test-Path $tools)) {
  $tools = Join-Path $root 'src\Ro3Emu.ClientTools\bin\Debug\net10.0\win-x64\Ro3Emu.ClientTools.exe'
}
if (-not (Test-Path $tools) -and (Test-Path (Join-Path $root 'src\Ro3Emu.ClientTools'))) {
  & $dotnet build (Join-Path $root 'src\Ro3Emu.ClientTools') --no-incremental -v q | Out-Null
  $tools = Join-Path $root 'src\Ro3Emu.ClientTools\bin\Debug\net10.0\win-x64\Ro3Emu.ClientTools.exe'
}
if (-not (Test-Path $tools)) { throw "Ro3Emu.ClientTools.exe not found at $tools" }

Write-Host "== [iOS] Staging and patching client DLLs for $Server"
$stage = Join-Path $PSScriptRoot 'build\stage'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force (Join-Path $stage 'ro3_Data') | Out-Null
Copy-Item -Recurse (Join-Path $Client 'ro3_Data\Managed') (Join-Path $stage 'ro3_Data\Managed')
$backup = Join-Path $Client 'ro3emu-backup'
if (Test-Path $backup) { Copy-Item -Recurse $backup (Join-Path $stage 'ro3emu-backup') }
New-Item -ItemType File (Join-Path $stage 'ro3.exe') | Out-Null

$patchArgs = @('patch', $stage, $Server.TrimEnd('/'), $Pem)
if ($VerboseLogs) { $patchArgs += '--verbose' }
& $tools @patchArgs
if ($LASTEXITCODE) { throw 'patch failed' }

Write-Host '== [iOS] Building Ro3Ios.Hooks'
& $dotnet build (Join-Path $PSScriptRoot 'hooks\Ro3Ios.Hooks.csproj') -v q | Out-Null
if ($LASTEXITCODE) { throw 'build Ro3Ios.Hooks failed' }
$hooks = Join-Path $PSScriptRoot 'hooks\bin\Debug\netstandard2.1\Ro3Ios.Hooks.dll'

Write-Host '== [iOS] Running iOS CIL Rewrites (IosPatch.cs)'
& $dotnet run (Join-Path $PSScriptRoot 'tools\IosPatch.cs') -- (Join-Path $stage 'ro3_Data\Managed') $hooks
if ($LASTEXITCODE) { throw 'IosPatch failed' }

Write-Host "== [iOS] Assembling Unity Project in $Project"
$template = Join-Path $PSScriptRoot 'unity-template'
foreach ($f in Get-ChildItem $template -Recurse -File) {
  $target = Join-Path $Project $f.FullName.Substring($template.Length + 1)
  New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
  Copy-Item -Force $f.FullName $target
}

$game = Join-Path $Project 'Assets\Plugins\Game'
if (Test-Path $game) { Remove-Item -Recurse -Force $game }
New-Item -ItemType Directory -Force $game | Out-Null

$engineRegex = '^(UnityEngine(\.[A-Za-z0-9]+Module)?|mscorlib|netstandard|System|System\.(Configuration|Core|Data|Drawing|IO\.Compression(\.FileSystem)?|Net\.Http|Numerics|Runtime\.Serialization|Transactions|Xml(\.Linq)?)|Mono\.Security)\.dll$'
foreach ($dll in Get-ChildItem (Join-Path $stage 'ro3_Data\Managed') -Filter '*.dll') {
  if ($dll.Name -match $engineRegex) { continue }
  Copy-Item -Force $dll.FullName $game
}

$iosPlugins = Join-Path $Project 'Assets\Plugins\iOS'
New-Item -ItemType Directory -Force $iosPlugins | Out-Null
Copy-Item -Force (Join-Path $PSScriptRoot 'native\fsni\fsni_darwin.c') $iosPlugins
Copy-Item -Force (Join-Path $PSScriptRoot 'native\stubs\stubs_ios.c') $iosPlugins
Copy-Item -Force (Join-Path $PSScriptRoot 'native\xlua\ro3_chunk.c') $iosPlugins

Write-Host '== [iOS] Project setup complete!'
Write-Host "   Run .\build-ipa.ps1 to generate the Xcode project."
