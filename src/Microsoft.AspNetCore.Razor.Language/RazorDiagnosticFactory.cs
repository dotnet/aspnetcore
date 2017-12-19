// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal static class RazorDiagnosticFactory
    {
        private const string DiagnosticPrefix = "RZ";

        #region General Errors

        // General Errors ID Offset = 0

        internal static readonly RazorDiagnosticDescriptor Directive_BlockDirectiveCannotBeImported =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}0000",
                () => Resources.BlockDirectiveCannotBeImported,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateDirective_BlockDirectiveCannotBeImported(string directive)
        {
            return RazorDiagnostic.Create(Directive_BlockDirectiveCannotBeImported, SourceSpan.Undefined, directive);
        }

        #endregion

        #region Language Errors

        // Language Errors ID Offset = 1000

        internal static readonly RazorDiagnosticDescriptor Parsing_UnterminatedStringLiteral =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1000",
                () => LegacyResources.ParseError_Unterminated_String_Literal,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnterminatedStringLiteral(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_UnterminatedStringLiteral, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_BlockCommentNotTerminated =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1001",
                () => LegacyResources.ParseError_BlockComment_Not_Terminated,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_BlockCommentNotTerminated(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_BlockCommentNotTerminated, location);
        }

        #endregion

        #region Semantic Errors

        // Semantic Errors ID Offset = 2000

        internal static readonly RazorDiagnosticDescriptor CodeTarget_UnsupportedExtension =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2000",
                () => Resources.Diagnostic_CodeTarget_UnsupportedExtension,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateCodeTarget_UnsupportedExtension(string documentKind, Type extensionType)
        {
            return RazorDiagnostic.Create(CodeTarget_UnsupportedExtension, SourceSpan.Undefined, documentKind, extensionType.Name);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DuplicateDirective =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2001",
                () => Resources.DuplicateDirective,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DuplicateDirective(string directive, SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_DuplicateDirective, location, directive);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_SectionsCannotBeNested =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2002",
                () => LegacyResources.ParseError_Sections_Cannot_Be_Nested,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_SectionsCannotBeNested(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_SectionsCannotBeNested, location, LegacyResources.SectionExample_CS);
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_CodeBlocksNotSupportedInAttributes =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2003",
                () => LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_CodeBlocksNotSupportedInAttributes(SourceSpan location)
        {
            var diagnostic = RazorDiagnostic.Create(TagHelper_CodeBlocksNotSupportedInAttributes, location);
            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InlineMarkupBlocksNotSupportedInAttributes =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2004",
                () => LegacyResources.TagHelpers_InlineMarkupBlocks_NotSupported_InAttributes,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InlineMarkupBlocksNotSupportedInAttributes(string expectedTypeName, SourceSpan location)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InlineMarkupBlocksNotSupportedInAttributes,
                location,
                expectedTypeName);

            return diagnostic;
        }

        #endregion

        #region TagHelper Errors

        // TagHelper Errors ID Offset = 3000

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChildNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3000",
                () => Resources.TagHelper_InvalidRestrictedChildNullOrWhitespace,
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(string tagHelperDisplayName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidRestrictedChildNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChild =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3001",
                () => Resources.TagHelper_InvalidRestrictedChild,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChild(string tagHelperDisplayName, string restrictedChild, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidRestrictedChild,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                restrictedChild,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3002",
                () => Resources.TagHelper_InvalidBoundAttributeNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(string tagHelperDisplayName, string propertyDisplayName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3003",
                () => Resources.TagHelper_InvalidBoundAttributeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeName(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName,
            char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNameStartsWith =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3004",
                () => Resources.TagHelper_InvalidBoundAttributeNameStartsWith,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNameStartsWith(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeNameStartsWith,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                "data-");

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefix =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3005",
                () => Resources.TagHelper_InvalidBoundAttributePrefix,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefix(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName,
            char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributePrefix,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefixStartsWith =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3006",
                () => Resources.TagHelper_InvalidBoundAttributePrefixStartsWith,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefixStartsWith(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributePrefixStartsWith,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                "data-");

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3007",
                () => Resources.TagHelper_InvalidTargetedTagNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedTagNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3008",
                () => Resources.TagHelper_InvalidTargetedTagName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedTagName(string invalidTagName, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedTagName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                invalidTagName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3009",
                () => Resources.TagHelper_InvalidTargetedParentTagNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedParentTagNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3010",
                () => Resources.TagHelper_InvalidTargetedParentTagName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedParentTagName(string invalidTagName, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedParentTagName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                invalidTagName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3009",
                () => Resources.TagHelper_InvalidTargetedAttributeNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedAttributeNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3010",
                () => Resources.TagHelper_InvalidTargetedAttributeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedAttributeName(string invalidAttributeName, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedAttributeName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                invalidAttributeName,
                invalidCharacter);

            return diagnostic;
        }

        #endregion
    }
}
