// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlDocumentTest : CsHtmlMarkupParserTestBase
    {
        private static readonly TestFile Nested1000 = TestFile.Create("TestFiles/nested-1000.html", typeof(HtmlDocumentTest));

        public HtmlDocumentTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void ParseDocument_NestedCodeBlockWithMarkupSetsDotAsMarkup()
        {
            ParseDocumentTest("@if (true) { @if(false) { <div>@something.</div> } }");
        }

        [Fact]
        public void ParseDocumentOutputsEmptyBlockWithEmptyMarkupSpanIfContentIsEmptyString()
        {
            ParseDocumentTest(string.Empty);
        }

        [Fact]
        public void ParseDocumentOutputsWhitespaceOnlyContentAsSingleWhitespaceMarkupSpan()
        {
            ParseDocumentTest("          ");
        }

        [Fact]
        public void ParseDocumentAcceptsSwapTokenAtEndOfFileAndOutputsZeroLengthCodeSpan()
        {
            ParseDocumentTest("@");
        }

        [Fact]
        public void ParseDocumentCorrectlyHandlesOddlySpacedHTMLElements()
        {
            ParseDocumentTest("<div ><p class = 'bar'> Foo </p></div >");
        }

        [Fact]
        public void ParseDocumentCorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
        {
            ParseDocumentTest("<div>Foo @if(true) {} Bar</div>");
        }

        [Fact]
        public void ParseDocumentWithinSectionDoesNotCreateDocumentLevelSpan()
        {
            ParseDocumentTest("@section Foo {" + Environment.NewLine
                            + "    <html></html>" + Environment.NewLine
                            + "}",
                new[] { SectionDirective.Directive, });
        }

        [Fact]
        public void ParseDocumentParsesWholeContentAsOneSpanIfNoSwapCharacterEncountered()
        {
            ParseDocumentTest("foo baz");
        }

        [Fact]
        public void ParseDocumentHandsParsingOverToCodeParserWhenAtSignEncounteredAndEmitsOutput()
        {
            ParseDocumentTest("foo @bar baz");
        }

        [Fact]
        public void ParseDocumentEmitsAtSignAsMarkupIfAtEndOfFile()
        {
            ParseDocumentTest("foo @");
        }

        [Fact]
        public void ParseDocumentEmitsCodeBlockIfFirstCharacterIsSwapCharacter()
        {
            ParseDocumentTest("@bar");
        }

        [Fact]
        public void ParseDocumentDoesNotSwitchToCodeOnEmailAddressInText()
        {
            ParseDocument("example@microsoft.com");
        }

        [Fact]
        public void ParseDocumentDoesNotSwitchToCodeOnEmailAddressInAttribute()
        {
            ParseDocumentTest("<a href=\"mailto:example@microsoft.com\">Email me</a>");
        }

        [Fact]
        public void ParseDocumentDoesNotReturnErrorOnMismatchedTags()
        {
            ParseDocumentTest("Foo <div><p></p></p> Baz");
        }

        [Fact]
        public void ParseDocumentReturnsOneMarkupSegmentIfNoCodeBlocksEncountered()
        {
            ParseDocumentTest("Foo Baz<!--Foo-->Bar<!--F> Qux");
        }

        [Fact]
        public void ParseDocumentRendersTextPseudoTagAsMarkup()
        {
            ParseDocumentTest("Foo <text>Foo</text>");
        }

        [Fact]
        public void ParseDocumentAcceptsEndTagWithNoMatchingStartTag()
        {
            ParseDocumentTest("Foo </div> Bar");
        }

        [Fact]
        public void ParseDocumentNoLongerSupportsDollarOpenBraceCombination()
        {
            ParseDocumentTest("<foo>${bar}</foo>");
        }

        [Fact]
        public void ParseDocumentIgnoresTagsInContentsOfScriptTag()
        {
            ParseDocumentTest(@"<script>foo<bar baz='@boz'></script>");
        }

        [Fact]
        public void ParseDocumentDoesNotRenderExtraNewLineAtTheEndOfVerbatimBlock()
        {
            ParseDocumentTest("@{\r\n}\r\n<html>");
        }

        [Fact]
        public void ParseDocumentDoesNotRenderExtraWhitespaceAndNewLineAtTheEndOfVerbatimBlock()
        {
            ParseDocumentTest("@{\r\n} \t\r\n<html>");
        }

        [Fact]
        public void ParseDocumentDoesNotRenderExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByCSharp()
        {
            ParseDocumentTest("@{<text>Blah</text>\r\n\r\n}<html>");
        }

        [Fact]
        public void ParseDocumentRendersExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByHtml()
        {
            ParseDocumentTest("@{<text>Blah</text>\r\n<input/>\r\n}<html>");
        }

        [Fact]
        public void ParseDocumentRendersExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByMarkupTransition()
        {
            ParseDocumentTest("@{<text>Blah</text>\r\n@: Bleh\r\n}<html>");
        }

        [Fact]
        public void ParseDocumentDoesNotIgnoreNewLineAtTheEndOfMarkupBlock()
        {
            ParseDocumentTest("@{\r\n}\r\n<html>\r\n");
        }

        [Fact]
        public void ParseDocumentDoesNotIgnoreWhitespaceAtTheEndOfVerbatimBlockIfNoNewlinePresent()
        {
            ParseDocumentTest("@{\r\n}   \t<html>\r\n");
        }

        [Fact]
        public void ParseDocumentHandlesNewLineInNestedBlock()
        {
            ParseDocumentTest("@{\r\n@if(true){\r\n} \r\n}\r\n<html>");
        }

        [Fact]
        public void ParseDocumentHandlesNewLineAndMarkupInNestedBlock()
        {
            ParseDocumentTest("@{\r\n@if(true){\r\n} <input> }");
        }

        [Fact]
        public void ParseDocumentHandlesExtraNewLineBeforeMarkupInNestedBlock()
        {
            ParseDocumentTest("@{\r\n@if(true){\r\n} \r\n<input> \r\n}<html>");
        }

        [Fact]
        public void ParseSectionIgnoresTagsInContentsOfScriptTag()
        {
            ParseDocumentTest(
                @"@section Foo { <script>foo<bar baz='@boz'></script> }",
                new[] { SectionDirective.Directive, });
        }

        [Fact]
        public void ParseBlockCanParse1000NestedElements()
        {
            var content = Nested1000.ReadAllText();

            // Assert - does not throw
            ParseDocument(content);
        }

        [Fact]
        public void ParseDocument_WithDoubleTransitionInAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='@@' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void ParseDocument_WithDoubleTransitionAtEndOfAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='abc@@' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void ParseDocument_WithDoubleTransitionAtBeginningOfAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='@@def' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void ParseDocument_WithDoubleTransitionBetweenAttributeValue_DoesNotThrow()
        {
            var input = "{<span foo='abc @@ def' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void ParseDocument_WithDoubleTransitionWithExpressionBlock_DoesNotThrow()
        {
            var input = "{<span foo='@@@(2+3)' bar='@(2+3)@@@DateTime.Now' baz='@DateTime.Now@@' bat='@DateTime.Now @@' zoo='@@@DateTime.Now' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void ParseDocument_WithDoubleTransitionInEmail_DoesNotThrow()
        {
            var input = "{<span foo='abc@def.com abc@@def.com @@' />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void ParseDocument_WithDoubleTransitionInRegex_DoesNotThrow()
        {
            var input = @"{<span foo=""/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+@@[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i"" />}";
            ParseDocumentTest(input);
        }

        [Fact]
        public void ParseDocument_WithUnexpectedTransitionsInAttributeValue_Throws()
        {
            ParseDocumentTest("<span foo='@ @' />");
        }
    }
}
