param(
    [Parameter(Mandatory = $true)]
    [string]$DalamudPath
)
$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $DalamudPath).Path
foreach ($file in @('Dalamud.dll', 'Dalamud.Bindings.ImGui.dll', 'FFXIVClientStructs.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $resolved $file))) {
        throw "Missing $file in the supplied Dalamud directory."
    }
}
$env:DALAMUD_HOME = $resolved
Push-Location $PSScriptRoot
try {
    dotnet restore XLinUI.csproj --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Dependency restore failed.' }
    dotnet build XLinUI.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed. Check that your soil Dalamud exposes these API 15 interfaces.' }
    Write-Host "Dev plugin DLL: $(Join-Path $PSScriptRoot 'bin\Release\XLinUI.dll')"
} finally {
    Pop-Location
}
