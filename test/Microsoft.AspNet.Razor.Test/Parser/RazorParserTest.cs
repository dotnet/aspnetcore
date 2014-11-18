// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
#if !ASPNETCORE50
using Moq;
using Moq.Protected;
#endif
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser
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

#if !ASPNETCORE50
        [Fact]
        public void GetTagHelperDescriptors_IsInvokedToLocateTagHelperDescriptors()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var parser = new Mock<RazorParser>(new CSharpCodeParser(),
                                               new HtmlMarkupParser(),
                                               Mock.Of<ITagHelperDescriptorResolver>());
            parser.CallBase = true;
            parser.Protected()
                  .Setup<IEnumerable<TagHelperDescriptor>>("GetTagHelperDescriptors", 
                                                           ItExpr.IsAny<Block>(), 
                                                           ItExpr.IsAny<ParserErrorSink>())
                  .Returns(Enumerable.Empty<TagHelperDescriptor>())
                  .Verifiable();

            // Act
            parser.Object.Parse(new StringReader("<p>Hello world. The time is @DateTime.UtcNow</p>"));

            // Assert
            parser.Verify();
        }
#endif

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

            public override void BuildSpan(SpanBuilder span, Razor.Text.SourceLocation start, string content)
            {
                throw new NotImplementedException();
            }
        }
    }
}
