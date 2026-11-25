#!/bin/bash
# macOS setup for Suzerain: The Zille War (Wleeaf Edition). Run it once after extracting the zip into the game folder.
# It clears the download quarantine from the mod's files, makes the scripts runnable, and copies the Steam launch
# option to the clipboard.
cd "$(dirname "$0")" || exit 1
shopt -s nullglob
apps=(*.app)
if [ ${#apps[@]} -ne 1 ] || [ ! -d MelonLoader ]; then
    echo "Put the contents of the zip into the Suzerain game folder (Steam > Suzerain > Manage > Browse local files),"
    echo "next to ${apps[0]:-SuzerainMac.app}, and run this again."
    read -r -p "Press Return to close."
    exit 1
fi

for f in MelonLoader MelonLoader.Bootstrap.dylib melonloader-launch.sh Mods UserData *.command; do
    xattr -dr com.apple.quarantine "$f" 2>/dev/null
done
chmod +x melonloader-launch.sh ./*.command

line="\"$PWD/melonloader-launch.sh\" %command%"
printf '%s' "$line" | pbcopy

cat <<EOF

The Zille War is ready. One step left, in Steam:

  Right-click Suzerain > Properties > General > Launch Options, and paste (it is already on your clipboard):

    $line

Then start the game. The first launch takes a few minutes while MelonLoader prepares itself.
On Apple Silicon Macs the game runs under Rosetta while the mod is installed.
EOF
if [ "$(uname -m)" = "arm64" ] && ! arch -x86_64 /usr/bin/true 2>/dev/null; then
    echo
    echo "Rosetta is not installed yet. Install it with:  softwareupdate --install-rosetta --agree-to-license"
fi
echo
read -r -p "Press Return to close."
