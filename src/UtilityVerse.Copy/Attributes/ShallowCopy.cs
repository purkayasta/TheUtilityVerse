/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System;
namespace UtilityVerse.Copy;

/// <summary>
/// Marks a class for shallow copy generation with configurable depth.
/// Default depth: 5, Maximum depth: 10
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ShallowCopy : Attribute
{
    /// <summary>
    /// Gets or sets the maximum recursion depth for shallow copying.
    /// Specifies how many levels deep the copy should recurse into nested objects.
    /// Default is 5, maximum is 10.
    /// Example: depth=3 will recurse up to 3 levels deep.
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