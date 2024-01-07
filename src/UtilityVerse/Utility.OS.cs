// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Runtime.InteropServices;

namespace UtilityVerse;

public sealed partial class Utility
{
    /// <summary>
    /// Is the running platform is windows?.
    /// </summary>
    public static bool IsItWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Is the app running on Linux?
    /// </summary>
    public static bool IsItLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Is the app running on mac?
    /// </summary>
    public static bool IsItMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Is the app is running on free bsd?
    /// </summary>
    public static bool IsBsd => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

#endif

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Framework runtime information.
    /// </summary>
    public static string FrameworkRuntime => RuntimeInformation.RuntimeIdentifier;

#endif
}
