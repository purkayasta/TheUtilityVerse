# UtilityVerse.Copy

Generate typed deep-copy and shallow-copy methods for partial C# classes, records,
structs, and record structs, using attributes or empty marker interfaces.
Opt into both modes to generate both methods.

Deep copying preserves stored fields and auto-properties, shared reference identity,
mutable object cycles, supported runtime derived types, nested collections, and
collection comparers. Stored members can explicitly share, reset, or use a
registered external copier. Shallow copying creates a new outer object and
shares every reference field, including arrays.

Requires Roslyn 4.11+ (.NET SDK 8.0.400+ or Visual Studio 2022 17.11+). Generated
code uses a small graph context from this package, with no model-member reflection
or external runtime dependencies. Retain the runtime package reference.

Unsupported shapes receive a compiler diagnostic instead of unsafe generated code.
See the [supported types, behavior changes, and limitations](https://github.com/purkayasta/TheUtilityVerse/blob/development/src/UtilityVerse.Copy/README.md)
before adopting the package.
