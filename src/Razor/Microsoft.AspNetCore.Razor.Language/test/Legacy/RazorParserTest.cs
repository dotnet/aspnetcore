// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class RazorParserTest
    {
        [Fact]
        public void CanParseStuff()
        {
            var parser = new RazorParser();
            var sourceDocument = TestRazorSourceDocument.CreateResource("TestFiles/Source/BasicMarkup.cshtml", GetType());
            var output = parser.Parse(sourceDocument);

            Assert.NotNull(output);
        }

        [Fact]
        public void ParseMethodCallsParseDocumentOnMarkupParserAndReturnsResults()
        {
            // Arrange
            var parser = new RazorParser();
            var expected =
"RazorDocument - [0..12)::12 - [foo @bar baz]" + Environment.NewLine +
"    MarkupBlock - [0..12)::12" + Environment.NewLine +
"        MarkupTextLiteral - [0..4)::4 - [foo ] - Gen<Markup> - SpanEditHandler;Accepts:Any" + Environment.NewLine +
"            Text;[foo];" + Environment.NewLine +
"            Whitespace;[ ];" + Environment.NewLine +
"        CSharpCodeBlock - [4..8)::4" + Environment.NewLine +
"            CSharpImplicitExpression - [4..8)::4" + Environment.NewLine +
"                CSharpTransition - [4..5)::1 - Gen<None> - SpanEditHandler;Accepts:None" + Environment.NewLine +
"                    Transition;[@];" + Environment.NewLine +
"                CSharpImplicitExpressionBody - [5..8)::3" + Environment.NewLine +
"                    CSharpCodeBlock - [5..8)::3" + Environment.NewLine +
"                        CSharpExpressionLiteral - [5..8)::3 - [bar] - Gen<Expr> - ImplicitExpressionEditHandler;Accepts:NonWhitespace;ImplicitExpression[RTD];K14" + Environment.NewLine +
"                            Identifier;[bar];" + Environment.NewLine +
"        MarkupTextLiteral - [8..12)::4 - [ baz] - Gen<Markup> - SpanEditHandler;Accepts:Any" + Environment.NewLine +
"            Whitespace;[ ];" + Environment.NewLine +
"            Text;[baz];" + Environment.NewLine;

            // Act
            var syntaxTree = parser.Parse(TestRazorSourceDocument.Create("foo @bar baz"));

            // Assert
            var actual = SyntaxNodeSerializer.Serialize(syntaxTree.Root);
            Assert.Equal(expected, actual);
        }
    }
}
