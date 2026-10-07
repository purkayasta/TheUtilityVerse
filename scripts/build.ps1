param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "UtilityVerse.slnx"

function Invoke-Step([string]$Name, [scriptblock]$Command) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE" }
}

Write-Host "==> Removing bin and obj folders" -ForegroundColor Cyan
Get-ChildItem -Path (Join-Path $root "src"), (Join-Path $root "tests") -Directory -Recurse -Include bin, obj -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force

Invoke-Step "Restore" { dotnet restore $solution }
Invoke-Step "Build" { dotnet build $solution --no-restore -c $Configuration }
Invoke-Step "Test" { dotnet test $solution --no-build -c $Configuration }
