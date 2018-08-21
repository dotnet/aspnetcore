// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlTokenizerTest : HtmlTokenizerTestBase
    {
        [Fact]
        public void Next_Returns_Null_When_EOF_Reached()
        {
            TestTokenizer("");
        }

        [Fact]
        public void Text_Is_Recognized()
        {
            TestTokenizer("foo-9309&smlkmb;::-3029022,.sdkq92384",
                          SyntaxFactory.Token(SyntaxKind.HtmlTextLiteral, "foo-9309&smlkmb;::-3029022,.sdkq92384"));
        }

        [Fact]
        public void Whitespace_Is_Recognized()
        {
            TestTokenizer(" \t\f ",
                          SyntaxFactory.Token(SyntaxKind.Whitespace, " \t\f "));
        }

        [Fact]
        public void Newline_Is_Recognized()
        {
            TestTokenizer("\n\r\r\n",
                          SyntaxFactory.Token(SyntaxKind.NewLine, "\n"),
                          SyntaxFactory.Token(SyntaxKind.NewLine, "\r"),
                          SyntaxFactory.Token(SyntaxKind.NewLine, "\r\n"));
        }

        [Fact]
        public void Transition_Is_Not_Recognized_Mid_Text_If_Surrounded_By_Alphanumeric_Characters()
        {
            TestSingleToken("foo@bar", SyntaxKind.HtmlTextLiteral);
        }

        [Fact]
        public void OpenAngle_Is_Recognized()
        {
            TestSingleToken("<", SyntaxKind.OpenAngle);
        }

        [Fact]
        public void Bang_Is_Recognized()
        {
            TestSingleToken("!", SyntaxKind.Bang);
        }

        [Fact]
        public void Solidus_Is_Recognized()
        {
            TestSingleToken("/", SyntaxKind.ForwardSlash);
        }

        [Fact]
        public void QuestionMark_Is_Recognized()
        {
            TestSingleToken("?", SyntaxKind.QuestionMark);
        }

        [Fact]
        public void LeftBracket_Is_Recognized()
        {
            TestSingleToken("[", SyntaxKind.LeftBracket);
        }

        [Fact]
        public void CloseAngle_Is_Recognized()
        {
            TestSingleToken(">", SyntaxKind.CloseAngle);
        }

        [Fact]
        public void RightBracket_Is_Recognized()
        {
            TestSingleToken("]", SyntaxKind.RightBracket);
        }

        [Fact]
        public void Equals_Is_Recognized()
        {
            TestSingleToken("=", SyntaxKind.Equals);
        }

        [Fact]
        public void DoubleQuote_Is_Recognized()
        {
            TestSingleToken("\"", SyntaxKind.DoubleQuote);
        }

        [Fact]
        public void SingleQuote_Is_Recognized()
        {
            TestSingleToken("'", SyntaxKind.SingleQuote);
        }

        [Fact]
        public void Transition_Is_Recognized()
        {
            TestSingleToken("@", SyntaxKind.Transition);
        }

        [Fact]
        public void DoubleHyphen_Is_Recognized()
        {
            TestSingleToken("--", SyntaxKind.DoubleHyphen);
        }

        [Fact]
        public void SingleHyphen_Is_Not_Recognized()
        {
            TestSingleToken("-", SyntaxKind.HtmlTextLiteral);
        }

        [Fact]
        public void SingleHyphen_Mid_Text_Is_Not_Recognized_As_Separate_Token()
        {
            TestSingleToken("foo-bar", SyntaxKind.HtmlTextLiteral);
        }

        [Fact]
        public void Next_Ignores_Star_At_EOF_In_RazorComment()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *",
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentLiteral, " Foo * Bar * Baz *"));
        }

        [Fact]
        public void Next_Ignores_Star_Without_Trailing_At()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *@",
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentLiteral, " Foo * Bar * Baz "),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"));
        }

        [Fact]
        public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
        {
            TestTokenizer(
                "@* Foo Bar Baz *@",
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentLiteral, " Foo Bar Baz "),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"));
        }
    }
}
