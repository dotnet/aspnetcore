// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Test.Tokenizer.Internal;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Internal
{
    public class CSharpTokenizerLiteralTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void Simple_Integer_Literal_Is_Recognized()
        {
            TestSingleToken("01189998819991197253", CSharpSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("42U", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("42u", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("42L", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("42l", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("42UL", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("42Ul", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("42uL", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("42ul", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("42LU", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("42Lu", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("42lU", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("42lu", CSharpSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Trailing_Letter_Is_Not_Part_Of_Integer_Literal_If_Not_Type_Sufix()
        {
            TestTokenizer("42a", new CSharpSymbol(0, 0, 0, "42", CSharpSymbolType.IntegerLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Simple_Hex_Literal_Is_Recognized()
        {
            TestSingleToken("0x0123456789ABCDEF", CSharpSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffix_Is_Recognized_In_Hex_Literal()
        {
            TestSingleToken("0xDEADBEEFU", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFu", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFL", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFl", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFUL", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFUl", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFuL", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFul", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFLU", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFLu", CSharpSymbolType.IntegerLiteral);

            TestSingleToken("0xDEADBEEFlU", CSharpSymbolType.IntegerLiteral);
            TestSingleToken("0xDEADBEEFlu", CSharpSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Trailing_Letter_Is_Not_Part_Of_Hex_Literal_If_Not_Type_Sufix()
        {
            TestTokenizer("0xDEADBEEFz", new CSharpSymbol(0, 0, 0, "0xDEADBEEF", CSharpSymbolType.IntegerLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Dot_Followed_By_Non_Digit_Is_Not_Part_Of_Real_Literal()
        {
            TestTokenizer("3.a", new CSharpSymbol(0, 0, 0, "3", CSharpSymbolType.IntegerLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Simple_Real_Literal_Is_Recognized()
        {
            TestTokenizer("3.14159", new CSharpSymbol(0, 0, 0, "3.14159", CSharpSymbolType.RealLiteral));
        }

        [Fact]
        public void Real_Literal_Between_Zero_And_One_Is_Recognized()
        {
            TestTokenizer(".14159", new CSharpSymbol(0, 0, 0, ".14159", CSharpSymbolType.RealLiteral));
        }

        [Fact]
        public void Integer_With_Real_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("42F", CSharpSymbolType.RealLiteral);
            TestSingleToken("42f", CSharpSymbolType.RealLiteral);
            TestSingleToken("42D", CSharpSymbolType.RealLiteral);
            TestSingleToken("42d", CSharpSymbolType.RealLiteral);
            TestSingleToken("42M", CSharpSymbolType.RealLiteral);
            TestSingleToken("42m", CSharpSymbolType.RealLiteral);
        }

        [Fact]
        public void Integer_With_Exponent_Is_Recognized()
        {
            TestSingleToken("1e10", CSharpSymbolType.RealLiteral);
            TestSingleToken("1E10", CSharpSymbolType.RealLiteral);
            TestSingleToken("1e+10", CSharpSymbolType.RealLiteral);
            TestSingleToken("1E+10", CSharpSymbolType.RealLiteral);
            TestSingleToken("1e-10", CSharpSymbolType.RealLiteral);
            TestSingleToken("1E-10", CSharpSymbolType.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("3.14F", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14f", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14D", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14d", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14M", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14m", CSharpSymbolType.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Exponent_Is_Recognized()
        {
            TestSingleToken("3.14E10", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14e10", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14E+10", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14e+10", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14E-10", CSharpSymbolType.RealLiteral);
            TestSingleToken("3.14e-10", CSharpSymbolType.RealLiteral);
        }

        [Fact]
        public void Real_Number_With_Exponent_And_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("3.14E+10F", CSharpSymbolType.RealLiteral);
        }

        [Fact]
        public void Single_Character_Literal_Is_Recognized()
        {
            TestSingleToken("'f'", CSharpSymbolType.CharacterLiteral);
        }

        [Fact]
        public void Multi_Character_Literal_Is_Recognized()
        {
            TestSingleToken("'foo'", CSharpSymbolType.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Is_Terminated_By_EOF_If_Unterminated()
        {
            TestSingleToken("'foo bar", CSharpSymbolType.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Not_Terminated_By_Escaped_Quote()
        {
            TestSingleToken("'foo\\'bar'", CSharpSymbolType.CharacterLiteral);
        }

        [Fact]
        public void Character_Literal_Is_Terminated_By_EOL_If_Unterminated()
        {
            TestTokenizer("'foo\n", new CSharpSymbol(0, 0, 0, "'foo", CSharpSymbolType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("'foo\\\n", new CSharpSymbol(0, 0, 0, "'foo\\", CSharpSymbolType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer("'foo\\\nflarg", new CSharpSymbol(0, 0, 0, "'foo\\", CSharpSymbolType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("'foo\\" + Environment.NewLine, new CSharpSymbol(0, 0, 0, "'foo\\", CSharpSymbolType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer($"'foo\\{Environment.NewLine}flarg", new CSharpSymbol(0, 0, 0, "'foo\\", CSharpSymbolType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Allows_Escaped_Escape()
        {
            TestTokenizer("'foo\\\\'blah", new CSharpSymbol(0, 0, 0, "'foo\\\\'", CSharpSymbolType.CharacterLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Is_Recognized()
        {
            TestSingleToken("\"foo\"", CSharpSymbolType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_By_EOF_If_Unterminated()
        {
            TestSingleToken("\"foo bar", CSharpSymbolType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Not_Terminated_By_Escaped_Quote()
        {
            TestSingleToken("\"foo\\\"bar\"", CSharpSymbolType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_By_EOL_If_Unterminated()
        {
            TestTokenizer("\"foo\n", new CSharpSymbol(0, 0, 0, "\"foo", CSharpSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("\"foo\\\n", new CSharpSymbol(0, 0, 0, "\"foo\\", CSharpSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_EOL_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer("\"foo\\\nflarg", new CSharpSymbol(0, 0, 0, "\"foo\\", CSharpSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash()
        {
            TestTokenizer("\"foo\\" + Environment.NewLine, new CSharpSymbol(0, 0, 0, "\"foo\\", CSharpSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Terminated_By_CRLF_Even_When_Last_Char_Is_Slash_And_Followed_By_Stuff()
        {
            TestTokenizer($"\"foo\\{Environment.NewLine}flarg", new CSharpSymbol(0, 0, 0, "\"foo\\", CSharpSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void String_Literal_Allows_Escaped_Escape()
        {
            TestTokenizer("\"foo\\\\\"blah", new CSharpSymbol(0, 0, 0, "\"foo\\\\\"", CSharpSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Verbatim_String_Literal_Can_Contain_Newlines()
        {
            TestSingleToken("@\"foo\nbar\nbaz\"", CSharpSymbolType.StringLiteral);
        }

        [Fact]
        public void Verbatim_String_Literal_Not_Terminated_By_Escaped_Double_Quote()
        {
            TestSingleToken("@\"foo\"\"bar\"", CSharpSymbolType.StringLiteral);
        }

        [Fact]
        public void Verbatim_String_Literal_Is_Terminated_By_Slash_Double_Quote()
        {
            TestTokenizer("@\"foo\\\"bar\"", new CSharpSymbol(0, 0, 0, "@\"foo\\\"", CSharpSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Verbatim_String_Literal_Is_Terminated_By_EOF()
        {
            TestSingleToken("@\"foo", CSharpSymbolType.StringLiteral);
        }
    }
}
