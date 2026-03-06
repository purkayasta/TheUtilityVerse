/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System;
namespace UtilityVerse.Copy;

/// <summary>
/// Marks a class for deep copy generation with configurable depth.
/// Default depth: 5. There is no upper bound — the caller controls the limit.
/// Use depth=0 to suppress copying entirely (returns null).
/// Use a negative depth to trigger a compile-time error (UV004).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class DeepCopy : Attribute
{
    /// <summary>
    /// Gets or sets the maximum recursion depth for deep copying.
    /// Specifies how many levels deep the copy should recurse into nested objects.
    /// Default is 5. Set to 0 to disable copying (returns null). Must be >= 0.
    /// Example: depth=3 will copy up to 3 levels; the 4th level is set to null.
    /// </summary>
    public int Depth { get; set; } = 5;
    
    public DeepCopy()
    {
    }
    
    public DeepCopy(int depth)
    {
        Depth = depth;
    }
}