// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBAutoCompleteTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void FunctionsDirective_AutoComplete_At_EOF()
        {
            ParseBlockTest("@Functions",
                new FunctionsBlock(
                    Factory.CodeTransition("@")
                           .Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("Functions")
                           .Accepts(AcceptedCharacters.None),
                    Factory.EmptyVB()
                           .AsFunctionsBody()
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString)
                           {
                               AutoCompleteString = SyntaxConstants.VB.EndFunctionsKeyword
                           })),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Functions", "End Functions"),
                    1, 0, 1));
        }

        [Fact]
        public void HelperDirective_AutoComplete_At_EOF()
        {
            ParseBlockTest("@Helper Strong(value As String)",
                new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Strong(value As String)", 8, 0, 8), headerComplete: true),
                    Factory.CodeTransition(),
                    Factory.MetaCode("Helper ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Strong(value As String)")
                           .Hidden()
                           .Accepts(AcceptedCharacters.None)
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = SyntaxConstants.VB.EndHelperKeyword }),
                    new StatementBlock()),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Helper", "End Helper"),
                    1, 0, 1));
        }

        [Fact]
        public void SectionDirective_AutoComplete_At_EOF()
        {
            ParseBlockTest("@Section Header",
                new SectionBlock(new SectionCodeGenerator("Header"),
                    Factory.CodeTransition(),
                    Factory.MetaCode("Section Header")
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = SyntaxConstants.VB.EndSectionKeyword }),
                    new MarkupBlock()),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Section", "End Section"),
                    1, 0, 1));
        }

        [Fact]
        public void VerbatimBlock_AutoComplete_At_EOF()
        {
            ParseBlockTest("@Code",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, new VBSymbol(5, 0, 5, String.Empty, VBSymbolType.Unknown))
                           .With(new StatementCodeGenerator())
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = SyntaxConstants.VB.EndCodeKeyword })),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Code", "End Code"),
                    1, 0, 1));
        }

        [Fact]
        public void FunctionsDirective_AutoComplete_At_StartOfFile()
        {
            ParseBlockTest("@Functions" + Environment.NewLine
                         + "foo",
                new FunctionsBlock(
                    Factory.CodeTransition("@").Accepts(AcceptedCharacters.None),
                    Factory.MetaCode("Functions").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\nfoo")
                           .AsFunctionsBody()
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString)
                           {
                               AutoCompleteString = SyntaxConstants.VB.EndFunctionsKeyword
                           })),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Functions", "End Functions"),
                    1, 0, 1));
        }

        [Fact]
        public void HelperDirective_AutoComplete_At_StartOfFile()
        {
            ParseBlockTest("@Helper Strong(value As String)" + Environment.NewLine
                         + "Foo",
                new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Strong(value As String)", 8, 0, 8), headerComplete: true),
                    Factory.CodeTransition(),
                    Factory.MetaCode("Helper ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Strong(value As String)")
                           .Hidden()
                           .Accepts(AcceptedCharacters.None)
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = SyntaxConstants.VB.EndHelperKeyword }),
                    new StatementBlock(
                        Factory.Code("\r\nFoo").AsStatement())),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Helper", "End Helper"),
                    1, 0, 1));
        }

        [Fact]
        public void SectionDirective_AutoComplete_At_StartOfFile()
        {
            ParseBlockTest("@Section Header" + Environment.NewLine
                         + "Foo",
                new SectionBlock(new SectionCodeGenerator("Header"),
                    Factory.CodeTransition(),
                    Factory.MetaCode("Section Header")
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = SyntaxConstants.VB.EndSectionKeyword }),
                    new MarkupBlock(
                        Factory.Markup("\r\nFoo")
                               .With(new MarkupCodeGenerator()))),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Section", "End Section"),
                    1, 0, 1));
        }

        [Fact]
        public void VerbatimBlock_AutoComplete_At_StartOfFile()
        {
            ParseBlockTest("@Code" + Environment.NewLine
                         + "Foo",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\nFoo")
                           .AsStatement()
                           .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = SyntaxConstants.VB.EndCodeKeyword })),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Code", "End Code"),
                    1, 0, 1));
        }
    }
}
