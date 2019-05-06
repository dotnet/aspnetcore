// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                          new HtmlToken("foo-9309&smlkmb;::-3029022,.sdkq92384", HtmlTokenType.Text));
        }

        [Fact]
        public void Whitespace_Is_Recognized()
        {
            TestTokenizer(" \t\f ",
                          new HtmlToken(" \t\f ", HtmlTokenType.WhiteSpace));
        }

        [Fact]
        public void Newline_Is_Recognized()
        {
            TestTokenizer("\n\r\r\n",
                          new HtmlToken("\n", HtmlTokenType.NewLine),
                          new HtmlToken("\r", HtmlTokenType.NewLine),
                          new HtmlToken("\r\n", HtmlTokenType.NewLine));
        }

        [Fact]
        public void Transition_Is_Not_Recognized_Mid_Text_If_Surrounded_By_Alphanumeric_Characters()
        {
            TestSingleToken("foo@bar", HtmlTokenType.Text);
        }

        [Fact]
        public void OpenAngle_Is_Recognized()
        {
            TestSingleToken("<", HtmlTokenType.OpenAngle);
        }

        [Fact]
        public void Bang_Is_Recognized()
        {
            TestSingleToken("!", HtmlTokenType.Bang);
        }

        [Fact]
        public void Solidus_Is_Recognized()
        {
            TestSingleToken("/", HtmlTokenType.ForwardSlash);
        }

        [Fact]
        public void QuestionMark_Is_Recognized()
        {
            TestSingleToken("?", HtmlTokenType.QuestionMark);
        }

        [Fact]
        public void LeftBracket_Is_Recognized()
        {
            TestSingleToken("[", HtmlTokenType.LeftBracket);
        }

        [Fact]
        public void CloseAngle_Is_Recognized()
        {
            TestSingleToken(">", HtmlTokenType.CloseAngle);
        }

        [Fact]
        public void RightBracket_Is_Recognized()
        {
            TestSingleToken("]", HtmlTokenType.RightBracket);
        }

        [Fact]
        public void Equals_Is_Recognized()
        {
            TestSingleToken("=", HtmlTokenType.Equals);
        }

        [Fact]
        public void DoubleQuote_Is_Recognized()
        {
            TestSingleToken("\"", HtmlTokenType.DoubleQuote);
        }

        [Fact]
        public void SingleQuote_Is_Recognized()
        {
            TestSingleToken("'", HtmlTokenType.SingleQuote);
        }

        [Fact]
        public void Transition_Is_Recognized()
        {
            TestSingleToken("@", HtmlTokenType.Transition);
        }

        [Fact]
        public void DoubleHyphen_Is_Recognized()
        {
            TestSingleToken("--", HtmlTokenType.DoubleHyphen);
        }

        [Fact]
        public void SingleHyphen_Is_Not_Recognized()
        {
            TestSingleToken("-", HtmlTokenType.Text);
        }

        [Fact]
        public void SingleHyphen_Mid_Text_Is_Not_Recognized_As_Separate_Token()
        {
            TestSingleToken("foo-bar", HtmlTokenType.Text);
        }

        [Fact]
        public void Next_Ignores_Star_At_EOF_In_RazorComment()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *",
                new HtmlToken("@", HtmlTokenType.RazorCommentTransition),
                new HtmlToken("*", HtmlTokenType.RazorCommentStar),
                new HtmlToken(" Foo * Bar * Baz *", HtmlTokenType.RazorComment));
        }

        [Fact]
        public void Next_Ignores_Star_Without_Trailing_At()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *@",
                new HtmlToken("@", HtmlTokenType.RazorCommentTransition),
                new HtmlToken("*", HtmlTokenType.RazorCommentStar),
                new HtmlToken(" Foo * Bar * Baz ", HtmlTokenType.RazorComment),
                new HtmlToken("*", HtmlTokenType.RazorCommentStar),
                new HtmlToken("@", HtmlTokenType.RazorCommentTransition));
        }

        [Fact]
        public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
        {
            TestTokenizer(
                "@* Foo Bar Baz *@",
                new HtmlToken("@", HtmlTokenType.RazorCommentTransition),
                new HtmlToken("*", HtmlTokenType.RazorCommentStar),
                new HtmlToken(" Foo Bar Baz ", HtmlTokenType.RazorComment),
                new HtmlToken("*", HtmlTokenType.RazorCommentStar),
                new HtmlToken("@", HtmlTokenType.RazorCommentTransition));
        }
    }
}
