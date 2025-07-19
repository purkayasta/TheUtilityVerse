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
}