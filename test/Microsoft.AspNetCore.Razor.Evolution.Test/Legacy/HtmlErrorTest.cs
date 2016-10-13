// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class HtmlErrorTest : CsHtmlMarkupParserTestBase
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
                new RazorError(
                    LegacyResources.ParseError_TextTagCannotContainAttributes,
                    new SourceLocation(1, 0, 1),
                    length: 4));
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
                new RazorError(
                    LegacyResources.ParseError_TextTagCannotContainAttributes,
                    new SourceLocation(8, 0, 8),
                    length: 4));
        }

        [Fact]
        public void ParseBlockThrowsExceptionIfBlockDoesNotStartWithTag()
        {
            ParseBlockTest("foo bar <baz>",
                new MarkupBlock(),
                new RazorError(
                    LegacyResources.ParseError_MarkupBlock_Must_Start_With_Tag,
                    SourceLocation.Zero,
                    length: 3));
        }

        [Fact]
        public void ParseBlockStartingWithEndTagProducesRazorErrorThenOutputsMarkupSegmentAndEndsBlock()
        {
            ParseBlockTest("</foo> bar baz",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharacters.None)),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                new RazorError(
                    LegacyResources.FormatParseError_UnexpectedEndTag("foo"),
                    new SourceLocation(2, 0, 2),
                    length: 3));
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
                new RazorError(
                    LegacyResources.FormatParseError_MissingEndTag("p"),
                    new SourceLocation(1, 0, 1),
                    length: 1));
        }

        [Fact]
        public void ParseBlockWithUnclosedTagAtEOFThrowsMissingEndTagException()
        {
            ParseBlockTest("<foo>blah blah blah blah blah",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("blah blah blah blah blah")),
                new RazorError(
                    LegacyResources.FormatParseError_MissingEndTag("foo"),
                    new SourceLocation(1, 0, 1),
                    length: 3));
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
                new RazorError(
                    LegacyResources.FormatParseError_UnfinishedTag("foo"),
                    new SourceLocation(1, 0, 1),
                    length: 3));
        }
    }
}
