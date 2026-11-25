#!/bin/bash
# One-click uninstaller for Suzerain: The Zille War (Wleeaf Edition) on macOS.
# Removes the mod, the Suzerain Modding Kit and MelonLoader (unless other MelonLoader mods still need it).
# Saves are never touched. melonloader-launch.sh is kept so the Steam launch option still starts the game;
# without MelonLoader it simply starts it unmodded.
cd "$(dirname "$0")" || exit 1
finish() { echo; read -r -p "Press Return to close."; exit "$1"; }

shopt -s nullglob
apps=(*.app)
if [ ${#apps[@]} -ne 1 ]; then
    echo "This uninstaller must sit in the Suzerain game folder, next to the game's .app."
    finish 1
fi
if pgrep -f "${apps[0]}/Contents/MacOS" >/dev/null 2>&1; then
    echo "Suzerain is still running. Quit the game, then run this again."
    finish 1
fi

rm -f Mods/ZilleWar.dll Mods/SuzerainModdingKit.dll "README - The Zille War.txt" \
      LICENSES/MelonLoader-LICENSE.md LICENSES/SuzerainModdingKit-LICENSE.txt
rm -rf UserData/ZilleWar
rmdir LICENSES 2>/dev/null

other="$(find Mods Plugins -maxdepth 1 -iname '*.dll' 2>/dev/null)"
if [ -n "$other" ]; then
    echo "Other MelonLoader mods are installed, so MelonLoader itself was kept for them:"
    echo "$other"
    echo
else
    rm -rf MelonLoader UserLibs MelonLoader.Bootstrap.dylib MelonLoader.Bootstrap.dylib.dSYM
    rm -f UserData/Loader.cfg UserData/MelonPreferences.cfg UserData/MelonStartScreen.cfg
    rmdir Mods Plugins UserData 2>/dev/null
    shim="$HOME/Library/Application Support/Torpor Games/.zillewar-userprofile"
    rm -f "$shim/AppData\\LocalLow\\Torpor Games\\Suzerain"
    rmdir "$shim" 2>/dev/null
fi

if [ -e Mods/ZilleWar.dll ] || [ -e UserData/ZilleWar ]; then
    echo "Some files could not be removed. Check that the game is closed, then try again."
    finish 1
fi

cat <<EOF
The Zille War has been removed. Suzerain starts as the unmodded game again.

Your saves were kept. The mod's own save data is in
  $HOME/Library/Application Support/Torpor Games/Suzerain/moddingkit
You can delete that folder if you will not play the mod again.

Last, clear the launch option in Steam > Suzerain > Properties > General > Launch Options.
After that you can also delete melonloader-launch.sh from the game folder.
EOF
rm -f "Set Up The Zille War.command" "$0"
finish 0
