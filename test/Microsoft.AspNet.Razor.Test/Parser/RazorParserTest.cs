// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
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
            RazorParser parser = new RazorParser(new CSharpCodeParser(), 
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
            RazorParser parser = new RazorParser(new CSharpCodeParser(), 
                                                 new HtmlMarkupParser(),
                                                 tagHelperDescriptorResolver: null);

            // Act
            ParserResults results = parser.Parse(new StringReader("foo @bar baz"));

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
        public void ParseMethodSetsUpRunWithSpecifiedCodeParserMarkupParserAndListenerAndPassesToMarkupParser()
        {
            RunParseWithListenerTest((parser, reader) => parser.Parse(reader));
        }

        private static void RunParseWithListenerTest(Action<RazorParser, TextReader> parserAction)
        {
            // Arrange
            ParserBase markupParser = new MockMarkupParser();
            ParserBase codeParser = new CSharpCodeParser();
            RazorParser parser = new RazorParser(codeParser, markupParser, tagHelperDescriptorResolver: null);
            TextReader expectedReader = new StringReader("foo");

            // Act
            parserAction(parser, expectedReader);

            // Assert
            ParserContext actualContext = markupParser.Context;
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
