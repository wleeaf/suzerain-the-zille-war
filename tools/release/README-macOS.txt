SUZERAIN: THE ZILLE WAR (WLEEAF EDITION), macOS PACKAGE (EXPERIMENTAL)
A story mod for Suzerain: Kingdom of Rizia. Version 1.0.0.

At the Turn 4 Alliance of Nations session, King Romus can answer Smolak by
declaring war on Wehlen. If you never declare war, the story stays vanilla.
This is a fan mod. It is not affiliated with Torpor Games.

EXPERIMENTAL
The mod was made and tested on Windows builds of the game. MelonLoader's
macOS support is new, and this package has not been tested on a Mac yet.
If it does not start, the uninstaller below puts everything back. Reports
from Mac players (with MelonLoader/Latest.log) are very welcome.

REQUIREMENTS
- Suzerain on Steam, with the Kingdom of Rizia DLC, game version 3.1.0.1.175.
- An Intel Mac, or an Apple Silicon Mac with Rosetta. MelonLoader is built for
  Intel, so on Apple Silicon the game runs under Rosetta while the mod is
  installed. The setup tells you if Rosetta is missing.
- An internet connection on the first launch: MelonLoader downloads the .NET
  runtime it runs on into its own folder.

This package includes MelonLoader 0.7.3 (Apache-2.0), Suzerain Modding Kit 2.4
(ISC) and the mod itself.

INSTALL
1. In Steam, right-click Suzerain > Manage > Browse local files. A Finder
   window opens on the folder with SuzerainMac.app in it.
2. Copy everything in this zip into that folder, next to SuzerainMac.app.
3. Right-click "Set Up The Zille War.command" > Open, and confirm Open.
   (Right-click is needed the first time, because the file is not from the App
   Store.) It prepares the files and copies a launch option to your clipboard.
4. In Steam, right-click Suzerain > Properties > General > Launch Options,
   and paste. It looks like:
     "/Users/you/Library/Application Support/Steam/steamapps/common/Suzerain/melonloader-launch.sh" %command%
5. Start the game. The first launch takes a few minutes.
6. Start a new Kingdom of Rizia game. The war is offered on Turn 4.

CHECKING THAT IT WORKS
The main menu shows THE ZILLE WAR under the Suzerain logo.
MelonLoader/Latest.log in the game folder shows "The Zille War (Wleeaf
Edition) initialized."

UNINSTALL (ONE CLICK)
Quit the game, then right-click "Uninstall The Zille War.command" > Open.
It removes the mod, the Suzerain Modding Kit and MelonLoader (kept only if
other MelonLoader mods need it), then itself. Your saves are never touched.
Last, clear the launch option in Steam; after that you can delete
melonloader-launch.sh. Until then it simply starts the unmodded game.

BUGS AND FEEDBACK
Please report them with what happened, your Mac (Intel or Apple Silicon),
and MelonLoader/Latest.log.
