using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Analyzers;

internal static class Rules
{
    internal static readonly DiagnosticDescriptor MissingPartialRule = new(
        id: "UV001",
        title: "Type must be marked as partial",
        messageFormat: "Type '{0}' must be marked as 'partial' to support DeepCopy/ShallowCopy generation",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor DeepRule = new(
        id: "UV002",
        title: "Nested type missing DeepCopy attribute",
        messageFormat: "Property '{0}' of type '{1}' requires nested type '{2}' to have DeepCopy attribute or implement IDeepCopy",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ShallowRule = new(
        id: "UV003",
        title: "Nested type missing ShallowCopy attribute",
        messageFormat: "Property '{0}' of type '{1}' requires nested type '{2}' to have ShallowCopy attribute or implement IShallowCopy",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor NegativeDepthRule = new(
        id: "UV004",
        title: "Depth must be zero or greater",
        messageFormat: "Type '{0}' has an invalid depth value ({1}). Depth must be 0 or greater. Use 0 to skip copying entirely.",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor NonPublicTypeRule = new(
        id: "UV006",
        title: "Non-public type decorated with copy attribute",
        messageFormat: "Type '{0}' is not public. DeepCopy/ShallowCopy generation requires a public type; the attribute will be ignored.",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor EnclosingTypeNotPartialRule = new(
        id: "UV009",
        title: "Enclosing type must be partial",
        messageFormat: "Type '{0}' is nested inside non-partial type '{1}', which must be declared 'partial' for DeepCopy/ShallowCopy generation",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor AbstractTypeRule = new(
        id: "UV008",
        title: "Abstract type decorated with copy attribute",
        messageFormat: "Type '{0}' is abstract and cannot be instantiated. DeepCopy/ShallowCopy generation for abstract types is not supported.",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}