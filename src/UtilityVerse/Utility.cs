// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Runtime.InteropServices;

namespace UtilityVerse;

/// <summary>
/// Base utility verse class for writing helpers.
/// </summary>
public sealed partial class Utility
{
    public static bool IsWindows { get => RuntimeInformation.IsOSPlatform(OSPlatform.Windows); }
}
