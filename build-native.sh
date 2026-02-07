#!/usr/bin/env bash
set -euo pipefail

# Build slatedb-c native libraries.
#
# Usage:
#   ./build-native.sh [--all] [path-to-slatedb-source]
#
# Without --all: builds only for the current platform (fast).
# With    --all: builds for all 6 platforms using cargo-zigbuild.
#
# If no source path is provided, clones slatedb into .slatedb-src/ automatically.
#
# Requirements:
#   - rustup with nightly toolchain: rustup install nightly
#   - For --all: cargo-zigbuild + zig: brew install zig && cargo install cargo-zigbuild

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SLATEDB_CLONE_DIR="$SCRIPT_DIR/.slatedb-src"

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
        echo "Updating existing slatedb clone..."
        git -C "$SLATEDB_CLONE_DIR" pull --ff-only
    else
        echo "Cloning slatedb..."
        git clone --depth 1 https://github.com/slatedb/slatedb.git "$SLATEDB_CLONE_DIR"
    fi
    SLATEDB_SRC="$SLATEDB_CLONE_DIR"
fi

if [ ! -f "$SLATEDB_SRC/Cargo.toml" ]; then
    echo "Error: $SLATEDB_SRC/Cargo.toml not found." >&2
    exit 1
fi

RUNTIMES_DIR="$SCRIPT_DIR/runtimes"

# .NET RID / Rust target / output library name (parallel arrays, bash 3.2 compatible)
# Windows uses GNU ABI for cross-compilation with zigbuild
RIDS=(         osx-arm64              osx-x64                linux-arm64                 linux-x64                   win-arm64                    win-x64               )
RUST_TARGETS=( aarch64-apple-darwin   x86_64-apple-darwin    aarch64-unknown-linux-gnu   x86_64-unknown-linux-gnu    aarch64-pc-windows-gnullvm   x86_64-pc-windows-gnu  )
LIB_NAMES=(   libslatedb_c.dylib     libslatedb_c.dylib     libslatedb_c.so             libslatedb_c.so             slatedb_c.dll                slatedb_c.dll          )

# Force nightly toolchain via PATH (Homebrew cargo/rustc ignores RUSTUP_TOOLCHAIN)
NIGHTLY_BIN="$(dirname "$(rustup which cargo --toolchain nightly)")"
export PATH="$NIGHTLY_BIN:$PATH"
echo "Using: $(cargo --version), $(rustc --version)"

# Detect native platform RID
detect_native_rid() {
    local arch
    case "$(uname -m)" in
        arm64|aarch64) arch="arm64" ;;
        *)             arch="x64" ;;
    esac
    case "$(uname -s)" in
        Darwin)              echo "osx-$arch" ;;
        Linux)               echo "linux-$arch" ;;
        MINGW*|MSYS*|CYGWIN*) echo "win-$arch" ;;
        *)                   echo "linux-$arch" ;;
    esac
}
NATIVE_RID="$(detect_native_rid)"

# Check zigbuild for cross builds
HAS_ZIGBUILD=false
if command -v cargo-zigbuild &>/dev/null || [ -f "$HOME/.cargo/bin/cargo-zigbuild" ]; then
    HAS_ZIGBUILD=true
fi

if [ "$BUILD_ALL" = true ] && [ "$HAS_ZIGBUILD" = false ]; then
    echo "Error: --all requires cargo-zigbuild. Install: brew install zig && cargo install cargo-zigbuild" >&2
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

    # macOS targets require a macOS host (no SDK available on Linux)
    case "$RID" in
        osx-*)
            if [ "$(uname -s)" != "Darwin" ]; then
                echo "  Skipping $RID (requires macOS host)"
                continue
            fi ;;
        linux-*|win-*)
            if [ "$(uname -s)" = "Darwin" ]; then
                echo "  Skipping $RID (built on Linux host)"
                continue
            fi ;;
    esac

    echo ""
    echo "=== Building $RID ($TARGET) ==="

    rustup target add --toolchain nightly "$TARGET" 2>/dev/null || true
    mkdir -p "$OUT_DIR"

    # Use zigbuild for cross-compilation, plain cargo for native
    if [ "$RID" = "$NATIVE_RID" ]; then
        BUILD_CMD="cargo build"
    else
        BUILD_CMD="cargo zigbuild"
    fi

    if $BUILD_CMD --release -p slatedb-c --target "$TARGET" \
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
