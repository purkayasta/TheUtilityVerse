/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System;
namespace UtilityVerse.Copy;

/// <summary>
/// Marks a class for deep copy generation with configurable depth.
/// Default depth: 5, Maximum depth: 10
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DeepCopy : Attribute
{
    /// <summary>
    /// Gets or sets the depth for deep copying. Default is 5, maximum is 10.
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