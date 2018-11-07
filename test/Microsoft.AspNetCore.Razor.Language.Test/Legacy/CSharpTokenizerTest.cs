// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerTest : CSharpTokenizerTestBase
    {
        private new CSharpToken IgnoreRemaining => (CSharpToken)base.IgnoreRemaining;

        [Fact]
        public void Next_Returns_Null_When_EOF_Reached()
        {
            TestTokenizer("");
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_CR()
        {
            TestTokenizer(
                "\r\ra",
                new CSharpToken("\r", CSharpTokenType.NewLine),
                new CSharpToken("\r", CSharpTokenType.NewLine),
                IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_LF()
        {
            TestTokenizer(
                "\n\na",
                new CSharpToken("\n", CSharpTokenType.NewLine),
                new CSharpToken("\n", CSharpTokenType.NewLine),
                IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_NEL()
        {
            // NEL: Unicode "Next Line" U+0085
            TestTokenizer(
                "\u0085\u0085a",
                new CSharpToken("\u0085", CSharpTokenType.NewLine),
                new CSharpToken("\u0085", CSharpTokenType.NewLine),
                IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_Line_Separator()
        {
            // Unicode "Line Separator" U+2028
            TestTokenizer(
                "\u2028\u2028a",
                new CSharpToken("\u2028", CSharpTokenType.NewLine),
                new CSharpToken("\u2028", CSharpTokenType.NewLine),
                IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Newline_Token_For_Single_Paragraph_Separator()
        {
            // Unicode "Paragraph Separator" U+2029
            TestTokenizer(
                "\u2029\u2029a",
                new CSharpToken("\u2029", CSharpTokenType.NewLine),
                new CSharpToken("\u2029", CSharpTokenType.NewLine),
                IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Single_Newline_Token_For_CRLF()
        {
            TestTokenizer(
                "\r\n\r\na",
                new CSharpToken("\r\n", CSharpTokenType.NewLine),
                new CSharpToken("\r\n", CSharpTokenType.NewLine),
                IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Token_For_Whitespace_Characters()
        {
            TestTokenizer(
                " \f\t\u000B \n ",
                new CSharpToken(" \f\t\u000B ", CSharpTokenType.WhiteSpace),
                new CSharpToken("\n", CSharpTokenType.NewLine),
                new CSharpToken(" ", CSharpTokenType.WhiteSpace));
        }

        [Fact]
        public void Transition_Is_Recognized()
        {
            TestSingleToken("@", CSharpTokenType.Transition);
        }

        [Fact]
        public void Transition_Is_Recognized_As_SingleCharacter()
        {
            TestTokenizer(
                "@(",
                new CSharpToken("@", CSharpTokenType.Transition),
                new CSharpToken("(", CSharpTokenType.LeftParenthesis));
        }
    }
}
