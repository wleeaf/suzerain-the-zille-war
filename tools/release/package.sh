#!/usr/bin/env bash
# Builds the player zip: MelonLoader + Suzerain Modding Kit + the mod + its map assets, ready to extract into the game folder.
# Usage: tools/release/package.sh [version]   (run from the repo root; the game does not need to be closed)
set -euo pipefail

VERSION="${1:-1.0.0}"
ML_VERSION="v0.7.3"
SMK_VERSION="v2.4"
SMK_SHA256="8a4af5ba01a167535e91e342c4d75fe834593cc1309927262aca051868fa1e96"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/dist"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
STAGE="$WORK/stage"
mkdir -p "$STAGE/Mods" "$STAGE/LICENSES" "$OUT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}" PATH="$PATH:$HOME/.dotnet"
dotnet build "$ROOT/ZilleWar" -p:UseSharedCompilation=false | grep -E " error |Build succeeded" | sort -u
DLL="$ROOT/ZilleWar/bin/x64/Debug/net6.0/ZilleWar.dll"

curl -fsSL -o "$WORK/ml.zip" "https://github.com/LavaGang/MelonLoader/releases/download/$ML_VERSION/MelonLoader.x64.zip"
curl -fsSL -o "$WORK/smk.dll" "https://github.com/suzerain-modding/suzerain-modding-kit/releases/download/$SMK_VERSION/SuzerainModdingKit.dll"
echo "$SMK_SHA256  $WORK/smk.dll" | sha256sum -c --quiet
curl -fsSL -o "$STAGE/LICENSES/MelonLoader-LICENSE.md" "https://raw.githubusercontent.com/LavaGang/MelonLoader/master/LICENSE.md"
curl -fsSL -o "$STAGE/LICENSES/SuzerainModdingKit-LICENSE.txt" "https://raw.githubusercontent.com/suzerain-modding/suzerain-modding-kit/main/LICENSE"

unzip -q "$WORK/ml.zip" -d "$STAGE"
cp "$WORK/smk.dll" "$STAGE/Mods/SuzerainModdingKit.dll"
cp "$DLL" "$STAGE/Mods/ZilleWar.dll"
cp -r "$ROOT/assets/UserData" "$STAGE/"
cp "$ROOT/tools/release/README.txt" "$STAGE/README - The Zille War.txt"

ZIP="$OUT/Suzerain-The-Zille-War-Wleeaf-Edition-$VERSION.zip"
rm -f "$ZIP"
(cd "$STAGE" && zip -qr -9 "$ZIP" .)
ls -la "$ZIP"
sha256sum "$ZIP"
