// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using UtilityVerse.Copy.Analyzers;

namespace UtilityVerse.Copy.UnitTest.Infrastructure;

internal static class AnalyzerVerifier
{
    private const string Usings = """
        #nullable enable
        using System;
        using System.Collections.Generic;
        using UtilityVerse.Copy;

        """;

    private static readonly ReferenceAssemblies OfflineNet90 = new("net9.0");

    public static Task VerifyAsync(string markupSource)
    {
        var test = new CSharpAnalyzerTest<ValidationAnalyzer, DefaultVerifier>
        {
            TestCode = Usings + markupSource,
            ReferenceAssemblies = OfflineNet90,
        };
        test.TestState.AdditionalReferences.AddRange(GeneratorHarness.References);

        return test.RunAsync();
    }
}
