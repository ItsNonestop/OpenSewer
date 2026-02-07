# GameLibs (User-Supplied)

Copy required game-managed DLLs from:
`Obenseuer/Obenseuer_Data/Managed/`

Required:
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.InputLegacyModule.dll` (preferred)
- `UnityEngine.InputModule.dll` (fallback if Legacy module does not exist)

These files are not committed to git.

Build command:

```powershell
./build.ps1 -Configuration Release
```

Or override path directly:

```powershell
dotnet build .\src\OpenSewer\OpenSewer.sln -c Release /p:GameManagedDir="C:\Path\To\Managed"
```
