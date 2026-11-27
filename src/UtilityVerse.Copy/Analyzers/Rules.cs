using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Analyzers;

internal static class Rules
{
    private const string DiagnosticGuide =
        "https://github.com/purkayasta/TheUtilityVerse/blob/development/src/UtilityVerse.Copy/README.md#";

    internal static readonly DiagnosticDescriptor MissingPartialRule = new(
        id: "UV001",
        title: "Type must be marked as partial",
        messageFormat: "Add 'partial' to the declaration of '{0}' to generate {1}",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Add the partial modifier to the named type so UtilityVerse can generate the requested copy methods.",
        helpLinkUri: DiagnosticGuide + "uv001");

    internal static readonly DiagnosticDescriptor DeepRule = new(
        id: "UV002",
        title: "Stored member needs deep-copy support",
        messageFormat: "Member '{0}' on '{1}' needs deep-copy support for '{2}'. {3}.",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A stored field or property contains a type that needs a deep-copy opt-in or an explicit member policy.",
        helpLinkUri: DiagnosticGuide + "uv002");

    internal static readonly DiagnosticDescriptor ShallowRule = new(
        id: "UV003",
        title: "Nested type missing ShallowCopy attribute",
        messageFormat: "Property '{0}' of type '{1}' requires nested type '{2}' to have ShallowCopy attribute or implement IShallowCopy",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    internal static readonly DiagnosticDescriptor UnsupportedShapeRule = new(
        id: "UV004",
        title: "Unsupported copy shape",
        messageFormat: "Cannot generate a safe copy for '{0}'. {1}.",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The declaration or stored state needs a supported shape before the requested copy method can be generated.",
        helpLinkUri: DiagnosticGuide + "uv004");

    internal static readonly DiagnosticDescriptor IgnoredCopyPolicyRule = new(
        id: "UV005",
        title: "Copy policy has no effect",
        messageFormat: "CopyPolicy '{0}' on '{1}' has no effect. {2}.",
        category: "UtilityVerse.Copy",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "CopyPolicy applies when generating a deep copy for a stored instance field or auto-property.",
        helpLinkUri: DiagnosticGuide + "uv005");
}
