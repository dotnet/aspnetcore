// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBImplicitExpressionTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void VB_Simple_ImplicitExpression()
        {
            ParseBlockTest("@foo not-part-of-the-block",
                new ExpressionBlock(
                    Factory.CodeTransition(),
                    Factory.Code("foo")
                           .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                           .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void VB_ImplicitExpression_With_Keyword_At_Start()
        {
            ParseBlockTest("@Partial",
                new ExpressionBlock(
                    Factory.CodeTransition(),
                    Factory.Code("Partial")
                           .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                           .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void VB_ImplicitExpression_With_Keyword_In_Body()
        {
            ParseBlockTest("@Html.Partial",
                new ExpressionBlock(
                    Factory.CodeTransition(),
                    Factory.Code("Html.Partial")
                           .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                           .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void VB_ImplicitExpression_With_MethodCallOrArrayIndex()
        {
            ParseBlockTest("@foo(42) not-part-of-the-block",
                new ExpressionBlock(
                    Factory.CodeTransition(),
                    Factory.Code("foo(42)")
                           .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                           .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void VB_ImplicitExpression_Terminates_If_Trailing_Dot_Not_Followed_By_Valid_Token()
        {
            ParseBlockTest("@foo(42). ",
                new ExpressionBlock(
                    Factory.CodeTransition(),
                    Factory.Code("foo(42)")
                           .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                           .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void VB_ImplicitExpression_Supports_Complex_Expressions()
        {
            ParseBlockTest("@foo(42).bar(Biz.Boz / 42 * 8)(1).Burf not part of the block",
                new ExpressionBlock(
                    Factory.CodeTransition()
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("foo(42).bar(Biz.Boz / 42 * 8)(1).Burf")
                           .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                           .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }
    }
}
