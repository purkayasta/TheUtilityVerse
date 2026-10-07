# 📦 UtilityVerse.Copy — Source Generator for DeepCopy and ShallowCopy in C\#

🚀 **UtilityVerse.Copy** is a Roslyn-based source generator that automatically creates `DeepCopy()` and `ShallowCopy()` methods for your models. Eliminate repetitive boilerplate and enjoy clean, maintainable code with zero runtime dependencies.

---

## ✨ Features

* ✅ Automatically generates `DeepCopy()` and/or `ShallowCopy()` methods at compile-time
* ✅ No runtime overhead — it's all generated in the background by Roslyn
* ✅ Supports both `[Attribute]`-based and `interface`-based opt-in mechanisms
* ✅ Works great for DTOs, ViewModels, and plain C# objects
* ✅ Support for common collection types, arrays, tuples, and more
* ⚠️ Requires a compiler with Roslyn 4.11 or newer (Visual Studio 2022 17.11+ or .NET SDK 8.0.400+); the consuming project can still target any framework
* ⚠️ Opted-in types must be `public` and `partial`

---



> Code is fully opensource and more details can be found here at: [Github](https://github.com/purkayasta/TheUtilityVerse/blob/development/src/UtilityVerse.Copy/README.md)