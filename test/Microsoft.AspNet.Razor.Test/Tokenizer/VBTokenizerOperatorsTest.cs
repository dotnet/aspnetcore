// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class VBTokenizerOperatorsTest : VBTokenizerTestBase
    {
        [Fact]
        public void Line_Continuation_Character_Is_Recognized()
        {
            TestSingleToken("_", VBSymbolType.LineContinuation);
        }

        [Fact]
        public void LeftParen_Is_Recognized()
        {
            TestSingleToken("(", VBSymbolType.LeftParenthesis);
        }

        [Fact]
        public void RightParen_Is_Recognized()
        {
            TestSingleToken(")", VBSymbolType.RightParenthesis);
        }

        [Fact]
        public void LeftBracket_Is_Recognized()
        {
            TestSingleToken("[", VBSymbolType.LeftBracket);
        }

        [Fact]
        public void RightBracket_Is_Recognized()
        {
            TestSingleToken("]", VBSymbolType.RightBracket);
        }

        [Fact]
        public void LeftBrace_Is_Recognized()
        {
            TestSingleToken("{", VBSymbolType.LeftBrace);
        }

        [Fact]
        public void RightBrace_Is_Recognized()
        {
            TestSingleToken("}", VBSymbolType.RightBrace);
        }

        [Fact]
        public void Bang_Is_Recognized()
        {
            TestSingleToken("!", VBSymbolType.Bang);
        }

        [Fact]
        public void Hash_Is_Recognized()
        {
            TestSingleToken("#", VBSymbolType.Hash);
        }

        [Fact]
        public void Comma_Is_Recognized()
        {
            TestSingleToken(",", VBSymbolType.Comma);
        }

        [Fact]
        public void Dot_Is_Recognized()
        {
            TestSingleToken(".", VBSymbolType.Dot);
        }

        [Fact]
        public void Colon_Is_Recognized()
        {
            TestSingleToken(":", VBSymbolType.Colon);
        }

        [Fact]
        public void QuestionMark_Is_Recognized()
        {
            TestSingleToken("?", VBSymbolType.QuestionMark);
        }

        [Fact]
        public void Concatenation_Is_Recognized()
        {
            TestSingleToken("&", VBSymbolType.Concatenation);
        }

        [Fact]
        public void Multiply_Is_Recognized()
        {
            TestSingleToken("*", VBSymbolType.Multiply);
        }

        [Fact]
        public void Add_Is_Recognized()
        {
            TestSingleToken("+", VBSymbolType.Add);
        }

        [Fact]
        public void Subtract_Is_Recognized()
        {
            TestSingleToken("-", VBSymbolType.Subtract);
        }

        [Fact]
        public void Divide_Is_Recognized()
        {
            TestSingleToken("/", VBSymbolType.Divide);
        }

        [Fact]
        public void IntegerDivide_Is_Recognized()
        {
            TestSingleToken("\\", VBSymbolType.IntegerDivide);
        }

        [Fact]
        public void Exponentiation_Is_Recognized()
        {
            TestSingleToken("^", VBSymbolType.Exponentiation);
        }

        [Fact]
        public void Equal_Is_Recognized()
        {
            TestSingleToken("=", VBSymbolType.Equal);
        }

        [Fact]
        public void LessThan_Is_Recognized()
        {
            TestSingleToken("<", VBSymbolType.LessThan);
        }

        [Fact]
        public void GreaterThan_Is_Recognized()
        {
            TestSingleToken(">", VBSymbolType.GreaterThan);
        }

        [Fact]
        public void Dollar_Is_Recognized()
        {
            TestSingleToken("$", VBSymbolType.Dollar);
        }
    }
}
