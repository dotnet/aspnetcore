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
    public class HtmlToCodeSwitchTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseBlockSwitchesWhenCharacterBeforeSwapIsNonAlphanumeric()
        {
            ParseBlockTest("<p>foo#@i</p>",
                new MarkupBlock(
                    Factory.Markup("<p>foo#"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("i").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup("</p>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSwitchesToCodeWhenSwapCharacterEncounteredMidTag()
        {
            ParseBlockTest("<foo @bar />",
                new MarkupBlock(
                    Factory.Markup("<foo "),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" />").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSwitchesToCodeWhenSwapCharacterEncounteredInAttributeValue()
        {
            ParseBlockTest("<foo bar=\"@baz\" />",
                new MarkupBlock(
                    Factory.Markup("<foo"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("bar", new LocationTagged<string>(" bar=\"", 4, 0, 4), new LocationTagged<string>("\"", 14, 0, 14)),
                        Factory.Markup(" bar=\"").With(SpanCodeGenerator.Null),
                        new MarkupBlock(new DynamicAttributeBlockCodeGenerator(new LocationTagged<string>(String.Empty, 10, 0, 10), 10, 0, 10),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("baz")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace))),
                        Factory.Markup("\"").With(SpanCodeGenerator.Null)),
                    Factory.Markup(" />").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSwitchesToCodeWhenSwapCharacterEncounteredInTagContent()
        {
            ParseBlockTest("<foo>@bar<baz>@boz</baz></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo>"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup("<baz>"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("boz")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup("</baz></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockParsesCodeWithinSingleLineMarkup()
        {
            ParseBlockTest("@:<li>Foo @Bar Baz" + Environment.NewLine
                         + "bork",
                new MarkupBlock(
                    Factory.MarkupTransition(),
                    Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                    Factory.Markup("<li>Foo ").With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString)),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" Baz\r\n")
                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))));
        }

        [Fact]
        public void ParseBlockSupportsCodeWithinComment()
        {
            ParseBlockTest("<foo><!-- @foo --></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><!-- "),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("foo")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" --></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsCodeWithinSGMLDeclaration()
        {
            ParseBlockTest("<foo><!DOCTYPE foo @bar baz></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><!DOCTYPE foo "),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" baz></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsCodeWithinCDataDeclaration()
        {
            ParseBlockTest("<foo><![CDATA[ foo @bar baz]]></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><![CDATA[ foo "),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" baz]]></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsCodeWithinXMLProcessingInstruction()
        {
            ParseBlockTest("<foo><?xml foo @bar baz?></foo>",
                new MarkupBlock(
                    Factory.Markup("<foo><?xml foo "),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" baz?></foo>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockDoesNotSwitchToCodeOnEmailAddressInText()
        {
            SingleSpanBlockTest("<foo>anurse@microsoft.com</foo>", BlockType.Markup, SpanKind.Markup, acceptedCharacters: AcceptedCharacters.None);
        }

        [Fact]
        public void ParseBlockDoesNotSwitchToCodeOnEmailAddressInAttribute()
        {
            ParseBlockTest("<a href=\"mailto:anurse@microsoft.com\">Email me</a>",
                new MarkupBlock(
                    Factory.Markup("<a"),
                    new MarkupBlock(new AttributeBlockCodeGenerator("href", new LocationTagged<string>(" href=\"", 2, 0, 2), new LocationTagged<string>("\"", 36, 0, 36)),
                        Factory.Markup(" href=\"").With(SpanCodeGenerator.Null),
                        Factory.Markup("mailto:anurse@microsoft.com")
                               .With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(String.Empty, 9, 0, 9), new LocationTagged<string>("mailto:anurse@microsoft.com", 9, 0, 9))),
                        Factory.Markup("\"").With(SpanCodeGenerator.Null)),
                    Factory.Markup(">Email me</a>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockGivesWhitespacePreceedingAtToCodeIfThereIsNoMarkupOnThatLine()
        {
            ParseBlockTest("   <ul>" + Environment.NewLine
                         + "    @foreach(var p in Products) {" + Environment.NewLine
                         + "        <li>Product: @p.Name</li>" + Environment.NewLine
                         + "    }" + Environment.NewLine
                         + "    </ul>",
                new MarkupBlock(
                    Factory.Markup("   <ul>\r\n"),
                    new StatementBlock(
                        Factory.Code("    ").AsStatement(),
                        Factory.CodeTransition(),
                        Factory.Code("foreach(var p in Products) {\r\n").AsStatement(),
                        new MarkupBlock(
                            Factory.Markup("        <li>Product: "),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("p.Name")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("</li>\r\n").Accepts(AcceptedCharacters.None)),
                        Factory.Code("    }\r\n").AsStatement().Accepts(AcceptedCharacters.None)),
                    Factory.Markup("    </ul>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseDocumentGivesWhitespacePreceedingAtToCodeIfThereIsNoMarkupOnThatLine()
        {
            ParseDocumentTest("   <ul>" + Environment.NewLine
                            + "    @foreach(var p in Products) {" + Environment.NewLine
                            + "        <li>Product: @p.Name</li>" + Environment.NewLine
                            + "    }" + Environment.NewLine
                            + "    </ul>",
                new MarkupBlock(
                    Factory.Markup("   <ul>\r\n"),
                    new StatementBlock(
                        Factory.Code("    ").AsStatement(),
                        Factory.CodeTransition(),
                        Factory.Code("foreach(var p in Products) {\r\n").AsStatement(),
                        new MarkupBlock(
                            Factory.Markup("        <li>Product: "),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("p.Name")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("</li>\r\n").Accepts(AcceptedCharacters.None)),
                        Factory.Code("    }\r\n").AsStatement().Accepts(AcceptedCharacters.None)),
                    Factory.Markup("    </ul>")));
        }

        [Fact]
        public void SectionContextGivesWhitespacePreceedingAtToCodeIfThereIsNoMarkupOnThatLine()
        {
            ParseDocumentTest("@section foo {" + Environment.NewLine
                            + "    <ul>" + Environment.NewLine
                            + "        @foreach(var p in Products) {" + Environment.NewLine
                            + "            <li>Product: @p.Name</li>" + Environment.NewLine
                            + "        }" + Environment.NewLine
                            + "    </ul>" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionCodeGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {").AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup("\r\n    <ul>\r\n"),
                            new StatementBlock(
                                Factory.Code("        ").AsStatement(),
                                Factory.CodeTransition(),
                                Factory.Code("foreach(var p in Products) {\r\n").AsStatement(),
                                new MarkupBlock(
                                    Factory.Markup("            <li>Product: "),
                                    new ExpressionBlock(
                                        Factory.CodeTransition(),
                                        Factory.Code("p.Name")
                                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                    Factory.Markup("</li>\r\n").Accepts(AcceptedCharacters.None)),
                                Factory.Code("        }\r\n").AsStatement().Accepts(AcceptedCharacters.None)),
                            Factory.Markup("    </ul>\r\n")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void CSharpCodeParserDoesNotAcceptLeadingOrTrailingWhitespaceInDesignMode()
        {
            ParseBlockTest("   <ul>" + Environment.NewLine
                         + "    @foreach(var p in Products) {" + Environment.NewLine
                         + "        <li>Product: @p.Name</li>" + Environment.NewLine
                         + "    }" + Environment.NewLine
                         + "    </ul>",
                new MarkupBlock(
                    Factory.Markup("   <ul>\r\n    "),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("foreach(var p in Products) {\r\n        ").AsStatement(),
                        new MarkupBlock(
                            Factory.Markup("<li>Product: "),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("p.Name").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("</li>").Accepts(AcceptedCharacters.None)),
                        Factory.Code("\r\n    }").AsStatement().Accepts(AcceptedCharacters.None)),
                    Factory.Markup("\r\n    </ul>").Accepts(AcceptedCharacters.None)),
                designTimeParser: true);
        }

        // Tests for "@@" escape sequence:
        [Fact]
        public void ParseBlockTreatsTwoAtSignsAsEscapeSequence()
        {
            HtmlParserTestUtils.RunSingleAtEscapeTest(ParseBlockTest);
        }

        [Fact]
        public void ParseBlockTreatsPairsOfAtSignsAsEscapeSequence()
        {
            HtmlParserTestUtils.RunMultiAtEscapeTest(ParseBlockTest);
        }

        [Fact]
        public void ParseDocumentTreatsTwoAtSignsAsEscapeSequence()
        {
            HtmlParserTestUtils.RunSingleAtEscapeTest(ParseDocumentTest, lastSpanAcceptedCharacters: AcceptedCharacters.Any);
        }

        [Fact]
        public void ParseDocumentTreatsPairsOfAtSignsAsEscapeSequence()
        {
            HtmlParserTestUtils.RunMultiAtEscapeTest(ParseDocumentTest, lastSpanAcceptedCharacters: AcceptedCharacters.Any);
        }

        [Fact]
        public void SectionBodyTreatsTwoAtSignsAsEscapeSequence()
        {
            ParseDocumentTest("@section Foo { <foo>@@bar</foo> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionCodeGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {").AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" <foo>"),
                            Factory.Markup("@").Hidden(),
                            Factory.Markup("@bar</foo> ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void SectionBodyTreatsPairsOfAtSignsAsEscapeSequence()
        {
            ParseDocumentTest("@section Foo { <foo>@@@@@bar</foo> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionCodeGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {").AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" <foo>"),
                            Factory.Markup("@").Hidden(),
                            Factory.Markup("@"),
                            Factory.Markup("@").Hidden(),
                            Factory.Markup("@"),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("bar")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("</foo> ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }
    }
}
