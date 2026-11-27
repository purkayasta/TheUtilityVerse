// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

namespace UtilityVerse.Copy;

/// <summary>Lets generated struct copiers register a destination box before copying its children.</summary>
public interface IBoxedDeepCopyable : IDeepCopyable
{
    /// <summary>Copies the original box using the supplied graph context.</summary>
    /// <remarks>For generated code. Use CopyContext.Copy to copy a boxed value.</remarks>
    object CopyBoxed(object source, CopyContext context);
}
