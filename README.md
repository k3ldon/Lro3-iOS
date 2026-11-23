# Lro3 iOS Client Port & Build Pipeline

Repositorio público oficial para el port nativo de **Ragnarok Online 3 (Lro3)** en Apple **iOS / iPadOS** (ARM64 / Metal).

## Arquitectura y Características
- **Backend IL2CPP AOT**: Ejecución estricta Ahead-Of-Time sin JIT para cumplir con las políticas del kernel Darwin.
- **Enlace Estático `__Internal`**: Reescritura CIL de P/Invokes (`Mono.Cecil`) para que librerías nativas (`fsni`, `xlua`, `sqlite3`) se resuelvan estáticamente en el enlace Mach-O de Xcode.
- **Pipeline Gráfico Apple Metal**: Shaders traducidos a Metal Shading Language (MSL) y fallbacks no destructivos.
- **Soporte de Notch y Dynamic Island**: Adaptación reactiva de la interfaz al área segura (`Screen.safeArea`).
- **Transferencia USB sin Jailbreak**: `UIFileSharingEnabled = true` en `Info.plist` para copiar datos mediante Finder o iTunes.

## GitHub Actions CI/CD
El workflow en `.github/workflows/build-ipa.yml` corre sobre runners oficiales de Apple Silicon **`macos-14`**:
1. Compila los puentes estáticos de C (`fsni_darwin.c`, `ro3_chunk.c`, `stubs_ios.c`).
2. Genera y enlaza el ejecutable ARM64 con Clang / LLVM.
3. Empaqueta el contenedor `Payload/ro3.app` en un archivo `Lro3-iOS.ipa`.
4. Publica el IPA directamente en GitHub Releases y en los artefactos de la ejecución.

## Sideloading (Instalación en iPhone)
Una vez descargado el `.ipa` generado por GitHub Actions:
- **AltStore / SideStore**: Instala y renueva la app semanalmente con tu Apple ID gratuito.
- **Sideloadly**: Instalación directa por cable USB.
- **TrollStore**: Instalación permanente en iOS 14.0 - 17.0.
