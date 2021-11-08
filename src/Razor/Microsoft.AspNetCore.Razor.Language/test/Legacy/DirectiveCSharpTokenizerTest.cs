// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

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
            SyntaxFactory.Token(SyntaxKind.RazorCommentLiteral, "included"),
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
