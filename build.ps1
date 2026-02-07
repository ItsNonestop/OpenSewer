param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$slnPath = Join-Path $repoRoot "src/OpenSewer/OpenSewer.sln"
$gameLibs = Join-Path $repoRoot "GameLibs"

$requiredDlls = @(
    "Assembly-CSharp.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll"
)

$inputDllPresent = (Test-Path (Join-Path $gameLibs "UnityEngine.InputLegacyModule.dll")) -or
                   (Test-Path (Join-Path $gameLibs "UnityEngine.InputModule.dll"))

$missing = @()
foreach ($dll in $requiredDlls) {
    if (-not (Test-Path (Join-Path $gameLibs $dll))) {
        $missing += $dll
    }
}
if (-not $inputDllPresent) {
    $missing += "UnityEngine.InputLegacyModule.dll or UnityEngine.InputModule.dll"
}

if ($missing.Count -gt 0) {
    Write-Error "Missing required GameLibs files: $($missing -join ', '). See GameLibs/README.md"
}

Write-Host "Building OpenSewer ($Configuration)..."
dotnet build $slnPath -c $Configuration

$dllPath = Join-Path $repoRoot "src/OpenSewer/bin/$Configuration/net472/OpenSewer.dll"
if (Test-Path $dllPath) {
    Write-Host "Build succeeded: $dllPath"
} else {
    Write-Warning "Build completed, but OpenSewer.dll was not found at expected path."
}
