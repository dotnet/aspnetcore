// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class DirectiveHtmlTokenizerTest : HtmlTokenizerTestBase
    {
        [Fact]
        public void Next_ReturnsNull_WhenHtmlIsSeen()
        {
            TestTokenizer(
                "\r\n <div>Ignored</div>",
                SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"),
                SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
                SyntaxFactory.Token(SyntaxKind.OpenAngle, "<"));
        }

        [Fact]
        public void Next_IncludesRazorComments_ReturnsNull_WhenHtmlIsSeen()
        {
            TestTokenizer(
                "\r\n @*included*@ <div>Ignored</div>",
                SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"),
                SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorComment, "included"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.Whitespace, " "),
                SyntaxFactory.Token(SyntaxKind.OpenAngle, "<"));
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new DirectiveHtmlTokenizer(source);
        }
    }
}
