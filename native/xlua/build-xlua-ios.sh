#!/usr/bin/env bash
# build-xlua-ios.sh: Compiles libxlua.a for ARM64 iOS with ro3_chunk decoding.
set -euo pipefail

OUT_DIR="${1:-./build/ios}"
mkdir -p "$OUT_DIR"

ISYSROOT=$(xcrun --sdk iphoneos --show-sdk-path)
CC=$(xcrun --sdk iphoneos --find clang)

CFLAGS="-arch arm64 -isysroot $ISYSROOT -miphoneos-version-min=14.0 -O3 -fPIC -D__IOS__"

$CC $CFLAGS -c ro3_chunk.c -o "$OUT_DIR/ro3_chunk.o"
echo "ro3_chunk compiled for iOS ARM64"
