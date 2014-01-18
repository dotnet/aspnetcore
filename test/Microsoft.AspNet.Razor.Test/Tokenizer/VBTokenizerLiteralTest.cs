// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Tokenizer
{
    public class VBTokenizerLiteralTest : VBTokenizerTestBase
    {
        [Fact]
        public void Decimal_Integer_Literal_Is_Recognized()
        {
            TestSingleToken("01189998819991197253", VBSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffixes_Are_Recognized_In_Decimal_Literal()
        {
            TestSingleToken("42S", VBSymbolType.IntegerLiteral);
            TestSingleToken("42I", VBSymbolType.IntegerLiteral);
            TestSingleToken("42L", VBSymbolType.IntegerLiteral);
            TestSingleToken("42US", VBSymbolType.IntegerLiteral);
            TestSingleToken("42UI", VBSymbolType.IntegerLiteral);
            TestSingleToken("42UL", VBSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Hex_Integer_Literal_Is_Recognized()
        {
            TestSingleToken("&HDeadBeef", VBSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffixes_Are_Recognized_In_Hex_Literal()
        {
            TestSingleToken("&HDeadBeefS", VBSymbolType.IntegerLiteral);
            TestSingleToken("&HDeadBeefI", VBSymbolType.IntegerLiteral);
            TestSingleToken("&HDeadBeefL", VBSymbolType.IntegerLiteral);
            TestSingleToken("&HDeadBeefUS", VBSymbolType.IntegerLiteral);
            TestSingleToken("&HDeadBeefUI", VBSymbolType.IntegerLiteral);
            TestSingleToken("&HDeadBeefUL", VBSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Octal_Integer_Literal_Is_Recognized()
        {
            TestSingleToken("&O77", VBSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Integer_Type_Suffixes_Are_Recognized_In_Octal_Literal()
        {
            TestSingleToken("&O77S", VBSymbolType.IntegerLiteral);
            TestSingleToken("&O77I", VBSymbolType.IntegerLiteral);
            TestSingleToken("&O77L", VBSymbolType.IntegerLiteral);
            TestSingleToken("&O77US", VBSymbolType.IntegerLiteral);
            TestSingleToken("&O77UI", VBSymbolType.IntegerLiteral);
            TestSingleToken("&O77UL", VBSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Incomplete_Type_Suffix_Is_Recognized()
        {
            TestSingleToken("42U", VBSymbolType.IntegerLiteral);
            TestSingleToken("&H42U", VBSymbolType.IntegerLiteral);
            TestSingleToken("&O77U", VBSymbolType.IntegerLiteral);
        }

        [Fact]
        public void Integer_With_FloatingPoint_Type_Suffix_Is_Recognized_As_FloatingPointLiteral()
        {
            TestSingleToken("42F", VBSymbolType.FloatingPointLiteral);
            TestSingleToken("42R", VBSymbolType.FloatingPointLiteral);
            TestSingleToken("42D", VBSymbolType.FloatingPointLiteral);
        }

        [Fact]
        public void Simple_FloatingPoint_Is_Recognized()
        {
            TestSingleToken("3.14159", VBSymbolType.FloatingPointLiteral);
        }

        [Fact]
        public void Integer_With_Exponent_Is_Recognized()
        {
            TestSingleToken("1E10", VBSymbolType.FloatingPointLiteral);
            TestSingleToken("1e10", VBSymbolType.FloatingPointLiteral);
            TestSingleToken("1E+10", VBSymbolType.FloatingPointLiteral);
            TestSingleToken("1e+10", VBSymbolType.FloatingPointLiteral);
            TestSingleToken("1E-10", VBSymbolType.FloatingPointLiteral);
            TestSingleToken("1e-10", VBSymbolType.FloatingPointLiteral);
        }

        [Fact]
        public void Simple_FloatingPoint_With_Exponent_Is_Recognized()
        {
            TestSingleToken("3.14159e10", VBSymbolType.FloatingPointLiteral);
        }

        [Fact]
        public void FloatingPoint_Between_Zero_And_One_Is_Recognized()
        {
            TestSingleToken(".314159e1", VBSymbolType.FloatingPointLiteral);
        }

        [Fact]
        public void Simple_String_Literal_Is_Recognized()
        {
            TestSingleToken("\"Foo Bar Baz\"", VBSymbolType.StringLiteral);
        }

        [Fact]
        public void Two_Double_Quotes_Are_Recognized_As_Escape_Sequence()
        {
            TestSingleToken("\"Foo \"\"Bar\"\" Baz\"", VBSymbolType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_At_EOF()
        {
            TestSingleToken("\"Foo", VBSymbolType.StringLiteral);
        }

        [Fact]
        public void String_Literal_Is_Terminated_At_EOL()
        {
            TestTokenizer("\"Foo\nBar", new VBSymbol(0, 0, 0, "\"Foo", VBSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void Character_Literal_Is_Recognized_By_Trailing_C_After_String_Literal()
        {
            TestSingleToken("\"abc\"c", VBSymbolType.CharacterLiteral);
        }

        [Fact]
        public void LeftDoubleQuote_Is_Valid_DoubleQuote()
        {
            // Repeat all the above tests with Unicode Left Double Quote Character U+201C: “
            TestSingleToken("“Foo Bar Baz“", VBSymbolType.StringLiteral);
            TestSingleToken("“Foo ““Bar““ Baz“", VBSymbolType.StringLiteral);
            TestSingleToken("“Foo", VBSymbolType.StringLiteral);
            TestSingleToken("“abc“c", VBSymbolType.CharacterLiteral);
            TestTokenizer("“Foo\nBar", new VBSymbol(0, 0, 0, "“Foo", VBSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void RightDoubleQuote_Is_Valid_DoubleQuote()
        {
            // Repeat all the above tests with Unicode Right Double Quote Character U+201D: ”
            TestSingleToken("”Foo Bar Baz”", VBSymbolType.StringLiteral);
            TestSingleToken("”Foo ””Bar”” Baz”", VBSymbolType.StringLiteral);
            TestSingleToken("”Foo", VBSymbolType.StringLiteral);
            TestSingleToken("”abc”c", VBSymbolType.CharacterLiteral);
            TestTokenizer("”Foo\nBar", new VBSymbol(0, 0, 0, "”Foo", VBSymbolType.StringLiteral), IgnoreRemaining);
        }

        [Fact]
        public void DateLiteral_Is_Recognized()
        {
            TestSingleToken("# 8/23/1970 3:45:39AM #", VBSymbolType.DateLiteral);
        }

        [Fact]
        public void DateLiteral_Is_Terminated_At_EndHash()
        {
            TestTokenizer("# 8/23/1970 # 3:45:39AM", new VBSymbol(0, 0, 0, "# 8/23/1970 #", VBSymbolType.DateLiteral), IgnoreRemaining);
        }

        [Fact]
        public void DateLiteral_Is_Terminated_At_EOF()
        {
            TestSingleToken("# 8/23/1970 3:45:39AM", VBSymbolType.DateLiteral);
        }

        [Fact]
        public void DateLiteral_Is_Terminated_At_EOL()
        {
            TestTokenizer("# 8/23/1970\n3:45:39AM", new VBSymbol(0, 0, 0, "# 8/23/1970", VBSymbolType.DateLiteral), IgnoreRemaining);
        }
    }
}
