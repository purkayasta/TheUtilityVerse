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

Decorate your classes/records using:

* `[DeepCopy]` — to generate a recursive deep copy method.
* `[ShallowCopy]` — to generate a simple shallow copy.

The attributes target classes only; structs and record structs opt in through the marker interfaces below.

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
> Your class or struct **must be `public` and marked `partial`** for the source generator to work. The analyzer reports `UV001` if `partial` is missing.
>
> Requires a compiler with Roslyn 4.11 or newer (Visual Studio 2022 17.11+ or .NET SDK 8.0.400+).

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
* ✅ Generic collections (declare properties as `List<T>`, `HashSet<T>`, `Dictionary<K,V>` or an interface such as `IEnumerable<T>`/`IList<T>`)
* ✅ Records, classes, structs (`partial` required)
* ✅ Deep recursive copying of nested properties

---


## 📣 Like It? Star It! ⭐

If you find UtilityVerse.Copy useful, give the repository a ⭐ and help others discover it!

