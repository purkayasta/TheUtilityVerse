#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
packages="$root/artifacts/packages"
scratch="$(mktemp -d "${TMPDIR:-/tmp}/utilityverse-packages.XXXXXX")"
trap 'rm -rf "$scratch"' EXIT

dotnet pack "$root/UtilityVerse.slnx" --no-restore -c Release -o "$packages"

cat > "$scratch/NuGet.Config" <<EOF
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="UtilityVerse*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
EOF

cat > "$scratch/Consumer.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net11.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="UtilityVerse" Version="1.1.0" />
    <PackageReference Include="UtilityVerse.Copy" Version="0.6.0" />
    <PackageReference Include="UtilityVerse.ASPNET" Version="0.6.0" />
  </ItemGroup>
</Project>
EOF

cat > "$scratch/Program.cs" <<'EOF'
using UtilityVerse;
using UtilityVerse.Copy;
using UtilityVerse.Extensions;

var node = new Node { Id = 7 };
node.Next = node;
var copy = node.DeepCopy();
if (ReferenceEquals(copy, node) || !ReferenceEquals(copy.Next, copy) || copy.Id != 7)
    throw new Exception("Packaged generator did not preserve the graph.");
var shallow = new Shallow { Items = new[] { 1 } };
if (!ReferenceEquals(shallow.Items, shallow.ShallowCopy().Items))
    throw new Exception("Packaged shallow copy did not share references.");
if (new[] { 1, 2 }.ToStr(",").Result != "1,2" || Utility.AddRetry(() => 42) != 42)
    throw new Exception("Packaged core helpers failed.");
if (UtilityVerse.ASPNET.Utility.CreateHttpRequest("name", "value").Query["name"] != "value")
    throw new Exception("Packaged ASPNET helper failed.");
Console.WriteLine("Fresh-package consumer passed.");

[DeepCopy]
public partial class Node
{
    public int Id { get; set; }
    public Node? Next { get; set; }
}

[ShallowCopy]
public partial record Shallow
{
    public int[]? Items { get; init; }
}
EOF

dotnet restore "$scratch/Consumer.csproj" --configfile "$scratch/NuGet.Config" --packages "$scratch/cache"
dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net8.0
dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net11.0
