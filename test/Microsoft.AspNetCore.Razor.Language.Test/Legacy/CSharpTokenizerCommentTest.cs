// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpTokenizerCommentTest : CSharpTokenizerTestBase
    {
        private new SyntaxToken IgnoreRemaining => (SyntaxToken)base.IgnoreRemaining;

        [Fact]
        public void Next_Ignores_Star_At_EOF_In_RazorComment()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *",
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorComment, " Foo * Bar * Baz *"));
        }

        [Fact]
        public void Next_Ignores_Star_Without_Trailing_At()
        {
            TestTokenizer(
                "@* Foo * Bar * Baz *@",
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorComment, " Foo * Bar * Baz "),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"));
        }

        [Fact]
        public void Next_Returns_RazorComment_Token_For_Entire_Razor_Comment()
        {
            TestTokenizer(
                "@* Foo Bar Baz *@",
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorComment, " Foo Bar Baz "),
                SyntaxFactory.Token(SyntaxKind.RazorCommentStar, "*"),
                SyntaxFactory.Token(SyntaxKind.RazorCommentTransition, "@"));
        }

        [Fact]
        public void Next_Returns_Comment_Token_For_Entire_Single_Line_Comment()
        {
            TestTokenizer("// Foo Bar Baz", SyntaxFactory.Token(SyntaxKind.CSharpComment, "// Foo Bar Baz"));
        }

        [Fact]
        public void Single_Line_Comment_Is_Terminated_By_Newline()
        {
            TestTokenizer("// Foo Bar Baz\na", SyntaxFactory.Token(SyntaxKind.CSharpComment, "// Foo Bar Baz"), IgnoreRemaining);
        }

        [Fact]
        public void Multi_Line_Comment_In_Single_Line_Comment_Has_No_Effect()
        {
            TestTokenizer("// Foo/*Bar*/ Baz\na", SyntaxFactory.Token(SyntaxKind.CSharpComment, "// Foo/*Bar*/ Baz"), IgnoreRemaining);
        }

        [Fact]
        public void Next_Returns_Comment_Token_For_Entire_Multi_Line_Comment()
        {
            TestTokenizer("/* Foo\nBar\nBaz */", SyntaxFactory.Token(SyntaxKind.CSharpComment, "/* Foo\nBar\nBaz */"));
        }

        [Fact]
        public void Multi_Line_Comment_Is_Terminated_By_End_Sequence()
        {
            TestTokenizer("/* Foo\nBar\nBaz */a", SyntaxFactory.Token(SyntaxKind.CSharpComment, "/* Foo\nBar\nBaz */"), IgnoreRemaining);
        }

        [Fact]
        public void Unterminated_Multi_Line_Comment_Captures_To_EOF()
        {
            TestTokenizer("/* Foo\nBar\nBaz", SyntaxFactory.Token(SyntaxKind.CSharpComment, "/* Foo\nBar\nBaz"), IgnoreRemaining);
        }

        [Fact]
        public void Nested_Multi_Line_Comments_Terminated_At_First_End_Sequence()
        {
            TestTokenizer("/* Foo/*\nBar\nBaz*/ */", SyntaxFactory.Token(SyntaxKind.CSharpComment, "/* Foo/*\nBar\nBaz*/"), IgnoreRemaining);
        }

        [Fact]
        public void Nested_Multi_Line_Comments_Terminated_At_Full_End_Sequence()
        {
            TestTokenizer("/* Foo\nBar\nBaz* */", SyntaxFactory.Token(SyntaxKind.CSharpComment, "/* Foo\nBar\nBaz* */"), IgnoreRemaining);
        }
    }
}
