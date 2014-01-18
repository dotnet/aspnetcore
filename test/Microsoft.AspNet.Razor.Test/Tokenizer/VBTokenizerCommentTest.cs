// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class VBTokenizerCommentTest : VBTokenizerTestBase
    {
        [Fact]
        public void Next_Ignores_Star_At_EOF_In_RazorComment()
        {
            TestTokenizer("@* Foo * Bar * Baz *",
                          new VBSymbol(0, 0, 0, "@", VBSymbolType.RazorCommentTransition),
                          new VBSymbol(1, 0, 1, "*", VBSymbolType.RazorCommentStar),
                          new VBSymbol(2, 0, 2, " Foo * Bar * Baz *", VBSymbolType.RazorComment));
        }

        [Fact]
        public void Next_Ignores_Star_Without_Trailing_At()
        {
            TestTokenizer("@* Foo * Bar * Baz *@",
                          new VBSymbol(0, 0, 0, "@", VBSymbolType.RazorCommentTransition),
                          new VBSymbol(1, 0, 1, "*", VBSymbolType.RazorCommentStar),
                          new VBSymbol(2, 0, 2, " Foo * Bar * Baz ", VBSymbolType.RazorComment),
                          new VBSymbol(19, 0, 19, "*", VBSymbolType.RazorCommentStar),
                          new VBSymbol(20, 0, 20, "@", VBSymbolType.RazorCommentTransition));
        }

        [Fact]
        public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
        {
            TestTokenizer("@* Foo Bar Baz *@",
                          new VBSymbol(0, 0, 0, "@", VBSymbolType.RazorCommentTransition),
                          new VBSymbol(1, 0, 1, "*", VBSymbolType.RazorCommentStar),
                          new VBSymbol(2, 0, 2, " Foo Bar Baz ", VBSymbolType.RazorComment),
                          new VBSymbol(15, 0, 15, "*", VBSymbolType.RazorCommentStar),
                          new VBSymbol(16, 0, 16, "@", VBSymbolType.RazorCommentTransition));
        }

        [Fact]
        public void Tick_Comment_Is_Recognized()
        {
            TestTokenizer("' Foo Bar Baz", new VBSymbol(0, 0, 0, "' Foo Bar Baz", VBSymbolType.Comment));
        }

        [Fact]
        public void Tick_Comment_Is_Terminated_By_Newline()
        {
            TestTokenizer("' Foo Bar Baz\na", new VBSymbol(0, 0, 0, "' Foo Bar Baz", VBSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void LeftQuote_Comment_Is_Recognized()
        {
            // U+2018 - Left Quote: ‘
            TestTokenizer("‘ Foo Bar Baz", new VBSymbol(0, 0, 0, "‘ Foo Bar Baz", VBSymbolType.Comment));
        }

        [Fact]
        public void LeftQuote_Comment_Is_Terminated_By_Newline()
        {
            // U+2018 - Left Quote: ‘
            TestTokenizer("‘ Foo Bar Baz\na", new VBSymbol(0, 0, 0, "‘ Foo Bar Baz", VBSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void RightQuote_Comment_Is_Recognized()
        {
            // U+2019 - Right Quote: ’
            TestTokenizer("’ Foo Bar Baz", new VBSymbol(0, 0, 0, "’ Foo Bar Baz", VBSymbolType.Comment));
        }

        [Fact]
        public void RightQuote_Comment_Is_Terminated_By_Newline()
        {
            // U+2019 - Right Quote: ’
            TestTokenizer("’ Foo Bar Baz\na", new VBSymbol(0, 0, 0, "’ Foo Bar Baz", VBSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Rem_Comment_Is_Recognized()
        {
            TestTokenizer("REM Foo Bar Baz", new VBSymbol(0, 0, 0, "REM Foo Bar Baz", VBSymbolType.Comment));
        }

        [Fact]
        public void Rem_Comment_Is_Terminated_By_Newline()
        {
            TestTokenizer("REM Foo Bar Baz\na", new VBSymbol(0, 0, 0, "REM Foo Bar Baz", VBSymbolType.Comment), IgnoreRemaining);
        }
    }
}
