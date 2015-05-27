// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.Html
{
    public class HtmlErrorTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseBlockAllowsInvalidTagNamesAsLongAsParserCanIdentifyEndTag()
        {
            ParseBlockTest("<1-foo+bar>foo</1-foo+bar>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<1-foo+bar>").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("foo"),
                    new MarkupTagBlock(
                        Factory.Markup("</1-foo+bar>").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void ParseBlockThrowsErrorIfStartTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text foo bar></text>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text foo bar>").Accepts(AcceptedCharacters.Any)),
                    new MarkupTagBlock(
                        Factory.MarkupTransition("</text>"))),
                new RazorError(RazorResources.ParseError_TextTagCannotContainAttributes, SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockThrowsErrorIfEndTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text></text foo bar>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text>")),
                    new MarkupTagBlock(
                        Factory.MarkupTransition("</text foo bar>").Accepts(AcceptedCharacters.Any))),
                new RazorError(RazorResources.ParseError_TextTagCannotContainAttributes, 6, 0, 6));
        }

        [Fact]
        public void ParseBlockThrowsExceptionIfBlockDoesNotStartWithTag()
        {
            ParseBlockTest("foo bar <baz>",
                new MarkupBlock(),
                new RazorError(RazorResources.ParseError_MarkupBlock_Must_Start_With_Tag, SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockStartingWithEndTagProducesRazorErrorThenOutputsMarkupSegmentAndEndsBlock()
        {
            ParseBlockTest("</foo> bar baz",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharacters.None)),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                new RazorError(RazorResources.FormatParseError_UnexpectedEndTag("foo"), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockWithUnclosedTopLevelTagThrowsMissingEndTagParserExceptionOnOutermostUnclosedTag()
        {
            ParseBlockTest("<p><foo></bar>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<p>").Accepts(AcceptedCharacters.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharacters.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</bar>").Accepts(AcceptedCharacters.None))),
                new RazorError(RazorResources.FormatParseError_MissingEndTag("p"), new SourceLocation(0, 0, 0)));
        }

        [Fact]
        public void ParseBlockWithUnclosedTagAtEOFThrowsMissingEndTagException()
        {
            ParseBlockTest("<foo>blah blah blah blah blah",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("blah blah blah blah blah")),
                new RazorError(RazorResources.FormatParseError_MissingEndTag("foo"), new SourceLocation(0, 0, 0)));
        }

        [Fact]
        public void ParseBlockWithUnfinishedTagAtEOFThrowsIncompleteTagException()
        {
            ParseBlockTest("<foo bar=baz",
                new MarkupBlock(
                    new MarkupTagBlock(
                    Factory.Markup("<foo"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("bar", new LocationTagged<string>(" bar=", 4, 0, 4), new LocationTagged<string>(string.Empty, 12, 0, 12)),
                            Factory.Markup(" bar=").With(SpanChunkGenerator.Null),
                            Factory.Markup("baz").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 9, 0, 9), new LocationTagged<string>("baz", 9, 0, 9)))))),
                new RazorError(RazorResources.FormatParseError_UnfinishedTag("foo"), new SourceLocation(0, 0, 0)));
        }
    }
}
