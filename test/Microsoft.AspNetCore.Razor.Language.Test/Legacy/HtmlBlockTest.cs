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
}");
        }

        [Fact]
        public void ParseBlockHandlesOpenAngleAtEof()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                            + "<");
        }

        [Fact]
        public void ParseBlockHandlesOpenAngleWithProperTagFollowingIt()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                            + "<" + Environment.NewLine
                            + "</html>",
                            designTime: true);
        }

        [Fact]
        public void TagWithoutCloseAngleDoesNotTerminateBlock()
        {
            ParseBlockTest("<                      " + Environment.NewLine
                         + "   ");
        }

        [Fact]
        public void ParseBlockAllowsStartAndEndTagsToDifferInCase()
        {
            ParseBlockTest("<li><p>Foo</P></lI>");
        }

        [Fact]
        public void ParseBlockReadsToEndOfLineIfFirstCharacterAfterTransitionIsColon()
        {
            ParseBlockTest("@:<li>Foo Bar Baz" + Environment.NewLine
                         + "bork");
        }

        [Fact]
        public void ParseBlockStopsParsingSingleLineBlockAtEOFIfNoEOLReached()
        {
            ParseBlockTest("@:foo bar");
        }

        [Fact]
        public void ParseBlockStopsAtMatchingCloseTagToStartTag()
        {
            ParseBlockTest("<a><b></b></a><c></c>");
        }

        [Fact]
        public void ParseBlockParsesUntilMatchingEndTagIfFirstNonWhitespaceCharacterIsStartTag()
        {
            ParseBlockTest("<baz><boz><biz></biz></boz></baz>");
        }

        [Fact]
        public void ParseBlockAllowsUnclosedTagsAsLongAsItCanRecoverToAnExpectedEndTag()
        {
            ParseBlockTest("<foo><bar><baz></foo>");
        }

        [Fact]
        public void ParseBlockWithSelfClosingTagJustEmitsTag()
        {
            ParseBlockTest("<foo />");
        }

        [Fact]
        public void ParseBlockCanHandleSelfClosingTagsWithinBlock()
        {
            ParseBlockTest("<foo><bar /></foo>");
        }

        [Fact]
        public void ParseBlockSupportsTagsWithAttributes()
        {
            ParseBlockTest("<foo bar=\"baz\"><biz><boz zoop=zork/></biz></foo>");
        }

        [Fact]
        public void ParseBlockAllowsCloseAngleBracketInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\">\" /></foo>");
        }

        [Fact]
        public void ParseBlockAllowsCloseAngleBracketInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'>\' /></foo>");
        }

        [Fact]
        public void ParseBlockAllowsSlashInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\"/\"></bar></foo>");
        }

        [Fact]
        public void ParseBlockAllowsSlashInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'/\'></bar></foo>");
        }

        [Fact]
        public void ParseBlockTerminatesAtEOF()
        {
            ParseBlockTest("<foo>");
        }

        [Fact]
        public void ParseBlockSupportsCommentAsBlock()
        {
            ParseBlockTest("<!-- foo -->");
        }

        [Fact]
        public void ParseBlockSupportsCommentWithExtraDashAsBlock()
        {
            ParseBlockTest("<!-- foo --->");
        }

        [Fact]
        public void ParseBlockSupportsCommentWithinBlock()
        {
            ParseBlockTest("<foo>bar<!-- zoop -->baz</foo>");
        }

        [Fact]
        public void HtmlCommentSupportsMultipleDashes()
        {
            ParseDocumentTest(
@"<div><!--- Hello World ---></div>
<div><!---- Hello World ----></div>
<div><!----- Hello World -----></div>
<div><!----- Hello < --- > World </div> -----></div>
");
        }

        [Fact]
        public void ParseBlockProperlyBalancesCommentStartAndEndTags()
        {
            ParseBlockTest("<!--<foo></bar>-->");
        }

        [Fact]
        public void ParseBlockTerminatesAtEOFWhenParsingComment()
        {
            ParseBlockTest("<!--<foo>");
        }

        [Fact]
        public void ParseBlockOnlyTerminatesCommentOnFullEndSequence()
        {
            ParseBlockTest("<!--<foo>--</bar>-->");
        }

        [Fact]
        public void ParseBlockTerminatesCommentAtFirstOccurrenceOfEndSequence()
        {
            ParseBlockTest("<foo><!--<foo></bar-->--></foo>");
        }

        [Fact]
        public void ParseBlockTreatsMalformedTagsAsContent()
        {
            ParseBlockTest("<foo></!-- bar --></foo>");
        }


        [Fact]
        public void ParseBlockParsesSGMLDeclarationAsEmptyTag()
        {
            ParseBlockTest("<foo><!DOCTYPE foo bar baz></foo>");
        }

        [Fact]
        public void ParseBlockTerminatesSGMLDeclarationAtFirstCloseAngle()
        {
            ParseBlockTest("<foo><!DOCTYPE foo bar> baz></foo>");
        }

        [Fact]
        public void ParseBlockParsesXMLProcessingInstructionAsEmptyTag()
        {
            ParseBlockTest("<foo><?xml foo bar baz?></foo>");
        }

        [Fact]
        public void ParseBlockTerminatesXMLProcessingInstructionAtQuestionMarkCloseAnglePair()
        {
            ParseBlockTest("<foo><?xml foo bar baz?> baz</foo>");
        }

        [Fact]
        public void ParseBlockDoesNotTerminateXMLProcessingInstructionAtCloseAngleUnlessPreceededByQuestionMark()
        {
            ParseBlockTest("<foo><?xml foo bar> baz?></foo>");
        }

        [Fact]
        public void ParseBlockSupportsScriptTagsWithLessThanSignsInThem()
        {
            ParseBlockTest(@"<script>if(foo<bar) { alert(""baz"");)</script>");
        }

        [Fact]
        public void ParseBlockSupportsScriptTagsWithSpacedLessThanSignsInThem()
        {
            ParseBlockTest(@"<script>if(foo < bar) { alert(""baz"");)</script>");
        }

        [Fact]
        public void ParseBlockAcceptsEmptyTextTag()
        {
            ParseBlockTest("<text/>");
        }

        [Fact]
        public void ParseBlockAcceptsTextTagAsOuterTagButDoesNotRender()
        {
            ParseBlockTest("<text>Foo Bar <foo> Baz</text> zoop");
        }

        [Fact]
        public void ParseBlockRendersLiteralTextTagIfDoubled()
        {
            ParseBlockTest("<text><text>Foo Bar <foo> Baz</text></text> zoop");
        }

        [Fact]
        public void ParseBlockDoesNotConsiderPsuedoTagWithinMarkupBlock()
        {
            ParseBlockTest("<foo><text><bar></bar></foo>");
        }

        [Fact]
        public void ParseBlockStopsParsingMidEmptyTagIfEOFReached()
        {
            ParseBlockTest("<br/");
        }

        [Fact]
        public void ParseBlockCorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
        {
            ParseBlockTest("<div>Foo @if(true) {} Bar</div>");
        }

        [Fact]
        public void ParseBlockIgnoresTagsInContentsOfScriptTag()
        {
            ParseBlockTest(@"<script>foo<bar baz='@boz'></script>");
        }
    }
}
