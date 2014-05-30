// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.Html
{
    public class HtmlBlockTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseBlockMethodThrowsArgNullExceptionOnNullContext()
        {
            // Arrange
            HtmlMarkupParser parser = new HtmlMarkupParser();

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => parser.ParseBlock());
            Assert.Equal(RazorResources.Parser_Context_Not_Set, exception.Message);
        }

        [Fact]
        public void ParseBlockHandlesOpenAngleAtEof()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                            + "<",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement(),
                        new MarkupBlock(
                            Factory.Markup("<")))),
                new RazorError(
                    RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, "}", "{"),
                    1, 0, 1));
        }

        [Fact]
        public void ParseBlockHandlesOpenAngleWithProperTagFollowingIt()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                            + "<" + Environment.NewLine
                            + "</html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement(),
                        new MarkupBlock(
                            Factory.Markup("<\r\n")
                        ),
                        new MarkupBlock(
                            Factory.Markup(@"</html>").Accepts(AcceptedCharacters.None)
                        ),
                        Factory.EmptyCSharp().AsStatement()
                    )
                ),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(RazorResources.FormatParseError_UnexpectedEndTag("html"), 7, 2, 0),
                    new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("code", "}", "{"), 1, 0, 1)
                });
        }

        [Fact]
        public void TagWithoutCloseAngleDoesNotTerminateBlock()
        {
            ParseBlockTest("<                      " + Environment.NewLine
                         + "   ",
                new MarkupBlock(
                    Factory.Markup("<                      \r\n   ")),
                designTimeParser: true,
                expectedErrors: new RazorError(RazorResources.FormatParseError_UnfinishedTag(string.Empty), 0, 0, 0));
        }

        [Fact]
        public void ParseBlockAllowsStartAndEndTagsToDifferInCase()
        {
            SingleSpanBlockTest("<li><p>Foo</P></lI>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockReadsToEndOfLineIfFirstCharacterAfterTransitionIsColon()
        {
            ParseBlockTest("@:<li>Foo Bar Baz" + Environment.NewLine
                         + "bork",
                new MarkupBlock(
                    Factory.MarkupTransition(),
                    Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                    Factory.Markup("<li>Foo Bar Baz\r\n")
                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                ));
        }

        [Fact]
        public void ParseBlockStopsParsingSingleLineBlockAtEOFIfNoEOLReached()
        {
            ParseBlockTest("@:foo bar",
                new MarkupBlock(
                    Factory.MarkupTransition(),
                    Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                    Factory.Markup(@"foo bar")
                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
                    ));
        }

        [Fact]
        public void ParseBlockStopsAtMatchingCloseTagToStartTag()
        {
            SingleSpanBlockTest("<a><b></b></a><c></c>", "<a><b></b></a>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockParsesUntilMatchingEndTagIfFirstNonWhitespaceCharacterIsStartTag()
        {
            SingleSpanBlockTest("<baz><boz><biz></biz></boz></baz>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockAllowsUnclosedTagsAsLongAsItCanRecoverToAnExpectedEndTag()
        {
            SingleSpanBlockTest("<foo><bar><baz></foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockWithSelfClosingTagJustEmitsTag()
        {
            SingleSpanBlockTest("<foo />", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockCanHandleSelfClosingTagsWithinBlock()
        {
            SingleSpanBlockTest("<foo><bar /></foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsTagsWithAttributes()
        {
            ParseBlockTest("<foo bar=\"baz\"><biz><boz zoop=zork/></biz></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("bar", new LocationTagged<string>(" bar=\"", 4, 0, 4), new LocationTagged<string>("\"", 13, 0, 13)),
                        Factory.Markup(" bar=\"").With(SpanCodeGenerator.Null),
                        Factory.Markup("baz").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 10, 0, 10), new LocationTagged<string>("baz", 10, 0, 10))),
                        Factory.Markup("\"").With(SpanCodeGenerator.Null)),
                    Factory.Markup("><biz><boz"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("zoop", new LocationTagged<string>(" zoop=", 24, 0, 24), new LocationTagged<string>(String.Empty, 34, 0, 34)),
                        Factory.Markup(" zoop=").With(SpanCodeGenerator.Null),
                        Factory.Markup("zork").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 30, 0, 30), new LocationTagged<string>("zork", 30, 0, 30)))),
                    Factory.Markup("/></biz></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockAllowsCloseAngleBracketInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\">\" /></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><bar"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("baz", new LocationTagged<string>(" baz=\"", 9, 0, 9), new LocationTagged<string>("\"", 16, 0, 16)),
                        Factory.Markup(" baz=\"").With(SpanCodeGenerator.Null),
                        Factory.Markup(">").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 15, 0, 15), new LocationTagged<string>(">", 15, 0, 15))),
                        Factory.Markup("\"").With(SpanCodeGenerator.Null)),
                    Factory.Markup(" /></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockAllowsCloseAngleBracketInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'>\' /></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><bar"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("baz", new LocationTagged<string>(" baz='", 9, 0, 9), new LocationTagged<string>("'", 16, 0, 16)),
                        Factory.Markup(" baz='").With(SpanCodeGenerator.Null),
                        Factory.Markup(">").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 15, 0, 15), new LocationTagged<string>(">", 15, 0, 15))),
                        Factory.Markup("'").With(SpanCodeGenerator.Null)),
                    Factory.Markup(" /></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockAllowsSlashInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\"/\"></bar></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><bar"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("baz", new LocationTagged<string>(" baz=\"", 9, 0, 9), new LocationTagged<string>("\"", 16, 0, 16)),
                        Factory.Markup(" baz=\"").With(SpanCodeGenerator.Null),
                        Factory.Markup("/").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 15, 0, 15), new LocationTagged<string>("/", 15, 0, 15))),
                        Factory.Markup("\"").With(SpanCodeGenerator.Null)),
                    Factory.Markup("></bar></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockAllowsSlashInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'/\'></bar></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><bar"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("baz", new LocationTagged<string>(" baz='", 9, 0, 9), new LocationTagged<string>("'", 16, 0, 16)),
                        Factory.Markup(" baz='").With(SpanCodeGenerator.Null),
                        Factory.Markup("/").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 15, 0, 15), new LocationTagged<string>("/", 15, 0, 15))),
                        Factory.Markup("'").With(SpanCodeGenerator.Null)),
                    Factory.Markup("></bar></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockTerminatesAtEOF()
        {
            SingleSpanBlockTest("<foo>", "<foo>", BlockType.Markup, SpanKind.Markup,
                                new RazorError(RazorResources.FormatParseError_MissingEndTag("foo"), new SourceLocation(0, 0, 0)));
        }

        [Fact]
        public void ParseBlockSupportsCommentAsBlock()
        {
            SingleSpanBlockTest("<!-- foo -->", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsCommentWithinBlock()
        {
            SingleSpanBlockTest("<foo>bar<!-- zoop -->baz</foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockProperlyBalancesCommentStartAndEndTags()
        {
            SingleSpanBlockTest("<!--<foo></bar>-->", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockTerminatesAtEOFWhenParsingComment()
        {
            SingleSpanBlockTest("<!--<foo>", "<!--<foo>", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseBlockOnlyTerminatesCommentOnFullEndSequence()
        {
            SingleSpanBlockTest("<!--<foo>--</bar>-->", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockTerminatesCommentAtFirstOccurrenceOfEndSequence()
        {
            SingleSpanBlockTest("<foo><!--<foo></bar-->--></foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockTreatsMalformedTagsAsContent()
        {
            SingleSpanBlockTest(
                "<foo></!-- bar --></foo>",
                "<foo></!-- bar -->",
                BlockType.Markup,
                SpanKind.Markup,
                AcceptedCharacters.None,
                new RazorError(RazorResources.FormatParseError_MissingEndTag("foo"), 0, 0, 0));
        }


        [Fact]
        public void ParseBlockParsesSGMLDeclarationAsEmptyTag()
        {
            SingleSpanBlockTest("<foo><!DOCTYPE foo bar baz></foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockTerminatesSGMLDeclarationAtFirstCloseAngle()
        {
            SingleSpanBlockTest("<foo><!DOCTYPE foo bar> baz></foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockParsesXMLProcessingInstructionAsEmptyTag()
        {
            SingleSpanBlockTest("<foo><?xml foo bar baz?></foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockTerminatesXMLProcessingInstructionAtQuestionMarkCloseAnglePair()
        {
            SingleSpanBlockTest("<foo><?xml foo bar?> baz</foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockDoesNotTerminateXMLProcessingInstructionAtCloseAngleUnlessPreceededByQuestionMark()
        {
            SingleSpanBlockTest("<foo><?xml foo bar> baz?></foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsScriptTagsWithLessThanSignsInThem()
        {
            SingleSpanBlockTest(@"<script>if(foo<bar) { alert(""baz"");)</script>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockSupportsScriptTagsWithSpacedLessThanSignsInThem()
        {
            SingleSpanBlockTest(@"<script>if(foo < bar) { alert(""baz"");)</script>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockAcceptsEmptyTextTag()
        {
            ParseBlockTest("<text/>",
                new MarkupBlock(
                    Factory.MarkupTransition("<text/>")
                ));
        }

        [Fact]
        public void ParseBlockAcceptsTextTagAsOuterTagButDoesNotRender()
        {
            ParseBlockTest("<text>Foo Bar <foo> Baz</text> zoop",
                new MarkupBlock(
                    Factory.MarkupTransition("<text>"),
                    Factory.Markup("Foo Bar <foo> Baz"),
                    Factory.MarkupTransition("</text>"),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)
                ));
        }

        [Fact]
        public void ParseBlockRendersLiteralTextTagIfDoubled()
        {
            ParseBlockTest("<text><text>Foo Bar <foo> Baz</text></text> zoop",
                new MarkupBlock(
                    Factory.MarkupTransition("<text>"),
                    Factory.Markup("<text>Foo Bar <foo> Baz</text>"),
                    Factory.MarkupTransition("</text>"),
                    Factory.Markup(" ").Accepts(AcceptedCharacters.None)
                ));
        }

        [Fact]
        public void ParseBlockDoesNotConsiderPsuedoTagWithinMarkupBlock()
        {
            ParseBlockTest("<foo><text><bar></bar></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><text><bar></bar></foo>").Accepts(AcceptedCharacters.None)
                ));
        }

        [Fact]
        public void ParseBlockStopsParsingMidEmptyTagIfEOFReached()
        {
            ParseBlockTest("<br/",
                new MarkupBlock(
                    Factory.Markup("<br/")
                ),
                new RazorError(RazorResources.FormatParseError_UnfinishedTag("br"), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockCorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
        {
            ParseBlockTest("<div>Foo @if(true) {} Bar</div>",
                new MarkupBlock(
                    Factory.Markup("<div>Foo "),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("if(true) {}").AsStatement()),
                    Factory.Markup(" Bar</div>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockIgnoresTagsInContentsOfScriptTag()
        {
            ParseBlockTest(@"<script>foo<bar baz='@boz'></script>",
                new MarkupBlock(
                    Factory.Markup("<script>foo<bar baz='"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("boz")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup("'></script>")
                           .Accepts(AcceptedCharacters.None)));
        }
    }
}
