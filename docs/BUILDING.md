# Building OpenSewer

## 1. Requirements for Building

- Visual Studio 2022 Build Tools or .NET SDK with MSBuild support
- .NET Framework 4.7.2 targeting pack
- Obenseuer installed locally

## 2. Copy game DLLs

From your game folder:
`Obenseuer/Obenseuer_Data/Managed/`

Copy these into repo folder `GameLibs/`:
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.InputLegacyModule.dll` (preferred)
- `UnityEngine.InputModule.dll` (fallback if Legacy module is not present)

Do not commit these DLLs.

## 3. Restore and build

From repo root:

```powershell
./build.ps1 -Configuration Release
```

Or directly:

```powershell
dotnet build .\src\OpenSewer\OpenSewer.sln -c Release
```

## 4. Output

Compiled plugin DLL:
`src/OpenSewer/bin/Release/net472/OpenSewer.dll`

## 5. Optional override path

If your managed DLLs are elsewhere:

```powershell
dotnet build .\src\OpenSewer\OpenSewer.sln -c Release /p:GameManagedDir="C:\Path\To\Obenseuer_Data\Managed"
```
