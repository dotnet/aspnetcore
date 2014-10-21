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
        public void RemoveTagHelperDirective_Succeeds()
        {
            ParseBlockTest("@removetaghelper \"Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo\"").AsRemoveTagHelper("Foo")));
        }

        [Fact]
        public void RemoveTagHelperDirective_SupportsSpaces()
        {
            ParseBlockTest("@removetaghelper   \"  Foo,   Bar \"   ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + "   ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"  Foo,   Bar \"   ").AsRemoveTagHelper("  Foo,   Bar ")));
        }

        [Fact]
        public void RemoveTagHelperDirective_RequiresValue()
        {
            ParseBlockTest("@removetaghelper ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.EmptyCSharp().AsRemoveTagHelper(string.Empty)),
                 new RazorError(
                    RazorResources.FormatParseError_DirectiveMustHaveValue(
                        SyntaxConstants.CSharp.RemoveTagHelperKeyword),
                    absoluteIndex: 17, lineIndex: 0, columnIndex: 17));
        }

        [Fact]
        public void RemoveTagHelperDirective_StartQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@removetaghelper \"Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo").AsRemoveTagHelper("Foo")),
                 new RazorError(
                     RazorResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 17, lineIndex: 0, columnIndex: 17),
                 new RazorError(
                     RazorResources.FormatParseError_DirectiveMustBeSurroundedByQuotes(
                        SyntaxConstants.CSharp.RemoveTagHelperKeyword),
                        absoluteIndex: 17, lineIndex: 0, columnIndex: 17));
        }

        [Fact]
        public void RemoveTagHelperDirective_EndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@removetaghelper Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo\"").AsRemoveTagHelper("Foo")),
                 new RazorError(
                     RazorResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 20, lineIndex: 0, columnIndex: 20),
                 new RazorError(
                     RazorResources.FormatParseError_DirectiveMustBeSurroundedByQuotes(
                        SyntaxConstants.CSharp.RemoveTagHelperKeyword),
                        absoluteIndex: 17, lineIndex: 0, columnIndex: 17));
        }

        [Fact]
        public void RemoveTagHelperDirective_RequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@removetaghelper Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo").AsRemoveTagHelper("Foo")),
                 new RazorError(
                     RazorResources.FormatParseError_DirectiveMustBeSurroundedByQuotes(
                        SyntaxConstants.CSharp.RemoveTagHelperKeyword),
                        absoluteIndex: 17, lineIndex: 0, columnIndex: 17));
        }

        [Fact]
        public void AddTagHelperDirective_Succeeds()
        {
            ParseBlockTest("@addtaghelper \"Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo\"").AsAddTagHelper("Foo")));
        }

        [Fact]
        public void AddTagHelperDirectiveSupportsSpaces()
        {
            ParseBlockTest("@addtaghelper   \"  Foo,   Bar \"   ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + "   ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"  Foo,   Bar \"   ").AsAddTagHelper("  Foo,   Bar ")));
        }

        [Fact]
        public void AddTagHelperDirectiveRequiresValue()
        {
            ParseBlockTest("@addtaghelper ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.EmptyCSharp().AsAddTagHelper(string.Empty)),
                 new RazorError(
                    RazorResources.FormatParseError_DirectiveMustHaveValue(SyntaxConstants.CSharp.AddTagHelperKeyword),
                    absoluteIndex: 14, lineIndex: 0, columnIndex: 14));
        }

        [Fact]
        public void AddTagHelperDirectiveWithStartQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@addtaghelper \"Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo").AsAddTagHelper("Foo")),
                 new RazorError(
                     RazorResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 14, lineIndex: 0, columnIndex: 14),
                 new RazorError(
                     RazorResources.FormatParseError_DirectiveMustBeSurroundedByQuotes(
                        SyntaxConstants.CSharp.AddTagHelperKeyword),
                        absoluteIndex: 14, lineIndex: 0, columnIndex: 14));
        }

        [Fact]
        public void AddTagHelperDirectiveWithEndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@addtaghelper Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo\"").AsAddTagHelper("Foo")),
                 new RazorError(
                     RazorResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 17, lineIndex: 0, columnIndex: 17),
                 new RazorError(
                     RazorResources.FormatParseError_DirectiveMustBeSurroundedByQuotes(
                        SyntaxConstants.CSharp.AddTagHelperKeyword),
                        absoluteIndex: 14, lineIndex: 0, columnIndex: 14));
        }

        [Fact]
        public void AddTagHelperDirectiveRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@addtaghelper Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo").AsAddTagHelper("Foo")),
                 new RazorError(
                     RazorResources.FormatParseError_DirectiveMustBeSurroundedByQuotes(
                        SyntaxConstants.CSharp.AddTagHelperKeyword),
                        absoluteIndex: 14, lineIndex: 0, columnIndex: 14));
        }

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
                        Factory.Markup(" "),
                        new MarkupTagBlock(
                            Factory.Markup("<p>")),
                        Factory.Markup("F", "{", "o", "}", "o"),
                        new MarkupTagBlock(
                            Factory.Markup("</p>")),
                        Factory.Markup(" ")),
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
