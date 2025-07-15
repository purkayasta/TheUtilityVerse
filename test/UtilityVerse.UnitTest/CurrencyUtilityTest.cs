// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Xunit.Abstractions;

namespace UtilityVerse.UnitTest;

public class CurrencyUtilityTest
{
    private readonly ITestOutputHelper _output;

    public CurrencyUtilityTest(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData(0.0001)]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(10)]
    public void DollarToCent_ShouldReturnRightCent_WhenRightDollarProvided(decimal input)
    {
        var calculatedValue = input * 100;
        var expectedValue = Utility.DollarToCent(input);

        _output.WriteLine("Dollar {0}$, Calculated Cent {1}c, Expected Cent: {2}c ", input, calculatedValue, expectedValue);

        Assert.Equal(expectedValue.Result, calculatedValue);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.0001)]
    [InlineData(10)]
    public void CentToDollar_ShouldReturnRightDollar_WhenRightCentIsProvided(decimal input)
    {
        var calculatedValue = input / 100;
        var expectedValue = Utility.CentToDollar(input);

        _output.WriteLine("Cent: {0}, Calculated Dollar: {1}$, Expected Dollar: {2}$", input, calculatedValue, expectedValue);
        Assert.Equal(expectedValue.Result, calculatedValue);
    }
}
