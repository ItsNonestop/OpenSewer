# Troubleshooting

## Plugin does not appear in game

- Confirm DLL path:
  `Obenseuer/BepInEx/plugins/OpenSewer/OpenSewer.dll`
- Confirm BepInEx 5 is installed and booting.
- Check `BepInEx/LogOutput.log` for plugin load errors.

## Build fails: missing Assembly-CSharp or UnityEngine DLLs

- Copy required DLLs to `GameLibs/`.
- Verify filenames match exactly.
- If your game build differs, pass `/p:GameManagedDir=<path>`.

## Build fails with .NET framework errors

- Install .NET Framework 4.7.2 targeting pack.
- Restart terminal/IDE and rebuild.

## BepInEx loads but keybind does nothing

- Make sure no other mod overrides the same key behavior.
- Check for errors in `BepInEx/LogOutput.log` from `com.opensewer.mod`.
