# Copy - Source Generator for DeepCopy and ShallowCopy in C#

🚀 **Copy** is a Roslyn-based source generator that reduces boilerplate by automatically generating `DeepCopy` and `ShallowCopy` methods for your models. Just use an attribute or implement a marker interface, and the generator handles the rest!


---

## ✨ Features

- ✅ Automatically generates `DeepCopy()` and/or `ShallowCopy()` methods
- ✅ Clean, zero-runtime dependency — generation happens at compile-time
- ✅ Supports both `[Attribute]` and `interface`-based opt-in mechanisms
- ✅ Great for DTOs, ViewModels, and data layer objects

---

## 📦 Installation

Add the package to your project using NuGet:

```bash
dotnet add package Copy
```


🛠️ How It Works

> MAKE SURE, your models/pocos/dtos are `partial`. Without this, it won't work.

### Option 1: Use Attributes

Annotate your model class with `[ShallowCopy]` or `[DeepCopy]`:
```c#
using Copy;

[ShallowCopy]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

```

This generates a ShallowCopy() method:
```c#
public Person ShallowCopy()
{
    return new Person
    {
        Name = this.Name,
        Age = this.Age
    };
}
```
### Option 2: Implement Marker Interfaces

Instead of using attributes, implement one of the marker interfaces:

```c#
public partial class Person : IShallowCopy
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```
This will have the same result: a ShallowCopy() method will be generated at compile-time.

🤿 Deep Copy Support

If your model contains reference-type properties or collections and you want a true deep copy, use `[DeepCopy]` or implement  `IDeepCopy`:
```c#
[DeepCopy]
public partial class Order
{
    public Customer Customer { get; set; }
    public List<Item> Items { get; set; }
}
```
The generator will recursively clone reference types where possible.

✅ Generated Code Is:

    Partial – Safe to regenerate on build

    Readable – Emitted to the intermediate folder for inspection

    Non-intrusive – Won’t modify your original source
