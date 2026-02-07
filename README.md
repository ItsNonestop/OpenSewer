# OpenSewer

OpenSewer is a BepInEx 5 mod for **Obenseuer** that adds an in-game utility menu with an item spawner.

The mod keeps gameplay hooks simple:
- Open/close menu with `U`
- In Item Spawner, `LMB` spawns one item
- `RMB` opens amount picker

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

- `U`: Toggle OpenSewer menu
- Item tile `LMB`: Spawn one
- Item tile `RMB`: Open amount picker
- Amount picker:
  - Slider for quick value control up to max stack
  - Type any positive amount to spawn multiple stacks when needed

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

## Contributing

See `CONTRIBUTING.md`.

## License

MIT. See `LICENSE`.
