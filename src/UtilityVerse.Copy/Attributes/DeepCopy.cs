// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD

using System;
namespace UtilityVerse.Copy;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class DeepCopy : Attribute
{
    /// <summary>Selects generated deep copying with the default root depth of 5.</summary>
    public DeepCopy() : this(5)
    {
    }

    /// <summary>Selects generated deep copying with a root depth from 1 through 100. Invalid values warn and use 5.</summary>
    public DeepCopy(int depth)
    {
        Depth = depth;
    }

    public int Depth { get; }
}
