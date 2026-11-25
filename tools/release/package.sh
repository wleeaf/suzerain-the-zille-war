#!/usr/bin/env bash
# Builds the player zips, ready to extract into the game folder:
#   ...-Windows-Linux-SteamDeck.zip  MelonLoader (Windows x64) + its portable .NET 6 runtime + Suzerain Modding Kit
#                                    + the mod + map assets + one-click uninstallers. Runs on Windows (x64, and Arm
#                                    through Windows' x64 emulation) and on Linux / Steam Deck through Proton.
#   ...-macOS-experimental.zip       MelonLoader (macOS) + Suzerain Modding Kit + the mod + map assets, a setup helper,
#                                    a launcher that handles Apple Silicon and the kit's save path, and an uninstaller.
# Usage: tools/release/package.sh [version]   (run from anywhere; the game does not need to be closed)
set -euo pipefail

VERSION="${1:-1.0.0}"
ML_VERSION="v0.7.3"
SMK_VERSION="v2.4"
SMK_SHA256="8a4af5ba01a167535e91e342c4d75fe834593cc1309927262aca051868fa1e96"
DOTNET_VERSION="6.0.36"
DOTNET_SHA512="935db5c6cee19f2c016e67168bfae7b491044735de76c673abb3b125dd325fd5e779d7efe12ba80178d46689ae70a25e558a3fa846417d44c5f4ca256e7f4bf2"
NAME="Suzerain-The-Zille-War-Wleeaf-Edition-$VERSION"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
REL="$ROOT/tools/release"
OUT="$ROOT/dist"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
mkdir -p "$OUT"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}" PATH="$PATH:$HOME/.dotnet"
dotnet build "$ROOT/ZilleWar" -p:UseSharedCompilation=false | grep -E " error |Build succeeded" | sort -u
DLL="$ROOT/ZilleWar/bin/x64/Debug/net6.0/ZilleWar.dll"

fetch() { curl -fsSL --retry 3 -o "$2" "$1"; }
fetch "https://github.com/LavaGang/MelonLoader/releases/download/$ML_VERSION/MelonLoader.x64.zip" "$WORK/ml-win.zip"
fetch "https://github.com/LavaGang/MelonLoader/releases/download/$ML_VERSION/MelonLoader.macOS.x64.zip" "$WORK/ml-mac.zip"
fetch "https://github.com/suzerain-modding/suzerain-modding-kit/releases/download/$SMK_VERSION/SuzerainModdingKit.dll" "$WORK/smk.dll"
echo "$SMK_SHA256  $WORK/smk.dll" | sha256sum -c --quiet
fetch "https://builds.dotnet.microsoft.com/dotnet/Runtime/$DOTNET_VERSION/dotnet-runtime-$DOTNET_VERSION-win-x64.zip" "$WORK/dotnet.zip"
echo "$DOTNET_SHA512  $WORK/dotnet.zip" | sha512sum -c --quiet
mkdir -p "$WORK/licenses"
fetch "https://raw.githubusercontent.com/LavaGang/MelonLoader/master/LICENSE.md" "$WORK/licenses/MelonLoader-LICENSE.md"
fetch "https://raw.githubusercontent.com/suzerain-modding/suzerain-modding-kit/main/LICENSE" "$WORK/licenses/SuzerainModdingKit-LICENSE.txt"

common() {  # $1 = stage dir: the mod, the kit, the map, licenses
    mkdir -p "$1/Mods" "$1/LICENSES"
    cp "$WORK/smk.dll" "$1/Mods/SuzerainModdingKit.dll"
    cp "$DLL" "$1/Mods/ZilleWar.dll"
    cp -r "$ROOT/assets/UserData" "$1/"
    cp "$WORK/licenses/"* "$1/LICENSES/"
}

pack() {  # $1 = stage dir, $2 = zip path
    rm -f "$2"
    (cd "$1" && zip -qr -9 -X "$2" .)
    ls -la "$2"
    sha256sum "$2"
}

# Windows / Linux / Steam Deck
WIN="$WORK/win"
mkdir -p "$WIN"
unzip -q "$WORK/ml-win.zip" -d "$WIN"
mkdir -p "$WIN/MelonLoader/Dependencies/dotnet"
unzip -q "$WORK/dotnet.zip" -d "$WIN/MelonLoader/Dependencies/dotnet"   # found first, so .NET is never installed system-wide
common "$WIN"
cp "$REL/README.txt" "$WIN/README - The Zille War.txt"
sed 's/$/\r/' "$REL/Uninstall The Zille War.bat" > "$WIN/Uninstall The Zille War.bat"
install -m 755 "$REL/uninstall-the-zille-war.sh" "$WIN/uninstall-the-zille-war.sh"
pack "$WIN" "$OUT/$NAME-Windows-Linux-SteamDeck.zip"

# macOS (experimental)
MAC="$WORK/mac"
mkdir -p "$MAC"
unzip -q "$WORK/ml-mac.zip" -d "$MAC" -x 'MelonLoader.Bootstrap.dylib.dSYM/*'
tail -n 1 "$MAC/melonloader-launch.sh" | grep -qx 'exec "\$@"'      # the addon replaces this last line
{ head -n -1 "$MAC/melonloader-launch.sh"; cat "$REL/macos/launch-addon.sh"; } > "$MAC/melonloader-launch.sh.new"
mv "$MAC/melonloader-launch.sh.new" "$MAC/melonloader-launch.sh"
bash -n "$MAC/melonloader-launch.sh"
chmod 755 "$MAC/melonloader-launch.sh"
common "$MAC"
cp "$REL/README-macOS.txt" "$MAC/README - The Zille War.txt"
install -m 755 "$REL/macos/Set Up The Zille War.command" "$REL/macos/Uninstall The Zille War.command" "$MAC/"
pack "$MAC" "$OUT/$NAME-macOS-experimental.zip"
