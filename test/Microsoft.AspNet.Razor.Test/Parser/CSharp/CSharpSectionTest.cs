// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpSectionTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseSectionBlockCapturesNewlineImmediatelyFollowing()
        {
            ParseDocumentTest("@section" + Environment.NewLine,
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator(string.Empty),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section" + Environment.NewLine))),
                new RazorError(
                        RazorResources.FormatParseError_Unexpected_Character_At_Section_Name_Start(RazorResources.ErrorComponent_EndOfFile),
                    8 + Environment.NewLine.Length, 1, 0));
        }

        [Fact]
        public void ParseSectionBlockCapturesWhitespaceToEndOfLineInSectionStatementMissingOpenBrace()
        {
            ParseDocumentTest("@section Foo         " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo         " + Environment.NewLine)),
                    Factory.Markup("    ")),
                new RazorError(RazorResources.ParseError_MissingOpenBraceAfterSection, 12, 0, 12));
        }

        [Fact]
        public void ParseSectionBlockCapturesWhitespaceToEndOfLineInSectionStatementMissingName()
        {
            ParseDocumentTest("@section         " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator(string.Empty),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section         " + Environment.NewLine)),
                    Factory.Markup("    ")),
                new RazorError(
                        RazorResources.FormatParseError_Unexpected_Character_At_Section_Name_Start(RazorResources.ErrorComponent_EndOfFile),
                    21 + Environment.NewLine.Length, 1, 4));
        }

        [Fact]
        public void ParseSectionBlockIgnoresSectionUnlessAllLowerCase()
        {
            ParseDocumentTest("@Section foo",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Section")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup(" foo")));
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfKeywordNotFollowedByIdentifierStartCharacter()
        {
            ParseDocumentTest("@section 9 { <p>Foo</p> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator(string.Empty),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section ")),
                    Factory.Markup("9 { "),
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup("Foo"),
                    new MarkupTagBlock(
                        Factory.Markup("</p>")),
                     Factory.Markup(" }")),
                new RazorError(
                        RazorResources.FormatParseError_Unexpected_Character_At_Section_Name_Start(RazorResources.FormatErrorComponent_Character("9")),
                    9, 0, 9));
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfNameNotFollowedByOpenBrace()
        {
            ParseDocumentTest("@section foo-bar { <p>Foo</p> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo")),
                    Factory.Markup("-bar { "),
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup("Foo"),
                    new MarkupTagBlock(
                        Factory.Markup("</p>")),
                    Factory.Markup(" }")),
                new RazorError(RazorResources.ParseError_MissingOpenBraceAfterSection, 12, 0, 12));
        }

        [Fact]
        public void ParserOutputsErrorOnNestedSections()
        {
            ParseDocumentTest("@section foo { @section bar { <p>Foo</p> } }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new SectionBlock(new SectionChunkGenerator("bar"),
                                Factory.CodeTransition(),
                                Factory.MetaCode("section bar {")
                                       .AutoCompleteWith(null, atEndOfSpan: true),
                                new MarkupBlock(
                                    Factory.Markup(" "),
                                    new MarkupTagBlock(
                                        Factory.Markup("<p>")),
                                    Factory.Markup("Foo"),
                                    new MarkupTagBlock(
                                        Factory.Markup("</p>")),
                                    Factory.Markup(" ")),
                                Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()),
                new RazorError(
                    RazorResources.FormatParseError_Sections_Cannot_Be_Nested(RazorResources.SectionExample_CS),
                    new SourceLocation(16, 0, 16),
                    7));
        }

        [Fact]
        public void ParseSectionBlockHandlesEOFAfterOpenBrace()
        {
            ParseDocumentTest("@section foo {",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {")
                               .AutoCompleteWith("}", atEndOfSpan: true),
                        new MarkupBlock())),
                new RazorError(
                    RazorResources.FormatParseError_Expected_X("}"),
                    14, 0, 14));
        }

        [Fact]
        public void ParseSectionBlockHandlesUnterminatedSection()
        {
            ParseDocumentTest("@section foo { <p>Foo{}</p>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {")
                               .AutoCompleteWith("}", atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            // Need to provide the markup span as fragments, since the parser will split the {} into separate symbols.
                            Factory.Markup("Foo", "{", "}"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>"))))),
                new RazorError(
                    RazorResources.FormatParseError_Expected_X("}"),
                    27, 0, 27));
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndAcceptsWhitespaceToEndOfLineIfSectionNotFollowedByOpenBrace()
        {
            ParseDocumentTest("@section foo      " + Environment.NewLine,
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo      " + Environment.NewLine))),
                new RazorError(RazorResources.ParseError_MissingOpenBraceAfterSection, 12, 0, 12));
        }

        [Fact]
        public void ParseSectionBlockAcceptsOpenBraceMultipleLinesBelowSectionName()
        {
            ParseDocumentTest("@section foo      " + Environment.NewLine
                            + Environment.NewLine
                            + Environment.NewLine
                            + Environment.NewLine
                            + Environment.NewLine
                            + Environment.NewLine
                            + "{" + Environment.NewLine
                            + "<p>Foo</p>" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode(string.Format("section foo      {0}{0}{0}{0}{0}{0}{{", Environment.NewLine))
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            Factory.Markup("Foo"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>")),
                            Factory.Markup(Environment.NewLine)),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockParsesNamedSectionCorrectly()
        {
            ParseDocumentTest("@section foo { <p>Foo</p> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            Factory.Markup("Foo"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>")),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockDoesNotRequireSpaceBetweenSectionNameAndOpenBrace()
        {
            ParseDocumentTest("@section foo{ <p>Foo</p> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo{")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            Factory.Markup("Foo"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>")),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockBalancesBraces()
        {
            ParseDocumentTest("@section foo { <script>(function foo() { return 1; })();</script> }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<script>")),
                            Factory.Markup("(function foo() { return 1; })();"),
                            new MarkupTagBlock(
                                Factory.Markup("</script>")),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockAllowsBracesInCSharpExpression()
        {
            ParseDocumentTest("@section foo { I really want to render a close brace, so here I go: @(\"}\") }",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" I really want to render a close brace, so here I go: "),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                                Factory.Code("\"}\"").AsExpression(),
                                Factory.MetaCode(")").Accepts(AcceptedCharacters.None)),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlock()
        {
            ParseDocumentTest("@section Foo {" + Environment.NewLine
                            + "@if(true) {" + Environment.NewLine
                            + "}" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new StatementBlock(
                                Factory.CodeTransition(),
                                Factory.Code($"if(true) {{{Environment.NewLine}}}{Environment.NewLine}").AsStatement()
                            )),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlockNoWhitespace()
        {
            ParseDocumentTest("@section Foo {" + Environment.NewLine
                            + "@if(true) {" + Environment.NewLine
                            + "}}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("Foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section Foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new StatementBlock(
                                Factory.CodeTransition(),
                                Factory.Code($"if(true) {{{Environment.NewLine}}}").AsStatement()
                            )),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockCorrectlyTerminatesWhenCloseBraceImmediatelyFollowsMarkup()
        {
            ParseDocumentTest("@section foo {something}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("foo"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section foo {")
                               .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup("something")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockParsesComment()
        {
            ParseDocumentTest("@section s {<!-- -->}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("s"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section s {")
                            .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup("<!-- -->")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        // This was a user reported bug (codeplex #710), the section parser wasn't handling
        // comments.
        [Fact]
        public void ParseSectionBlockParsesCommentWithDelimiters()
        {
            ParseDocumentTest("@section s {<!-- > \" '-->}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("s"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section s {")
                            .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup("<!-- > \" '-->")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockCommentRecoversFromUnclosedTag()
        {
            ParseDocumentTest(
                "@section s {" + Environment.NewLine + "<a" + Environment.NewLine + "<!--  > \" '-->}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("s"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section s {")
                            .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new MarkupTagBlock(
                                Factory.Markup("<a" + Environment.NewLine)),
                            Factory.Markup("<!--  > \" '-->")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockParsesXmlProcessingInstruction()
        {
            ParseDocumentTest(
                "@section s { <? xml bleh ?>}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new SectionBlock(new SectionChunkGenerator("s"),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section s {")
                            .AutoCompleteWith(null, atEndOfSpan: true),
                        new MarkupBlock(
                            Factory.Markup(" <? xml bleh ?>")),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        public static TheoryData SectionWithEscapedTransitionData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                return new TheoryData<string, Block>
                {
                    {
                        "@section s {<span foo='@@' />}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new SectionBlock(new SectionChunkGenerator("s"),
                                factory.CodeTransition(),
                                factory.MetaCode("section s {")
                                    .AutoCompleteWith(null, atEndOfSpan: true),
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 17, 0, 17), new LocationTagged<string>("'", 25, 0, 25)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 23, 0, 23), new LocationTagged<string>("@", 23, 0, 23))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />"))),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                            factory.EmptyHtml())
                    },
                    {
                        "@section s {<span foo='@DateTime.Now @@' />}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new SectionBlock(new SectionChunkGenerator("s"),
                                factory.CodeTransition(),
                                factory.MetaCode("section s {")
                                    .AutoCompleteWith(null, atEndOfSpan: true),
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 17, 0, 17), new LocationTagged<string>("'", 39, 0, 39)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 23, 0, 23), 23, 0, 23),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.Code("DateTime.Now")
                                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                    .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                     new MarkupBlock(
                                        factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 36, 0, 36), new LocationTagged<string>("@", 37, 0, 37))).Accepts(AcceptedCharacters.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />"))),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                            factory.EmptyHtml())
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SectionWithEscapedTransitionData))]
        public void ParseSectionBlock_WithDoubleTransition_DoesNotThrow(string input, Block expected)
        {
            ParseDocumentTest(input, expected);
        }

        private static SpanFactory CreateDefaultSpanFactory()
        {
            return new SpanFactory
            {
                MarkupTokenizerFactory = doc => new HtmlTokenizer(doc),
                CodeTokenizerFactory = doc => new CSharpTokenizer(doc)
            };
        }
    }
}
