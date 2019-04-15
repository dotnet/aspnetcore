// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerOperatorsTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void LeftBrace_Is_Recognized()
        {
            TestSingleToken("{", SyntaxKind.LeftBrace);
        }

        [Fact]
        public void Plus_Is_Recognized()
        {
            TestSingleToken("+", SyntaxKind.Plus);
        }

        [Fact]
        public void Assign_Is_Recognized()
        {
            TestSingleToken("=", SyntaxKind.Assign);
        }

        [Fact]
        public void Arrow_Is_Recognized()
        {
            TestSingleToken("->", SyntaxKind.Arrow);
        }

        [Fact]
        public void AndAssign_Is_Recognized()
        {
            TestSingleToken("&=", SyntaxKind.AndAssign);
        }

        [Fact]
        public void RightBrace_Is_Recognized()
        {
            TestSingleToken("}", SyntaxKind.RightBrace);
        }

        [Fact]
        public void Minus_Is_Recognized()
        {
            TestSingleToken("-", SyntaxKind.Minus);
        }

        [Fact]
        public void LessThan_Is_Recognized()
        {
            TestSingleToken("<", SyntaxKind.LessThan);
        }

        [Fact]
        public void Equals_Is_Recognized()
        {
            TestSingleToken("==", SyntaxKind.Equals);
        }

        [Fact]
        public void OrAssign_Is_Recognized()
        {
            TestSingleToken("|=", SyntaxKind.OrAssign);
        }

        [Fact]
        public void LeftBracket_Is_Recognized()
        {
            TestSingleToken("[", SyntaxKind.LeftBracket);
        }

        [Fact]
        public void Star_Is_Recognized()
        {
            TestSingleToken("*", SyntaxKind.Star);
        }

        [Fact]
        public void GreaterThan_Is_Recognized()
        {
            TestSingleToken(">", SyntaxKind.GreaterThan);
        }

        [Fact]
        public void NotEqual_Is_Recognized()
        {
            TestSingleToken("!=", SyntaxKind.NotEqual);
        }

        [Fact]
        public void XorAssign_Is_Recognized()
        {
            TestSingleToken("^=", SyntaxKind.XorAssign);
        }

        [Fact]
        public void RightBracket_Is_Recognized()
        {
            TestSingleToken("]", SyntaxKind.RightBracket);
        }

        [Fact]
        public void Slash_Is_Recognized()
        {
            TestSingleToken("/", SyntaxKind.Slash);
        }

        [Fact]
        public void QuestionMark_Is_Recognized()
        {
            TestSingleToken("?", SyntaxKind.QuestionMark);
        }

        [Fact]
        public void LessThanEqual_Is_Recognized()
        {
            TestSingleToken("<=", SyntaxKind.LessThanEqual);
        }

        [Fact]
        public void LeftShift_Is_Not_Specially_Recognized()
        {
            TestTokenizer("<<",
                SyntaxFactory.Token(SyntaxKind.LessThan, "<"),
                SyntaxFactory.Token(SyntaxKind.LessThan, "<"));
        }

        [Fact]
        public void LeftParen_Is_Recognized()
        {
            TestSingleToken("(", SyntaxKind.LeftParenthesis);
        }

        [Fact]
        public void Modulo_Is_Recognized()
        {
            TestSingleToken("%", SyntaxKind.Modulo);
        }

        [Fact]
        public void NullCoalesce_Is_Recognized()
        {
            TestSingleToken("??", SyntaxKind.NullCoalesce);
        }

        [Fact]
        public void GreaterThanEqual_Is_Recognized()
        {
            TestSingleToken(">=", SyntaxKind.GreaterThanEqual);
        }

        [Fact]
        public void EqualGreaterThan_Is_Recognized()
        {
            TestSingleToken("=>", SyntaxKind.GreaterThanEqual);
        }

        [Fact]
        public void RightParen_Is_Recognized()
        {
            TestSingleToken(")", SyntaxKind.RightParenthesis);
        }

        [Fact]
        public void And_Is_Recognized()
        {
            TestSingleToken("&", SyntaxKind.And);
        }

        [Fact]
        public void DoubleColon_Is_Recognized()
        {
            TestSingleToken("::", SyntaxKind.DoubleColon);
        }

        [Fact]
        public void PlusAssign_Is_Recognized()
        {
            TestSingleToken("+=", SyntaxKind.PlusAssign);
        }

        [Fact]
        public void Semicolon_Is_Recognized()
        {
            TestSingleToken(";", SyntaxKind.Semicolon);
        }

        [Fact]
        public void Tilde_Is_Recognized()
        {
            TestSingleToken("~", SyntaxKind.Tilde);
        }

        [Fact]
        public void DoubleOr_Is_Recognized()
        {
            TestSingleToken("||", SyntaxKind.DoubleOr);
        }

        [Fact]
        public void ModuloAssign_Is_Recognized()
        {
            TestSingleToken("%=", SyntaxKind.ModuloAssign);
        }

        [Fact]
        public void Colon_Is_Recognized()
        {
            TestSingleToken(":", SyntaxKind.Colon);
        }

        [Fact]
        public void Not_Is_Recognized()
        {
            TestSingleToken("!", SyntaxKind.Not);
        }

        [Fact]
        public void DoubleAnd_Is_Recognized()
        {
            TestSingleToken("&&", SyntaxKind.DoubleAnd);
        }

        [Fact]
        public void DivideAssign_Is_Recognized()
        {
            TestSingleToken("/=", SyntaxKind.DivideAssign);
        }

        [Fact]
        public void Comma_Is_Recognized()
        {
            TestSingleToken(",", SyntaxKind.Comma);
        }

        [Fact]
        public void Xor_Is_Recognized()
        {
            TestSingleToken("^", SyntaxKind.Xor);
        }

        [Fact]
        public void Decrement_Is_Recognized()
        {
            TestSingleToken("--", SyntaxKind.Decrement);
        }

        [Fact]
        public void MultiplyAssign_Is_Recognized()
        {
            TestSingleToken("*=", SyntaxKind.MultiplyAssign);
        }

        [Fact]
        public void Dot_Is_Recognized()
        {
            TestSingleToken(".", SyntaxKind.Dot);
        }

        [Fact]
        public void Or_Is_Recognized()
        {
            TestSingleToken("|", SyntaxKind.Or);
        }

        [Fact]
        public void Increment_Is_Recognized()
        {
            TestSingleToken("++", SyntaxKind.Increment);
        }

        [Fact]
        public void MinusAssign_Is_Recognized()
        {
            TestSingleToken("-=", SyntaxKind.MinusAssign);
        }

        [Fact]
        public void RightShift_Is_Not_Specially_Recognized()
        {
            TestTokenizer(">>",
                SyntaxFactory.Token(SyntaxKind.GreaterThan, ">"),
                SyntaxFactory.Token(SyntaxKind.GreaterThan, ">"));
        }

        [Fact]
        public void Hash_Is_Recognized()
        {
            TestSingleToken("#", SyntaxKind.Hash);
        }
    }
}
