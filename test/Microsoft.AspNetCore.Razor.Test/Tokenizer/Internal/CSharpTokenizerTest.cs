// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Test.Tokenizer.Internal;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Internal
{
    public class CSharpTokenizerTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void Next_Returns_Null_When_EOF_Reached()
        {
            TestTokenizer("");
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_CR()
        {
            TestTokenizer("\r\ra",
                          new CSharpSymbol(0, 0, 0, "\r", CSharpSymbolType.NewLine),
                          new CSharpSymbol(1, 1, 0, "\r", CSharpSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_LF()
        {
            TestTokenizer("\n\na",
                          new CSharpSymbol(0, 0, 0, "\n", CSharpSymbolType.NewLine),
                          new CSharpSymbol(1, 1, 0, "\n", CSharpSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_NEL()
        {
            // NEL: Unicode "Next Line" U+0085
            TestTokenizer("\u0085\u0085a",
                          new CSharpSymbol(0, 0, 0, "\u0085", CSharpSymbolType.NewLine),
                          new CSharpSymbol(1, 1, 0, "\u0085", CSharpSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_Line_Separator()
        {
            // Unicode "Line Separator" U+2028
            TestTokenizer("\u2028\u2028a",
                          new CSharpSymbol(0, 0, 0, "\u2028", CSharpSymbolType.NewLine),
                          new CSharpSymbol(1, 1, 0, "\u2028", CSharpSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_Paragraph_Separator()
        {
            // Unicode "Paragraph Separator" U+2029
            TestTokenizer("\u2029\u2029a",
                          new CSharpSymbol(0, 0, 0, "\u2029", CSharpSymbolType.NewLine),
                          new CSharpSymbol(1, 1, 0, "\u2029", CSharpSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Single_Newline_Token_For_CRLF()
        {
            TestTokenizer("\r\n\r\na",
                          new CSharpSymbol(0, 0, 0, "\r\n", CSharpSymbolType.NewLine),
                          new CSharpSymbol(2, 1, 0, "\r\n", CSharpSymbolType.NewLine),
                          IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Token_For_Whitespace_Characters()
        {
            TestTokenizer(" \f\t\u000B \n ",
                          new CSharpSymbol(0, 0, 0, " \f\t\u000B ", CSharpSymbolType.WhiteSpace),
                          new CSharpSymbol(5, 0, 5, "\n", CSharpSymbolType.NewLine),
                          new CSharpSymbol(6, 1, 0, " ", CSharpSymbolType.WhiteSpace));
        }

        [Fact]
        public void Transition_Is_Recognized()
        {
            TestSingleToken("@", CSharpSymbolType.Transition);
        }

        [Fact]
        public void Transition_Is_Recognized_As_SingleCharacter()
        {
            TestTokenizer("@(",
                          new CSharpSymbol(0, 0, 0, "@", CSharpSymbolType.Transition),
                          new CSharpSymbol(1, 0, 1, "(", CSharpSymbolType.LeftParenthesis));
        }
    }
}
