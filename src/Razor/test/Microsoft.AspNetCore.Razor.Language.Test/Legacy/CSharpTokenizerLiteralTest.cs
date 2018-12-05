// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerLiteralTest : CSharpTokenizerTestBase
    {
        private new SyntaxToken IgnoreRemaining => (SyntaxToken)base.IgnoreRemaining;

        [Fact]
        public void Simple_Integer_Literal_Is_Recognized()
        {
            TestSingleToken("01189998819991197253", SyntaxKind.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("42U", SyntaxKind.IntegerLiteral);
            TestSingleToken("42u", SyntaxKind.IntegerLiteral);

            TestSingleToken("42L", SyntaxKind.IntegerLiteral);
            TestSingleToken("42l", SyntaxKind.IntegerLiteral);

            TestSingleToken("42UL", SyntaxKind.IntegerLiteral);
            TestSingleToken("42Ul", SyntaxKind.IntegerLiteral);

            TestSingleToken("42uL", SyntaxKind.IntegerLiteral);
            TestSingleToken("42ul", SyntaxKind.IntegerLiteral);

            TestSingleToken("42LU", SyntaxKind.IntegerLiteral);
            TestSingleToken("42Lu", SyntaxKind.IntegerLiteral);

            TestSingleToken("42lU", SyntaxKind.IntegerLiteral);
            TestSingleToken("42lu", SyntaxKind.IntegerLiteral);
        }

        [Fact]
        public void Trailing_Letter_Is_Not_Part_Of_Integer_Literal_If_Not_Type_Sufix()
        {
            TestTokenizer("42a", SyntaxFactory.Token(SyntaxKind.IntegerLiteral, "42"), IgnoreRemaining);
        }

        [Fact]
        public void Simple_Hex_Literal_Is_Recognized()
        {
            TestSingleToken("0x0123456789ABCDEF", SyntaxKind.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffix_Is_Recognized_In_Hex_Literal()
        {
            TestSingleToken("0xDEADBEEFU", SyntaxKind.IntegerLiteral);
            TestSingleToken("0xDEADBEEFu", SyntaxKind.IntegerLiteral);

            TestSingleToken("0xDEADBEEFL", SyntaxKind.IntegerLiteral);
            TestSingleToken("0xDEADBEEFl", SyntaxKind.IntegerLiteral);

            TestSingleToken("0xDEADBEEFUL", SyntaxKind.IntegerLiteral);
            TestSingleToken("0xDEADBEEFUl", SyntaxKind.IntegerLiteral);

            TestSingleToken("0xDEADBEEFuL", SyntaxKind.IntegerLiteral);
            TestSingleToken("0xDEADBEEFul", SyntaxKind.IntegerLiteral);

            TestSingleToken("0xDEADBEEFLU", SyntaxKind.IntegerLiteral);
            TestSingleToken("0xDEADBEEFLu", SyntaxKind.IntegerLiteral);

            TestSingleToken("0xDEADBEEFlU", SyntaxKind.IntegerLiteral);
            TestSingleToken("0xDEADBEEFlu", SyntaxKind.IntegerLiteral);
        }

        [Fact]
        public void Trailing_Letter_Is_Not_Part_Of_Hex_Literal_If_Not_Type_Sufix()
        {
            TestTokenizer("0xDEADBEEFz", SyntaxFactory.Token(SyntaxKind.IntegerLiteral, "0xDEADBEEF"), IgnoreRemaining);
        }

        [Fact]
        public void Dot_Followed_By_Non_Digit_Is_Not_Part_Of_Real_Literal()
        {
            TestTokenizer("3.a", SyntaxFactory.Token(SyntaxKind.IntegerLiteral, "3"), IgnoreRemaining);
        }

        [Fact]
        public void Simple_Real_Literal_Is_Recognized()
        {
            TestTokenizer("3.14159", SyntaxFactory.Token(SyntaxKind.RealLiteral, "3.14159"));
        }

        [Fact]
        public void Real_Literal_Between_Zero_And_One_Is_Recognized()
        {
            TestTokenizer(".14159", SyntaxFactory.Token(SyntaxKind.RealLiteral, ".14159"));
        }

        [Fact]
        public void Integer_With_Real_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("42F", SyntaxKind.RealLiteral);
            TestSingleToken("42f", SyntaxKind.RealLiteral);
            TestSingleToken("42D", SyntaxKind.RealLiteral);
            TestSingleToken("42d", SyntaxKind.RealLiteral);
            TestSingleToken("42M", SyntaxKind.RealLiteral);
            TestSingleToken("42m", SyntaxKind.RealLiteral);
        }

        [Fact]
        public void Integer_With_Exponent_Is_Recognized()
        {
            TestSingleToken("1e10", SyntaxKind.RealLiteral);
            TestSingleToken("1E10", SyntaxKind.RealLiteral);
            TestSingleToken("1e+10", SyntaxKind.RealLiteral);
            TestSingleToken("1E+10", SyntaxKind.RealLiteral);
            TestSingleToken("1e-10", SyntaxKind.RealLiteral);
            TestSingleToken("1E-10", SyntaxKind.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("3.14F", SyntaxKind.RealLiteral);
            TestSingleToken("3.14f", SyntaxKind.RealLiteral);
            TestSingleToken("3.14D", SyntaxKind.RealLiteral);
            TestSingleToken("3.14d", SyntaxKind.RealLiteral);
            TestSingleToken("3.14M", SyntaxKind.RealLiteral);
            TestSingleToken("3.14m", SyntaxKind.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Exponent_Is_Recognized()
        {
            TestSingleToken("3.14E10", SyntaxKind.RealLiteral);
            TestSingleToken("3.14e10", SyntaxKind.RealLiteral);
            TestSingleToken("3.14E+10", SyntaxKind.RealLiteral);
            TestSingleToken("3.14e+10", SyntaxKind.RealLiteral);
            TestSingleToken("3.14E-10", SyntaxKind.RealLiteral);
            TestSingleToken("3.14e-10", SyntaxKind.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Exponent_And_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("3.14E+10F", SyntaxKind.RealLiteral);
        }

        [Fact]
        public void Single_Character_Literal_Is_Recognized()
        {
            TestSingleToken("'f'", SyntaxKind.CharacterLiteral);
        }

        [Fact]
        public void Multi_Character_Literal_Is_Recognized()
        {
            TestSingleToken("'foo'", SyntaxKind.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Is_Terminated_By_EOF_If_Unterminated()
        {
            TestSingleToken("'foo bar", SyntaxKind.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Not_Terminated_By_Escaped_Quote()
        {
            TestSingleToken("'foo\\'bar'", SyntaxKind.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Is_Terminated_By_EOL_If_Unterminated()
        {
            TestTokenizer("'foo\n", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'foo"), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("'foo\\\n", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer("'foo\\\nflarg", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("'foo\\" + Environment.NewLine, SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer($"'foo\\{Environment.NewLine}flarg", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Allows_Escaped_Escape()
        {
            TestTokenizer("'foo\\\\'blah", SyntaxFactory.Token(SyntaxKind.CharacterLiteral, "'foo\\\\'"), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Is_Recognized()
        {
            TestSingleToken("\"foo\"", SyntaxKind.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_By_EOF_If_Unterminated()
        {
            TestSingleToken("\"foo bar", SyntaxKind.StringLiteral);
        }

        [Fact]
        public void String_Literal_Not_Terminated_By_Escaped_Quote()
        {
            TestSingleToken("\"foo\\\"bar\"", SyntaxKind.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_By_EOL_If_Unterminated()
        {
            TestTokenizer("\"foo\n", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"foo"), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("\"foo\\\n", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer("\"foo\\\nflarg", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("\"foo\\" + Environment.NewLine, SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer($"\"foo\\{Environment.NewLine}flarg", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"foo\\"), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Allows_Escaped_Escape()
        {
            TestTokenizer("\"foo\\\\\"blah", SyntaxFactory.Token(SyntaxKind.StringLiteral, "\"foo\\\\\""), IgnoreRemaining);
        }

        [Fact]
        public void Verbatim_String_Literal_Can_Contain_Newlines()
        {
            TestSingleToken("@\"foo\nbar\nbaz\"", SyntaxKind.StringLiteral);
        }

        [Fact]
        public void Verbatim_String_Literal_Not_Terminated_By_Escaped_Double_Quote()
        {
            TestSingleToken("@\"foo\"\"bar\"", SyntaxKind.StringLiteral);
        }

        [Fact]
        public void Verbatim_String_Literal_Is_Terminated_By_Slash_Double_Quote()
        {
            TestTokenizer("@\"foo\\\"bar\"", SyntaxFactory.Token(SyntaxKind.StringLiteral, "@\"foo\\\""), IgnoreRemaining);
        }

        [Fact]
        public void Verbatim_String_Literal_Is_Terminated_By_EOF()
        {
            TestSingleToken("@\"foo", SyntaxKind.StringLiteral);
        }
    }
}
