// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser
{
    public class RazorParserTest
    {
        [Fact]
        public void ParseMethodCallsParseDocumentOnMarkupParserAndReturnsResults()
        {
            var factory = SpanFactory.CreateCsHtml();

            // Arrange
            var parser = new RazorParser(new CSharpCodeParser(),
                                         new HtmlMarkupParser(),
                                         tagHelperDescriptorResolver: null);

            // Act/Assert
            ParserTestBase.EvaluateResults(parser.Parse(new StringReader("foo @bar baz")),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ParseMethodUsesProvidedParserListenerIfSpecified()
        {
            var factory = SpanFactory.CreateCsHtml();

            // Arrange
            var parser = new RazorParser(new CSharpCodeParser(),
                                         new HtmlMarkupParser(),
                                         tagHelperDescriptorResolver: null);

            // Act
            var results = parser.Parse(new StringReader("foo @bar baz"));

            // Assert
            ParserTestBase.EvaluateResults(results,
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void GetTagHelperDescriptors_IsInvokedToLocateTagHelperDescriptors()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var parser = new Mock<RazorParser>(
                new CSharpCodeParser(),
                new HtmlMarkupParser(),
                Mock.Of<ITagHelperDescriptorResolver>());
            parser.CallBase = true;
            parser
                .Protected()
                .Setup<IEnumerable<TagHelperDescriptor>>("GetTagHelperDescriptors", ItExpr.IsAny<Block>(), ItExpr.IsAny<ErrorSink>())
                .Returns(Enumerable.Empty<TagHelperDescriptor>())
                .Verifiable();

            // Act
            parser.Object.Parse(new StringReader("<p>Hello world. The time is @DateTime.UtcNow</p>"));

            // Assert
            parser.Verify();
        }

        [Fact]
        public void ParseMethodSetsUpRunWithSpecifiedCodeParserMarkupParserAndListenerAndPassesToMarkupParser()
        {
            RunParseWithListenerTest((parser, reader) => parser.Parse(reader));
        }

        private static void RunParseWithListenerTest(Action<RazorParser, TextReader> parserAction)
        {
            // Arrange
            var markupParser = new MockMarkupParser();
            var codeParser = new CSharpCodeParser();
            var parser = new RazorParser(codeParser, markupParser, tagHelperDescriptorResolver: null);
            var expectedReader = new StringReader("foo");

            // Act
            parserAction(parser, expectedReader);

            // Assert
            var actualContext = markupParser.Context;
            Assert.NotNull(actualContext);
            Assert.Same(markupParser, actualContext.MarkupParser);
            Assert.Same(markupParser, actualContext.ActiveParser);
            Assert.Same(codeParser, actualContext.CodeParser);
        }

        private class MockMarkupParser : ParserBase
        {
            public override bool IsMarkupParser
            {
                get
                {
                    return true;
                }
            }

            public override void ParseDocument()
            {
                using (Context.StartBlock(BlockType.Markup))
                {
                }
            }

            public override void ParseSection(Tuple<string, string> nestingSequences, bool caseSensitive = true)
            {
                using (Context.StartBlock(BlockType.Markup))
                {
                }
            }

            public override void ParseBlock()
            {
                using (Context.StartBlock(BlockType.Markup))
                {
                }
            }

            protected override ParserBase OtherParser
            {
                get { return Context.CodeParser; }
            }

            public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
            {
                throw new NotImplementedException();
            }
        }
    }
}
