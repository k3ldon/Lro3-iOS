# App Store Compliance Rules for Lro3-iOS

All contributors, engineers, and AI coding agents working on Lro3-iOS must strictly adhere to the **Apple App Store Review Guidelines** using the `app-store-compliance` skill.

## 1. Zero Tolerance Pre-Submission Checklist

Before any build is packaged, submitted to App Store Connect, or distributed via TestFlight:

- **Privacy Manifest (`PrivacyInfo.xcprivacy`)**:
  - Must declare all required reason APIs (`NSPrivacyAccessedAPITypes`) used by Unity, C libraries, and plugins (e.g. `NSPrivacyAccessedAPICategoryFileTimestamp`, `NSPrivacyAccessedAPICategorySystemBootTime`, `NSPrivacyAccessedAPICategoryDiskSpace`).
  - Must declare all tracked data and tracking domains (`NSPrivacyTracking = false` unless explicitly configured).
- **Usage Descriptions (`Info.plist`)**:
  - `NSCameraUsageDescription`, `NSMicrophoneUsageDescription`, `NSLocationWhenInUseUsageDescription` must be populated with clear, user-facing, localized justifications.
- **Account Deletion (Guideline 5.1.1(v))**:
  - If user registration or login is provided, an in-app account deletion flow must be accessible directly within settings.
- **In-App Purchases (Guideline 3.1.1)**:
  - All digital goods, currencies, and gacha pulls must use StoreKit / Apple In-App Purchase. No external web payment links without StoreKit External Purchase entitlement.
- **App Completeness (Guideline 2.1)**:
  - No crash at startup, no missing shaders (magenta `#FF00FF`), no placeholder dead buttons, and full support for IPv6-only networks.
- **Metadata & Copy Rules (Guideline 2.3)**:
  - Never reference Android, Google Play, APK, or test versions in the UI, strings, or app metadata.
  - Screenshots must depict real in-app gameplay, not marketing splash art only.

## 2. Compliance Audit Command

Run the compliance audit at any time using:
```bash
/app-store-audit
```
Or execute the compliance guard directly:
```bash
bash ~/.config/kilo/hooks/app-store-compliance-guard.sh .
```
Any Critical or High risk finding is a **Release Blocker**.
