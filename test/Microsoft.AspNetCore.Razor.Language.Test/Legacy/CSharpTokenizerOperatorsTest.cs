// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerOperatorsTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void LeftBrace_Is_Recognized()
        {
            TestSingleToken("{", CSharpTokenType.LeftBrace);
        }

        [Fact]
        public void Plus_Is_Recognized()
        {
            TestSingleToken("+", CSharpTokenType.Plus);
        }

        [Fact]
        public void Assign_Is_Recognized()
        {
            TestSingleToken("=", CSharpTokenType.Assign);
        }

        [Fact]
        public void Arrow_Is_Recognized()
        {
            TestSingleToken("->", CSharpTokenType.Arrow);
        }

        [Fact]
        public void AndAssign_Is_Recognized()
        {
            TestSingleToken("&=", CSharpTokenType.AndAssign);
        }

        [Fact]
        public void RightBrace_Is_Recognized()
        {
            TestSingleToken("}", CSharpTokenType.RightBrace);
        }

        [Fact]
        public void Minus_Is_Recognized()
        {
            TestSingleToken("-", CSharpTokenType.Minus);
        }

        [Fact]
        public void LessThan_Is_Recognized()
        {
            TestSingleToken("<", CSharpTokenType.LessThan);
        }

        [Fact]
        public void Equals_Is_Recognized()
        {
            TestSingleToken("==", CSharpTokenType.Equals);
        }

        [Fact]
        public void OrAssign_Is_Recognized()
        {
            TestSingleToken("|=", CSharpTokenType.OrAssign);
        }

        [Fact]
        public void LeftBracket_Is_Recognized()
        {
            TestSingleToken("[", CSharpTokenType.LeftBracket);
        }

        [Fact]
        public void Star_Is_Recognized()
        {
            TestSingleToken("*", CSharpTokenType.Star);
        }

        [Fact]
        public void GreaterThan_Is_Recognized()
        {
            TestSingleToken(">", CSharpTokenType.GreaterThan);
        }

        [Fact]
        public void NotEqual_Is_Recognized()
        {
            TestSingleToken("!=", CSharpTokenType.NotEqual);
        }

        [Fact]
        public void XorAssign_Is_Recognized()
        {
            TestSingleToken("^=", CSharpTokenType.XorAssign);
        }

        [Fact]
        public void RightBracket_Is_Recognized()
        {
            TestSingleToken("]", CSharpTokenType.RightBracket);
        }

        [Fact]
        public void Slash_Is_Recognized()
        {
            TestSingleToken("/", CSharpTokenType.Slash);
        }

        [Fact]
        public void QuestionMark_Is_Recognized()
        {
            TestSingleToken("?", CSharpTokenType.QuestionMark);
        }

        [Fact]
        public void LessThanEqual_Is_Recognized()
        {
            TestSingleToken("<=", CSharpTokenType.LessThanEqual);
        }

        [Fact]
        public void LeftShift_Is_Not_Specially_Recognized()
        {
            TestTokenizer("<<",
                new CSharpToken("<", CSharpTokenType.LessThan),
                new CSharpToken("<", CSharpTokenType.LessThan));
        }

        [Fact]
        public void LeftParen_Is_Recognized()
        {
            TestSingleToken("(", CSharpTokenType.LeftParenthesis);
        }

        [Fact]
        public void Modulo_Is_Recognized()
        {
            TestSingleToken("%", CSharpTokenType.Modulo);
        }

        [Fact]
        public void NullCoalesce_Is_Recognized()
        {
            TestSingleToken("??", CSharpTokenType.NullCoalesce);
        }

        [Fact]
        public void GreaterThanEqual_Is_Recognized()
        {
            TestSingleToken(">=", CSharpTokenType.GreaterThanEqual);
        }

        [Fact]
        public void EqualGreaterThan_Is_Recognized()
        {
            TestSingleToken("=>", CSharpTokenType.GreaterThanEqual);
        }

        [Fact]
        public void RightParen_Is_Recognized()
        {
            TestSingleToken(")", CSharpTokenType.RightParenthesis);
        }

        [Fact]
        public void And_Is_Recognized()
        {
            TestSingleToken("&", CSharpTokenType.And);
        }

        [Fact]
        public void DoubleColon_Is_Recognized()
        {
            TestSingleToken("::", CSharpTokenType.DoubleColon);
        }

        [Fact]
        public void PlusAssign_Is_Recognized()
        {
            TestSingleToken("+=", CSharpTokenType.PlusAssign);
        }

        [Fact]
        public void Semicolon_Is_Recognized()
        {
            TestSingleToken(";", CSharpTokenType.Semicolon);
        }

        [Fact]
        public void Tilde_Is_Recognized()
        {
            TestSingleToken("~", CSharpTokenType.Tilde);
        }

        [Fact]
        public void DoubleOr_Is_Recognized()
        {
            TestSingleToken("||", CSharpTokenType.DoubleOr);
        }

        [Fact]
        public void ModuloAssign_Is_Recognized()
        {
            TestSingleToken("%=", CSharpTokenType.ModuloAssign);
        }

        [Fact]
        public void Colon_Is_Recognized()
        {
            TestSingleToken(":", CSharpTokenType.Colon);
        }

        [Fact]
        public void Not_Is_Recognized()
        {
            TestSingleToken("!", CSharpTokenType.Not);
        }

        [Fact]
        public void DoubleAnd_Is_Recognized()
        {
            TestSingleToken("&&", CSharpTokenType.DoubleAnd);
        }

        [Fact]
        public void DivideAssign_Is_Recognized()
        {
            TestSingleToken("/=", CSharpTokenType.DivideAssign);
        }

        [Fact]
        public void Comma_Is_Recognized()
        {
            TestSingleToken(",", CSharpTokenType.Comma);
        }

        [Fact]
        public void Xor_Is_Recognized()
        {
            TestSingleToken("^", CSharpTokenType.Xor);
        }

        [Fact]
        public void Decrement_Is_Recognized()
        {
            TestSingleToken("--", CSharpTokenType.Decrement);
        }

        [Fact]
        public void MultiplyAssign_Is_Recognized()
        {
            TestSingleToken("*=", CSharpTokenType.MultiplyAssign);
        }

        [Fact]
        public void Dot_Is_Recognized()
        {
            TestSingleToken(".", CSharpTokenType.Dot);
        }

        [Fact]
        public void Or_Is_Recognized()
        {
            TestSingleToken("|", CSharpTokenType.Or);
        }

        [Fact]
        public void Increment_Is_Recognized()
        {
            TestSingleToken("++", CSharpTokenType.Increment);
        }

        [Fact]
        public void MinusAssign_Is_Recognized()
        {
            TestSingleToken("-=", CSharpTokenType.MinusAssign);
        }

        [Fact]
        public void RightShift_Is_Not_Specially_Recognized()
        {
            TestTokenizer(">>",
                new CSharpToken(">", CSharpTokenType.GreaterThan),
                new CSharpToken(">", CSharpTokenType.GreaterThan));
        }

        [Fact]
        public void Hash_Is_Recognized()
        {
            TestSingleToken("#", CSharpTokenType.Hash);
        }
    }
}
