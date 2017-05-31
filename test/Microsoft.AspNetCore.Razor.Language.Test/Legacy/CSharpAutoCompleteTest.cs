// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpAutoCompleteTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void FunctionsDirectiveAutoCompleteAtEOF()
        {
            ParseBlockTest(
                "@functions{",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.FunctionsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("functions").Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharacters.None)),
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(CSharpCodeParser.FunctionsDirectiveDescriptor.Name, "}", "{"),
                    new SourceLocation(10, 0, 10),
                    length: 1));
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtEOF()
        {
            ParseBlockTest("@section Header {",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.SectionDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("section").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Header", CSharpSymbolType.Identifier)
                        .AsDirectiveToken(CSharpCodeParser.SectionDirectiveDescriptor.Tokens.First()),
                    Factory.Span(SpanKind.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharacters.None),
                    new MarkupBlock(
                        Factory.EmptyHtml())),
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("section", "}", "{"),
                    new SourceLocation(16, 0, 16),
                    length: 1));
        }

        [Fact]
        public void VerbatimBlockAutoCompleteAtEOF()
        {
            ParseBlockTest("@{",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
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
            ParseBlockTest("@functions{" + Environment.NewLine + "foo",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.FunctionsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("functions").Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharacters.None),
                Factory.Code(Environment.NewLine + "foo").AsStatement()),
                    new RazorError(
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("functions", "}", "{"),
                        new SourceLocation(10, 0, 10),
                        length: 1));
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtStartOfFile()
        {
            ParseBlockTest("@section Header {" + Environment.NewLine
                         + "<p>Foo</p>",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.SectionDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("section").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Header", CSharpSymbolType.Identifier).AsDirectiveToken(CSharpCodeParser.SectionDirectiveDescriptor.Tokens.First()),
                    Factory.Span(SpanKind.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharacters.None),
                    new MarkupBlock(
                        Factory.Markup(Environment.NewLine),
                        new MarkupTagBlock(
                            Factory.Markup("<p>")),
                        Factory.Markup("Foo"),
                        new MarkupTagBlock(
                            Factory.Markup("</p>")))),
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("section", "}", "{"),
                    new SourceLocation(16, 0, 16),
                    length: 1));
        }

        [Fact]
        public void VerbatimBlockAutoCompleteAtStartOfFile()
        {
            ParseBlockTest("@{" + Environment.NewLine
                         + "<p></p>",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(Environment.NewLine)
                                   .AsStatement()
                                   .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = "}" }),
                               new MarkupBlock(
                                    new MarkupTagBlock(
                                        Factory.Markup("<p>").Accepts(AcceptedCharacters.None)),
                                    new MarkupTagBlock(
                                        Factory.Markup("</p>").Accepts(AcceptedCharacters.None))),
                               Factory.Span(SpanKind.Code, new CSharpSymbol(string.Empty, CSharpSymbolType.Unknown))
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
