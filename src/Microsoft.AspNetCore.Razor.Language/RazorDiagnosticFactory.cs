// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal static class RazorDiagnosticFactory
    {
        private const string DiagnosticPrefix = "RZ";

        #region General Errors

        /*
         * General Errors ID Offset = 0
         */

        #endregion

        #region Language Errors

        /*
         * Language Errors ID Offset = 1000
         */

        #endregion

        #region Semantic Errors

        /*
         * Semantic Errors ID Offset = 2000
         */

        #endregion

        #region TagHelper Errors

        /*
         * TagHelper Errors ID Offset = 3000
         */

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChildNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3000",
                () => Resources.InvalidRestrictedChildNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(string tagHelperType)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidRestrictedChildNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperType);

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChild =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3001",
                () => Resources.InvalidRestrictedChild,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChild(string restrictedChild, string tagHelperType, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidRestrictedChild,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                restrictedChild,
                tagHelperType,
                invalidCharacter);

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3002",
                () => Resources.InvalidBoundAttributeNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(string containingTypeName, string propertyName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                propertyName);

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3003",
                () => Resources.InvalidBoundAttributeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeName(
            string containingTypeName, 
            string propertyName,
            string invalidName,
            char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                propertyName,
                invalidName,
                invalidCharacter);

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNameStartsWith =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3004",
                () => Resources.InvalidBoundAttributeNameStartsWith,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNameStartsWith(
            string containingTypeName,
            string propertyName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeNameStartsWith,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                propertyName,
                invalidName,
                "data-");

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefix =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3005",
                () => Resources.InvalidBoundAttributePrefix,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefix(
            string containingTypeName,
            string propertyName,
            string invalidName,
            char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributePrefix,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                propertyName,
                invalidName,
                invalidCharacter);

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefixStartsWith =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3006",
                () => Resources.InvalidBoundAttributePrefixStartsWith,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefixStartsWith(
            string containingTypeName,
            string propertyName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributePrefixStartsWith,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                containingTypeName,
                propertyName,
                invalidName,
                "data-");

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3007",
                () => Resources.InvalidTargetedTagNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedTagNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3008",
                () => Resources.InvalidTargetedTagName,
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

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3009",
                () => Resources.InvalidTargetedParentTagNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedParentTagNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3010",
                () => Resources.InvalidTargetedParentTagName,
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

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3009",
                () => Resources.InvalidTargetedAttributeNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedAttributeNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        private static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3010",
                () => Resources.InvalidTargetedAttributeName,
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
