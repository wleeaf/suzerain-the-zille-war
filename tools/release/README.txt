SUZERAIN: THE ZILLE WAR (WLEEAF EDITION)
A story mod for Suzerain: Kingdom of Rizia. Version 1.0.0.

At the Turn 4 Alliance of Nations session, King Romus can answer Smolak by
declaring war on Wehlen. What follows is a three-operation campaign on the
war map, planned in War Councils, with the strait, the garrison, foreign
powers, the press, ceasefires, a treaty, and endings in which Zille is
liberated, divided or lost. If you never declare war, the story stays vanilla.

This is a fan mod. It is not affiliated with Torpor Games.

WHERE IT RUNS
- Windows 10 and 11, on Intel/AMD (x64) and on Arm PCs (Snapdragon), where
  Windows runs the game through its built-in x64 emulation.
- Linux and Steam Deck, through Proton (the launch option below is needed).
- macOS has its own, experimental package.
- Not on consoles, phones or tablets: mods cannot be loaded there.

REQUIREMENTS
- Suzerain on Steam, with the Kingdom of Rizia DLC.
- Game version 3.1.0.1.175. A later game patch can stop the mod from loading
  until it is updated.

This package already includes everything else:
- MelonLoader 0.7.3 (version.dll and the MelonLoader folder), Apache-2.0
- The .NET 6 runtime MelonLoader runs on, inside MelonLoader/Dependencies,
  so nothing is installed on your system, MIT
- Suzerain Modding Kit 2.4 (Mods/SuzerainModdingKit.dll), ISC
- The mod itself (Mods/ZilleWar.dll and UserData/ZilleWar/map)

INSTALL (WINDOWS)
1. In Steam, right-click Suzerain > Manage > Browse local files.
2. Extract everything in this zip into that folder. Allow it to merge folders.
   Afterwards version.dll sits next to Suzerain.exe.
3. Start the game. The first launch takes a few minutes: MelonLoader prepares
   itself and a console window opens. This only happens once.
4. Start a new Kingdom of Rizia game. The war is offered on Turn 4.

INSTALL (LINUX / STEAM DECK, PROTON)
Do steps 1 to 4 above, and before starting the game:
- Right-click Suzerain > Properties > General > Launch Options, and paste:
    WINEDLLOVERRIDES="version=n,b" %command%
Without it, Proton ignores MelonLoader and the game starts without mods.
On Steam Deck, do the install in Desktop Mode, then play in Game Mode.

CHECKING THAT IT WORKS
The main menu shows THE ZILLE WAR under the Suzerain logo. The console window
(or MelonLoader/Latest.log in the game folder) shows "Suzerain Modding Kit"
and "The Zille War (Wleeaf Edition) initialized."

SAVES
Start a new Kingdom of Rizia game to play the mod. Saves made without it are
not supported. Back up your saves before modding, as with any mod.

UNINSTALL (ONE CLICK)
Close the game, then in the game folder:
- Windows: double-click "Uninstall The Zille War.bat".
- Linux / Steam Deck: double-click "uninstall-the-zille-war.sh" (or run
  bash uninstall-the-zille-war.sh in a terminal).
It removes the mod, the Suzerain Modding Kit, MelonLoader and its .NET
runtime, and then itself. If you have other MelonLoader mods, MelonLoader is
kept for them. Your saves are never touched. On Linux, also clear the launch
option; it does nothing without the mod.

BUGS AND FEEDBACK
Please report them with what happened, the turn, and MelonLoader/Latest.log.
