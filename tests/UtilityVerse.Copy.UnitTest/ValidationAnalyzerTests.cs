// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class ValidationAnalyzerTests
{
    [Fact]
    public Task UV001_Reports_WhenDeepCopyTypeIsNotPartial() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public class {|UV001:Person|}
        {
            public int Id { get; set; }
        }
        """);

    [Fact]
    public Task UV001_Reports_WhenShallowCopyInterfaceTypeIsNotPartial() => AnalyzerVerifier.VerifyAsync("""
        public class {|UV001:Person|} : IShallowCopy
        {
            public int Id { get; set; }
        }
        """);

    [Fact]
    public Task UV002_Reports_WhenDeepCopyPropertyTypeIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Address
        {
            public string? Street { get; set; }
        }

        [DeepCopy]
        public partial class Person
        {
            public Address? {|UV002:Home|} { get; set; }
        }
        """);

    [Fact]
    public Task UV002_Reports_WhenDeepCopyListElementTypeIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Address
        {
            public string? Street { get; set; }
        }

        [DeepCopy]
        public partial class Person
        {
            public List<Address>? {|UV002:Addresses|} { get; set; }
        }
        """);

    [Fact]
    public Task UV002_Reports_WhenDeepCopyDictionaryValueTypeIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Address
        {
            public string? Street { get; set; }
        }

        [DeepCopy]
        public partial class Person
        {
            public Dictionary<string, Address>? {|UV002:ByName|} { get; set; }
        }
        """);

    [Fact]
    public Task UV003_Reports_WhenShallowCopyPropertyTypeIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Address
        {
            public string? Street { get; set; }
        }

        [ShallowCopy]
        public partial class Person
        {
            public Address? {|UV003:Home|} { get; set; }
        }
        """);

    [Fact]
    public Task UV003_Reports_WhenShallowCopyListElementTypeIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Address
        {
            public string? Street { get; set; }
        }

        [ShallowCopy]
        public partial class Person
        {
            public List<Address>? {|UV003:Addresses|} { get; set; }
        }
        """);

    [Fact]
    public Task UV003_Reports_WhenPropertyTypeOnlyOptsIntoDeepCopy() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public partial class Address
        {
            public string? Street { get; set; }
        }

        [ShallowCopy]
        public partial class Person
        {
            public Address? {|UV003:Home|} { get; set; }
        }
        """);

    [Fact]
    public Task Analyzer_ReportsNothing_WhenNestedTypesAreOptedIn() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public partial class Address
        {
            public string? Street { get; set; }
        }

        public partial class Tag : IDeepCopy
        {
            public string? Label { get; set; }
        }

        [DeepCopy]
        public partial class Person
        {
            public Address? Home { get; set; }
            public List<Address>? Addresses { get; set; }
            public HashSet<Tag>? Tags { get; set; }
            public Dictionary<string, Address>? ByName { get; set; }
        }
        """);

    [Fact]
    public Task Analyzer_ReportsNothing_WhenPropertiesArePrimitivesBuiltInsOrValueTypes() => AnalyzerVerifier.VerifyAsync("""
        public enum Status { Ready, Done }

        public struct Point
        {
            public int X { get; set; }
        }

        [DeepCopy]
        public partial class Scalars
        {
            public int Id { get; set; }
            public int? Optional { get; set; }
            public string? Name { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid Key { get; set; }
            public decimal Amount { get; set; }
            public Status Status { get; set; }
            public Point Location { get; set; }
            public (int, string) Pair { get; set; }
            public List<int>? Numbers { get; set; }
            public Dictionary<string, Guid>? Keys { get; set; }
        }
        """);

    [Fact]
    public Task Analyzer_ReportsNothing_WhenTypeIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Address
        {
            public string? Street { get; set; }
        }

        public class Person
        {
            public Address? Home { get; set; }
        }
        """);

    [Fact]
    public Task Analyzer_IgnoresGetOnlyProperty_WhenItsTypeIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Address
        {
            public string? Street { get; set; }
        }

        [DeepCopy]
        public partial class Person
        {
            public Address Home { get; } = new();
        }
        """);
}
