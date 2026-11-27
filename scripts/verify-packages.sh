#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
scratch="$(mktemp -d "${TMPDIR:-/tmp}/utilityverse-packages.XXXXXX")"
packages="$scratch/packages"
trap 'rm -rf "$scratch"' EXIT

dotnet pack "$root/UtilityVerse.slnx" --no-restore -c Release -o "$packages"

core_package="$packages/UtilityVerse.0.7.0.nupkg"
if unzip -p "$core_package" UtilityVerse.nuspec | grep 'dependency id="UtilityVerse.Shared"' > /dev/null; then
    echo "UtilityVerse must bundle Shared instead of requiring a separate package."
    exit 1
fi

core_contents="$(unzip -Z1 "$core_package")"
while IFS= read -r framework; do
    for extension in dll xml; do
        if ! printf '%s\n' "$core_contents" | grep -Fx "lib/$framework/UtilityVerse.Shared.$extension" > /dev/null; then
            echo "Missing bundled Shared $extension for $framework."
            exit 1
        fi
    done
done < <(printf '%s\n' "$core_contents" | sed -n 's@^lib/\([^/]*\)/UtilityVerse.dll$@\1@p')

if [ -e "$packages/UtilityVerse.Shared.1.0.0.nupkg" ]; then
    echo "Shared must not produce a standalone package."
    exit 1
fi

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
    <PackageReference Include="UtilityVerse" Version="0.7.0" />
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

var depthRoot = new DepthNode();
var depthTail = depthRoot;

for (var level = 1; level < 10; level++)
{
    depthTail.Next = new DepthNode();
    depthTail = depthTail.Next;
}

depthTail.Next = depthRoot;
new CopyContext().Copy(depthRoot);
((IDeepCopyable)depthRoot).Copy(new CopyContext());
var interfaceRoot = new InterfaceDepthNode();
interfaceRoot.Next = interfaceRoot;
interfaceRoot.DeepCopy();
interfaceRoot.Next = new InterfaceDepthNode();

try
{
    interfaceRoot.DeepCopy();
    throw new Exception("Packaged depth limit was not enforced.");
}
catch (NotSupportedException)
{
}

Console.WriteLine("Fresh-package consumer passed.");

[DeepCopy(depth: 10)]
public partial class DepthNode
{
    public DepthNode? Next { get; set; }
}

[CopyDepth(1)]
public partial class InterfaceDepthNode : IDeepCopy
{
    public InterfaceDepthNode? Next { get; set; }
}

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

cp "$scratch/Program.cs" "$scratch/OriginalProgram.txt"

dotnet restore "$scratch/Consumer.csproj" --configfile "$scratch/NuGet.Config" --packages "$scratch/cache"
dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net8.0
dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net11.0

# Verify the installed documentation and execute every complete example program.
for package_name in UtilityVerse UtilityVerse.Shared UtilityVerse.ASPNET UtilityVerse.Copy; do
    example_document="$scratch/$package_name.examples.md"

    if [ "$package_name" = "UtilityVerse.Shared" ]; then
        cp "$root/src/$package_name/examples.md" "$example_document"
    else
        package_archive=""

        for candidate in "$packages/$package_name."[0-9]*.nupkg; do
            package_archive="$candidate"
        done

        unzip -p "$package_archive" examples.md > "$example_document"
        cmp "$root/src/$package_name/examples.md" "$example_document"

        if ! unzip -Z1 "$package_archive" | grep -E "^lib/[^/]+/$package_name\.xml$" > /dev/null; then
            echo "Missing XML method documentation in $package_name."
            exit 1
        fi
    fi

    example_count="$(awk -v destination="$scratch" '
        /^```csharp$/ {
            count++
            active = 1
            next
        }
        /^```$/ {
            active = 0
            next
        }
        active {
            print > (destination "/example-" count ".txt")
        }
        END {
            print count + 0
        }
    ' "$example_document")"

    if [ "$example_count" -eq 0 ]; then
        echo "No runnable examples found for $package_name."
        exit 1
    fi

    for ((example_index = 1; example_index <= example_count; example_index++)); do
        cp "$scratch/example-$example_index.txt" "$scratch/Program.cs"
        echo "==> $package_name example $example_index/$example_count"
        dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net8.0
        dotnet run --project "$scratch/Consumer.csproj" --no-restore -c Release -f net11.0
    done

done

# Diagnostic fixtures are compiled alongside the original consumer smoke test.
cp "$scratch/OriginalProgram.txt" "$scratch/Program.cs"
cat > "$scratch/Diagnostics.cs" <<'EOF'
using UtilityVerse.Copy;

[DeepCopy]
public class AttributeModel { }

public class MarkerModel : IDeepCopy { }

public class Unconfigured { }

[DeepCopy]
public partial class ParentModel
{
    public List<Unconfigured>? Children { get; set; }
}

[DeepCopy]
file partial class FileLocalModel { }

public partial class ShallowModel : IShallowCopy
{
    [CopyPolicy(CopyBehavior.Reset)]
    public int Value { get; set; }
}
EOF

sarif="$scratch/copy-diagnostics.sarif"
if diagnostic_output="$(dotnet build "$scratch/Consumer.csproj" --no-restore -c Release -f net11.0 \
    -p:WarningsNotAsErrors=UV005 -p:ErrorLog="$sarif" 2>&1)"; then
    echo "Expected analyzer errors from the installed Copy package."
    exit 1
fi

for expected in \
    "error UV001: Add 'partial' to the declaration of 'AttributeModel' to generate DeepCopy()" \
    "error UV001: Add 'partial' to the declaration of 'MarkerModel' to generate DeepCopy()" \
    "error UV002: Member 'Children' on 'ParentModel' needs deep-copy support for 'Unconfigured'" \
    "error UV004: Cannot generate a safe copy for 'FileLocalModel'" \
    "warning UV005:"; do
    if [[ "$diagnostic_output" != *"$expected"* ]]; then
        printf 'Missing diagnostic %s\n%s\n' "$expected" "$diagnostic_output"
        exit 1
    fi
done

test -s "$sarif"
sarif_content="$(< "$sarif")"
for rule in uv001 uv002 uv004 uv005; do
    if [[ "$sarif_content" != *"README.md#$rule"* ]]; then
        printf 'Missing help link for %s in the compiler SARIF log\n' "$rule"
        exit 1
    fi
done

echo "Fresh-package analyzer diagnostics passed."

mkdir -p "$root/artifacts/packages"
cp "$packages/"*.nupkg "$packages/"*.snupkg "$root/artifacts/packages/"
