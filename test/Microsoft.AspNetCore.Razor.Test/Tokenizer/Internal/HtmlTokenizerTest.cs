// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Test.Tokenizer.Internal;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Internal
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
                          new HtmlSymbol(0, 0, 0, "foo-9309&smlkmb;::-3029022,.sdkq92384", HtmlSymbolType.Text));
        }

        [Fact]
        public void Whitespace_Is_Recognized()
        {
            TestTokenizer(" \t\f ",
                          new HtmlSymbol(0, 0, 0, " \t\f ", HtmlSymbolType.WhiteSpace));
        }

        [Fact]
        public void Newline_Is_Recognized()
        {
            TestTokenizer("\n\r\r\n",
                          new HtmlSymbol(0, 0, 0, "\n", HtmlSymbolType.NewLine),
                          new HtmlSymbol(1, 1, 0, "\r", HtmlSymbolType.NewLine),
                          new HtmlSymbol(2, 2, 0, "\r\n", HtmlSymbolType.NewLine));
        }

        [Fact]
        public void Transition_Is_Not_Recognized_Mid_Text_If_Surrounded_By_Alphanumeric_Characters()
        {
            TestSingleToken("foo@bar", HtmlSymbolType.Text);
        }

        [Fact]
        public void OpenAngle_Is_Recognized()
        {
            TestSingleToken("<", HtmlSymbolType.OpenAngle);
        }

        [Fact]
        public void Bang_Is_Recognized()
        {
            TestSingleToken("!", HtmlSymbolType.Bang);
        }

        [Fact]
        public void Solidus_Is_Recognized()
        {
            TestSingleToken("/", HtmlSymbolType.ForwardSlash);
        }

        [Fact]
        public void QuestionMark_Is_Recognized()
        {
            TestSingleToken("?", HtmlSymbolType.QuestionMark);
        }

        [Fact]
        public void LeftBracket_Is_Recognized()
        {
            TestSingleToken("[", HtmlSymbolType.LeftBracket);
        }

        [Fact]
        public void CloseAngle_Is_Recognized()
        {
            TestSingleToken(">", HtmlSymbolType.CloseAngle);
        }

        [Fact]
        public void RightBracket_Is_Recognized()
        {
            TestSingleToken("]", HtmlSymbolType.RightBracket);
        }

        [Fact]
        public void Equals_Is_Recognized()
        {
            TestSingleToken("=", HtmlSymbolType.Equals);
        }

        [Fact]
        public void DoubleQuote_Is_Recognized()
        {
            TestSingleToken("\"", HtmlSymbolType.DoubleQuote);
        }

        [Fact]
        public void SingleQuote_Is_Recognized()
        {
            TestSingleToken("'", HtmlSymbolType.SingleQuote);
        }

        [Fact]
        public void Transition_Is_Recognized()
        {
            TestSingleToken("@", HtmlSymbolType.Transition);
        }

        [Fact]
        public void DoubleHyphen_Is_Recognized()
        {
            TestSingleToken("--", HtmlSymbolType.DoubleHyphen);
        }

        [Fact]
        public void SingleHyphen_Is_Not_Recognized()
        {
            TestSingleToken("-", HtmlSymbolType.Text);
        }

        [Fact]
        public void SingleHyphen_Mid_Text_Is_Not_Recognized_As_Separate_Token()
        {
            TestSingleToken("foo-bar", HtmlSymbolType.Text);
        }

        [Fact]
        public void Next_Ignores_Star_At_EOF_In_RazorComment()
        {
            TestTokenizer("@* Foo * Bar * Baz *",
                          new HtmlSymbol(0, 0, 0, "@", HtmlSymbolType.RazorCommentTransition),
                          new HtmlSymbol(1, 0, 1, "*", HtmlSymbolType.RazorCommentStar),
                          new HtmlSymbol(2, 0, 2, " Foo * Bar * Baz *", HtmlSymbolType.RazorComment));
        }

        [Fact]
        public void Next_Ignores_Star_Without_Trailing_At()
        {
            TestTokenizer("@* Foo * Bar * Baz *@",
                          new HtmlSymbol(0, 0, 0, "@", HtmlSymbolType.RazorCommentTransition),
                          new HtmlSymbol(1, 0, 1, "*", HtmlSymbolType.RazorCommentStar),
                          new HtmlSymbol(2, 0, 2, " Foo * Bar * Baz ", HtmlSymbolType.RazorComment),
                          new HtmlSymbol(19, 0, 19, "*", HtmlSymbolType.RazorCommentStar),
                          new HtmlSymbol(20, 0, 20, "@", HtmlSymbolType.RazorCommentTransition));
        }

        [Fact]
        public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
        {
            TestTokenizer("@* Foo Bar Baz *@",
                          new HtmlSymbol(0, 0, 0, "@", HtmlSymbolType.RazorCommentTransition),
                          new HtmlSymbol(1, 0, 1, "*", HtmlSymbolType.RazorCommentStar),
                          new HtmlSymbol(2, 0, 2, " Foo Bar Baz ", HtmlSymbolType.RazorComment),
                          new HtmlSymbol(15, 0, 15, "*", HtmlSymbolType.RazorCommentStar),
                          new HtmlSymbol(16, 0, 16, "@", HtmlSymbolType.RazorCommentTransition));
        }
    }
}
