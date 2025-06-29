// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Shared;

namespace UtilityVerse;

public sealed partial class Utility
{
    /// <summary>
    /// This will convert dollar value to cent
    /// </summary>
    /// <param name="dollar"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<decimal> DollarToCent(decimal dollar)
    {
        try
        {
            return new UtilityVerseResult<decimal>(dollar * 100);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new UtilityVerseResult<decimal>("Argument Out Of Range");
        }
        catch (ArithmeticException)
        {
            return new UtilityVerseResult<decimal>("Math Exception");
        }
        catch (Exception unknownException)
        {
            return new UtilityVerseResult<decimal>($"Unknown exception: {unknownException}");
        }
    }


    /// <summary>
    /// This will convert cent into dollar value
    /// </summary>
    /// <param name="cent"></param>
    /// <returns>decimal</returns>
    public static UtilityVerseResult<decimal> CentToDollar(decimal cent)
    {
        try
        {
            return new UtilityVerseResult<decimal>(result: cent / 100);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new UtilityVerseResult<decimal>(error: "Out of Range Exception");
        }
        catch (ArithmeticException)
        {
            return new UtilityVerseResult<decimal>(error: "Arithmetic Exception Occurred");
        }
        catch (Exception exception)
        {
            return new UtilityVerseResult<decimal>(error: $"Unknow Exception: {exception}");
        }
    }
}
