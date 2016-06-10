// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Test.Tokenizer.Internal;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Internal
{
    public class CSharpTokenizerCommentTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void Next_Ignores_Star_At_EOF_In_RazorComment()
        {
            TestTokenizer("@* Foo * Bar * Baz *",
                          new CSharpSymbol(0, 0, 0, "@", CSharpSymbolType.RazorCommentTransition),
                          new CSharpSymbol(1, 0, 1, "*", CSharpSymbolType.RazorCommentStar),
                          new CSharpSymbol(2, 0, 2, " Foo * Bar * Baz *", CSharpSymbolType.RazorComment));
        }

        [Fact]
        public void Next_Ignores_Star_Without_Trailing_At()
        {
            TestTokenizer("@* Foo * Bar * Baz *@",
                          new CSharpSymbol(0, 0, 0, "@", CSharpSymbolType.RazorCommentTransition),
                          new CSharpSymbol(1, 0, 1, "*", CSharpSymbolType.RazorCommentStar),
                          new CSharpSymbol(2, 0, 2, " Foo * Bar * Baz ", CSharpSymbolType.RazorComment),
                          new CSharpSymbol(19, 0, 19, "*", CSharpSymbolType.RazorCommentStar),
                          new CSharpSymbol(20, 0, 20, "@", CSharpSymbolType.RazorCommentTransition));
        }

        [Fact]
        public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
        {
            TestTokenizer("@* Foo Bar Baz *@",
                          new CSharpSymbol(0, 0, 0, "@", CSharpSymbolType.RazorCommentTransition),
                          new CSharpSymbol(1, 0, 1, "*", CSharpSymbolType.RazorCommentStar),
                          new CSharpSymbol(2, 0, 2, " Foo Bar Baz ", CSharpSymbolType.RazorComment),
                          new CSharpSymbol(15, 0, 15, "*", CSharpSymbolType.RazorCommentStar),
                          new CSharpSymbol(16, 0, 16, "@", CSharpSymbolType.RazorCommentTransition));
        }

        [Fact]
        public void Next_Returns_Comment_Token_For_Entire_Single_Line_Comment()
        {
            TestTokenizer("// Foo Bar Baz", new CSharpSymbol(0, 0, 0, "// Foo Bar Baz", CSharpSymbolType.Comment));
        }

        [Fact]
        public void Single_Line_Comment_Is_Terminated_By_Newline()
        {
            TestTokenizer("// Foo Bar Baz\na", new CSharpSymbol(0, 0, 0, "// Foo Bar Baz", CSharpSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Multi_Line_Comment_In_Single_Line_Comment_Has_No_Effect()
        {
            TestTokenizer("// Foo/*Bar*/ Baz\na", new CSharpSymbol(0, 0, 0, "// Foo/*Bar*/ Baz", CSharpSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Comment_Token_For_Entire_Multi_Line_Comment()
        {
            TestTokenizer("/* Foo\nBar\nBaz */", new CSharpSymbol(0, 0, 0, "/* Foo\nBar\nBaz */", CSharpSymbolType.Comment));
        }

        [Fact]
        public void Multi_Line_Comment_Is_Terminated_By_End_Sequence()
        {
            TestTokenizer("/* Foo\nBar\nBaz */a", new CSharpSymbol(0, 0, 0, "/* Foo\nBar\nBaz */", CSharpSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Unterminated_Multi_Line_Comment_Captures_To_EOF()
        {
            TestTokenizer("/* Foo\nBar\nBaz", new CSharpSymbol(0, 0, 0, "/* Foo\nBar\nBaz", CSharpSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Nested_Multi_Line_Comments_Terminated_At_First_End_Sequence()
        {
            TestTokenizer("/* Foo/*\nBar\nBaz*/ */", new CSharpSymbol(0, 0, 0, "/* Foo/*\nBar\nBaz*/", CSharpSymbolType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Nested_Multi_Line_Comments_Terminated_At_Full_End_Sequence()
        {
            TestTokenizer("/* Foo\nBar\nBaz* */", new CSharpSymbol(0, 0, 0, "/* Foo\nBar\nBaz* */", CSharpSymbolType.Comment), IgnoreRemaining);
        }
    }
}
