# OpenSewer

OpenSewer is a BepInEx 5 mod for **Obenseuer**.

Current `v0.5.0` source is set up as a UI reverse-engineering build so the next GUI can be rebuilt to match native in-game UI.

## Requirements

- Obenseuer
- BepInEx 5 (x64, for the game)
- .NET Framework 4.7.2 targeting pack (for local builds)
- Game managed DLLs copied into `GameLibs/` (not committed)

## Install (Players)

1. Install BepInEx 5 into your Obenseuer game folder.
2. Download the OpenSewer release zip.
3. Extract `OpenSewer.dll` to:
   `Obenseuer/BepInEx/plugins/OpenSewer/OpenSewer.dll`
4. Start the game and press `U`.

## Controls

- `U`: Toggle OpenSewer UI Reference Tool
- `F8`: Dump full UI hierarchy for all canvases
- `F9`: Dump hovered UI hits under mouse
- `F10`: Dump hovered UI branch as C# reference scaffold

Dumps are written to:
- `BepInEx/LogOutput_OpenSewer/`

## Build From Source

See `docs/BUILDING.md` for full step-by-step instructions.

Quick version:
1. Copy required game DLLs to `GameLibs/` (see `GameLibs/README.md`).
2. Build:
   - `./build.ps1`
   - or `dotnet build .\src\OpenSewer\OpenSewer.sln -c Release`
3. Output DLL:
   `src/OpenSewer/bin/Release/net472/OpenSewer.dll`

## Troubleshooting

See `docs/TROUBLESHOOTING.md`.

Common issues:
- Missing `Assembly-CSharp.dll` or Unity DLLs in `GameLibs/`
- Wrong framework/tooling (missing .NET Framework 4.7.2 targeting pack)
- BepInEx not loading plugin (wrong path or old BepInEx install)

## Release Checklist

1. Bump version in:
   - `src/OpenSewer/OpenSewer.csproj` (`<Version>`)
   - `src/OpenSewer/Plugin.cs` (`PluginVersion`)
2. Build release: `./build.ps1 -Configuration Release`
3. Verify output: `src/OpenSewer/bin/Release/net472/OpenSewer.dll`
4. Create release zip with structure:
   - `BepInEx/plugins/OpenSewer/OpenSewer.dll`
5. Publish GitHub release notes with version + install steps.

---

<img width="1243" height="784" alt="MAIN" src="https://github.com/user-attachments/assets/d59833da-1357-41d7-886d-3b3f0f372dd8" />
<img width="1127" height="620" alt="ITEM" src="https://github.com/user-attachments/assets/435fc3ae-c66c-4638-8a41-ca3d3ffb9b65" />
<img width="1137" height="624" alt="FURNITURE" src="https://github.com/user-attachments/assets/7915f82d-9287-4395-b348-91b3ec005a52" />
<img width="1197" height="601" alt="STATS" src="https://github.com/user-attachments/assets/eb253b0a-c2e3-4de4-a95f-f0d6c169b00a" />


---

## References
Massive thank you to Github user [ShiggityShaggs](https://github.com/shiggityshaggs), his repositories for both 
[ObenseuerItemCodex](https://github.com/shiggityshaggs/ObenseuerItemCodex)
&
[ObenseuerFurnitureCodex](https://github.com/shiggityshaggs/ObenseuerFurnitureCodex)
have both helped massively in saving time in implimentation of Item & Furniture Spawning within the menu and making sure Item/Furniture images aligned correctly with item

## Contributing

See `CONTRIBUTING.md`.

## License

MIT. See `LICENSE`.
