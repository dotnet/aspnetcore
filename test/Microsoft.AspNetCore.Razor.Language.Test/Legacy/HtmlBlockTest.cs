// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlBlockTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseBlockHandlesUnbalancedTripleDashHTMLComments()
        {
            ParseDocumentTest(
@"@{
    <!-- Hello, I'm a comment that shouldn't break razor --->
}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        Factory.Code(Environment.NewLine).AsStatement().AutoCompleteWith(null),
                        new MarkupBlock(
                            Factory.Markup("    "),
                            Factory.Markup("<!-- Hello, I'm a comment that shouldn't break razor --->").Accepts(AcceptedCharactersInternal.None),
                            Factory.Markup(Environment.NewLine).Accepts(AcceptedCharactersInternal.None)),
                        Factory.EmptyCSharp().AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()),
                new RazorDiagnostic[0]);
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
                        Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        Factory.Code(Environment.NewLine)
                            .AsStatement()
                            .AutoCompleteWith("}"),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.Markup("<"))))),
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), LegacyResources.BlockName_Code, "}", "{"));
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
                        Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        Factory.Code(Environment.NewLine)
                            .AsStatement()
                            .AutoCompleteWith("}"),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.Markup("<" + Environment.NewLine))
                        ),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.Markup("</html>").Accepts(AcceptedCharactersInternal.None))
                        ),
                        Factory.EmptyCSharp().AsStatement()
                    )
                ),
                designTime: true,
                expectedErrors: new[]
                {
                    RazorDiagnostic.Create(new RazorError(
                        LegacyResources.FormatParseError_UnexpectedEndTag("html"),
                        new SourceLocation(5 + Environment.NewLine.Length * 2, 2, 2),
                        length: 4)),
                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                        new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), LegacyResources.BlockName_Code, "}", "{"),
                });
        }

        [Fact]
        public void TagWithoutCloseAngleDoesNotTerminateBlock()
        {
            ParseBlockTest("<                      " + Environment.NewLine
                         + "   ",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup($"<                      {Environment.NewLine}   "))),
                designTime: true,
                expectedErrors: RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_UnfinishedTag(string.Empty),
                    new SourceLocation(1, 0, 1),
                    length: 1)));
        }

        [Fact]
        public void ParseBlockAllowsStartAndEndTagsToDifferInCase()
        {
            ParseBlockTest("<li><p>Foo</P></lI>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<li>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<p>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("Foo"),
                    new MarkupTagBlock(
                        Factory.Markup("</P>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</lI>").Accepts(AcceptedCharactersInternal.None))
                    ));
        }

        [Fact]
        public void ParseBlockReadsToEndOfLineIfFirstCharacterAfterTransitionIsColon()
        {
            ParseBlockTest("@:<li>Foo Bar Baz" + Environment.NewLine
                         + "bork",
                new MarkupBlock(
                    Factory.MarkupTransition(),
                    Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                    Factory.Markup("<li>Foo Bar Baz" + Environment.NewLine)
                           .With(new SpanEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharactersInternal.None))
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
                           .With(new SpanEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
                    ));
        }

        [Fact]
        public void ParseBlockStopsAtMatchingCloseTagToStartTag()
        {
            ParseBlockTest("<a><b></b></a><c></c>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<b>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</b>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</a>").Accepts(AcceptedCharactersInternal.None))
                    ));
        }

        [Fact]
        public void ParseBlockParsesUntilMatchingEndTagIfFirstNonWhitespaceCharacterIsStartTag()
        {
            ParseBlockTest("<baz><boz><biz></biz></boz></baz>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<baz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<boz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<biz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</biz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</boz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</baz>").Accepts(AcceptedCharactersInternal.None))
                    ));
        }

        [Fact]
        public void ParseBlockAllowsUnclosedTagsAsLongAsItCanRecoverToAnExpectedEndTag()
        {
            ParseBlockTest("<foo><bar><baz></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<bar>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<baz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))
                    ));
        }

        [Fact]
        public void ParseBlockWithSelfClosingTagJustEmitsTag()
        {
            ParseBlockTest("<foo />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo />").Accepts(AcceptedCharactersInternal.None))
                    ));
        }

        [Fact]
        public void ParseBlockCanHandleSelfClosingTagsWithinBlock()
        {
            ParseBlockTest("<foo><bar /></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<bar />").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))
                    ));
        }

        [Fact]
        public void ParseBlockSupportsTagsWithAttributes()
        {
            ParseBlockTest("<foo bar=\"baz\"><biz><boz zoop=zork/></biz></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("bar", new LocationTagged<string>(" bar=\"", 4, 0, 4), new LocationTagged<string>("\"", 13, 0, 13)),
                            Factory.Markup(" bar=\"").With(SpanChunkGenerator.Null),
                            Factory.Markup("baz").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 10, 0, 10), new LocationTagged<string>("baz", 10, 0, 10))),
                            Factory.Markup("\"").With(SpanChunkGenerator.Null)),
                        Factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<biz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<boz"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("zoop", new LocationTagged<string>(" zoop=", 24, 0, 24), new LocationTagged<string>(string.Empty, 34, 0, 34)),
                            Factory.Markup(" zoop=").With(SpanChunkGenerator.Null),
                            Factory.Markup("zork").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 30, 0, 30), new LocationTagged<string>("zork", 30, 0, 30)))),
                        Factory.Markup("/>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</biz>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockAllowsCloseAngleBracketInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\">\" /></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<bar"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("baz", new LocationTagged<string>(" baz=\"", 9, 0, 9), new LocationTagged<string>("\"", 16, 0, 16)),
                            Factory.Markup(" baz=\"").With(SpanChunkGenerator.Null),
                            Factory.Markup(">").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>(">", 15, 0, 15))),
                            Factory.Markup("\"").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockAllowsCloseAngleBracketInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'>\' /></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<bar"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("baz", new LocationTagged<string>(" baz='", 9, 0, 9), new LocationTagged<string>("'", 16, 0, 16)),
                            Factory.Markup(" baz='").With(SpanChunkGenerator.Null),
                            Factory.Markup(">").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>(">", 15, 0, 15))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockAllowsSlashInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\"/\"></bar></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<bar"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("baz", new LocationTagged<string>(" baz=\"", 9, 0, 9), new LocationTagged<string>("\"", 16, 0, 16)),
                            Factory.Markup(" baz=\"").With(SpanChunkGenerator.Null),
                            Factory.Markup("/").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>("/", 15, 0, 15))),
                            Factory.Markup("\"").With(SpanChunkGenerator.Null)),
                        Factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</bar>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockAllowsSlashInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'/\'></bar></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<bar"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("baz", new LocationTagged<string>(" baz='", 9, 0, 9), new LocationTagged<string>("'", 16, 0, 16)),
                            Factory.Markup(" baz='").With(SpanChunkGenerator.Null),
                            Factory.Markup("/").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 15, 0, 15), new LocationTagged<string>("/", 15, 0, 15))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</bar>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockTerminatesAtEOF()
        {
            ParseBlockTest("<foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_MissingEndTag("foo"),
                    new SourceLocation(1, 0, 1),
                    length: 3)));
        }

        [Fact]
        public void ParseBlockSupportsCommentAsBlock()
        {
            SingleSpanBlockTest("<!-- foo -->", BlockKindInternal.Markup, SpanKindInternal.Markup, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockSupportsCommentWithinBlock()
        {
            ParseBlockTest("<foo>bar<!-- zoop -->baz</foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("bar"),
                    Factory.Markup("<!-- zoop -->").Accepts(AcceptedCharactersInternal.None),
                    Factory.Markup("baz"),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        public static TheoryData HtmlCommentSupportsMultipleDashesData
        {
            get
            {
                var factory = new SpanFactory();

                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<div><!--- Hello World ---></div>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div>").Accepts(AcceptedCharactersInternal.None)),
                            factory.Markup("<!--- Hello World --->").Accepts(AcceptedCharactersInternal.None),
                            new MarkupTagBlock(
                                factory.Markup("</div>").Accepts(AcceptedCharactersInternal.None)))
                    },
                    {
                        "<div><!---- Hello World ----></div>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div>").Accepts(AcceptedCharactersInternal.None)),
                            factory.Markup("<!---- Hello World ---->").Accepts(AcceptedCharactersInternal.None),
                            new MarkupTagBlock(
                                factory.Markup("</div>").Accepts(AcceptedCharactersInternal.None)))
                    },
                    {
                        "<div><!----- Hello World -----></div>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div>").Accepts(AcceptedCharactersInternal.None)),
                            factory.Markup("<!----- Hello World ----->").Accepts(AcceptedCharactersInternal.None),
                            new MarkupTagBlock(
                                factory.Markup("</div>").Accepts(AcceptedCharactersInternal.None)))
                    },
                    {
                        "<div><!----- Hello < --- > World </div> -----></div>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div>").Accepts(AcceptedCharactersInternal.None)),
                            factory.Markup("<!----- Hello < --- > World </div> ----->").Accepts(AcceptedCharactersInternal.None),
                            new MarkupTagBlock(
                                factory.Markup("</div>").Accepts(AcceptedCharactersInternal.None)))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(HtmlCommentSupportsMultipleDashesData))]
        public void HtmlCommentSupportsMultipleDashes(string documentContent, object expectedOutput)
        {
            FixupSpans = true;

            ParseBlockTest(documentContent, (MarkupBlock)expectedOutput);
        }

        [Fact]
        public void ParseBlockProperlyBalancesCommentStartAndEndTags()
        {
            SingleSpanBlockTest("<!--<foo></bar>-->", BlockKindInternal.Markup, SpanKindInternal.Markup, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockTerminatesAtEOFWhenParsingComment()
        {
            SingleSpanBlockTest("<!--<foo>", "<!--<foo>", BlockKindInternal.Markup, SpanKindInternal.Markup);
        }

        [Fact]
        public void ParseBlockOnlyTerminatesCommentOnFullEndSequence()
        {
            SingleSpanBlockTest("<!--<foo>--</bar>-->", BlockKindInternal.Markup, SpanKindInternal.Markup, acceptedCharacters: AcceptedCharactersInternal.None);
        }

        [Fact]
        public void ParseBlockTerminatesCommentAtFirstOccurrenceOfEndSequence()
        {
            ParseBlockTest("<foo><!--<foo></bar-->--></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("<!--<foo></bar-->").Accepts(AcceptedCharactersInternal.None),
                    Factory.Markup("-->"),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockTreatsMalformedTagsAsContent()
        {
            ParseBlockTest("<foo></!-- bar --></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</!-- bar -->").Accepts(AcceptedCharactersInternal.None))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_MissingEndTag("foo"),
                    new SourceLocation(1, 0, 1),
                    length: 3)));
        }


        [Fact]
        public void ParseBlockParsesSGMLDeclarationAsEmptyTag()
        {
            ParseBlockTest("<foo><!DOCTYPE foo bar baz></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("<!DOCTYPE foo bar baz>").Accepts(AcceptedCharactersInternal.None),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockTerminatesSGMLDeclarationAtFirstCloseAngle()
        {
            ParseBlockTest("<foo><!DOCTYPE foo bar> baz></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("<!DOCTYPE foo bar>").Accepts(AcceptedCharactersInternal.None),
                    Factory.Markup(" baz>"),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockParsesXMLProcessingInstructionAsEmptyTag()
        {
            ParseBlockTest("<foo><?xml foo bar baz?></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("<?xml foo bar baz?>").Accepts(AcceptedCharactersInternal.None),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockTerminatesXMLProcessingInstructionAtQuestionMarkCloseAnglePair()
        {
            ParseBlockTest("<foo><?xml foo bar baz?> baz</foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("<?xml foo bar baz?>").Accepts(AcceptedCharactersInternal.None),
                    Factory.Markup(" baz"),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockDoesNotTerminateXMLProcessingInstructionAtCloseAngleUnlessPreceededByQuestionMark()
        {
            ParseBlockTest("<foo><?xml foo bar> baz?></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("<?xml foo bar> baz?>").Accepts(AcceptedCharactersInternal.None),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockSupportsScriptTagsWithLessThanSignsInThem()
        {
            ParseBlockTest(@"<script>if(foo<bar) { alert(""baz"");)</script>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<script>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(@"if(foo<bar) { alert(""baz"");)"),
                    new MarkupTagBlock(
                        Factory.Markup("</script>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockSupportsScriptTagsWithSpacedLessThanSignsInThem()
        {
            ParseBlockTest(@"<script>if(foo < bar) { alert(""baz"");)</script>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<script>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(@"if(foo < bar) { alert(""baz"");)"),
                    new MarkupTagBlock(
                        Factory.Markup("</script>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockAcceptsEmptyTextTag()
        {
            ParseBlockTest("<text/>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text/>"))
                ));
        }

        [Fact]
        public void ParseBlockAcceptsTextTagAsOuterTagButDoesNotRender()
        {
            ParseBlockTest("<text>Foo Bar <foo> Baz</text> zoop",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text>")),
                    Factory.Markup("Foo Bar ").Accepts(AcceptedCharactersInternal.None),
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(" Baz"),
                    new MarkupTagBlock(
                        Factory.MarkupTransition("</text>"))));
        }

        [Fact]
        public void ParseBlockRendersLiteralTextTagIfDoubled()
        {
            ParseBlockTest("<text><text>Foo Bar <foo> Baz</text></text> zoop",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text>")),
                    new MarkupTagBlock(
                        Factory.Markup("<text>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("Foo Bar "),
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(" Baz"),
                    new MarkupTagBlock(
                        Factory.Markup("</text>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.MarkupTransition("</text>"))));
        }

        [Fact]
        public void ParseBlockDoesNotConsiderPsuedoTagWithinMarkupBlock()
        {
            ParseBlockTest("<foo><text><bar></bar></foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<foo>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<text>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<bar>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</bar>").Accepts(AcceptedCharactersInternal.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</foo>").Accepts(AcceptedCharactersInternal.None))
                ));
        }

        [Fact]
        public void ParseBlockStopsParsingMidEmptyTagIfEOFReached()
        {
            ParseBlockTest("<br/",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<br/"))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_UnfinishedTag("br"),
                    new SourceLocation(1, 0, 1),
                    length: 2)));
        }

        [Fact]
        public void ParseBlockCorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
        {
            ParseBlockTest("<div>Foo @if(true) {} Bar</div>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<div>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("Foo "),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("if(true) {}").AsStatement()),
                    Factory.Markup(" Bar"),
                    new MarkupTagBlock(
                        Factory.Markup("</div>").Accepts(AcceptedCharactersInternal.None))));
        }

        [Fact]
        public void ParseBlockIgnoresTagsInContentsOfScriptTag()
        {
            ParseBlockTest(@"<script>foo<bar baz='@boz'></script>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<script>").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("foo<bar baz='"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("boz")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    Factory.Markup("'>"),
                    new MarkupTagBlock(
                        Factory.Markup("</script>").Accepts(AcceptedCharactersInternal.None))));
        }
    }
}
