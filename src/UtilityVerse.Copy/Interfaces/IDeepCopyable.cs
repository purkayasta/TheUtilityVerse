// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

namespace UtilityVerse.Copy;

/// <summary>Implemented by generated code to share one context across an object graph.</summary>
public interface IDeepCopyable
{
    /// <summary>Copies this value using the supplied graph context.</summary>
    object Copy(CopyContext context);
}
