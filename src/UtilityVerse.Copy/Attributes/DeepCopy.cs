/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System;
namespace UtilityVerse.Copy;

/// <summary>
/// Marks a class for deep copy generation with a configurable recursion depth.
/// Depth is a hard recursion cap (default 5, no upper bound). Circular references are detected via
/// an identity map and reproduced faithfully — a cycle terminates when an object is revisited rather
/// than at the depth cap, so the copied graph keeps the same cycle structure as the original.
/// The cap only truncates genuinely deep, acyclic chains (the level beyond the cap becomes null).
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
    /// Example: depth=3 copies up to 3 levels of an acyclic chain; the 4th level is set to null.
    /// For circular references the identity map preserves the cycle before the cap is reached.
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