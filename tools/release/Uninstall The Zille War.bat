@echo off
rem One-click uninstaller for Suzerain: The Zille War (Wleeaf Edition) on Windows.
rem Removes the mod, the Suzerain Modding Kit and MelonLoader (unless other MelonLoader mods still need it).
rem Saves are never touched.
setlocal EnableExtensions
title Uninstall The Zille War
cd /d "%~dp0"

if not exist "Suzerain.exe" (
  echo This uninstaller must sit in the Suzerain game folder, next to Suzerain.exe.
  echo.
  pause
  exit /b 1
)

tasklist /FI "IMAGENAME eq Suzerain.exe" /NH 2>nul | find /I "Suzerain.exe" >nul
if not errorlevel 1 (
  echo Suzerain is still running. Close the game, then run this again.
  echo.
  pause
  exit /b 1
)

echo Removing The Zille War from %CD%
echo.

del /f /q "Mods\ZilleWar.dll" "Mods\SuzerainModdingKit.dll" 2>nul
if exist "UserData\ZilleWar" rmdir /s /q "UserData\ZilleWar"
del /f /q "README - The Zille War.txt" 2>nul
del /f /q "LICENSES\MelonLoader-LICENSE.md" "LICENSES\SuzerainModdingKit-LICENSE.txt" 2>nul
rmdir "LICENSES" 2>nul

set "OTHER="
for %%D in (Mods Plugins) do if exist "%%D\*.dll" set "OTHER=1"

if defined OTHER goto keep_loader
del /f /q "version.dll" "dobby.dll" 2>nul
if exist "MelonLoader" rmdir /s /q "MelonLoader"
if exist "UserLibs" rmdir /s /q "UserLibs"
del /f /q "UserData\Loader.cfg" "UserData\MelonPreferences.cfg" "UserData\MelonStartScreen.cfg" 2>nul
for %%D in (Mods Plugins UserData) do rmdir "%%D" 2>nul
goto check

:keep_loader
echo Other MelonLoader mods are installed, so MelonLoader itself was kept for them:
for %%D in (Mods Plugins) do if exist "%%D\*.dll" dir /b "%%D\*.dll"
echo To remove MelonLoader as well, delete version.dll and the MelonLoader folder.
echo.

:check
set "LEFT="
if exist "Mods\ZilleWar.dll" set "LEFT=1"
if exist "UserData\ZilleWar" set "LEFT=1"
if not defined OTHER if exist "version.dll" set "LEFT=1"
if not defined OTHER if exist "MelonLoader" set "LEFT=1"
if defined LEFT goto failed

echo The Zille War has been removed. Suzerain starts as the unmodded game again.
echo.
echo Your saves were kept. The mod's own save data is in
echo   %USERPROFILE%\AppData\LocalLow\Torpor Games\Suzerain\moddingkit
echo You can delete that folder if you will not play the mod again.
echo.
pause
del /f /q "uninstall-the-zille-war.sh" 2>nul
(goto) 2>nul & del "%~f0" 2>nul

:failed
echo Some files could not be removed. Make sure the game is closed, then
echo right-click this file and choose "Run as administrator".
echo.
pause
exit /b 1
