# Suzerain: The Zille War (Wleeaf Edition)

A story mod for Suzerain: Kingdom of Rizia. King Romus can declare war on Wehlen at the
Turn 4 Alliance session and take Zille back by force, alongside the existing Pales war.
Peace games stay vanilla. A fan mod, not affiliated with Torpor Games.

## Playing it

Download the zip from the Releases page, extract it into the game folder, and on Linux or
Steam Deck add the launch option below. The full player instructions are in
`tools/release/README.txt`, which ships inside the zip.

## Releasing

`tools/release/package.sh <version>` builds the mod and writes
`dist/Suzerain-The-Zille-War-Wleeaf-Edition-<version>.zip` with MelonLoader 0.7.3,
Suzerain Modding Kit 2.4 (checksum-pinned), the mod, its map assets (`assets/UserData`) and
the licenses.

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
