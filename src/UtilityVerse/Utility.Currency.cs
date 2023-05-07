namespace UtilityVerse;

public partial class Utility
{
    /// <summary>
    /// This will convert dollar value to cent
    /// </summary>
    /// <param name="dollar"></param>
    /// <returns></returns>
    public static decimal DollarToCent(decimal dollar) => dollar * 100;


    /// <summary>
    /// This will convert cent into dollar value
    /// </summary>
    /// <param name="cent"></param>
    /// <returns></returns>
    public static decimal CentToDollar(decimal cent) => cent / 100;
}
