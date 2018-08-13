// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal class HtmlTextTokenSyntax : SyntaxToken
    {
        internal HtmlTextTokenSyntax(GreenNode green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        internal new InternalSyntax.HtmlTextTokenSyntax Green => (InternalSyntax.HtmlTextTokenSyntax)base.Green;

        public string Value => Text;

        internal override SyntaxToken WithLeadingTriviaCore(SyntaxNode trivia)
        {
            return new InternalSyntax.HtmlTextTokenSyntax(Text, trivia?.Green, GetTrailingTrivia().Node?.Green).CreateRed(Parent, Position) as HtmlTextTokenSyntax;
        }

        internal override SyntaxToken WithTrailingTriviaCore(SyntaxNode trivia)
        {
            return new InternalSyntax.HtmlTextTokenSyntax(Text, GetLeadingTrivia().Node?.Green, trivia?.Green).CreateRed(Parent, Position) as HtmlTextTokenSyntax;
        }
    }
}
