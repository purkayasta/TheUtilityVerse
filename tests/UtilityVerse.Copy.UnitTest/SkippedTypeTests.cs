// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class SkippedTypeTests
{
    [Theory]
    [InlineData("[DeepCopy] public abstract partial class Skipped { public int Id { get; set; } }")]
    [InlineData("[ShallowCopy] public abstract partial class Skipped { public int Id { get; set; } }")]
    [InlineData("[DeepCopy] public class Skipped { public int Id { get; set; } }")]
    [InlineData("[ShallowCopy] public class Skipped { public int Id { get; set; } }")]
    [InlineData("[DeepCopy] internal partial class Skipped { public int Id { get; set; } }")]
    [InlineData("[ShallowCopy] internal partial class Skipped { public int Id { get; set; } }")]
    [InlineData("public partial class Skipped { public int Id { get; set; } }")]
    public void Generate_ProducesNoSource_WhenTypeIsAbstractNonPartialNonPublicOrNotOptedIn(string declaration)
    {
        var run = GeneratorHarness.Run($"namespace Sample; {declaration}");

        Assert.Empty(run.GeneratedSources);
    }
}
