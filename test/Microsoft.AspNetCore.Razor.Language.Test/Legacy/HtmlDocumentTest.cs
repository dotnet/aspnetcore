// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlDocumentTest : CsHtmlMarkupParserTestBase
    {
        private static readonly TestFile Nested1000 = TestFile.Create("TestFiles/nested-1000.html", typeof(HtmlDocumentTest));

        [Fact]
        public void ParseDocument_NestedCodeBlockWithMarkupSetsDotAsMarkup()
        {
            ParseDocumentTest("@if (true) { @if(false) { <div>@something.</div> } }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("if (true) { ").AsStatement(),
                        new StatementBlock(
                            Factory.CodeTransition(),
                            Factory.Code("if(false) {").AsStatement(),
                            new MarkupBlock(
                                Factory.Markup(" "),
                                BlockFactory.MarkupTagBlock("<div>", AcceptedCharacters.None),
                                Factory.EmptyHtml(),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("something")
                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                                        .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                Factory.Markup("."),
                                BlockFactory.MarkupTagBlock("</div>", AcceptedCharacters.None),
                                Factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                            Factory.Code("}").AsStatement()),
                        Factory.Code(" }").AsStatement())));
        }

        [Fact]
        public void ParseDocumentOutputsEmptyBlockWithEmptyMarkupSpanIfContentIsEmptyString()
        {
            ParseDocumentTest(string.Empty, new MarkupBlock(Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseDocumentOutputsWhitespaceOnlyContentAsSingleWhitespaceMarkupSpan()
        {
            SingleSpanDocumentTest("          ", BlockKind.Markup, SpanKind.Markup);
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
                new RazorError(
                    LegacyResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                    new SourceLocation(1, 0, 1),
                    length: 1));
        }

        [Fact]
        public void ParseDocumentCorrectlyHandlesOddlySpacedHTMLElements()
        {
            ParseDocumentTest("<div ><p class = 'bar'> Foo </p></div >",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<div >"),
                    new MarkupTagBlock(
                        Factory.Markup("<p"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "class", prefix: new LocationTagged<string>(" class = '", 8, 0, 8), suffix: new LocationTagged<string>("'", 21, 0, 21)),
                            Factory.Markup(" class = '").With(SpanChunkGenerator.Null),
                            Factory.Markup("bar").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(string.Empty, 18, 0, 18), value: new LocationTagged<string>("bar", 18, 0, 18))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(">")),
                    Factory.Markup(" Foo "),
                    BlockFactory.MarkupTagBlock("</p>"),
                    BlockFactory.MarkupTagBlock("</div >")));
        }

        [Fact]
        public void ParseDocumentCorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
        {
            ParseDocumentTest("<div>Foo @if(true) {} Bar</div>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<div>"),
                    Factory.Markup("Foo "),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("if(true) {}").AsStatement()),
                    Factory.Markup(" Bar"),
                    BlockFactory.MarkupTagBlock("</div>")));
        }

        [Fact]
        public void ParseDocumentWithinSectionDoesNotCreateDocumentLevelSpan()
        {
            ParseDocumentTest("@section Foo {" + Environment.NewLine
                            + "    <html></html>" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.SectionDirectiveDescriptor),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharacters.None),
                        Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                        Factory.Span(SpanKind.Code, "Foo", CSharpSymbolType.Identifier).AsDirectiveToken(CSharpCodeParser.SectionDirectiveDescriptor.Tokens[0]),
                        Factory.Span(SpanKind.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharacters.None),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine + "    "),
                            BlockFactory.MarkupTagBlock("<html>"),
                            BlockFactory.MarkupTagBlock("</html>"),
                            Factory.Markup(Environment.NewLine)),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseDocumentParsesWholeContentAsOneSpanIfNoSwapCharacterEncountered()
        {
            SingleSpanDocumentTest("foo baz", BlockKind.Markup, SpanKind.Markup);
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
                new RazorError(
                    LegacyResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                    new SourceLocation(5, 0, 5),
                    length: 1));
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
            SingleSpanDocumentTest("anurse@microsoft.com", BlockKind.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentDoesNotSwitchToCodeOnEmailAddressInAttribute()
        {
            ParseDocumentTest("<a href=\"mailto:anurse@microsoft.com\">Email me</a>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator("href", new LocationTagged<string>(" href=\"", 2, 0, 2), new LocationTagged<string>("\"", 36, 0, 36)),
                            Factory.Markup(" href=\"").With(SpanChunkGenerator.Null),
                            Factory.Markup("mailto:anurse@microsoft.com")
                                   .With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 9, 0, 9), new LocationTagged<string>("mailto:anurse@microsoft.com", 9, 0, 9))),
                            Factory.Markup("\"").With(SpanChunkGenerator.Null)),
                        Factory.Markup(">")),
                    Factory.Markup("Email me"),
                    BlockFactory.MarkupTagBlock("</a>")));
        }

        [Fact]
        public void ParseDocumentDoesNotReturnErrorOnMismatchedTags()
        {
            ParseDocumentTest("Foo <div><p></p></p> Baz",
                new MarkupBlock(
                    Factory.Markup("Foo "),
                    BlockFactory.MarkupTagBlock("<div>"),
                    BlockFactory.MarkupTagBlock("<p>"),
                    BlockFactory.MarkupTagBlock("</p>"),
                    BlockFactory.MarkupTagBlock("</p>"),
                    Factory.Markup(" Baz")));
        }

        [Fact]
        public void ParseDocumentReturnsOneMarkupSegmentIfNoCodeBlocksEncountered()
        {
            SingleSpanDocumentTest("Foo Baz<!--Foo-->Bar<!--F> Qux", BlockKind.Markup, SpanKind.Markup);
        }

        [Fact]
        public void ParseDocumentRendersTextPseudoTagAsMarkup()
        {
            ParseDocumentTest("Foo <text>Foo</text>",
                new MarkupBlock(
                    Factory.Markup("Foo "),
                    BlockFactory.MarkupTagBlock("<text>"),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</text>")));
        }

        [Fact]
        public void ParseDocumentAcceptsEndTagWithNoMatchingStartTag()
        {
            ParseDocumentTest("Foo </div> Bar",
                new MarkupBlock(
                    Factory.Markup("Foo "),
                    BlockFactory.MarkupTagBlock("</div>"),
                    Factory.Markup(" Bar")));
        }

        [Fact]
        public void ParseDocumentNoLongerSupportsDollarOpenBraceCombination()
        {
            ParseDocumentTest("<foo>${bar}</foo>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<foo>"),
                    Factory.Markup("${bar}"),
                    BlockFactory.MarkupTagBlock("</foo>")));
        }

        [Fact]
        public void ParseDocumentIgnoresTagsInContentsOfScriptTag()
        {
            ParseDocumentTest(@"<script>foo<bar baz='@boz'></script>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<script>"),
                    Factory.Markup("foo<bar baz='"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("boz")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup("'>"),
                    BlockFactory.MarkupTagBlock("</script>")));
        }

        [Fact]
        public void ParseDocumentDoesNotRenderExtraNewLineAtTheEndOfVerbatimBlock()
        {
            ParseDocumentTest("@{\r\n}\r\n<html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement().AutoCompleteWith(null, false),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("\r\n").With(SpanChunkGenerator.Null),
                    BlockFactory.MarkupTagBlock("<html>")));
        }

        [Fact]
        public void ParseDocumentDoesNotRenderExtraWhitespaceAndNewLineAtTheEndOfVerbatimBlock()
        {
            ParseDocumentTest("@{\r\n} \t\r\n<html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement().AutoCompleteWith(null, false),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.Markup(" \t\r\n").With(SpanChunkGenerator.Null),
                    BlockFactory.MarkupTagBlock("<html>")));
        }

        [Fact]
        public void ParseDocumentDoesNotRenderExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByCSharp()
        {
            ParseDocumentTest("@{<text>Blah</text>\r\n\r\n}<html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.MarkupTransition("<text>")),
                            Factory.Markup("Blah").Accepts(AcceptedCharacters.None),
                            new MarkupTagBlock(
                                Factory.MarkupTransition("</text>"))),
                        Factory.Code("\r\n\r\n").AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    BlockFactory.MarkupTagBlock("<html>")));
        }

        [Fact]
        public void ParseDocumentRendersExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByHtml()
        {
            ParseDocumentTest("@{<text>Blah</text>\r\n<input/>\r\n}<html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.MarkupTransition("<text>")),
                            Factory.Markup("Blah").Accepts(AcceptedCharacters.None),
                            new MarkupTagBlock(
                                Factory.MarkupTransition("</text>")),
                            Factory.Markup("\r\n").Accepts(AcceptedCharacters.None)),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.Markup("<input/>").Accepts(AcceptedCharacters.None)),
                            Factory.Markup("\r\n").Accepts(AcceptedCharacters.None)),
                        Factory.EmptyCSharp().AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    BlockFactory.MarkupTagBlock("<html>")));
        }

        [Fact]
        public void ParseDocumentRendersExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByMarkupTransition()
        {
            ParseDocumentTest("@{<text>Blah</text>\r\n@: Bleh\r\n}<html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.MarkupTransition("<text>")),
                            Factory.Markup("Blah").Accepts(AcceptedCharacters.None),
                            new MarkupTagBlock(
                                Factory.MarkupTransition("</text>")),
                            Factory.Markup("\r\n").Accepts(AcceptedCharacters.None)),
                        new MarkupBlock(
                            Factory.MarkupTransition(),
                            Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                            Factory.Markup(" Bleh\r\n")
                                .With(new SpanEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
                                .Accepts(AcceptedCharacters.None)),
                        Factory.EmptyCSharp().AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    BlockFactory.MarkupTagBlock("<html>")));
        }

        [Fact]
        public void ParseDocumentDoesNotIgnoreNewLineAtTheEndOfMarkupBlock()
        {
            ParseDocumentTest("@{\r\n}\r\n<html>\r\n",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement().AutoCompleteWith(null, false),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("\r\n").With(SpanChunkGenerator.Null),
                    BlockFactory.MarkupTagBlock("<html>"),
                    Factory.Markup("\r\n")));
        }

        [Fact]
        public void ParseDocumentDoesNotIgnoreWhitespaceAtTheEndOfVerbatimBlockIfNoNewlinePresent()
        {
            ParseDocumentTest("@{\r\n}   \t<html>\r\n",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement().AutoCompleteWith(null, false),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("   \t"),
                    BlockFactory.MarkupTagBlock("<html>"),
                    Factory.Markup("\r\n")));
        }

        [Fact]
        public void ParseDocumentHandlesNewLineInNestedBlock()
        {
            ParseDocumentTest("@{\r\n@if(true){\r\n} \r\n}\r\n<html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement().AutoCompleteWith(null, false),
                        new StatementBlock(
                            Factory.CodeTransition(),
                            Factory.Code("if(true){\r\n}").AsStatement()),
                        Factory.Code(" \r\n").AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("\r\n").With(SpanChunkGenerator.Null),
                    BlockFactory.MarkupTagBlock("<html>")));
        }

        [Fact]
        public void ParseDocumentHandlesNewLineAndMarkupInNestedBlock()
        {
            ParseDocumentTest("@{\r\n@if(true){\r\n} <input> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement().AutoCompleteWith(null, false),
                        new StatementBlock(
                            Factory.CodeTransition(),
                            Factory.Code("if(true){\r\n}").AsStatement()),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<input>").Accepts(AcceptedCharacters.None)),
                            Factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                        Factory.EmptyCSharp().AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseDocumentHandlesExtraNewLineBeforeMarkupInNestedBlock()
        {
            ParseDocumentTest("@{\r\n@if(true){\r\n} \r\n<input> \r\n}<html>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement().AutoCompleteWith(null, false),
                        new StatementBlock(
                            Factory.CodeTransition(),
                            Factory.Code("if(true){\r\n}").AsStatement()),
                        Factory.Code(" \r\n").AsStatement(),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.Markup("<input>").Accepts(AcceptedCharacters.None)),
                            Factory.Markup(" \r\n").Accepts(AcceptedCharacters.None)),
                        Factory.EmptyCSharp().AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    new MarkupTagBlock(
                        Factory.Markup("<html>"))));
        }

        [Fact]
        public void ParseSectionIgnoresTagsInContentsOfScriptTag()
        {
            ParseDocumentTest(@"@section Foo { <script>foo<bar baz='@boz'></script> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.SectionDirectiveDescriptor),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharacters.None),
                        Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                        Factory.Span(SpanKind.Code, "Foo", CSharpSymbolType.Identifier).AsDirectiveToken(CSharpCodeParser.SectionDirectiveDescriptor.Tokens[0]),
                        Factory.Span(SpanKind.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharacters.None),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            BlockFactory.MarkupTagBlock("<script>"),
                            Factory.Markup("foo<bar baz='"),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("boz")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: false)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("'>"),
                            BlockFactory.MarkupTagBlock("</script>"),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseBlockCanParse1000NestedElements()
        {
            var content = Nested1000.ReadAllText();
            ParseDocument(content);
        }

        public static TheoryData BlockWithEscapedTransitionData
        {
            get
            {
                var factory = new SpanFactory();
                var datetimeBlock = new ExpressionBlock(
                    factory.CodeTransition(),
                    factory.Code("DateTime.Now")
                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                        .Accepts(AcceptedCharacters.NonWhiteSpace));

                return new TheoryData<string, Block>
                {
                    {
                        // Double transition in attribute value
                        "<span foo='@@' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 13, 0, 13)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("@", 11, 0, 11))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        // Double transition at the end of attribute value
                        "<span foo='abc@@' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 16, 0, 16)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    factory.Markup("abc").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("abc", 11, 0, 11))),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), new LocationTagged<string>("@", 14, 0, 14))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        // Double transition at the beginning of attribute value
                        "<span foo='@@def' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 16, 0, 16)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("@", 11, 0, 11))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("def").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 13, 0, 13), new LocationTagged<string>("def", 13, 0, 13))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        // Double transition in between attribute value
                        "<span foo='abc @@ def' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 21, 0, 21)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    factory.Markup("abc").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("abc", 11, 0, 11))),
                                    new MarkupBlock(
                                        factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 14, 0, 14), new LocationTagged<string>("@", 15, 0, 15))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup(" def").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 17, 0, 17), new LocationTagged<string>("def", 18, 0, 18))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        // Double transition with expression block
                        "<span foo='@@@DateTime.Now' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 26, 0, 26)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("@", 11, 0, 11))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 13, 0, 13), 13, 0, 13),
                                        factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                        datetimeBlock),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        "<span foo='@DateTime.Now @@' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 27, 0, 27)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), 11, 0, 11),
                                        datetimeBlock),
                                    new MarkupBlock(
                                        factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 24, 0, 24), new LocationTagged<string>("@", 25, 0, 25))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        "<span foo='@(2+3)@@@DateTime.Now' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 32, 0, 32)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), 11, 0, 11),
                                        new ExpressionBlock(
                                            factory.CodeTransition(),
                                            factory.MetaCode("(").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None),
                                            factory.Code("2+3").AsExpression(),
                                            factory.MetaCode(")").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None))),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 17, 0, 17), new LocationTagged<string>("@", 17, 0, 17))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 19, 0, 19), 19, 0, 19),
                                        factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                        datetimeBlock),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                            factory.Markup(" />")))
                    },
                    {
                        "<span foo='@@@(2+3)' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 19, 0, 19)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("@", 11, 0, 11))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 13, 0, 13), 13, 0, 13),
                                        factory.EmptyHtml().With(SpanChunkGenerator.Null),
                                        new ExpressionBlock(
                                            factory.CodeTransition(),
                                            factory.MetaCode("(").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None),
                                            factory.Code("2+3").AsExpression(),
                                            factory.MetaCode(")").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                            factory.Markup(" />")))
                    },
                    {
                        "<span foo='@DateTime.Now@@' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 26, 0, 26)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), 11, 0, 11),
                                        datetimeBlock),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 24, 0, 24), new LocationTagged<string>("@", 24, 0, 24))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        // Double transition with email in attribute value
                        "<span foo='abc@def.com @@' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 25, 0, 25)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    factory.Markup("abc@def.com").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("abc@def.com", 11, 0, 11))),
                                    new MarkupBlock(
                                        factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 22, 0, 22), new LocationTagged<string>("@", 23, 0, 23))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        "<span foo='abc@@def.com @@' />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 26, 0, 26)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    factory.Markup("abc").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("abc", 11, 0, 11))),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 14, 0, 14), new LocationTagged<string>("@", 14, 0, 14))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("def.com").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 16, 0, 16), new LocationTagged<string>("def.com", 16, 0, 16))),
                                    new MarkupBlock(
                                        factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 23, 0, 23), new LocationTagged<string>("@", 24, 0, 24))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        // Double transition before end of file
                        "<span foo='@@",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>(string.Empty, 13, 0, 13)),
                                    factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("@", 11, 0, 11))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)))),
                            factory.EmptyHtml())
                    },
                    {
                        // Double transition in complex regex in attribute value
                        @"<span foo=""/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+@@[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i"" />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<span"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo=\"", 5, 0, 5), new LocationTagged<string>("\"", 111, 0, 111)),
                                    factory.Markup(" foo=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup(@"/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>(@"/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+", 11, 0, 11))),
                                    new MarkupBlock(
                                        factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 43, 0, 43), new LocationTagged<string>("@", 43, 0, 43))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup(@"[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 45, 0, 45), new LocationTagged<string>(@"[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i", 45, 0, 45))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BlockWithEscapedTransitionData))]
        public void ParseBlock_WithDoubleTransition_DoesNotThrow(string input, object expected)
        {
            FixupSpans = true;

            // Act & Assert
            ParseDocumentTest(input, (Block)expected);
        }

        [Fact]
        public void ParseDocument_WithUnexpectedTransitionsInAttributeValue_Throws()
        {
            // Arrange
            var expected = new MarkupBlock(
                new MarkupTagBlock(
                    Factory.Markup("<span"),
                    new MarkupBlock(
                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 14, 0, 14)),
                        Factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), 11, 0, 11),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.EmptyCSharp().AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace))),
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(" ", 12, 0, 12), 12, 0, 12),
                            Factory.Markup(" ").With(SpanChunkGenerator.Null),
                            new ExpressionBlock(
                                Factory.CodeTransition().Accepts(AcceptedCharacters.None).With(SpanChunkGenerator.Null),
                                Factory.EmptyCSharp().AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace))),
                        Factory.Markup("'").With(SpanChunkGenerator.Null)),
                    Factory.Markup(" />")));
            var expectedErrors = new RazorError[]
            {
                new RazorError(
                    @"A space or line break was encountered after the ""@"" character.  Only valid identifiers, keywords, comments, ""("" and ""{"" are valid at the start of a code block and they must occur immediately following ""@"" with no space in between.",
                    new SourceLocation(12, 0, 12),
                    length: 1),
                new RazorError(
                    @"""' />"" is not valid at the start of a code block.  Only identifiers, keywords, comments, ""("" and ""{"" are valid.",
                    new SourceLocation(14, 0, 14),
                    length: 4),
            };

            // Act & Assert
            ParseDocumentTest("<span foo='@ @' />", expected, expectedErrors);
        }
    }
}
