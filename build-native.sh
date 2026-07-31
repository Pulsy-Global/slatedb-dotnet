#!/usr/bin/env bash
set -euo pipefail

# Build SlateDB UniFFI native libraries.
#
# Usage:
#   ./build-native.sh [--all] [path-to-slatedb-source]
#
# Without --all: builds only for the current platform (fast).
# With    --all: builds every target supported by the current host.
#
# If no source path is provided, clones slatedb into .slatedb-src/ automatically.
#
# Override the default SlateDB ref with:
#   SLATEDB_REF=<tag-or-sha> ./build-native.sh
#
# Requirements:
#   - rustup with the SlateDB toolchain (1.91.1 by default)
#   - For Linux --all: cargo-zigbuild + zig

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SLATEDB_CLONE_DIR="$SCRIPT_DIR/.slatedb-src"
SLATEDB_REF="${SLATEDB_REF:-v0.15.0}"
SLATEDB_RUST_TOOLCHAIN="${SLATEDB_RUST_TOOLCHAIN:-1.91.1}"
HOST_OS="$(uname -s)"

# Parse arguments
BUILD_ALL=false
SLATEDB_SRC=""
for arg in "$@"; do
    case "$arg" in
        --all) BUILD_ALL=true ;;
        *)     SLATEDB_SRC="$arg" ;;
    esac
done

# Clone or update source
if [ -z "$SLATEDB_SRC" ]; then
    if [ -d "$SLATEDB_CLONE_DIR/.git" ]; then
        if [ -n "$(git -C "$SLATEDB_CLONE_DIR" status --porcelain)" ]; then
            echo "Error: $SLATEDB_CLONE_DIR has local changes. Clean it or pass an explicit source path." >&2
            exit 1
        fi
        echo "Checking out slatedb $SLATEDB_REF in $SLATEDB_CLONE_DIR"
        git -C "$SLATEDB_CLONE_DIR" fetch --depth 1 origin "$SLATEDB_REF"
        git -C "$SLATEDB_CLONE_DIR" checkout --detach FETCH_HEAD
    else
        echo "Cloning slatedb $SLATEDB_REF..."
        git clone --depth 1 --branch "$SLATEDB_REF" https://github.com/slatedb/slatedb.git "$SLATEDB_CLONE_DIR"
    fi
    SLATEDB_SRC="$SLATEDB_CLONE_DIR"
else
    echo "Using provided slatedb source: $SLATEDB_SRC"
fi

if [ ! -f "$SLATEDB_SRC/Cargo.toml" ]; then
    echo "Error: $SLATEDB_SRC/Cargo.toml not found." >&2
    exit 1
fi

RUNTIMES_DIR="$SCRIPT_DIR/runtimes"

# .NET RID / Rust target / output library name (parallel arrays, bash 3.2 compatible)
RIDS=(         osx-arm64              osx-x64                linux-arm64                 linux-x64                   win-arm64                    win-x64               )
RUST_TARGETS=( aarch64-apple-darwin   x86_64-apple-darwin    aarch64-unknown-linux-gnu   x86_64-unknown-linux-gnu    aarch64-pc-windows-msvc      x86_64-pc-windows-msvc )
LIB_NAMES=(   libslatedb_uniffi.dylib libslatedb_uniffi.dylib libslatedb_uniffi.so         libslatedb_uniffi.so         slatedb_uniffi.dll           slatedb_uniffi.dll     )

# Invoke the pinned toolchain explicitly so this also works with Windows paths.
CARGO=(rustup run "$SLATEDB_RUST_TOOLCHAIN" cargo)
RUSTC_BIN="$(rustup which --toolchain "$SLATEDB_RUST_TOOLCHAIN" rustc)"
echo "Using: $("${CARGO[@]}" --version), $("$RUSTC_BIN" --version)"

