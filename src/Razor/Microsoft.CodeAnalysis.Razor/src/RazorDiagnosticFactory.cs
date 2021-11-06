// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

internal static class RazorDiagnosticFactory
{
    private const string DiagnosticPrefix = "RZ";

    // Razor.Language starts at 0, 1000, 2000, 3000. Therefore, we should offset by 500 to ensure we can easily
    // maintain this list of diagnostic descriptors in conjunction with the one in Razor.Language.

    #region General Errors

    // General Errors ID Offset = 500

    #endregion

    #region Language Errors

    // Language Errors ID Offset = 1500

    #endregion

    #region Semantic Errors

    // Semantic Errors ID Offset = 2500

    #endregion

    #region TagHelper Errors

    // TagHelper Errors ID Offset = 3500

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributeNameNullOrEmpty =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3500",
            () => CodeAnalysisResources.TagHelper_InvalidAttributeNameNotNullOrEmpty,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidAttributeNameNullOrEmpty(string tagHelperDisplayName, string propertyDisplayName)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_InvalidAttributeNameNullOrEmpty,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            tagHelperDisplayName,
            propertyDisplayName,
            TagHelperTypes.HtmlAttributeNameAttribute,
            TagHelperTypes.HtmlAttributeName.Name);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributePrefixNotNull =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3501",
            () => CodeAnalysisResources.TagHelper_InvalidAttributePrefixNotNull,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidAttributePrefixNotNull(string tagHelperDisplayName, string propertyDisplayName)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_InvalidAttributePrefixNotNull,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            tagHelperDisplayName,
            propertyDisplayName,
            TagHelperTypes.HtmlAttributeNameAttribute,
            TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix,
            "IDictionary<string, TValue>");

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributePrefixNull =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3502",
            () => CodeAnalysisResources.TagHelper_InvalidAttributePrefixNull,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidAttributePrefixNull(string tagHelperDisplayName, string propertyDisplayName)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_InvalidAttributePrefixNull,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            tagHelperDisplayName,
            propertyDisplayName,
            TagHelperTypes.HtmlAttributeNameAttribute,
            TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix,
            "IDictionary<string, TValue>");

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeCharacter =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3503",
            () => CodeAnalysisResources.TagHelper_InvalidRequiredAttributeCharacter,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidRequiredAttributeCharacter(char invalidCharacter, string requiredAttributes)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_InvalidRequiredAttributeCharacter,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            invalidCharacter,
            requiredAttributes);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor TagHelper_PartialRequiredAttributeOperator =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3504",
            () => CodeAnalysisResources.TagHelper_PartialRequiredAttributeOperator,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_PartialRequiredAttributeOperator(char partialOperator, string requiredAttributes)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_PartialRequiredAttributeOperator,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            requiredAttributes,
            partialOperator);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeOperator =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3505",
            () => CodeAnalysisResources.TagHelper_InvalidRequiredAttributeOperator,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidRequiredAttributeOperator(char invalidOperator, string requiredAttributes)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_InvalidRequiredAttributeOperator,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            invalidOperator,
            requiredAttributes);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeMismatchedQuotes =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3506",
            () => CodeAnalysisResources.TagHelper_InvalidRequiredAttributeMismatchedQuotes,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes(char quote, string requiredAttributes)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_InvalidRequiredAttributeMismatchedQuotes,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            requiredAttributes,
            quote);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor TagHelper_CouldNotFindMatchingEndBrace =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3507",
            () => CodeAnalysisResources.TagHelper_CouldNotFindMatchingEndBrace,
            RazorDiagnosticSeverity.Error);
    public static RazorDiagnostic CreateTagHelper_CouldNotFindMatchingEndBrace(string requiredAttributes)
    {
        var diagnostic = RazorDiagnostic.Create(
            TagHelper_CouldNotFindMatchingEndBrace,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            requiredAttributes);

        return diagnostic;
    }


    #endregion
}
