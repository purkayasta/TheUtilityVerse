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

Push-Location $root
try {
    $changed = @(git diff --name-only HEAD -- '*.cs'; git ls-files --others --exclude-standard -- '*.cs') |
        Where-Object { $_ -and (Test-Path $_) }
}
finally { Pop-Location }

if ($changed) {
    Invoke-Step "Format changed C# files" { dotnet format $solution --include @changed }
}
else {
    Write-Host "==> No changed C# files to format" -ForegroundColor Cyan
}

Invoke-Step "Restore" { dotnet restore $solution }
Invoke-Step "Build" { dotnet build $solution --no-restore -c $Configuration }
Invoke-Step "Test" { dotnet test $solution --no-build -c $Configuration }
