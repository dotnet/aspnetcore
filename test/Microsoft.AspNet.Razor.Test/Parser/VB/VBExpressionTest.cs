// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBExpressionTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlockCorrectlyHandlesCodeBlockInBodyOfExplicitExpressionDueToUnclosedExpression()
        {
            ParseBlockTest(@"(
@Code
    Dim foo = bar
End Code",
                new ExpressionBlock(
                    Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                    Factory.EmptyVB().AsExpression()),
                new RazorError(
                    String.Format(
                        RazorResources.ParseError_Expected_EndOfBlock_Before_EOF,
                        RazorResources.BlockName_ExplicitExpression,
                        ")", "("),
                     SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockAcceptsNonEnglishCharactersThatAreValidIdentifiers()
        {
            ImplicitExpressionTest("हळूँजद॔.", "हळूँजद॔");
        }

        [Fact]
        public void ParseBlockDoesNotTreatXmlAxisPropertyAsTransitionToMarkup()
        {
            SingleSpanBlockTest(
                @"If foo Is Nothing Then
    Dim bar As XElement
    Dim foo = bar.<foo>
End If",
                BlockType.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockDoesNotTreatXmlAttributePropertyAsTransitionToMarkup()
        {
            SingleSpanBlockTest(
                @"If foo Is Nothing Then
    Dim bar As XElement
    Dim foo = bar.@foo
End If",
                BlockType.Statement,
                SpanKind.Code,
                acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsSimpleImplicitExpression()
        {
            ImplicitExpressionTest("Foo");
        }

        [Fact]
        public void ParseBlockSupportsImplicitExpressionWithDots()
        {
            ImplicitExpressionTest("Foo.Bar.Baz");
        }

        [Fact]
        public void ParseBlockSupportsImplicitExpressionWithParens()
        {
            ImplicitExpressionTest("Foo().Bar().Baz()");
        }

        [Fact]
        public void ParseBlockSupportsImplicitExpressionWithStuffInParens()
        {
            ImplicitExpressionTest("Foo().Bar(sdfkhj sdfksdfjs \")\" sjdfkjsdf).Baz()");
        }

        [Fact]
        public void ParseBlockSupportsImplicitExpressionWithCommentInParens()
        {
            ImplicitExpressionTest("Foo().Bar(sdfkhj sdfksdfjs \")\" '))))))))\r\nsjdfkjsdf).Baz()");
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("Foo(Of String).Bar(1, 2, 3).Biz")]
        [InlineData("Foo(Of String).Bar(\")\").Biz")]
        [InlineData("Foo(Of String).Bar(\"Foo\"\"Bar)\"\"Baz\").Biz")]
        [InlineData("\"foo\r\nbar")]
        [InlineData("Foo.Bar. _\r\nREM )\r\nBaz()\r\n")]
        [InlineData("Foo.Bar. _\r\n' )\r\nBaz()\r\n")]
        public void ValidExplicitExpressions(string body)
        {
            ParseBlockTest("(" + body + ")",
                new ExpressionBlock(
                    Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                    Factory.Code(body).AsExpression(),
                    Factory.MetaCode(")").Accepts(AcceptedCharacters.None)));
        }
    }
}
