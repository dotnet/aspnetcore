// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerLiteralTest : CSharpTokenizerTestBase
    {
        private new CSharpToken IgnoreRemaining => (CSharpToken)base.IgnoreRemaining;

        [Fact]
        public void Simple_Integer_Literal_Is_Recognized()
        {
            TestSingleToken("01189998819991197253", CSharpTokenType.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("42U", CSharpTokenType.IntegerLiteral);
            TestSingleToken("42u", CSharpTokenType.IntegerLiteral);

            TestSingleToken("42L", CSharpTokenType.IntegerLiteral);
            TestSingleToken("42l", CSharpTokenType.IntegerLiteral);

            TestSingleToken("42UL", CSharpTokenType.IntegerLiteral);
            TestSingleToken("42Ul", CSharpTokenType.IntegerLiteral);

            TestSingleToken("42uL", CSharpTokenType.IntegerLiteral);
            TestSingleToken("42ul", CSharpTokenType.IntegerLiteral);

            TestSingleToken("42LU", CSharpTokenType.IntegerLiteral);
            TestSingleToken("42Lu", CSharpTokenType.IntegerLiteral);

            TestSingleToken("42lU", CSharpTokenType.IntegerLiteral);
            TestSingleToken("42lu", CSharpTokenType.IntegerLiteral);
        }

        [Fact]
        public void Trailing_Letter_Is_Not_Part_Of_Integer_Literal_If_Not_Type_Sufix()
        {
            TestTokenizer("42a", new CSharpToken("42", CSharpTokenType.IntegerLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Simple_Hex_Literal_Is_Recognized()
        {
            TestSingleToken("0x0123456789ABCDEF", CSharpTokenType.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffix_Is_Recognized_In_Hex_Literal()
        {
            TestSingleToken("0xDEADBEEFU", CSharpTokenType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFu", CSharpTokenType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFL", CSharpTokenType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFl", CSharpTokenType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFUL", CSharpTokenType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFUl", CSharpTokenType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFuL", CSharpTokenType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFul", CSharpTokenType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFLU", CSharpTokenType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFLu", CSharpTokenType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFlU", CSharpTokenType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFlu", CSharpTokenType.IntegerLiteral);
        }

        [Fact]
        public void Trailing_Letter_Is_Not_Part_Of_Hex_Literal_If_Not_Type_Sufix()
        {
            TestTokenizer("0xDEADBEEFz", new CSharpToken("0xDEADBEEF", CSharpTokenType.IntegerLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Dot_Followed_By_Non_Digit_Is_Not_Part_Of_Real_Literal()
        {
            TestTokenizer("3.a", new CSharpToken("3", CSharpTokenType.IntegerLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Simple_Real_Literal_Is_Recognized()
        {
            TestTokenizer("3.14159", new CSharpToken("3.14159", CSharpTokenType.RealLiteral));
        }

        [Fact]
        public void Real_Literal_Between_Zero_And_One_Is_Recognized()
        {
            TestTokenizer(".14159", new CSharpToken(".14159", CSharpTokenType.RealLiteral));
        }

        [Fact]
        public void Integer_With_Real_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("42F", CSharpTokenType.RealLiteral);
            TestSingleToken("42f", CSharpTokenType.RealLiteral);
            TestSingleToken("42D", CSharpTokenType.RealLiteral);
            TestSingleToken("42d", CSharpTokenType.RealLiteral);
            TestSingleToken("42M", CSharpTokenType.RealLiteral);
            TestSingleToken("42m", CSharpTokenType.RealLiteral);
        }

        [Fact]
        public void Integer_With_Exponent_Is_Recognized()
        {
            TestSingleToken("1e10", CSharpTokenType.RealLiteral);
            TestSingleToken("1E10", CSharpTokenType.RealLiteral);
            TestSingleToken("1e+10", CSharpTokenType.RealLiteral);
            TestSingleToken("1E+10", CSharpTokenType.RealLiteral);
            TestSingleToken("1e-10", CSharpTokenType.RealLiteral);
            TestSingleToken("1E-10", CSharpTokenType.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("3.14F", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14f", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14D", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14d", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14M", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14m", CSharpTokenType.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Exponent_Is_Recognized()
        {
            TestSingleToken("3.14E10", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14e10", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14E+10", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14e+10", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14E-10", CSharpTokenType.RealLiteral);
            TestSingleToken("3.14e-10", CSharpTokenType.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Exponent_And_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("3.14E+10F", CSharpTokenType.RealLiteral);
        }

        [Fact]
        public void Single_Character_Literal_Is_Recognized()
        {
            TestSingleToken("'f'", CSharpTokenType.CharacterLiteral);
        }

        [Fact]
        public void Multi_Character_Literal_Is_Recognized()
        {
            TestSingleToken("'foo'", CSharpTokenType.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Is_Terminated_By_EOF_If_Unterminated()
        {
            TestSingleToken("'foo bar", CSharpTokenType.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Not_Terminated_By_Escaped_Quote()
        {
            TestSingleToken("'foo\\'bar'", CSharpTokenType.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Is_Terminated_By_EOL_If_Unterminated()
        {
            TestTokenizer("'foo\n", new CSharpToken("'foo", CSharpTokenType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("'foo\\\n", new CSharpToken("'foo\\", CSharpTokenType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer("'foo\\\nflarg", new CSharpToken("'foo\\", CSharpTokenType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("'foo\\" + Environment.NewLine, new CSharpToken("'foo\\", CSharpTokenType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer($"'foo\\{Environment.NewLine}flarg", new CSharpToken("'foo\\", CSharpTokenType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Allows_Escaped_Escape()
        {
            TestTokenizer("'foo\\\\'blah", new CSharpToken("'foo\\\\'", CSharpTokenType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Is_Recognized()
        {
            TestSingleToken("\"foo\"", CSharpTokenType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_By_EOF_If_Unterminated()
        {
            TestSingleToken("\"foo bar", CSharpTokenType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Not_Terminated_By_Escaped_Quote()
        {
            TestSingleToken("\"foo\\\"bar\"", CSharpTokenType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_By_EOL_If_Unterminated()
        {
            TestTokenizer("\"foo\n", new CSharpToken("\"foo", CSharpTokenType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("\"foo\\\n", new CSharpToken("\"foo\\", CSharpTokenType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer("\"foo\\\nflarg", new CSharpToken("\"foo\\", CSharpTokenType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("\"foo\\" + Environment.NewLine, new CSharpToken("\"foo\\", CSharpTokenType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer($"\"foo\\{Environment.NewLine}flarg", new CSharpToken("\"foo\\", CSharpTokenType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Allows_Escaped_Escape()
        {
            TestTokenizer("\"foo\\\\\"blah", new CSharpToken("\"foo\\\\\"", CSharpTokenType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Verbatim_String_Literal_Can_Contain_Newlines()
        {
            TestSingleToken("@\"foo\nbar\nbaz\"", CSharpTokenType.StringLiteral);
        }

        [Fact]
        public void Verbatim_String_Literal_Not_Terminated_By_Escaped_Double_Quote()
        {
            TestSingleToken("@\"foo\"\"bar\"", CSharpTokenType.StringLiteral);
        }

        [Fact]
        public void Verbatim_String_Literal_Is_Terminated_By_Slash_Double_Quote()
        {
            TestTokenizer("@\"foo\\\"bar\"", new CSharpToken("@\"foo\\\"", CSharpTokenType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Verbatim_String_Literal_Is_Terminated_By_EOF()
        {
            TestSingleToken("@\"foo", CSharpTokenType.StringLiteral);
        }
    }
}
