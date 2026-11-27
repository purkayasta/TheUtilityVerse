// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;

namespace UtilityVerse.Copy;

/// <summary>Chooses how a stored member is handled by a generated deep copy.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class CopyPolicyAttribute : Attribute
{
    /// <summary>Selects how a stored member is copied, shared, reset, or dispatched to an external copier.</summary>
    public CopyPolicyAttribute(CopyBehavior behavior)
    {
        Behavior = behavior;
    }

    /// <summary>Gets the policy applied to the stored member.</summary>
    public CopyBehavior Behavior { get; }
}

/// <summary>Controls the value assigned to a stored member during a deep copy.</summary>
public enum CopyBehavior
{
    /// <summary>Copy the value according to its declared shape.</summary>
    Copy,

    /// <summary>Assign the original value.</summary>
    Share,

    /// <summary>Assign the default value of the member type.</summary>
    Reset,

    /// <summary>Use the external copier registered for the value's exact runtime type.</summary>
    Custom
}