# Detect native platform RID
detect_native_rid() {
    local arch
    case "$(uname -m)" in
        arm64|aarch64) arch="arm64" ;;
        *)             arch="x64" ;;
    esac
    case "$HOST_OS" in
        Darwin)              echo "osx-$arch" ;;
        Linux)               echo "linux-$arch" ;;
        MINGW*|MSYS*|CYGWIN*) echo "win-$arch" ;;
        *)                   echo "linux-$arch" ;;
    esac
}
NATIVE_RID="$(detect_native_rid)"

# Only Linux cross-architecture builds need zigbuild. macOS and Windows use
# their platform SDK/toolchain for the other architecture.
HAS_ZIGBUILD=false
if "${CARGO[@]}" zigbuild --help &>/dev/null; then
    HAS_ZIGBUILD=true
fi

if [ "$BUILD_ALL" = true ] && [ "$HOST_OS" = "Linux" ] && [ "$HAS_ZIGBUILD" = false ]; then
    echo "Error: Linux --all requires cargo-zigbuild and zig." >&2
    exit 1
fi

SUCCEEDED=""
FAILED=""

for i in "${!RIDS[@]}"; do
    RID="${RIDS[$i]}"
    TARGET="${RUST_TARGETS[$i]}"
    LIB_NAME="${LIB_NAMES[$i]}"
    OUT_DIR="$RUNTIMES_DIR/$RID/native"

    # Skip non-native platforms unless --all
    if [ "$BUILD_ALL" = false ] && [ "$RID" != "$NATIVE_RID" ]; then
        continue
    fi

    # Native libraries are built on their corresponding operating system.
    case "$HOST_OS:$RID" in
        Darwin:osx-*|Linux:linux-*|MINGW*:win-*|MSYS*:win-*|CYGWIN*:win-*) ;;
        *)
            echo "  Skipping $RID (unsupported on $HOST_OS host)"
            continue ;;
    esac

    echo ""
    echo "=== Building $RID ($TARGET) ==="

    rustup target add --toolchain "$SLATEDB_RUST_TOOLCHAIN" "$TARGET"
    mkdir -p "$OUT_DIR"

    # Zig provides the Linux cross-linker. Platform toolchains handle macOS
    # and Windows cross-architecture builds directly.
    if [ "$HOST_OS" = "Linux" ] && [ "$RID" != "$NATIVE_RID" ]; then
        BUILD_CMD=("${CARGO[@]}" zigbuild)
    else
        # cargo resolves rustc through PATH independently. Pin it explicitly so
        # a system/Homebrew Rust installation cannot override the rustup toolchain.
        BUILD_CMD=(env "RUSTC=$RUSTC_BIN" "${CARGO[@]}" build)
    fi

    if "${BUILD_CMD[@]}" --release -p slatedb-uniffi --target "$TARGET" \
        --manifest-path "$SLATEDB_SRC/Cargo.toml" 2>&1; then

        SRC="$SLATEDB_SRC/target/$TARGET/release/$LIB_NAME"
        if [ -f "$SRC" ]; then
            cp "$SRC" "$OUT_DIR/$LIB_NAME"

            # Strip debug symbols to reduce size
            case "$LIB_NAME" in
                *.dylib) strip -x "$OUT_DIR/$LIB_NAME" 2>/dev/null || true ;;
                *.so)    strip --strip-debug "$OUT_DIR/$LIB_NAME" 2>/dev/null || true ;;
                *.dll)   strip --strip-debug "$OUT_DIR/$LIB_NAME" 2>/dev/null || true ;;
            esac

            SIZE=$(du -h "$OUT_DIR/$LIB_NAME" | cut -f1)
            echo "  -> $OUT_DIR/$LIB_NAME ($SIZE)"
            SUCCEEDED="$SUCCEEDED $RID"
        else
            echo "  Error: built successfully but $SRC not found" >&2
            FAILED="$FAILED $RID"
        fi
    else
        echo "  Build failed for $RID" >&2
        FAILED="$FAILED $RID"
    fi
done

echo ""
echo "=== Summary ==="
echo "Succeeded:${SUCCEEDED:- none}"
echo "Failed:   ${FAILED:- none}"
echo ""
echo "Native libraries are in: $RUNTIMES_DIR/"

if [ -n "$FAILED" ]; then
    exit 1
fi
