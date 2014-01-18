// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBExpressionsInCodeTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void InnerImplicitExpressionWithOnlySingleAtAcceptsSingleSpaceOrNewlineAtDesignTime()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    @" + Environment.NewLine
                         + "End Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    ").AsStatement(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.EmptyVB()
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords, acceptTrailingDot: true)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Code("\r\n").AsStatement(),
                    Factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_VB, 11, 1, 5)
                });
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptDotAfterAt()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    @." + Environment.NewLine
                         + "End Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    ").AsStatement(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.EmptyVB()
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords, acceptTrailingDot: true)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Code(".\r\n").AsStatement(),
                    Factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(
                        String.Format(RazorResources.ParseError_Unexpected_Character_At_Start_Of_CodeBlock_VB, "."), 
                        11, 1, 5)
                });
        }

        [Theory]
        [InlineData("Foo.Bar.", true)]
        [InlineData("Foo", true)]
        [InlineData("Foo.Bar.Baz", true)]
        [InlineData("Foo().Bar().Baz()", true)]
        [InlineData("Foo().Bar(sdfkhj sdfksdfjs \")\" sjdfkjsdf).Baz()", true)]
        [InlineData("Foo().Bar(sdfkhj sdfksdfjs \")\" '))))))))\r\nsjdfkjsdf).Baz()", true)]
        [InlineData("Foo", false)]
        [InlineData("Foo(Of String).Bar(1, 2, 3).Biz", false)]
        [InlineData("Foo(Of String).Bar(\")\").Biz", false)]
        [InlineData("Foo(Of String).Bar(\"Foo\"\"Bar)\"\"Baz\").Biz", false)]
        [InlineData("Foo.Bar. _\r\nREM )\r\nBaz()\r\n", false)]
        [InlineData("Foo.Bar. _\r\n' )\r\nBaz()\r\n", false)]
        public void ExpressionInCode(string expression, bool isImplicit)
        {
            ExpressionBlock expressionBlock;
            if (isImplicit)
            {
                expressionBlock =
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code(expression)
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords, acceptTrailingDot: true)
                               .Accepts(AcceptedCharacters.NonWhiteSpace));
            }
            else
            {
                expressionBlock =
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                        Factory.Code(expression).AsExpression(),
                        Factory.MetaCode(")").Accepts(AcceptedCharacters.None));
            }

            string code;
            if (isImplicit)
            {
                code = "If foo IsNot Nothing Then" + Environment.NewLine
                     + "    @" + expression + Environment.NewLine
                     + "End If";
            }
            else
            {
                code = "If foo IsNot Nothing Then" + Environment.NewLine
                     + "    @(" + expression + ")" + Environment.NewLine
                     + "End If";
            }

            ParseBlockTest(code,
                new StatementBlock(
                    Factory.Code("If foo IsNot Nothing Then\r\n    ")
                           .AsStatement(),
                    expressionBlock,
                    Factory.Code("\r\nEnd If")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }
    }
}
