#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Debug}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
solution="$root/UtilityVerse.slnx"

echo "==> Removing bin and obj folders"
find "$root/src" "$root/tests" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

cd "$root"
changed=()
while IFS= read -r file; do
    [ -n "$file" ] && [ -f "$file" ] && changed+=("$file")
done < <({ git diff --name-only HEAD -- '*.cs'; git ls-files --others --exclude-standard -- '*.cs'; } | sort -u)

if [ "${#changed[@]}" -gt 0 ]; then
    echo "==> Format changed C# files"
    dotnet format "$solution" --include "${changed[@]}"
else
    echo "==> No changed C# files to format"
fi

echo "==> Restore"
dotnet restore "$solution"

echo "==> Build"
dotnet build "$solution" --no-restore -c "$configuration"

echo "==> Test"
dotnet test "$solution" --no-build -c "$configuration"
