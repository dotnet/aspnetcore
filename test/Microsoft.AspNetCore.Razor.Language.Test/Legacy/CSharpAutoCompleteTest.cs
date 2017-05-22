// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpAutoCompleteTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void FunctionsDirectiveAutoCompleteAtEOF()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(FunctionsDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnostic.Create(
                    new RazorError(
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(FunctionsDirective.Directive.Directive, "}", "{"),
                        new SourceLocation(10, 0, 10),
                        length: 1)));

            // Act & Assert
            ParseBlockTest(
                "@functions{",
                new[] { FunctionsDirective.Directive },
                new DirectiveBlock(chunkGenerator,
                    Factory.CodeTransition(),
                    Factory.MetaCode("functions").Accepts(AcceptedCharactersInternal.None),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtEOF()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnostic.Create(
                    new RazorError(
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("section", "}", "{"),
                        new SourceLocation(16, 0, 16),
                        length: 1)));

            // Act & Assert
            ParseBlockTest(
                "@section Header {",
                new[] { SectionDirective.Directive },
                new DirectiveBlock(chunkGenerator,
                    Factory.CodeTransition(),
                    Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                    Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                    Factory.Span(SpanKindInternal.Code, "Header", CSharpSymbolType.Identifier)
                        .AsDirectiveToken(SectionDirective.Directive.Tokens.First()),
                    Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                    new MarkupBlock(
                        Factory.EmptyHtml())));
        }

        [Fact]
        public void VerbatimBlockAutoCompleteAtEOF()
        {
            ParseBlockTest("@{",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                    Factory.EmptyCSharp()
                        .AsStatement()
                        .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = "}" })
                    ),
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(
                        LegacyResources.BlockName_Code, "}", "{"),
                    new SourceLocation(1, 0, 1),
                    length: 1));
        }

        [Fact]
        public void FunctionsDirectiveAutoCompleteAtStartOfFile()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(FunctionsDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnostic.Create(
                    new RazorError(
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("functions", "}", "{"),
                        new SourceLocation(10, 0, 10),
                        length: 1)));

            // Act & Assert
            ParseBlockTest(
                "@functions{" + Environment.NewLine + "foo",
                new[] { FunctionsDirective.Directive },
                new DirectiveBlock(chunkGenerator,
                    Factory.CodeTransition(),
                    Factory.MetaCode("functions").Accepts(AcceptedCharactersInternal.None),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                Factory.Code(Environment.NewLine + "foo").AsStatement()));
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtStartOfFile()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnostic.Create(
                    new RazorError(
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("section", "}", "{"),
                        new SourceLocation(16, 0, 16),
                        length: 1)));

            // Act & Assert
            ParseBlockTest(
                "@section Header {" + Environment.NewLine + "<p>Foo</p>",
                new[] { SectionDirective.Directive },
                new DirectiveBlock(chunkGenerator,
                    Factory.CodeTransition(),
                    Factory.MetaCode("section").Accepts(AcceptedCharactersInternal.None),
                    Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                    Factory.Span(SpanKindInternal.Code, "Header", CSharpSymbolType.Identifier).AsDirectiveToken(SectionDirective.Directive.Tokens.First()),
                    Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                    new MarkupBlock(
                        Factory.Markup(Environment.NewLine),
                        new MarkupTagBlock(
                            Factory.Markup("<p>")),
                        Factory.Markup("Foo"),
                        new MarkupTagBlock(
                            Factory.Markup("</p>")))));
        }

        [Fact]
        public void VerbatimBlockAutoCompleteAtStartOfFile()
        {
            ParseBlockTest(
                "@{" + Environment.NewLine + "<p></p>",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                    Factory.Code(Environment.NewLine)
                        .AsStatement()
                        .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = "}" }),
                    new MarkupBlock(
                        new MarkupTagBlock(
                            Factory.Markup("<p>").Accepts(AcceptedCharactersInternal.None)),
                        new MarkupTagBlock(
                            Factory.Markup("</p>").Accepts(AcceptedCharactersInternal.None))),
                    Factory.Span(SpanKindInternal.Code, new CSharpSymbol(string.Empty, CSharpSymbolType.Unknown))
                        .With(new StatementChunkGenerator())
                    ),
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(
                        LegacyResources.BlockName_Code, "}", "{"),
                    new SourceLocation(1, 0, 1),
                    length: 1));
        }
    }
}
