// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal static class SyntaxFactory
    {
        internal static HtmlTextSyntax HtmlText(SyntaxList<SyntaxToken> textTokens)
        {
            return new HtmlTextSyntax(textTokens.Node);
        }

        internal static HtmlTextTokenSyntax HtmlTextToken(string text, params RazorDiagnostic[] diagnostics)
        {
            return new HtmlTextTokenSyntax(text, diagnostics);
        }

        internal static WhitespaceTokenSyntax WhitespaceToken(string text, params RazorDiagnostic[] diagnostics)
        {
            return new WhitespaceTokenSyntax(text, diagnostics);
        }

        internal static NewLineTokenSyntax NewLineToken(string text, params RazorDiagnostic[] diagnostics)
        {
            return new NewLineTokenSyntax(text, diagnostics);
        }

        internal static PunctuationSyntax Punctuation(SyntaxKind syntaxKind, string text, params RazorDiagnostic[] diagnostics)
        {
            return new PunctuationSyntax(syntaxKind, text, diagnostics);
        }

        internal static UnknownTokenSyntax UnknownToken(string text, params RazorDiagnostic[] diagnostics)
        {
            return new UnknownTokenSyntax(text, diagnostics);
        }
    }
}
