/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System;
namespace UtilityVerse.Copy;

/// <summary>
/// Marks a class for shallow copy generation with configurable depth.
/// Default depth: 5. There is no upper bound — the caller controls the limit.
/// Use depth=0 for a classic shallow copy (root is cloned; all complex properties are shared references).
/// Use a negative depth to trigger a compile-time error (UV004).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class ShallowCopy : Attribute
{
    /// <summary>
    /// Gets or sets the maximum recursion depth for shallow copying.
    /// Specifies how many levels deep the copy should recurse into nested objects.
    /// Default is 5. Set to 0 for a classic shallow copy (shared references beyond root). Must be >= 0.
    /// Example: depth=1 copies the root and its immediate complex properties as new instances.
    /// </summary>
    public int Depth { get; set; } = 5;
    
    public ShallowCopy()
    {
    }
    
    public ShallowCopy(int depth)
    {
        Depth = depth;
    }
}