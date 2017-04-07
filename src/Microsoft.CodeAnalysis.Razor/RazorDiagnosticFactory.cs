// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class RazorDiagnosticFactory
    {
        private const string DiagnosticPrefix = "RZ";

        /*
         * Razor.Language starts at 0, 1000, 2000, 3000. Therefore, we should offset by 500 to ensure we can easily
         * maintain this list of diagnostic descriptors in conjunction with the one in Razor.Language.
         */

        #region General Errors

        /*
         * General Errors ID Offset = 500
         */

        #endregion

        #region Language Errors

        /*
         * Language Errors ID Offset = 1500
         */

        #endregion

        #region Semantic Errors

        /*
         * Semantic Errors ID Offset = 2500
         */

        #endregion

        #region TagHelper Errors

        /*
         * TagHelper Errors ID Offset = 3500
         */

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributeNameNullOrEmpty =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3500",
                () => Resources.TagHelperDescriptorFactory_InvalidAttributeNameNotNullOrEmpty,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidAttributeNameNullOrEmpty(string containingTypeName, string boundPropertyName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidAttributeNameNullOrEmpty,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                boundPropertyName,
                TagHelperTypes.HtmlAttributeNameAttribute,
                TagHelperTypes.HtmlAttributeName.Name);

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributePrefixNotNull =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3501",
                () => Resources.TagHelperDescriptorFactory_InvalidAttributePrefixNotNull,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidAttributePrefixNotNull(string containingTypeName, string boundPropertyName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidAttributePrefixNotNull,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                boundPropertyName,
                TagHelperTypes.HtmlAttributeNameAttribute,
                TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix,
                "IDictionary<string, TValue>");

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidAttributePrefixNull =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3502",
                () => Resources.TagHelperDescriptorFactory_InvalidAttributePrefixNull,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidAttributePrefixNull(string containingTypeName, string boundPropertyName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidAttributePrefixNull,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                boundPropertyName,
                TagHelperTypes.HtmlAttributeNameAttribute,
                TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix,
                "IDictionary<string, TValue>");

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeCharacter =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3503",
                () => Resources.TagHelperDescriptorFactory_InvalidRequiredAttributeCharacter,
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

        private static readonly RazorDiagnosticDescriptor TagHelper_PartialRequiredAttributeOperator =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3504",
                () => Resources.TagHelperDescriptorFactory_PartialRequiredAttributeOperator,
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

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeOperator =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3505",
                () => Resources.TagHelperDescriptorFactory_InvalidRequiredAttributeOperator,
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

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredAttributeMismatchedQuotes =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3506",
                () => Resources.TagHelperDescriptorFactory_InvalidRequiredAttributeMismatchedQuotes,
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

        private static readonly RazorDiagnosticDescriptor TagHelper_CouldNotFindMatchingEndBrace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3507",
                () => Resources.TagHelperDescriptorFactory_CouldNotFindMatchingEndBrace,
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
}
