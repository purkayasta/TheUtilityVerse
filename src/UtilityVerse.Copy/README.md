
---

# 📦 UtilityVerse.Copy — Source Generator for DeepCopy and ShallowCopy in C\#

![Nuget](https://img.shields.io/nuget/v/UtilityVerse.Copy) ![Nuget](https://img.shields.io/nuget/dt/UtilityVerse.Copy?style=plastic)
---

## 📖 What is UtilityVerse.Copy?

**UtilityVerse.Copy** is a Roslyn-based source generator for C#. It automatically creates `DeepCopy()` and `ShallowCopy()` methods for your classes and structs at compile-time. With zero runtime dependencies, your copy logic is safe, efficient, and maintainable — without writing repetitive code.

---

## ❓ Why Use It?

* Copying objects (deep or shallow) usually requires verbose, error-prone boilerplate.
* UtilityVerse.Copy removes that burden by generating fully-typed copy methods at compile-time using Roslyn.
* No runtime reflection, no magic — just pure, reliable, compiled code.
* Perfect for:

  * DTOs
  * ViewModels
  * Configuration models
  * Game entities
  * Domain models needing clean clone operations

---

## ✨ Features

* ✅ Auto-generates `DeepCopy()` and/or `ShallowCopy()` methods
* ✅ Pure compile-time code generation (no runtime dependencies)
* ✅ Supports both `[Attributes]` and `Marker Interfaces`
* ✅ Handles:

  * Common collections (List, HashSet, Dictionary, etc.)
  * Arrays, Tuples, ValueTuples
  * Primitive types, strings, and nested objects
  * Records, classes, and structs (as long as they are `partial`)
* ✅ Safe — generated code is separate and non-intrusive

---

## 🔑 API — What You Need to Use

### 1️⃣ Attributes

Decorate your classes/structs using:

* `[DeepCopy]` — to generate a recursive deep copy method.
* `[ShallowCopy]` — to generate a simple shallow copy.

Example:

```csharp
using UtilityVerse.Copy;

[DeepCopy]
public partial class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

---

### 2️⃣ Marker Interfaces

If you prefer code-first opt-in:

* Implement `IDeepCopy` for deep copy generation.
* Implement `IShallowCopy` for shallow copy generation.

Example:

```csharp
using UtilityVerse.Copy;

public partial class Product : IDeepCopy
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

---

> **Important:**
> Your class or struct **must be marked `partial`** for the source generator to work.

---

## 🚀 Tutorial — How to Use

### Step 1️⃣ — Install via NuGet

```bash
dotnet add package UtilityVerse.Copy
```

---

### Step 2️⃣ — Add `[DeepCopy]` or `[ShallowCopy]`

```csharp
using UtilityVerse.Copy;

[DeepCopy]
public partial class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<Address> Addresses { get; set; } = [];
}

[DeepCopy]
public partial class Address
{
    public string? StreetNumber { get; set; }
}
```

---

### Step 3️⃣ — Generated Code (Example)

`DeepCopy()` for `User` will be automatically generated:

```csharp
public User DeepCopy()
{
    return new User
    {
        Id = this.Id,
        Name = this.Name,
        Addresses = this.Addresses?.Select(x => x?.DeepCopy()).ToList()
    };
}
```

Similarly for `Address`:

```csharp
public Address DeepCopy()
{
    return new Address
    {
        StreetNumber = this.StreetNumber
    };
}
```

---

### Step 4️⃣ — Use It!

```csharp
var userClone = existingUser.DeepCopy();
```

---

## 📚 Supported Types

* ✅ Primitives (`int`, `string`, `bool`, etc.)
* ✅ Arrays
* ✅ Tuples & ValueTuples
* ✅ Generic collections (`List<T>`, `Dictionary<K,V>`, etc.)
* ✅ Records, classes, structs (`partial` required)
* ✅ Deep recursive copying of nested properties

---


## 🔢 Depth Control

Both `[DeepCopy]` and `[ShallowCopy]` accept an optional `maxDepth` parameter that controls how many levels of nested objects are copied.

```csharp
[DeepCopy(2)]      // copy up to 2 levels deep
[ShallowCopy(1)]   // shallow-copy one level of nesting
public partial class Order { ... }
```

The default depth when no argument is provided is **5**.

---

### How depth is counted

Depth starts at **0** for the root object. Each nested object call increments the depth by 1.

```
Order          ← depth 0  (always copied)
  Address      ← depth 1
    City       ← depth 2  (copied only if maxDepth >= 2)
```

With `[DeepCopy(2)]` on `Order`, both `Address` and `City` are copied. A fourth level would be `null`.

---

### DeepCopy depth semantics

| Situation | Result |
|---|---|
| Property is within depth | Recursively deep-copied as a new instance |
| Property exceeds depth | `null` (reference types) or `default` (value types) |
| Circular reference within depth | Preserved via identity map (same copied instance reused) |
| Circular reference at depth boundary | Stopped — returns `null` instead of looping |

```csharp
[DeepCopy(2)]
public partial class ChainNode
{
    public int Value { get; set; }
    public ChainNode? Next { get; set; }
}

var n3 = new ChainNode { Value = 3 };
var n2 = new ChainNode { Value = 2, Next = n3 };
var n1 = new ChainNode { Value = 1, Next = n2 };

var clone = n1.DeepCopy();
// clone.Value       == 1   ✓
// clone.Next.Value  == 2   ✓  (depth 1, within limit)
// clone.Next.Next   == null    (depth 2 reached, truncated)
```

---

### ShallowCopy depth semantics

ShallowCopy uses `MemberwiseClone` at the root and then selectively overrides complex properties based on depth.

| Situation | Result |
|---|---|
| Property is within depth | A new shallow-copied instance (new object, shared sub-references) |
| Property exceeds depth | Original reference shared as-is (not copied) |

```csharp
[ShallowCopy(0)]  // classic shallow copy — all nested references shared
public partial class Invoice { ... }

[ShallowCopy(1)]  // BillingAddress is a new instance; its own properties are shared
public partial class Receipt { ... }
```

---

### Circular reference handling

Both generators track already-copied objects via an internal identity map (`Dictionary<object, object>`). If the same object is encountered again during traversal, the previously created copy is reused — preserving referential integrity in the clone.

```csharp
var alice = new Friend { Name = "Alice" };
var bob   = new Friend { Name = "Bob" };
alice.BestFriend = bob;
bob.BestFriend   = alice;   // circular reference

var clone = alice.DeepCopy();
// clone.BestFriend.BestFriend == clone  ✓  (cycle preserved, no stack overflow)
```

---

### What is NOT supported

| Limitation | Reason |
|---|---|
| Depth cannot be negative | Compile-time analyzer (UV004) reports an error |
| Cannot use `private` for the internal copy method | The generated `DeepCopyInternal`/`ShallowCopyInternal` must be `internal` because the recursive calls cross type boundaries (e.g. `Order` calling into `Address.DeepCopyInternal`) |
| Abstract types are not generated | A compile-time warning (UV008) is reported; abstract types cannot be instantiated |
| Non-public types produce a warning | Analyzer UV006 warns that the generated `public` method on a non-public type is likely unreachable |
| Structs do not track circular refs | Structs are value types and cannot be keys in the identity map; they are always copied by initialiser |
| `IDeepCopy`/`IShallowCopy` marker interfaces do not accept depth | Depth can only be specified via the attribute constructor: `[DeepCopy(n)]` |

---

## 🗒 Release Notes

### v2.x — Depth & Circular Reference Support

- **Configurable copy depth** — `[DeepCopy(n)]` and `[ShallowCopy(n)]` now accept an optional `maxDepth` argument. Default is `5`.
- **Circular reference safety** — deep and shallow copies use an identity map (`Dictionary<object, object>`) to detect already-copied objects, preventing stack overflows and preserving referential integrity in the clone.
- **Struct objectMap propagation** — structs now share the same identity map as their parent copy call, ensuring consistent behaviour when a struct holds reference-type properties that appear elsewhere in the object graph.
- **Single internal entry point** — `DeepCopyInternal`/`ShallowCopyInternal` are now single-overload methods that always receive the `objectMap`; the previous two-parameter bridge overload (which created an isolated map) has been removed.
- **Compile-time depth validation** — negative depth values are rejected at compile time (analyzer UV004).

---

## 📣 Like It? Star It! ⭐

If you find UtilityVerse.Copy useful, give the repository a ⭐ and help others discover it!

---

Let me know if you'd like this structured as a template file or want me to generate sample output code files directly.
