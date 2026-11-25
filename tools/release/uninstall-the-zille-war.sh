#!/usr/bin/env bash
# One-click uninstaller for Suzerain: The Zille War (Wleeaf Edition) on Linux and Steam Deck (Proton).
# Removes the mod, the Suzerain Modding Kit and MelonLoader (unless other MelonLoader mods still need it).
# Saves are never touched. Double-click it in the file manager, or run: bash uninstall-the-zille-war.sh
cd "$(dirname "$(readlink -f "$0")")" || exit 1

say() {
    if [ -t 1 ]; then
        printf '%s\n' "$1"
    elif command -v kdialog >/dev/null 2>&1; then
        kdialog --title "The Zille War" --msgbox "$1"
    elif command -v zenity >/dev/null 2>&1; then
        zenity --info --no-wrap --title "The Zille War" --text "$1"
    else
        printf '%s\n' "$1"
    fi
}

if [ ! -f Suzerain.exe ]; then
    say "This uninstaller must sit in the Suzerain game folder, next to Suzerain.exe."
    exit 1
fi
if pgrep -x Suzerain.exe >/dev/null 2>&1; then
    say "Suzerain is still running. Close the game, then run this again."
    exit 1
fi

rm -f Mods/ZilleWar.dll Mods/SuzerainModdingKit.dll "README - The Zille War.txt" \
      LICENSES/MelonLoader-LICENSE.md LICENSES/SuzerainModdingKit-LICENSE.txt
rm -rf UserData/ZilleWar
rmdir LICENSES 2>/dev/null

other="$(find Mods Plugins -maxdepth 1 -iname '*.dll' 2>/dev/null)"
if [ -n "$other" ]; then
    loader="Other MelonLoader mods are installed, so MelonLoader itself was kept for them:
$other
To remove it as well, delete version.dll and the MelonLoader folder.

"
else
    loader=""
    rm -rf MelonLoader UserLibs
    rm -f version.dll dobby.dll UserData/Loader.cfg UserData/MelonPreferences.cfg UserData/MelonStartScreen.cfg
    rmdir Mods Plugins UserData 2>/dev/null
fi

if [ -e Mods/ZilleWar.dll ] || [ -e UserData/ZilleWar ] || { [ -z "$other" ] && [ -e version.dll ]; }; then
    say "Some files could not be removed. Check that the game is closed and that you own the game folder, then try again."
    exit 1
fi

saves="$(readlink -f ../..)/compatdata/1207650/pfx/drive_c/users/steamuser/AppData/LocalLow/Torpor Games/Suzerain/moddingkit"
say "${loader}The Zille War has been removed. Suzerain starts as the unmodded game again.

Your saves were kept. The mod's own save data is in
$saves
You can delete that folder if you will not play the mod again.

The launch option WINEDLLOVERRIDES=\"version=n,b\" %command% does nothing now; you can clear it in Steam > Suzerain > Properties."

rm -f "Uninstall The Zille War.bat" "$0"
