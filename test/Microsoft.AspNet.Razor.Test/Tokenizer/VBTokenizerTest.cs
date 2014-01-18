// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class VBTokenizerTest : VBTokenizerTestBase
    {
        [Fact]
        public void Constructor_Throws_ArgNull_If_Null_Source_Provided()
        {
            Assert.ThrowsArgumentNull(() => new CSharpTokenizer(null), "source");
        }

        [Fact]
        public void Next_Returns_Null_When_EOF_Reached()
        {
            TestTokenizer("");
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_CR()
        {
            TestTokenizer("\r\ra",
                          new VBSymbol(0, 0, 0, "\r", VBSymbolType.NewLine),
                          new VBSymbol(1, 1, 0, "\r", VBSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_LF()
        {
            TestTokenizer("\n\na",
                          new VBSymbol(0, 0, 0, "\n", VBSymbolType.NewLine),
                          new VBSymbol(1, 1, 0, "\n", VBSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_NEL()
        {
            // NEL: Unicode "Next Line" U+0085
            TestTokenizer("\u0085\u0085a",
                          new VBSymbol(0, 0, 0, "\u0085", VBSymbolType.NewLine),
                          new VBSymbol(1, 1, 0, "\u0085", VBSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_Line_Separator()
        {
            // Unicode "Line Separator" U+2028
            TestTokenizer("\u2028\u2028a",
                          new VBSymbol(0, 0, 0, "\u2028", VBSymbolType.NewLine),
                          new VBSymbol(1, 1, 0, "\u2028", VBSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_Paragraph_Separator()
        {
            // Unicode "Paragraph Separator" U+2029
            TestTokenizer("\u2029\u2029a",
                          new VBSymbol(0, 0, 0, "\u2029", VBSymbolType.NewLine),
                          new VBSymbol(1, 1, 0, "\u2029", VBSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Single_Newline_Token_For_CRLF()
        {
            TestTokenizer("\r\n\r\na",
                          new VBSymbol(0, 0, 0, "\r\n", VBSymbolType.NewLine),
                          new VBSymbol(2, 1, 0, "\r\n", VBSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Token_For_Whitespace_Characters()
        {
            TestTokenizer(" \f\t\u000B \n ",
                          new VBSymbol(0, 0, 0, " \f\t\u000B ", VBSymbolType.WhiteSpace),
                          new VBSymbol(5, 0, 5, "\n", VBSymbolType.NewLine),
                          new VBSymbol(6, 1, 0, " ", VBSymbolType.WhiteSpace));
        }

        [Fact]
        public void Transition_Is_Recognized()
        {
            TestSingleToken("@", VBSymbolType.Transition);
        }
    }
}
