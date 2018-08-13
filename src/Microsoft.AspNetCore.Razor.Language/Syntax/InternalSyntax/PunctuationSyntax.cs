// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal class PunctuationSyntax : SyntaxToken
    {
        internal PunctuationSyntax(SyntaxKind kind, string name, RazorDiagnostic[] diagnostics)
            : this(kind, name, null, null, diagnostics, null)
        {
        }

        internal PunctuationSyntax(SyntaxKind kind, string name, GreenNode leadingTrivia, GreenNode trailingTrivia)
            : this(kind, name, leadingTrivia, trailingTrivia, null, null)
        {
        }

        internal PunctuationSyntax(SyntaxKind kind, string name, GreenNode leadingTrivia, GreenNode trailingTrivia, RazorDiagnostic[] diagnostics, SyntaxAnnotation[] annotations)
            : base(kind, name, leadingTrivia, trailingTrivia, diagnostics, annotations)
        {
        }

        internal override SyntaxNode CreateRed(SyntaxNode parent, int position) => new Syntax.PunctuationSyntax(this, parent, position);

        public override SyntaxToken TokenWithLeadingTrivia(GreenNode trivia)
        {
            return new PunctuationSyntax(Kind, Text, trivia, TrailingTrivia);
        }

        public override SyntaxToken TokenWithTrailingTrivia(GreenNode trivia)
        {
            return new PunctuationSyntax(Kind, Text, LeadingTrivia, trivia);
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[] diagnostics)
        {
            return new PunctuationSyntax(Kind, Text, LeadingTrivia, TrailingTrivia, diagnostics, GetAnnotations());
        }

        internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
        {
            return new PunctuationSyntax(Kind, Text, LeadingTrivia, TrailingTrivia, GetDiagnostics(), annotations);
        }
    }
}
