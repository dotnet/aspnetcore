// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class DirectiveCSharpTokenizerTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void Next_ReturnsNull_AfterTokenizingFirstDirective()
        {
            TestTokenizer(
                "\r\n @something \r\n @this is ignored",
                SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"),
                SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
                SyntaxFactory.Token(SyntaxKind.Transition, "@"),
                SyntaxFactory.Token(SyntaxKind.Identifier, "something"),
                SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
                SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"));
        }

        [Fact]
        public void Next_IncludesComments_ReturnsNull_AfterTokenizingFirstDirective()
        {
            TestTokenizer(
                "@*included*@\r\n @something   \"value\"\r\n @this is ignored",
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorComment, "included"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"),
                SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
                SyntaxFactory.Token(SyntaxKind.Transition, "@"),
                SyntaxFactory.Token(SyntaxKind.Identifier, "something"),
                SyntaxFactory.Token(SyntaxKind.Whitespace, "   "),
                SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"value\""),
                SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"));
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new DirectiveCSharpTokenizer(source);
        }
    }
}
