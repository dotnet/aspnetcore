// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpDirectivesTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void InheritsDirective()
        {
            ParseBlockTest("@inherits System.Web.WebPages.WebPage",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.InheritsKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("System.Web.WebPages.WebPage")
                           .AsBaseType("System.Web.WebPages.WebPage")));
        }

        [Fact]
        public void InheritsDirectiveSupportsArrays()
        {
            ParseBlockTest("@inherits string[[]][]",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.InheritsKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("string[[]][]")
                           .AsBaseType("string[[]][]")));
        }

        [Fact]
        public void InheritsDirectiveSupportsNestedGenerics()
        {
            ParseBlockTest("@inherits System.Web.Mvc.WebViewPage<IEnumerable<MvcApplication2.Models.RegisterModel>>",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.InheritsKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("System.Web.Mvc.WebViewPage<IEnumerable<MvcApplication2.Models.RegisterModel>>")
                           .AsBaseType("System.Web.Mvc.WebViewPage<IEnumerable<MvcApplication2.Models.RegisterModel>>")));
        }

        [Fact]
        public void InheritsDirectiveSupportsTypeKeywords()
        {
            ParseBlockTest("@inherits string",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.InheritsKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("string")
                           .AsBaseType("string")));
        }

        [Fact]
        public void InheritsDirectiveSupportsVSTemplateTokens()
        {
            ParseBlockTest("@inherits $rootnamespace$.MyBase",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.InheritsKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("$rootnamespace$.MyBase")
                           .AsBaseType("$rootnamespace$.MyBase")));
        }

        [Fact]
        public void SessionStateDirectiveWorks()
        {
            ParseBlockTest("@sessionstate InProc",
                           new DirectiveBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode(SyntaxConstants.CSharp.SessionStateKeyword + " ")
                                   .Accepts(AcceptedCharacters.None),
                               Factory.Code("InProc")
                                      .AsRazorDirectiveAttribute("sessionstate", "InProc")
                               ));
        }

        [Fact]
        public void SessionStateDirectiveParsesInvalidSessionValue()
        {
            ParseBlockTest("@sessionstate Blah",
                           new DirectiveBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode(SyntaxConstants.CSharp.SessionStateKeyword + " ")
                                   .Accepts(AcceptedCharacters.None),
                               Factory.Code("Blah")
                                   .AsRazorDirectiveAttribute("sessionstate", "Blah")
                               ));
        }

        [Fact]
        public void FunctionsDirective()
        {
            ParseBlockTest("@functions { foo(); bar(); }",
                new FunctionsBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.FunctionsKeyword + " {")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code(" foo(); bar(); ")
                           .AsFunctionsBody(),
                    Factory.MetaCode("}")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void EmptyFunctionsDirective()
        {
            ParseBlockTest("@functions { }",
                new FunctionsBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.FunctionsKeyword + " {")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code(" ")
                           .AsFunctionsBody(),
                    Factory.MetaCode("}")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void SectionDirective()
        {
            ParseBlockTest("@section Header { <p>F{o}o</p> }",
                new SectionBlock(new SectionCodeGenerator("Header"),
                    Factory.CodeTransition(),
                    Factory.MetaCode("section Header {")
                           .AutoCompleteWith(null, atEndOfSpan: true)
                           .Accepts(AcceptedCharacters.Any),
                    new MarkupBlock(
                        Factory.Markup(" <p>F", "{", "o", "}", "o", "</p> ")),
                    Factory.MetaCode("}")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void HelperDirective()
        {
            ParseBlockTest("@helper Strong(string value) { foo(); }",
                new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Strong(string value) {", new SourceLocation(8, 0, 8)), headerComplete: true),
                    Factory.CodeTransition(),
                    Factory.MetaCode("helper ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Strong(string value) {")
                           .Hidden()
                           .Accepts(AcceptedCharacters.None),
                    new StatementBlock(
                        Factory.Code(" foo(); ")
                               .AsStatement()
                               .With(new StatementCodeGenerator())),
                    Factory.Code("}")
                           .Hidden()
                           .Accepts(AcceptedCharacters.None)));
        }
    }
}
