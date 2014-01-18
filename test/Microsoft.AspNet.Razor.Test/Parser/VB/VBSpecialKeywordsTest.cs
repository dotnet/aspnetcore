// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBSpecialKeywordsTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseInheritsStatementMarksInheritsSpanAsCanGrowIfMissingTrailingSpace()
        {
            ParseBlockTest("inherits",
                new DirectiveBlock(
                    Factory.MetaCode("inherits")),
                new RazorError(
                    RazorResources.ParseError_InheritsKeyword_Must_Be_Followed_By_TypeName,
                    8, 0, 8));
        }

        [Fact]
        public void InheritsBlockAcceptsMultipleGenericArguments()
        {
            ParseBlockTest("inherits Foo.Bar(Of Biz(Of Qux), String, Integer).Baz",
                new DirectiveBlock(
                    Factory.MetaCode("inherits ").Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo.Bar(Of Biz(Of Qux), String, Integer).Baz")
                           .AsBaseType("Foo.Bar(Of Biz(Of Qux), String, Integer).Baz")));
        }

        [Fact]
        public void InheritsDirectiveSupportsVSTemplateTokens()
        {
            ParseBlockTest("@Inherits $rootnamespace$.MyBase",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Inherits ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("$rootnamespace$.MyBase")
                           .AsBaseType("$rootnamespace$.MyBase")));
        }

        [Fact]
        public void InheritsBlockOutputsErrorIfInheritsNotFollowedByTypeButAcceptsEntireLineAsCode()
        {
            ParseBlockTest("inherits                " + Environment.NewLine
                         + "foo",
                new DirectiveBlock(
                    Factory.MetaCode("inherits                ").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n").AsBaseType(String.Empty)),
                new RazorError(
                    RazorResources.ParseError_InheritsKeyword_Must_Be_Followed_By_TypeName,
                    8, 0, 8));
        }

        [Fact]
        public void ParseBlockShouldSupportNamespaceImports()
        {
            ParseBlockTest("Imports Foo.Bar.Baz.Biz.Boz",
                new DirectiveBlock(
                    Factory.MetaCode("Imports Foo.Bar.Baz.Biz.Boz")
                           .With(new AddImportCodeGenerator(
                               ns: " Foo.Bar.Baz.Biz.Boz",
                               namespaceKeywordLength: SyntaxConstants.VB.ImportsKeywordLength))));
        }

        [Fact]
        public void ParseBlockShowsErrorIfNamespaceNotOnSameLineAsImportsKeyword()
        {
            ParseBlockTest("Imports" + Environment.NewLine
                         + "Foo",
                new DirectiveBlock(
                    Factory.MetaCode("Imports\r\n")
                           .With(new AddImportCodeGenerator(
                               ns: "\r\n",
                               namespaceKeywordLength: SyntaxConstants.VB.ImportsKeywordLength))),
                new RazorError(
                    RazorResources.ParseError_NamespaceOrTypeAliasExpected,
                    7, 0, 7));
        }

        [Fact]
        public void ParseBlockShowsErrorIfTypeBeingAliasedNotOnSameLineAsImportsKeyword()
        {
            ParseBlockTest("Imports Foo =" + Environment.NewLine
                         + "System.Bar",
                new DirectiveBlock(
                    Factory.MetaCode("Imports Foo =\r\n")
                           .With(new AddImportCodeGenerator(
                               ns: " Foo =\r\n",
                               namespaceKeywordLength: SyntaxConstants.VB.ImportsKeywordLength))));
        }

        [Fact]
        public void ParseBlockShouldSupportTypeAliases()
        {
            ParseBlockTest("Imports Foo = Bar.Baz.Biz.Boz",
                new DirectiveBlock(
                    Factory.MetaCode("Imports Foo = Bar.Baz.Biz.Boz")
                           .With(new AddImportCodeGenerator(
                               ns: " Foo = Bar.Baz.Biz.Boz",
                               namespaceKeywordLength: SyntaxConstants.VB.ImportsKeywordLength))));
        }

        [Fact]
        public void ParseBlockThrowsErrorIfOptionIsNotFollowedByStrictOrExplicit()
        {
            ParseBlockTest("Option FizzBuzz On",
                new DirectiveBlock(
                    Factory.MetaCode("Option FizzBuzz On")
                           .With(new SetVBOptionCodeGenerator(optionName: null, value: true))),
                new RazorError(
                    String.Format(RazorResources.ParseError_UnknownOption, "FizzBuzz"),
                    7, 0, 7));
        }

        [Fact]
        public void ParseBlockThrowsErrorIfOptionStrictIsNotFollowedByOnOrOff()
        {
            ParseBlockTest("Option Strict Yes",
                new DirectiveBlock(
                    Factory.MetaCode("Option Strict Yes")
                           .With(SetVBOptionCodeGenerator.Strict(true))),
                new RazorError(
                    String.Format(
                        RazorResources.ParseError_InvalidOptionValue,
                        "Strict", "Yes"),
                    14, 0, 14));
        }

        [Fact]
        public void ParseBlockReadsToAfterOnKeywordIfOptionStrictBlock()
        {
            ParseBlockTest("Option Strict On Foo Bar Baz",
                new DirectiveBlock(
                    Factory.MetaCode("Option Strict On")
                           .With(SetVBOptionCodeGenerator.Strict(true))));
        }

        [Fact]
        public void ParseBlockReadsToAfterOffKeywordIfOptionStrictBlock()
        {
            ParseBlockTest("Option Strict Off Foo Bar Baz",
                new DirectiveBlock(
                    Factory.MetaCode("Option Strict Off")
                           .With(SetVBOptionCodeGenerator.Strict(false))));
        }

        [Fact]
        public void ParseBlockReadsToAfterOnKeywordIfOptionExplicitBlock()
        {
            ParseBlockTest("Option Explicit On Foo Bar Baz",
                new DirectiveBlock(
                    Factory.MetaCode("Option Explicit On")
                           .With(SetVBOptionCodeGenerator.Explicit(true))));
        }

        [Fact]
        public void ParseBlockReadsToAfterOffKeywordIfOptionExplicitBlock()
        {
            ParseBlockTest("Option Explicit Off Foo Bar Baz",
                new DirectiveBlock(
                    Factory.MetaCode("Option Explicit Off")
                           .With(SetVBOptionCodeGenerator.Explicit(false))));
        }
    }
}
