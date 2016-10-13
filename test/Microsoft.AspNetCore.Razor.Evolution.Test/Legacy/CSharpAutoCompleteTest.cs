// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class CSharpAutoCompleteTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void FunctionsDirectiveAutoCompleteAtEOF()
        {
            ParseBlockTest(
                "@functions{",
                new FunctionsBlock(
                    Factory.CodeTransition("@")
                        .Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("functions{")
                        .Accepts(AcceptedCharacters.None),
                    Factory.EmptyCSharp()
                        .AsFunctionsBody()
                        .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString)
                        {
                            AutoCompleteString = "}"
                        })),
                new RazorError(
                    LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("functions", "}", "{"),
                    new SourceLocation(10, 0, 10),
                    length: 1));
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtEOF()
        {
            ParseBlockTest("@section Header {",
                new SectionBlock(new SectionChunkGenerator("Header"),
                    Factory.CodeTransition(),
                    Factory.MetaCode("section Header {")
                           .AutoCompleteWith("}", atEndOfSpan: true)
                           .Accepts(AcceptedCharacters.Any),
                    new MarkupBlock()),
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
            ParseBlockTest("@functions{" + Environment.NewLine
                         + "foo",
                           new FunctionsBlock(
                               Factory.CodeTransition("@")
                                   .Accepts(AcceptedCharacters.None),
                               Factory.MetaCode("functions{")
                                   .Accepts(AcceptedCharacters.None),
                               Factory.Code(Environment.NewLine + "foo")
                                   .AsFunctionsBody()
                                   .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString)
                                   {
                                       AutoCompleteString = "}"
                                   })),
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
                new SectionBlock(new SectionChunkGenerator("Header"),
                    Factory.CodeTransition(),
                    Factory.MetaCode("section Header {")
                           .AutoCompleteWith("}", atEndOfSpan: true)
                           .Accepts(AcceptedCharacters.Any),
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
                               Factory.Span(SpanKind.Code, new CSharpSymbol(Factory.LocationTracker.CurrentLocation, string.Empty, CSharpSymbolType.Unknown))
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
