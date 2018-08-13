// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal class NewLineTokenSyntax : SyntaxToken
    {
        internal NewLineTokenSyntax(GreenNode green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        internal new InternalSyntax.NewLineTokenSyntax Green => (InternalSyntax.NewLineTokenSyntax)base.Green;

        public string Value => Text;

        internal override SyntaxToken WithLeadingTriviaCore(SyntaxNode trivia)
        {
            return new InternalSyntax.NewLineTokenSyntax(Text, trivia?.Green, GetTrailingTrivia().Node?.Green).CreateRed(Parent, Position) as NewLineTokenSyntax;
        }

        internal override SyntaxToken WithTrailingTriviaCore(SyntaxNode trivia)
        {
            return new InternalSyntax.NewLineTokenSyntax(Text, GetLeadingTrivia().Node?.Green, trivia?.Green).CreateRed(Parent, Position) as NewLineTokenSyntax;
        }
    }
}
