// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


namespace UtilityVerse.Contracts;

/// <summary>
/// Timer Enum
/// </summary>
public enum TimeEnum
{
    /// <summary>
    /// This represent second
    /// </summary>
    Second,

    /// <summary>
    /// This represent millisecond
    /// </summary>
    MilliSecond
}

/// <summary>
/// Encoding Enum
/// </summary>
public enum EncodingEnum
{
    /// <summary>
    /// UTF8
    /// </summary>
    UTF8,

    /// <summary>
    /// UTF32
    /// </summary>
    UTF32,

    /// <summary>
    /// UNICODE
    /// </summary>
    Unicode,

    /// <summary>
    /// ASCII
    /// </summary>
    Ascii,

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// LATIN
    /// </summary>
    Latin
#endif
}
