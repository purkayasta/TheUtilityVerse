// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;

namespace UtilityVerse.Copy;

/// <summary>Configures the graph depth when this model is copied as a root.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class CopyDepthAttribute : Attribute
{
    /// <summary>Configures root depth for an interface-opted-in model. Values outside 1 through 100 warn and use 5.</summary>
    public CopyDepthAttribute(int depth)
    {
        Depth = depth;
    }

    public int Depth { get; }
}
