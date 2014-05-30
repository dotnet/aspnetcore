// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class CSharpTokenizerOperatorsTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void LeftBrace_Is_Recognized()
        {
            TestSingleToken("{", CSharpSymbolType.LeftBrace);
        }

        [Fact]
        public void Plus_Is_Recognized()
        {
            TestSingleToken("+", CSharpSymbolType.Plus);
        }

        [Fact]
        public void Assign_Is_Recognized()
        {
            TestSingleToken("=", CSharpSymbolType.Assign);
        }

        [Fact]
        public void Arrow_Is_Recognized()
        {
            TestSingleToken("->", CSharpSymbolType.Arrow);
        }

        [Fact]
        public void AndAssign_Is_Recognized()
        {
            TestSingleToken("&=", CSharpSymbolType.AndAssign);
        }

        [Fact]
        public void RightBrace_Is_Recognized()
        {
            TestSingleToken("}", CSharpSymbolType.RightBrace);
        }

        [Fact]
        public void Minus_Is_Recognized()
        {
            TestSingleToken("-", CSharpSymbolType.Minus);
        }

        [Fact]
        public void LessThan_Is_Recognized()
        {
            TestSingleToken("<", CSharpSymbolType.LessThan);
        }

        [Fact]
        public void Equals_Is_Recognized()
        {
            TestSingleToken("==", CSharpSymbolType.Equals);
        }

        [Fact]
        public void OrAssign_Is_Recognized()
        {
            TestSingleToken("|=", CSharpSymbolType.OrAssign);
        }

        [Fact]
        public void LeftBracket_Is_Recognized()
        {
            TestSingleToken("[", CSharpSymbolType.LeftBracket);
        }

        [Fact]
        public void Star_Is_Recognized()
        {
            TestSingleToken("*", CSharpSymbolType.Star);
        }

        [Fact]
        public void GreaterThan_Is_Recognized()
        {
            TestSingleToken(">", CSharpSymbolType.GreaterThan);
        }

        [Fact]
        public void NotEqual_Is_Recognized()
        {
            TestSingleToken("!=", CSharpSymbolType.NotEqual);
        }

        [Fact]
        public void XorAssign_Is_Recognized()
        {
            TestSingleToken("^=", CSharpSymbolType.XorAssign);
        }

        [Fact]
        public void RightBracket_Is_Recognized()
        {
            TestSingleToken("]", CSharpSymbolType.RightBracket);
        }

        [Fact]
        public void Slash_Is_Recognized()
        {
            TestSingleToken("/", CSharpSymbolType.Slash);
        }

        [Fact]
        public void QuestionMark_Is_Recognized()
        {
            TestSingleToken("?", CSharpSymbolType.QuestionMark);
        }

        [Fact]
        public void LessThanEqual_Is_Recognized()
        {
            TestSingleToken("<=", CSharpSymbolType.LessThanEqual);
        }

        [Fact]
        public void LeftShift_Is_Not_Specially_Recognized()
        {
            TestTokenizer("<<",
                new CSharpSymbol(0, 0, 0, "<", CSharpSymbolType.LessThan),
                new CSharpSymbol(1, 0, 1, "<", CSharpSymbolType.LessThan));
        }

        [Fact]
        public void LeftParen_Is_Recognized()
        {
            TestSingleToken("(", CSharpSymbolType.LeftParenthesis);
        }

        [Fact]
        public void Modulo_Is_Recognized()
        {
            TestSingleToken("%", CSharpSymbolType.Modulo);
        }

        [Fact]
        public void NullCoalesce_Is_Recognized()
        {
            TestSingleToken("??", CSharpSymbolType.NullCoalesce);
        }

        [Fact]
        public void GreaterThanEqual_Is_Recognized()
        {
            TestSingleToken(">=", CSharpSymbolType.GreaterThanEqual);
        }

        [Fact]
        public void EqualGreaterThan_Is_Recognized()
        {
            TestSingleToken("=>", CSharpSymbolType.GreaterThanEqual);
        }

        [Fact]
        public void RightParen_Is_Recognized()
        {
            TestSingleToken(")", CSharpSymbolType.RightParenthesis);
        }

        [Fact]
        public void And_Is_Recognized()
        {
            TestSingleToken("&", CSharpSymbolType.And);
        }

        [Fact]
        public void DoubleColon_Is_Recognized()
        {
            TestSingleToken("::", CSharpSymbolType.DoubleColon);
        }

        [Fact]
        public void PlusAssign_Is_Recognized()
        {
            TestSingleToken("+=", CSharpSymbolType.PlusAssign);
        }

        [Fact]
        public void Semicolon_Is_Recognized()
        {
            TestSingleToken(";", CSharpSymbolType.Semicolon);
        }

        [Fact]
        public void Tilde_Is_Recognized()
        {
            TestSingleToken("~", CSharpSymbolType.Tilde);
        }

        [Fact]
        public void DoubleOr_Is_Recognized()
        {
            TestSingleToken("||", CSharpSymbolType.DoubleOr);
        }

        [Fact]
        public void ModuloAssign_Is_Recognized()
        {
            TestSingleToken("%=", CSharpSymbolType.ModuloAssign);
        }

        [Fact]
        public void Colon_Is_Recognized()
        {
            TestSingleToken(":", CSharpSymbolType.Colon);
        }

        [Fact]
        public void Not_Is_Recognized()
        {
            TestSingleToken("!", CSharpSymbolType.Not);
        }

        [Fact]
        public void DoubleAnd_Is_Recognized()
        {
            TestSingleToken("&&", CSharpSymbolType.DoubleAnd);
        }

        [Fact]
        public void DivideAssign_Is_Recognized()
        {
            TestSingleToken("/=", CSharpSymbolType.DivideAssign);
        }

        [Fact]
        public void Comma_Is_Recognized()
        {
            TestSingleToken(",", CSharpSymbolType.Comma);
        }

        [Fact]
        public void Xor_Is_Recognized()
        {
            TestSingleToken("^", CSharpSymbolType.Xor);
        }

        [Fact]
        public void Decrement_Is_Recognized()
        {
            TestSingleToken("--", CSharpSymbolType.Decrement);
        }

        [Fact]
        public void MultiplyAssign_Is_Recognized()
        {
            TestSingleToken("*=", CSharpSymbolType.MultiplyAssign);
        }

        [Fact]
        public void Dot_Is_Recognized()
        {
            TestSingleToken(".", CSharpSymbolType.Dot);
        }

        [Fact]
        public void Or_Is_Recognized()
        {
            TestSingleToken("|", CSharpSymbolType.Or);
        }

        [Fact]
        public void Increment_Is_Recognized()
        {
            TestSingleToken("++", CSharpSymbolType.Increment);
        }

        [Fact]
        public void MinusAssign_Is_Recognized()
        {
            TestSingleToken("-=", CSharpSymbolType.MinusAssign);
        }

        [Fact]
        public void RightShift_Is_Not_Specially_Recognized()
        {
            TestTokenizer(">>",
                new CSharpSymbol(0, 0, 0, ">", CSharpSymbolType.GreaterThan),
                new CSharpSymbol(1, 0, 1, ">", CSharpSymbolType.GreaterThan));
        }

        [Fact]
        public void Hash_Is_Recognized()
        {
            TestSingleToken("#", CSharpSymbolType.Hash);
        }
    }
}
