
# ---- Added by the Suzerain: The Zille War package (macOS) ----

# The Suzerain Modding Kit keeps the mod's save data under %userprofile%\AppData\LocalLow\Torpor Games\Suzerain,
# a Windows path. On macOS the game saves to ~/Library/Application Support/Torpor Games/Suzerain, so the kit is
# pointed there through a link whose name is that Windows path (macOS allows backslashes in file names).
ZW_SAVES="$HOME/Library/Application Support/Torpor Games/Suzerain"
ZW_SHIM="$HOME/Library/Application Support/Torpor Games/.zillewar-userprofile"
mkdir -p "$ZW_SAVES" "$ZW_SHIM"
ln -sfn "$ZW_SAVES" "$ZW_SHIM/AppData\\LocalLow\\Torpor Games\\Suzerain"
export userprofile="$ZW_SHIM"

# MelonLoader's macOS bootstrap is built for Intel (x86_64) only. On Apple Silicon the game is started under
# Rosetta so the bootstrap can load; arch -e passes the DYLD variables, which macOS strips from arch itself.
if [ "$(uname -m)" = "arm64" ] && [ "${1:-}" = "$BINARY" ]; then
    if ! lipo -archs "$BINARY" 2>/dev/null | grep -qw x86_64; then
        echo "zillewar: this build of the game has no Intel version to run under Rosetta; starting it without mods" >&2
    elif ! arch -x86_64 /usr/bin/true 2>/dev/null; then
        echo "zillewar: Rosetta is not installed (softwareupdate --install-rosetta); starting the game without mods" >&2
    else
        exec arch -x86_64 -e DYLD_INSERT_LIBRARIES="$DYLD_INSERT_LIBRARIES" -e DYLD_LIBRARY_PATH="$DYLD_LIBRARY_PATH" \
            -e userprofile="$userprofile" "$@"
    fi
    if [ -n "${STEAM_DYLD_INSERT_LIBRARIES:-}" ]; then
        export DYLD_INSERT_LIBRARIES="$STEAM_DYLD_INSERT_LIBRARIES"
    else
        unset DYLD_INSERT_LIBRARIES
    fi
fi

exec "$@"
