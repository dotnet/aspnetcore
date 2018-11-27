// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerCommentTest : CSharpTokenizerTestBase
    {
        private new CSharpToken IgnoreRemaining => (CSharpToken)base.IgnoreRemaining;

        [Fact]
        public void Next_Ignores_Star_At_EOF_In_RazorComment()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *",
                new CSharpToken("@", CSharpTokenType.RazorCommentTransition),
                new CSharpToken("*", CSharpTokenType.RazorCommentStar),
                new CSharpToken(" Foo * Bar * Baz *", CSharpTokenType.RazorComment));
        }

        [Fact]
        public void Next_Ignores_Star_Without_Trailing_At()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *@",
                new CSharpToken("@", CSharpTokenType.RazorCommentTransition),
                new CSharpToken("*", CSharpTokenType.RazorCommentStar),
                new CSharpToken(" Foo * Bar * Baz ", CSharpTokenType.RazorComment),
                new CSharpToken("*", CSharpTokenType.RazorCommentStar),
                new CSharpToken("@", CSharpTokenType.RazorCommentTransition));
        }

        [Fact]
        public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
        {
            TestTokenizer(
                "@* Foo Bar Baz *@",
                new CSharpToken("@", CSharpTokenType.RazorCommentTransition),
                new CSharpToken("*", CSharpTokenType.RazorCommentStar),
                new CSharpToken(" Foo Bar Baz ", CSharpTokenType.RazorComment),
                new CSharpToken("*", CSharpTokenType.RazorCommentStar),
                new CSharpToken("@", CSharpTokenType.RazorCommentTransition));
        }

        [Fact]
        public void Next_Returns_Comment_Token_For_Entire_Single_Line_Comment()
        {
            TestTokenizer("// Foo Bar Baz", new CSharpToken("// Foo Bar Baz", CSharpTokenType.Comment));
        }

        [Fact]
        public void Single_Line_Comment_Is_Terminated_By_Newline()
        {
            TestTokenizer("// Foo Bar Baz\na", new CSharpToken("// Foo Bar Baz", CSharpTokenType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Multi_Line_Comment_In_Single_Line_Comment_Has_No_Effect()
        {
            TestTokenizer("// Foo/*Bar*/ Baz\na", new CSharpToken("// Foo/*Bar*/ Baz", CSharpTokenType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Comment_Token_For_Entire_Multi_Line_Comment()
        {
            TestTokenizer("/* Foo\nBar\nBaz */", new CSharpToken("/* Foo\nBar\nBaz */", CSharpTokenType.Comment));
        }

        [Fact]
        public void Multi_Line_Comment_Is_Terminated_By_End_Sequence()
        {
            TestTokenizer("/* Foo\nBar\nBaz */a", new CSharpToken("/* Foo\nBar\nBaz */", CSharpTokenType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Unterminated_Multi_Line_Comment_Captures_To_EOF()
        {
            TestTokenizer("/* Foo\nBar\nBaz", new CSharpToken("/* Foo\nBar\nBaz", CSharpTokenType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Nested_Multi_Line_Comments_Terminated_At_First_End_Sequence()
        {
            TestTokenizer("/* Foo/*\nBar\nBaz*/ */", new CSharpToken("/* Foo/*\nBar\nBaz*/", CSharpTokenType.Comment), IgnoreRemaining);
        }

        [Fact]
        public void Nested_Multi_Line_Comments_Terminated_At_Full_End_Sequence()
        {
            TestTokenizer("/* Foo\nBar\nBaz* */", new CSharpToken("/* Foo\nBar\nBaz* */", CSharpTokenType.Comment), IgnoreRemaining);
        }
    }
}
