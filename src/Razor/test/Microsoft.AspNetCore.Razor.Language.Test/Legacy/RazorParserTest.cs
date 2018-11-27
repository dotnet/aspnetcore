// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
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
            var factory = new SpanFactory();
            var parser = new RazorParser();

            // Act/Assert
            ParserTestBase.EvaluateResults(parser.Parse(TestRazorSourceDocument.Create("foo @bar baz")),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ParseMethodUsesProvidedParserListenerIfSpecified()
        {
            // Arrange
            var factory = new SpanFactory();
            var parser = new RazorParser();

            // Act
            var results = parser.Parse(TestRazorSourceDocument.Create("foo @bar baz"));

            // Assert
            ParserTestBase.EvaluateResults(results,
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void Parse_SyntaxTreeSpansAreLinked()
        {
            // Arrange
            var factory = new SpanFactory();
            var parser = new RazorParser();

            // Act
            var results = parser.Parse(TestRazorSourceDocument.Create("foo @bar baz"));

            // Assert
            var spans = results.Root.Flatten().ToArray();
            for (var i = 0; i < spans.Length - 1; i++)
            {
                Assert.Same(spans[i + 1], spans[i].Next);
            }

            for (var i = spans.Length - 1; i > 0; i--)
            {
                Assert.Same(spans[i - 1], spans[i].Previous);
            }
        }
    }
}
