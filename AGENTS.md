# Working on Lro3-iOS

Rules for anyone changing this repository, engineers and AI coding agents alike.

## 1. Compliance Rule (Apple App Store Review Guidelines)

This project must strictly adhere to the `app-store-compliance` skill and Apple App Store Review Guidelines. See [.kilo/rules/app-store-compliance.md](.kilo/rules/app-store-compliance.md) for full details.

- **Privacy Manifest**: `PrivacyInfo.xcprivacy` must accompany every build with valid reason codes.
- **No Private APIs**: Prohibited from linking or invoking Darwin private APIs.
- **Account Deletion**: In-app account deletion required (Guideline 5.1.1(v)).
- **Payment Compliance**: No external web purchase links for digital items.
- **Audit Requirement**: Before creating releases or packaging IPAs, run `/app-store-audit` or verify that `app-store-compliance-guard` passes with 0 Critical findings.

## 2. iOS Native Architecture

- **Engine**: Unity 2022.3.62f3 · IL2CPP AOT ARM64 · Apple Metal (MSL) · iOS 14.0+.
- **Native Bridges**: Native C plugins (`fsni_darwin.c`, `ro3_chunk.c`, `stubs_ios.c`) compile into `libPlugins.a` and link statically into `UnityFramework`.
- **Code Stripping**: All game assemblies (`Assembly-CSharp`, `Cinemachine`, `HappyEngine.*`, `Ro3Emu.*`) must have `isPreloaded = 1` in `.meta` and be preserved in `unity-template/Assets/link.xml`.
- **Shaders**: Must provide Metal MSL shaders; fallback to `Hidden/Ro3Ios/Fallback` or `UI/Default` on load.
- **Data Layout**: Full game bundles reside in `Application.persistentDataPath/ro3_Data/StreamingAssets/VFS/` (`Documents` container).
