// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpSectionTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseSectionBlockCapturesNewlineImmediatelyFollowing()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_DirectiveExpectsIdentifier(
                    new SourceSpan(new SourceLocation(8, 0, 8), contentLength: Environment.NewLine.Length), SectionDirective.Directive.Directive));

            // Act & Assert
            ParseDocumentTest(
                "@section" + Environment.NewLine,
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, string.Empty, CSharpSymbolType.Unknown)
                            .AsDirectiveToken(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Member))),
                    Factory.Markup(Environment.NewLine)));
        }

        [Fact]
        public void ParseSectionBlockCapturesWhitespaceToEndOfLineInSectionStatementMissingOpenBrace()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_UnexpectedEOFAfterDirective(
                    new SourceSpan(new SourceLocation(25 + Environment.NewLine.Length, 1, 4), contentLength: 1),
                    SectionDirective.Directive.Directive,
                    "{"));

            // Act & Assert
            ParseDocumentTest(
                "@section Foo         " + Environment.NewLine + "    ",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "Foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, "         " + Environment.NewLine + "    ", markup: false).Accepts(AcceptedCharactersInternal.AllWhiteSpace)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockCapturesWhitespaceToEndOfLineInSectionStatementMissingName()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_DirectiveExpectsIdentifier(
                    new SourceSpan(new SourceLocation(17, 0, 17), contentLength: Environment.NewLine.Length), SectionDirective.Directive.Directive));

            // Act & Assert
            ParseDocumentTest(
                "@section         " + Environment.NewLine + "    ",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, "         ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory
                            .Span(SpanKindInternal.Code, string.Empty, CSharpSymbolType.Unknown)
                            .AsDirectiveToken(DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Member))),
                    Factory.Markup(Environment.NewLine + "    ")));
        }

        [Fact]
        public void ParseSectionBlockIgnoresSectionUnlessAllLowerCase()
        {
            ParseDocumentTest(
                "@Section foo",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Section")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    Factory.Markup(" foo")));
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfKeywordNotFollowedByIdentifierStartCharacter()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_DirectiveExpectsIdentifier(
                    new SourceSpan(new SourceLocation(9, 0, 9), contentLength: 1), SectionDirective.Directive.Directive));

            // Act & Assert
            ParseDocumentTest(
                "@section 9 { <p>Foo</p> }",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace)),
                    Factory.Markup("9 { "),
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup("Foo"),
                    new MarkupTagBlock(
                        Factory.Markup("</p>")),
                    Factory.Markup(" }")));
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfNameNotFollowedByOpenBrace()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_UnexpectedDirectiveLiteral(
                    new SourceSpan(new SourceLocation(12, 0, 12), contentLength: 1),
                    SectionDirective.Directive.Directive,
                    "{"));

            // Act & Assert
            ParseDocumentTest(
                "@section foo-bar { <p>Foo</p> }",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0])),
                    Factory.Markup("-bar { "),
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup("Foo"),
                    new MarkupTagBlock(
                        Factory.Markup("</p>")),
                    Factory.Markup(" }")));
        }

        [Fact]
        public void ParserOutputsErrorOnNestedSections()
        {
            // Arrange
            var erroredChunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            erroredChunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_DirectiveMustAppearAtStartOfLine(
                    new SourceSpan(new SourceLocation(16, 0, 16), contentLength: 7), "section"));
            erroredChunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_SectionsCannotBeNested(
                    new SourceSpan(new SourceLocation(15, 0, 15), 8)));

            // Act & Assert
            ParseDocumentTest(
                "@section foo { @section bar { <p>Foo</p> } }",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new DirectiveBlock(erroredChunkGenerator,
                                Factory.CodeTransition(),
                                Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                                Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                                Factory.Span(SpanKindInternal.Code, "bar", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                                Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                                Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    Factory.Markup(" "),
                                    new MarkupTagBlock(
                                        Factory.Markup("<p>")),
                                    Factory.Markup("Foo"),
                                    new MarkupTagBlock(
                                        Factory.Markup("</p>")),
                                    Factory.Markup(" ")),
                                Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockHandlesEOFAfterOpenBrace()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(13, 0, 13), contentLength: 1), SectionDirective.Directive.Directive, "}", "{"));

            // Act & Assert
            ParseDocumentTest(
                "@section foo {",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                    Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                    Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                    Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                    Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                    new MarkupBlock(
                        Factory.EmptyHtml()))));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\n")]
        [InlineData(" abc")]
        [InlineData(" \n abc")]
        public void ParseSectionBlockHandlesEOFAfterOpenContent(string postStartBrace)
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(13, 0, 13), contentLength: 1), "section", "}", "{"));

            // Act & Assert
            ParseDocumentTest(
                "@section foo {" + postStartBrace,
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(postStartBrace)))));
        }

        [Fact]
        public void ParseSectionBlockHandlesUnterminatedSection()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(13, 0, 13), contentLength: 1), "section", "}", "{"));

            // Act & Assert
            ParseDocumentTest(
                "@section foo { <p>Foo{}</p>",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            // Need to provide the markup span as fragments, since the parser will split the {} into separate symbols.
                            Factory.Markup("Foo", "{", "}"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>"))))));
        }

        [Fact]
        public void ParseSectionBlockHandlesUnterminatedSectionWithNestedIf()
        {
            // Arrange
            var newLine = Environment.NewLine;
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(13 + newLine.Length, 1, 0), contentLength: 1), "section", "}", "{"));
            var spaces = "    ";

            // Act & Assert
            ParseDocumentTest(
                string.Format(
                    "@section Test{0}{{{0}{1}@if(true){0}{1}{{{0}{1}{1}<p>Hello World</p>{0}{1}}}",
                    newLine,
                    spaces),
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "Test", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, Environment.NewLine, CSharpSymbolType.NewLine).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(newLine),
                            new StatementBlock(
                                Factory.Code(spaces).AsStatement(),
                                Factory.CodeTransition(),
                                Factory.Code($"if(true){newLine}{spaces}{{{newLine}").AsStatement(),
                                new MarkupBlock(
                                    Factory.Markup($"{spaces}{spaces}"),
                                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                    Factory.Markup("Hello World"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                                    Factory.Markup(newLine).Accepts(AcceptedCharactersInternal.None)),
                                Factory.Code($"{spaces}}}").AsStatement())))));
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndAcceptsWhitespaceToEndOfLineIfSectionNotFollowedByOpenBrace()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_UnexpectedEOFAfterDirective(
                    new SourceSpan(new SourceLocation(18 + Environment.NewLine.Length, 1, 0), contentLength: 1),
                    SectionDirective.Directive.Directive,
                    "{"));

            // Act & Assert
            ParseDocumentTest(
                "@section foo      " + Environment.NewLine,
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, "      " + Environment.NewLine, markup: false).Accepts(AcceptedCharactersInternal.AllWhiteSpace)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockAcceptsOpenBraceMultipleLinesBelowSectionName()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section foo      "
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + "{" + Environment.NewLine
                + "<p>Foo</p>" + Environment.NewLine
                + "}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, "      " + string.Format("{0}{0}{0}{0}{0}{0}", Environment.NewLine), markup: false).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            Factory.Markup("Foo"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>")),
                            Factory.Markup(Environment.NewLine)),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockParsesNamedSectionCorrectly()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section foo { <p>Foo</p> }",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            Factory.Markup("Foo"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>")),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockDoesNotRequireSpaceBetweenSectionNameAndOpenBrace()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section foo{ <p>Foo</p> }",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<p>")),
                            Factory.Markup("Foo"),
                            new MarkupTagBlock(
                                Factory.Markup("</p>")),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockBalancesBraces()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section foo { <script>(function foo() { return 1; })();</script> }",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(" "),
                            new MarkupTagBlock(
                                Factory.Markup("<script>")),
                            Factory.Markup("(function foo() { return 1; })();"),
                            new MarkupTagBlock(
                                Factory.Markup("</script>")),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockAllowsBracesInCSharpExpression()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section foo { I really want to render a close brace, so here I go: @(\"}\") }",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(" I really want to render a close brace, so here I go: "),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                                Factory.Code("\"}\"").AsExpression(),
                                Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)),
                            Factory.Markup(" ")),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlock()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section Foo {" + Environment.NewLine
                + "@if(true) {" + Environment.NewLine
                + "}" + Environment.NewLine
                + "}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "Foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new StatementBlock(
                                Factory.CodeTransition(),
                                Factory.Code($"if(true) {{{Environment.NewLine}}}{Environment.NewLine}").AsStatement()
                            )),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlockNoWhitespace()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section Foo {" + Environment.NewLine
                + "@if(true) {" + Environment.NewLine
                + "}}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "Foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new StatementBlock(
                                Factory.CodeTransition(),
                                Factory.Code($"if(true) {{{Environment.NewLine}}}").AsStatement()
                            )),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockCorrectlyTerminatesWhenCloseBraceImmediatelyFollowsMarkup()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section foo {something}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "foo", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup("something")),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockParsesComment()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section s {<!-- -->}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "s", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            BlockFactory.HtmlCommentBlock(" "),
                            Factory.EmptyHtml()),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        // This was a user reported bug (codeplex #710), the section parser wasn't handling
        // comments.
        [Fact]
        public void ParseSectionBlockParsesCommentWithDelimiters()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section s {<!-- > \" '-->}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "s", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            BlockFactory.HtmlCommentBlock(" > \" '"),
                            Factory.EmptyHtml()),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockCommentRecoversFromUnclosedTag()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section s {" + Environment.NewLine + "<a" + Environment.NewLine + "<!--  > \" '-->}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "s", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(Environment.NewLine),
                            new MarkupTagBlock(
                                Factory.Markup("<a" + Environment.NewLine)),
                                BlockFactory.HtmlCommentBlock("  > \" '"),
                                Factory.EmptyHtml()),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseSectionBlockParsesXmlProcessingInstruction()
        {
            // Act & Assert
            ParseDocumentTest(
                "@section s { <? xml bleh ?>}",
                new[] { SectionDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "s", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                        Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                        Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            Factory.Markup(" <? xml bleh ?>")),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        public static TheoryData SectionWithEscapedTransitionData
        {
            get
            {
                var factory = new SpanFactory();

                return new TheoryData<string, Block>
                {
                    {
                        "@section s {<span foo='@@' />}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                                factory.CodeTransition(),
                                factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                                factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                                factory.Span(SpanKindInternal.Code, "s", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                                factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                                factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<span"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 17, 0, 17), new LocationTagged<string>("'", 25, 0, 25)),
                                        factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 23, 0, 23), new LocationTagged<string>("@", 23, 0, 23))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />"))),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                            factory.EmptyHtml())
                    },
                    {
                        "@section s {<span foo='@DateTime.Now @@' />}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new DirectiveBlock(new DirectiveChunkGenerator(SectionDirective.Directive),
                                factory.CodeTransition(),
                                factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                                factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                                factory.Span(SpanKindInternal.Code, "s", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens[0]),
                                factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                                factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
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
                                                    .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                                     new MarkupBlock(
                                        factory.Markup(" @").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 36, 0, 36), new LocationTagged<string>("@", 37, 0, 37))).Accepts(AcceptedCharactersInternal.None),
                                        factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />"))),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                            factory.EmptyHtml())
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SectionWithEscapedTransitionData))]
        public void ParseSectionBlock_WithDoubleTransition_DoesNotThrow(string input, object expected)
        {
            FixupSpans = true;

            ParseDocumentTest(input, new[] { SectionDirective.Directive }, (Block)expected);
        }
    }
}
