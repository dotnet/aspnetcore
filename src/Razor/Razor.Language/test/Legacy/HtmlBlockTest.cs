// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlBlockTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void HandlesUnbalancedTripleDashHTMLComments()
        {
            ParseDocumentTest(
@"@{
    <!-- Hello, I'm a comment that shouldn't break razor --->
}");
        }

        [Fact]
        public void HandlesOpenAngleAtEof()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                            + "<");
        }

        [Fact]
        public void HandlesOpenAngleWithProperTagFollowingIt()
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
        public void AllowsStartAndEndTagsToDifferInCase()
        {
            ParseBlockTest("<li><p>Foo</P></lI>");
        }

        [Fact]
        public void ReadsToEndOfLineIfFirstCharacterAfterTransitionIsColon()
        {
            ParseBlockTest("@:<li>Foo Bar Baz" + Environment.NewLine
                         + "bork");
        }

        [Fact]
        public void StopsParsingSingleLineBlockAtEOFIfNoEOLReached()
        {
            ParseBlockTest("@:foo bar");
        }

        [Fact]
        public void StopsAtMatchingCloseTagToStartTag()
        {
            ParseBlockTest("<a><b></b></a><c></c>");
        }

        [Fact]
        public void ParsesUntilMatchingEndTagIfFirstNonWhitespaceCharacterIsStartTag()
        {
            ParseBlockTest("<baz><boz><biz></biz></boz></baz>");
        }

        [Fact]
        public void AllowsUnclosedTagsAsLongAsItCanRecoverToAnExpectedEndTag()
        {
            ParseBlockTest("<foo><bar><baz></foo>");
        }

        [Fact]
        public void WithSelfClosingTagJustEmitsTag()
        {
            ParseBlockTest("<foo />");
        }

        [Fact]
        public void CanHandleSelfClosingTagsWithinBlock()
        {
            ParseBlockTest("<foo><bar /></foo>");
        }

        [Fact]
        public void SupportsTagsWithAttributes()
        {
            ParseBlockTest("<foo bar=\"baz\"><biz><boz zoop=zork/></biz></foo>");
        }

        [Fact]
        public void AllowsCloseAngleBracketInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\">\" /></foo>");
        }

        [Fact]
        public void AllowsCloseAngleBracketInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'>\' /></foo>");
        }

        [Fact]
        public void AllowsSlashInAttributeValueIfDoubleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\"/\"></bar></foo>");
        }

        [Fact]
        public void AllowsSlashInAttributeValueIfSingleQuoted()
        {
            ParseBlockTest("<foo><bar baz=\'/\'></bar></foo>");
        }

        [Fact]
        public void TerminatesAtEOF()
        {
            ParseBlockTest("<foo>");
        }

        [Fact]
        public void SupportsCommentAsBlock()
        {
            ParseBlockTest("<!-- foo -->");
        }

        [Fact]
        public void SupportsCommentWithExtraDashAsBlock()
        {
            ParseBlockTest("<!-- foo --->");
        }

        [Fact]
        public void SupportsCommentWithinBlock()
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
        public void ProperlyBalancesCommentStartAndEndTags()
        {
            ParseBlockTest("<!--<foo></bar>-->");
        }

        [Fact]
        public void TerminatesAtEOFWhenParsingComment()
        {
            ParseBlockTest("<!--<foo>");
        }

        [Fact]
        public void OnlyTerminatesCommentOnFullEndSequence()
        {
            ParseBlockTest("<!--<foo>--</bar>-->");
        }

        [Fact]
        public void TerminatesCommentAtFirstOccurrenceOfEndSequence()
        {
            ParseBlockTest("<foo><!--<foo></bar-->--></foo>");
        }

        [Fact]
        public void TreatsMalformedTagsAsContent()
        {
            ParseBlockTest("<foo></!-- bar --></foo>");
        }


        [Fact]
        public void ParsesSGMLDeclarationAsEmptyTag()
        {
            ParseBlockTest("<foo><!DOCTYPE foo bar baz></foo>");
        }

        [Fact]
        public void TerminatesSGMLDeclarationAtFirstCloseAngle()
        {
            ParseBlockTest("<foo><!DOCTYPE foo bar> baz></foo>");
        }

        [Fact]
        public void ParsesXMLProcessingInstructionAsEmptyTag()
        {
            ParseBlockTest("<foo><?xml foo bar baz?></foo>");
        }

        [Fact]
        public void TerminatesXMLProcessingInstructionAtQuestionMarkCloseAnglePair()
        {
            ParseBlockTest("<foo><?xml foo bar baz?> baz</foo>");
        }

        [Fact]
        public void DoesNotTerminateXMLProcInstrAtCloseAngleUnlessPreceededByQuestionMark()
        {
            // ParseBlockDoesNotTerminateXMLProcessingInstructionAtCloseAngleUnlessPreceededByQuestionMark
            ParseBlockTest("<foo><?xml foo bar> baz?></foo>");
        }

        [Fact]
        public void SupportsScriptTagsWithLessThanSignsInThem()
        {
            ParseBlockTest(@"<script>if(foo<bar) { alert(""baz"");)</script>");
        }

        [Fact]
        public void SupportsScriptTagsWithSpacedLessThanSignsInThem()
        {
            ParseBlockTest(@"<script>if(foo < bar) { alert(""baz"");)</script>");
        }

        [Fact]
        public void AcceptsEmptyTextTag()
        {
            ParseBlockTest("<text/>");
        }

        [Fact]
        public void AcceptsTextTagAsOuterTagButDoesNotRender()
        {
            ParseBlockTest("<text>Foo Bar <foo> Baz</text> zoop");
        }

        [Fact]
        public void RendersLiteralTextTagIfDoubled()
        {
            ParseBlockTest("<text><text>Foo Bar <foo> Baz</text></text> zoop");
        }

        [Fact]
        public void DoesNotConsiderPsuedoTagWithinMarkupBlock()
        {
            ParseBlockTest("<foo><text><bar></bar></foo>");
        }

        [Fact]
        public void StopsParsingMidEmptyTagIfEOFReached()
        {
            ParseBlockTest("<br/");
        }

        [Fact]
        public void CorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
        {
            ParseBlockTest("<div>Foo @if(true) {} Bar</div>");
        }

        [Fact]
        public void IgnoresTagsInContentsOfScriptTag()
        {
            ParseBlockTest(@"<script>foo<bar baz='@boz'></script>");
        }
    }
}
