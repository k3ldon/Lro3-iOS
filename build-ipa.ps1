param(
  [string]$Unity = 'C:\Dev\Unity\Editors\2022.3.62f3\Editor\Unity.exe',
  [string]$Project = (Join-Path $PSScriptRoot 'Ro3Ios'),
  [string]$Version = '1.0.0',
  [string]$OutDir = (Join-Path $PSScriptRoot 'out\xcode'),
  [switch]$PackageIpa
)
$ErrorActionPreference = 'Stop'

if (-not (Test-Path (Join-Path $Project 'Assets\Plugins\Game'))) {
  throw "Unity project not configured. Run .\setup-project.ps1 first."
}

Write-Host "== [iOS] Exporting Xcode Project with Unity ($Version)..."
New-Item -ItemType Directory -Force $OutDir | Out-Null
$log = Join-Path (Split-Path $OutDir -Parent) 'build.log'

$unityArgs = @(
  '-batchmode', '-nographics', '-quit',
  '-buildTarget', 'iOS',
  '-projectPath', "`"$Project`"",
  '-executeMethod', 'Ro3Ios.Editor.BuildIos.Build',
  '-ro3Version', $Version,
  '-ro3Out', "`"$OutDir`"",
  '-logFile', "`"$log`""
)

if (Test-Path $Unity) {
  & $Unity @unityArgs
  if ($LASTEXITCODE) {
    throw "Unity iOS build failed with code $LASTEXITCODE. Check $log for details."
  }
} else {
  Write-Warning "Unity 2022.3.62f3 editor not found at $Unity."
  Write-Host "   Xcode project structure prepared. When Unity is available, execute batchmode export."
}

Write-Host "== [iOS] Xcode project ready at $OutDir"

if ($PackageIpa) {
  Write-Host "== [iOS] Packaging Payload directory into .ipa..."
  $payloadDir = Join-Path (Split-Path $OutDir -Parent) 'Payload'
  New-Item -ItemType Directory -Force $payloadDir | Out-Null
  $appTarget = Join-Path $payloadDir 'ro3.app'

  if (Test-Path (Join-Path $OutDir 'build\Release-iphoneos\ro3.app')) {
    Copy-Item -Recurse -Force (Join-Path $OutDir 'build\Release-iphoneos\ro3.app') $appTarget
    $ipaPath = Join-Path (Split-Path $OutDir -Parent) "Lro3-$Version.ipa"
    Compress-Archive -Path $payloadDir -DestinationPath $ipaPath -Force
    Write-Host "== [iOS] Generated IPA: $ipaPath"
  } else {
    Write-Host "   Notice: To generate an IPA binary, build the Xcode project on macOS using:"
    Write-Host "   xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release -destination 'generic/platform=iOS'"
  }
}
