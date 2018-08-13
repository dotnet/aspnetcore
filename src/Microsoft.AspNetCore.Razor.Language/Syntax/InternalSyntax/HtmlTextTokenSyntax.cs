// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal class HtmlTextTokenSyntax : SyntaxToken
    {
        internal HtmlTextTokenSyntax(string text, params RazorDiagnostic[] diagnostics)
            : base(SyntaxKind.HtmlTextLiteralToken, text, null, null, diagnostics, null)
        {
        }

        internal HtmlTextTokenSyntax(string text, GreenNode leadingTrivia, GreenNode trailingTrivia)
            : base(SyntaxKind.HtmlTextLiteralToken, text, leadingTrivia, trailingTrivia)
        {
        }

        protected HtmlTextTokenSyntax(SyntaxKind kind, string name, GreenNode leadingTrivia, GreenNode trailingTrivia)
            : base(kind, name, leadingTrivia, trailingTrivia)
        {
        }

        protected HtmlTextTokenSyntax(SyntaxKind kind, string name, GreenNode leadingTrivia, GreenNode trailingTrivia, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
            : base(kind, name, leadingTrivia, trailingTrivia, diagnostics, annotations)
        {
        }

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
        {
            return new Syntax.HtmlTextTokenSyntax(this, parent, position);
        }

        public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
        {
            return new HtmlTextTokenSyntax(Kind, Text, trivia, TrailingTrivia);
        }

        public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
        {
            return new HtmlTextTokenSyntax(Kind, Text, LeadingTrivia, trivia);
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] diagnostics)
        {
            return new HtmlTextTokenSyntax(Kind, Text, LeadingTrivia, TrailingTrivia, diagnostics, GetAnnotations());
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new HtmlTextTokenSyntax(Kind, Text, LeadingTrivia, TrailingTrivia, GetDiagnostics(), annotations);
        }
    }
}
