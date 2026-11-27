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
var stored = new StoredDerived();
stored.ReadBaseValues().Add(node);
stored.Values.Add(node);
var storedCopy = stored.DeepCopy();
if (storedCopy.Id != 2 || storedCopy.ReadBaseId() != 7 ||
    ReferenceEquals(storedCopy.Values, stored.Values) ||
    ReferenceEquals(storedCopy.ReadBaseValues(), stored.ReadBaseValues()) ||
    !ReferenceEquals(storedCopy.Values[0], storedCopy.ReadBaseValues()[0]) ||
    !ReferenceEquals(storedCopy.Values[0], storedCopy.Values[0].Next))
{
    throw new Exception("Packaged generator did not preserve virtual backing storage.");
}

var collectionNode = new CollectionNode { Id = 11, Label = "derived" };
collectionNode.Next = collectionNode;
var collectionItems = new List<CollectionNode> { collectionNode };
var covariantCopy = new CovariantHolder { Wide = collectionItems, Narrow = collectionItems }.DeepCopy();
var reverseCopy = new ReverseCovariantHolder { Wide = collectionItems, Narrow = collectionItems }.DeepCopy();
if (covariantCopy.Wide is not List<CollectionNode> ||
    !ReferenceEquals(covariantCopy.Wide, covariantCopy.Narrow) ||
    !ReferenceEquals(reverseCopy.Wide, reverseCopy.Narrow) ||
    ReferenceEquals(covariantCopy.Narrow, collectionItems) ||
    covariantCopy.Narrow![0].Label != "derived" ||
    !ReferenceEquals(covariantCopy.Narrow[0], covariantCopy.Narrow[0].Next))
{
    throw new Exception("Packaged generator did not preserve covariant collection aliases.");
}

#if NET9_0_OR_GREATER
var generic = new StoredGenericDerived<int>();
generic.ReadBaseValues().Add(7);
generic.Values.Add(2);
var genericCopy = generic.DeepCopy();
if (genericCopy.ReadBaseValues()[0] != 7 || genericCopy.Values[0] != 2 ||
    ReferenceEquals(genericCopy.Values, generic.Values))
{
    throw new Exception("Packaged generator did not preserve generic backing storage.");
}
#endif

Console.WriteLine("Fresh-package consumer passed.");

[DeepCopy]
public partial class Node
{
    public int Id { get; set; }
    public Node? Next { get; set; }
}

[DeepCopy]
public partial class CollectionNode : Node
{
    public string? Label { get; set; }
}

[DeepCopy]
public partial class CovariantHolder
{
    public IEnumerable<Node>? Wide { get; set; }

    public List<CollectionNode>? Narrow { get; set; }
}

[DeepCopy]
public partial class ReverseCovariantHolder
{
    public List<CollectionNode>? Narrow { get; set; }

    public IEnumerable<Node>? Wide { get; set; }
}

[ShallowCopy]
public partial record Shallow
{
    public int[]? Items { get; init; }
}

[DeepCopy]
public partial class StoredBase
{
    public virtual int Id { get; init; } = 7;

    public virtual List<Node> Values { get; } = new();
}

[DeepCopy]
public partial class StoredDerived : StoredBase
{
    public override int Id { get; init; } = 2;

    public override List<Node> Values { get; } = new();

    public int ReadBaseId() => base.Id;

    public List<Node> ReadBaseValues() => base.Values;
}

#if NET9_0_OR_GREATER
[DeepCopy]
public partial class StoredGenericBase<T>
{
    public virtual List<T> Values { get; } = new();
}

[DeepCopy]
public partial class StoredGenericDerived<T> : StoredGenericBase<T>
{
    public override List<T> Values { get; } = new();

    public List<T> ReadBaseValues() => base.Values;
}
#endif

EOF

dotnet restore "$scratch/Consumer.csproj" --configfile "$scratch/NuGet.Config" --packages "$scratch/cache"
dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net8.0
dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net11.0
