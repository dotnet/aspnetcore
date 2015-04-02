// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpAutoCompleteTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void FunctionsDirectiveAutoCompleteAtEOF()
        {
            ParseBlockTest("@functions{",
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
                           new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("functions", "}", "{"),
                                          1, 0, 1));
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtEOF()
        {
            ParseBlockTest("@section Header {",
                new SectionBlock(new SectionCodeGenerator("Header"),
                    Factory.CodeTransition(),
                    Factory.MetaCode("section Header {")
                           .AutoCompleteWith("}", atEndOfSpan: true)
                           .Accepts(AcceptedCharacters.Any),
                    new MarkupBlock()),
                new RazorError(
                    RazorResources.FormatParseError_Expected_X("}"),
                    17, 0, 17));
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
                           new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, "}", "{"),
                                          1, 0, 1));
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
                           new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("functions", "}", "{"),
                                          1, 0, 1));
        }

        [Fact]
        public void SectionDirectiveAutoCompleteAtStartOfFile()
        {
            ParseBlockTest("@section Header {" + Environment.NewLine
                         + "<p>Foo</p>",
                new SectionBlock(new SectionCodeGenerator("Header"),
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
                new RazorError(RazorResources.FormatParseError_Expected_X("}"),
                                27 + Environment.NewLine.Length, 1, 10));
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
                               Factory.Span(SpanKind.Code, new CSharpSymbol(Factory.LocationTracker.CurrentLocation, String.Empty, CSharpSymbolType.Unknown))
                                   .With(new StatementCodeGenerator())
                               ),
                           new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, "}", "{"),
                                          1, 0, 1));
        }
    }
}
