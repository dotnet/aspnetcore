// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlErrorTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseBlockAllowsInvalidTagNamesAsLongAsParserCanIdentifyEndTag()
        {
            ParseBlockTest("<1-foo+bar>foo</1-foo+bar>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<1-foo+bar>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("foo"),
                    new MarkupTagBlock(
                        Factory.Markup("</1-foo+bar>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockThrowsErrorIfStartTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text foo bar></text>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text foo bar>").Accepts(AcceptedCharactersInternal.Any)),
                    new MarkupTagBlock(
                        Factory.MarkupTransition("</text>"))),
                RazorDiagnosticFactory.CreateParsing_TextTagCannotContainAttributes(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 4)));
        }

        [Fact]
        public void ParseBlockThrowsErrorIfEndTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text></text foo bar>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text>")),
                    new MarkupTagBlock(
                        Factory.MarkupTransition("</text foo bar>").Accepts(AcceptedCharactersInternal.Any))),
                RazorDiagnosticFactory.CreateParsing_TextTagCannotContainAttributes(
                    new SourceSpan(new SourceLocation(8, 0, 8), contentLength: 4)));
        }

        [Fact]
        public void ParseBlockThrowsExceptionIfBlockDoesNotStartWithTag()
        {
            ParseBlockTest("foo bar <baz>",
                new MarkupBlock(),
                RazorDiagnosticFactory.CreateParsing_MarkupBlockMustStartWithTag(
                    new SourceSpan(SourceLocation.Zero, contentLength: 3)));
        }

        [Fact]
        public void ParseBlockStartingWithEndTagProducesRazorErrorThenOutputsMarkupSegmentAndEndsBlock()
        {
            ParseBlockTest("</foo> bar baz",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                    new SourceSpan(new SourceLocation(2, 0, 2), contentLength: 3), "foo"));
        }

        [Fact]
        public void ParseBlockWithUnclosedTopLevelTagThrowsMissingEndTagParserExceptionOnOutermostUnclosedTag()
        {
            ParseBlockTest("<p><foo></bar>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<p>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</bar>").Accepts(AcceptedCharactersInternal.None))),
                RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"));
        }

        [Fact]
        public void ParseBlockWithUnclosedTagAtEOFThrowsMissingEndTagException()
        {
            ParseBlockTest("<foo>blah blah blah blah blah",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("blah blah blah blah blah")),
                RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 3), "foo"));
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
                RazorDiagnosticFactory.CreateParsing_UnfinishedTag(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 3), "foo"));
        }
    }
}
