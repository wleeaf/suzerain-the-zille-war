# Suzerain: The Zille War (Wleeaf Edition)

A story mod for Suzerain: Kingdom of Rizia. King Romus can declare war on Wehlen at the
Turn 4 Alliance session and take Zille back by force, alongside the existing Pales war.
Peace games stay vanilla. A fan mod, not affiliated with Torpor Games.

## Playing it

Download the zip for your platform from the Releases page and extract it into the game folder;
on Linux or Steam Deck add the launch option below. The full player instructions are in
`tools/release/README.txt` (and `README-macOS.txt`), which ship inside the zips. Each zip
carries a one-click uninstaller that removes the mod, the kit and MelonLoader and leaves saves alone.

| Platform | Package | Status |
| --- | --- | --- |
| Windows x64 | Windows-Linux-SteamDeck | same binaries as the tested Proton setup |
| Windows on Arm | Windows-Linux-SteamDeck | runs through Windows' x64 emulation; untested |
| Linux / Steam Deck (Proton) | Windows-Linux-SteamDeck | tested end to end: install, self-test, uninstall |
| macOS (Intel, Apple Silicon via Rosetta) | macOS-experimental | untested; see below |
| Consoles, iOS, Android | none | no mod loader exists |

On macOS the Suzerain Modding Kit's save path is a hard-coded Windows path
(`%userprofile%\AppData\LocalLow\...`). The macOS launcher points it at
`~/Library/Application Support/Torpor Games/Suzerain` through a link whose name is that path, and
starts the game under Rosetta on Apple Silicon, since MelonLoader's macOS bootstrap is x86_64 only.

## Releasing

`tools/release/package.sh <version>` builds the mod and writes two zips to `dist/`:
`...-Windows-Linux-SteamDeck.zip` (MelonLoader 0.7.3 x64, its portable .NET 6.0.36 runtime in
`MelonLoader/Dependencies/dotnet` so nothing is installed system-wide, the checksum-pinned Suzerain
Modding Kit 2.4, the mod, its map assets, the licenses, `Uninstall The Zille War.bat` and
`uninstall-the-zille-war.sh`) and `...-macOS-experimental.zip` (MelonLoader 0.7.3 macOS with
`tools/release/macos/launch-addon.sh` appended to its launcher, a setup and an uninstall `.command`).

`tools/carousel/make_carousel.py` turns the screenshots in `~/Pictures/ZilleWar-reddit` into the
1080x1350 carousel slides in `~/Pictures/ZilleWar-reddit/carousel`.

Built on [Suzerain Modding Kit](https://github.com/suzerain-modding/suzerain-modding-kit)
(SMK) and MelonLoader.

## Environment (as set up on 2026-11-21)

| Piece | Version | Location |
| --- | --- | --- |
| Suzerain | 3.1.0.1.175 (Steam, Proton) | `~/.local/share/Steam/steamapps/common/Suzerain` |
| MelonLoader | 0.7.3 (Windows x64 build) | game folder (`version.dll`, `MelonLoader/`) |
| SMK | v2.4 | `<game>/Mods/SuzerainModdingKit.dll` |
| .NET SDK | 10.0.402 (targets net6.0) | `~/.dotnet` |
| .NET 6 desktop runtime | via `protontricks 1207650 dotnetdesktop6` | Proton prefix |

Steam launch options for Suzerain must start with `WINEDLLOVERRIDES="version=n,b"`,
for example:

```
WINEDLLOVERRIDES="version=n,b" %command%
```

Save backup taken before modding: `backups/saves-2026-11-21-pre-modding.tar.zst`
(restore by extracting into `<prefix>/drive_c/users/steamuser/AppData/LocalLow/`).

## Build and deploy

Close the game first, then:

```bash
dotnet build ZilleWar -t:Deploy
```

This copies `ZilleWar.dll` into the game's `Mods` folder. In game, `Ctrl+D` opens
the SMK debug overlay; mod logs go to the MelonLoader console and
`<game>/MelonLoader/Latest.log`.

After the first launch with MelonLoader, the IL2CPP interop assemblies appear in
`<game>/MelonLoader/Il2CppAssemblies/` for anything SMK doesn't wrap.
