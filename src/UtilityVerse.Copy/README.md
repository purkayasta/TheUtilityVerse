---

# 📦 UtilityVerse.Copy — Source Generator for DeepCopy and ShallowCopy in C\#

🚀 **UtilityVerse.Copy** is a Roslyn-based source generator that automatically creates `DeepCopy()` and `ShallowCopy()` methods for your models. Eliminate repetitive boilerplate and enjoy clean, maintainable code with zero runtime dependencies.

---

## ✨ Features

* ✅ Automatically generates `DeepCopy()` and/or `ShallowCopy()` methods at compile-time
* ✅ No runtime overhead — it's all generated in the background by Roslyn
* ✅ Supports both `[Attribute]`-based and `interface`-based opt-in mechanisms
* ✅ Works great for DTOs, ViewModels, and plain C# objects
* ✅ Support for common collection types, arrays, tuples, and more

---


## > Give it a star if you like the project. 👏 🌠 🌟


---

## 📦 Installation

Install the NuGet package:

```bash
dotnet add package UtilityVerse.Copy
```

---

## 🛠️ How It Works

> **Important**: Your classes must be marked `partial` for the generator to emit code successfully.

### ✅ Option 1: Use Attributes

Apply `[ShallowCopy]` or `[DeepCopy]` to your class or struct:

```csharp
using UtilityVerse.Copy;

[ShallowCopy]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

This generates a `ShallowCopy()` method at compile-time:

```csharp
public Person ShallowCopy()
{
    return (Person)this.MemberWiseClone();
}
```

For deep copy:

```csharp
using UtilityVerse.Copy;
[DeepCopy]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

Generates a deep recursive `DeepCopy()` method that copies nested references and collections.

```csharp
public Person DeepCopy()
{
    return new Person
    {
        Name = this.Name,
        Age = this.Age
    };
}
```

---

### ✅ Option 2: Use Marker Interfaces

Prefer no attributes? Just implement the marker interfaces:

```csharp
using UtilityVerse.Copy;
public partial class Person : IShallowCopy
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

```csharp
using UtilityVerse.Copy;
public partial class Person : IDeepCopy
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

The generator will handle the rest automatically!

---

## 📚 Supported Types

* ✅ Primitives and strings
* ✅ Arrays
* ✅ Tuples and `ValueTuple`
* ✅ Generic collections: `List<T>`, `IEnumerable<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `HashSet<T>`, etc.
* ✅ Dictionary-like collections: `Dictionary<TKey, TValue>`, `ConcurrentDictionary`, `FrozenDictionary`, etc.
* ✅ Record types, classes, structs (as long as they are `partial`)
* ✅ Nested properties recursively copied in `DeepCopy()`

---

## 🧪 Sample Output

Given this class:

```csharp
[DeepCopy]
public partial class User
{
    public string Name { get; set; }
    public List<Address> Addresses { get; set; }
}
```

Generated deep copy:

```csharp
public User DeepCopy()
{
    return new User
    {
        Name = this.Name,
        Addresses = this.Addresses?.Select(x => x?.DeepCopy()).ToList()
    };
}
```

---

## 🔒 Safe and Reliable

* 💡 **Partial**: Won’t overwrite your code — generated code lives alongside your class
* 🧾 **Readable**: Generated files are emitted to the intermediate folder (obj)
* ⚙️ **Non-Intrusive**: No reflection, no extra dependencies

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🤝 Contributing

We welcome contributions! Please see the [CONTRIBUTE.md](CONTRIBUTE.md) file for guidelines.

---