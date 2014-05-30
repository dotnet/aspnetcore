// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
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
            SingleSpanBlockTest("<1-foo+bar>foo</1-foo+bar>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockThrowsErrorIfStartTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text foo bar></text>",
                new MarkupBlock(
                    Factory.MarkupTransition("<text").Accepts(AcceptedCharacters.Any),
                    Factory.Markup(" foo bar>"),
                    Factory.MarkupTransition("</text>")),
                new RazorError(RazorResources.ParseError_TextTagCannotContainAttributes, SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockThrowsErrorIfEndTextTagContainsTextAfterName()
        {
            ParseBlockTest("<text></text foo bar>",
                new MarkupBlock(
                    Factory.MarkupTransition("<text>"),
                    Factory.MarkupTransition("</text").Accepts(AcceptedCharacters.Any),
                    Factory.Markup(" ")),
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
                    Factory.Markup("</foo> ").Accepts(AcceptedCharacters.None)),
                new RazorError(RazorResources.FormatParseError_UnexpectedEndTag("foo"), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockWithUnclosedTopLevelTagThrowsMissingEndTagParserExceptionOnOutermostUnclosedTag()
        {
            ParseBlockTest("<p><foo></bar>",
                new MarkupBlock(
                    Factory.Markup("<p><foo></bar>").Accepts(AcceptedCharacters.None)),
                new RazorError(RazorResources.FormatParseError_MissingEndTag("p"), new SourceLocation(0, 0, 0)));
        }

        [Fact]
        public void ParseBlockWithUnclosedTagAtEOFThrowsMissingEndTagException()
        {
            ParseBlockTest("<foo>blah blah blah blah blah",
                new MarkupBlock(
                    Factory.Markup("<foo>blah blah blah blah blah")),
                new RazorError(RazorResources.FormatParseError_MissingEndTag("foo"), new SourceLocation(0, 0, 0)));
        }

        [Fact]
        public void ParseBlockWithUnfinishedTagAtEOFThrowsIncompleteTagException()
        {
            ParseBlockTest("<foo bar=baz",
                new MarkupBlock(
                    Factory.Markup("<foo"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("bar", new LocationTagged<string>(" bar=", 4, 0, 4), new LocationTagged<string>(String.Empty, 12, 0, 12)),
                        Factory.Markup(" bar=").With(SpanCodeGenerator.Null),
                        Factory.Markup("baz").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(string.Empty, 9, 0, 9), new LocationTagged<string>("baz", 9, 0, 9))))),
                new RazorError(RazorResources.FormatParseError_UnfinishedTag("foo"), new SourceLocation(0, 0, 0)));
        }
    }
}
