// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.Html
{
    public class HtmlDocumentTest : CsHtmlMarkupParserTestBase
    {
        private static readonly TestFile Nested1000 = TestFile.Create("nested-1000.html");

        [Fact]
        public void ParseDocumentMethodThrowsArgNullExceptionOnNullContext()
        {
            // Arrange
            HtmlMarkupParser parser = new HtmlMarkupParser();

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => parser.ParseDocument(), RazorResources.Parser_Context_Not_Set);
        }

        [Fact]
        public void ParseSectionMethodThrowsArgNullExceptionOnNullContext()
        {
            // Arrange
            HtmlMarkupParser parser = new HtmlMarkupParser();

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => parser.ParseSection(null, true), RazorResources.Parser_Context_Not_Set);
        }

        [Fact]
        public void ParseDocumentOutputsEmptyBlockWithEmptyMarkupSpanIfContentIsEmptyString()
        {
            ParseDocumentTest(String.Empty, new MarkupBlock(Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseDocumentOutputsWhitespaceOnlyContentAsSingleWhitespaceMarkupSpan()
        {
            SingleSpanDocumentTest("          ", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentAcceptsSwapTokenAtEndOfFileAndOutputsZeroLengthCodeSpan()
        {
            ParseDocumentTest("@",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.EmptyCSharp()
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.EmptyHtml()),
                new RazorError(RazorResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock, 1, 0, 1));
        }

        [Fact]
        public void ParseDocumentCorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
        {
            ParseDocumentTest("<div>Foo @if(true) {} Bar</div>",
                new MarkupBlock(
                    Factory.Markup("<div>Foo "),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("if(true) {}").AsStatement()),
                    Factory.Markup(" Bar</div>")));
        }

        [Fact]
        public void ParseDocumentWithinSectionDoesNotCreateDocumentLevelSpan()
        {
            ParseDocumentTest("@section Foo {" + Environment.NewLine
                            + "    <html></html>" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionCodeGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup("\r\n    <html></html>\r\n")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseDocumentParsesWholeContentAsOneSpanIfNoSwapCharacterEncountered()
        {
            SingleSpanDocumentTest("foo <bar>baz</bar>", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentHandsParsingOverToCodeParserWhenAtSignEncounteredAndEmitsOutput()
        {
            ParseDocumentTest("foo @bar baz",
                new MarkupBlock(
                    Factory.Markup("foo "),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" baz")));
        }

        [Fact]
        public void ParseDocumentEmitsAtSignAsMarkupIfAtEndOfFile()
        {
            ParseDocumentTest("foo @",
                new MarkupBlock(
                    Factory.Markup("foo "),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.EmptyCSharp()
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.EmptyHtml()),
                new RazorError(RazorResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock, 5, 0, 5));
        }

        [Fact]
        public void ParseDocumentEmitsCodeBlockIfFirstCharacterIsSwapCharacter()
        {
            ParseDocumentTest("@bar",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseDocumentDoesNotSwitchToCodeOnEmailAddressInText()
        {
            SingleSpanDocumentTest("<foo>anurse@microsoft.com</foo>", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentDoesNotSwitchToCodeOnEmailAddressInAttribute()
        {
            ParseDocumentTest("<a href=\"mailto:anurse@microsoft.com\">Email me</a>",
                new MarkupBlock(
                    Factory.Markup("<a"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("href", new LocationTagged<string>(" href=\"", 2, 0, 2), new LocationTagged<string>("\"", 36, 0, 36)),
                        Factory.Markup(" href=\"").With(SpanCodeGenerator.Null),
                        Factory.Markup("mailto:anurse@microsoft.com")
                               .With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 9, 0, 9), new LocationTagged<string>("mailto:anurse@microsoft.com", 9, 0, 9))),
                        Factory.Markup("\"").With(SpanCodeGenerator.Null)),
                    Factory.Markup(">Email me</a>")));
        }

        [Fact]
        public void ParseDocumentDoesNotReturnErrorOnMismatchedTags()
        {
            SingleSpanDocumentTest("Foo <div><p></p></p> Baz", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentReturnsOneMarkupSegmentIfNoCodeBlocksEncountered()
        {
            SingleSpanDocumentTest("Foo <p>Baz<!--Foo-->Bar<!-F> Qux", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentRendersTextPseudoTagAsMarkup()
        {
            SingleSpanDocumentTest("Foo <text>Foo</text>", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentAcceptsEndTagWithNoMatchingStartTag()
        {
            SingleSpanDocumentTest("Foo </div> Bar", BlockType.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentNoLongerSupportsDollarOpenBraceCombination()
        {
            ParseDocumentTest("<foo>${bar}</foo>",
                new MarkupBlock(
                    Factory.Markup("<foo>${bar}</foo>")));
        }

        [Fact]
        public void ParseDocumentIgnoresTagsInContentsOfScriptTag()
        {
            ParseDocumentTest(@"<script>foo<bar baz='@boz'></script>",
                new MarkupBlock(
                    Factory.Markup("<script>foo<bar baz='"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("boz")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup("'></script>")));
        }

        [Fact]
        public void ParseSectionIgnoresTagsInContentsOfScriptTag()
        {
            ParseDocumentTest(@"@section Foo { <script>foo<bar baz='@boz'></script> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionCodeGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {"),
                        new MarkupBlock(
                            Factory.Markup(" <script>foo<bar baz='"),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("boz")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("'></script> ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseBlockCanParse1000NestedElements()
        {
            string content = Nested1000.ReadAllText();
            SingleSpanDocumentTest(content, BlockType.Markup, SpanKind.Markup);
        }
    }
}
