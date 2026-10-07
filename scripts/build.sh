#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Debug}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
solution="$root/UtilityVerse.slnx"

echo "==> Removing bin and obj folders"
find "$root/src" "$root/tests" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

echo "==> Restore"
dotnet restore "$solution"

echo "==> Build"
dotnet build "$solution" --no-restore -c "$configuration"

echo "==> Test"
dotnet test "$solution" --no-build -c "$configuration"
