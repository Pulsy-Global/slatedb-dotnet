#!/usr/bin/env bash
set -euo pipefail

# Generate the internal C# bindings from SlateDB's UniFFI metadata.
#
# Usage:
#   ./generate-uniffi-bindings.sh [path-to-slatedb-source]
#
# Override the default SlateDB ref with:
#   SLATEDB_REF=<tag-or-sha> ./generate-uniffi-bindings.sh

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SLATEDB_CLONE_DIR="$SCRIPT_DIR/.slatedb-src"
SLATEDB_REF="${SLATEDB_REF:-v0.15.0}"
SLATEDB_RUST_TOOLCHAIN="${SLATEDB_RUST_TOOLCHAIN:-1.91.1}"
SLATEDB_SRC="${1:-}"
GENERATOR_VERSION="v0.11.0+v0.31.0"
GENERATOR_ROOT="$SCRIPT_DIR/.tools/uniffi-bindgen-cs"
GENERATOR="$GENERATOR_ROOT/bin/uniffi-bindgen-cs"
OUT_DIR="$SCRIPT_DIR/Pulsy.SlateDB/Native/Generated"
OUT_FILE="$OUT_DIR/SlateDbUniffi.g.cs"

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

SLATEDB_SRC="$(cd "$SLATEDB_SRC" && pwd)"

if [ ! -f "$SLATEDB_SRC/Cargo.toml" ]; then
    echo "Error: $SLATEDB_SRC/Cargo.toml not found." >&2
    exit 1
fi

RUST_BIN="$(dirname "$(rustup which cargo --toolchain "$SLATEDB_RUST_TOOLCHAIN")")"
export PATH="$RUST_BIN:$PATH"

if [ ! -x "$GENERATOR" ] ||
    ! "$GENERATOR" --version 2>/dev/null | grep -Fq "0.11.0+v0.31.0"; then
    cargo install uniffi-bindgen-cs \
        --git https://github.com/NordSecurity/uniffi-bindgen-cs \
        --tag "$GENERATOR_VERSION" \
        --locked \
        --force \
        --root "$GENERATOR_ROOT"
fi

cargo build --release -p slatedb-uniffi \
    --manifest-path "$SLATEDB_SRC/Cargo.toml"

LIB_PATH="$SLATEDB_SRC/target/release/libslatedb_uniffi.dylib"
if [ "$(uname -s)" = "Linux" ]; then
    LIB_PATH="$SLATEDB_SRC/target/release/libslatedb_uniffi.so"
elif [ "$(uname -s)" != "Darwin" ]; then
    LIB_PATH="$SLATEDB_SRC/target/release/slatedb_uniffi.dll"
fi

if [ ! -f "$LIB_PATH" ]; then
    echo "Error: generated library input not found: $LIB_PATH" >&2
    exit 1
fi

mkdir -p "$OUT_DIR"
(
    cd "$SLATEDB_SRC"
    "$GENERATOR" "$LIB_PATH" --library --no-format --out-dir "$OUT_DIR"
)

GENERATED="$OUT_DIR/slatedb.cs"
if [ ! -f "$GENERATED" ]; then
    echo "Error: generator did not produce $GENERATED" >&2
    exit 1
fi

mv "$GENERATED" "$OUT_FILE"
perl -0pi -e 's/#nullable enable/#nullable enable\n#pragma warning disable CS0108 \/\/ UniFFI error variant Exception.Data hides System.Exception.Data./' "$OUT_FILE"
perl -pi -e 's/[ \t]+$//' "$OUT_FILE"
perl -0pi -e 's/\s+\z/\n/' "$OUT_FILE"
echo "Generated $OUT_FILE"
