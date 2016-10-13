// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class CSharpDirectivesTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void TagHelperPrefixDirective_NoValueSucceeds()
        {
            ParseBlockTest("@tagHelperPrefix \"\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"\"")
                        .AsTagHelperPrefixDirective(string.Empty)));
        }

        [Fact]
        public void TagHelperPrefixDirective_Succeeds()
        {
            ParseBlockTest("@tagHelperPrefix Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo")
                        .AsTagHelperPrefixDirective("Foo")));
        }

        [Fact]
        public void TagHelperPrefixDirective_WithQuotes_Succeeds()
        {
            ParseBlockTest("@tagHelperPrefix \"Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo\"")
                        .AsTagHelperPrefixDirective("Foo")));
        }

        [Fact]
        public void TagHelperPrefixDirective_RequiresValue()
        {
            ParseBlockTest("@tagHelperPrefix ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.EmptyCSharp()
                        .AsTagHelperPrefixDirective(string.Empty)
                        .Accepts(AcceptedCharacters.AnyExceptNewline)),
                 new RazorError(
                    LegacyResources.FormatParseError_DirectiveMustHaveValue(
                        SyntaxConstants.CSharp.TagHelperPrefixKeyword),
                    absoluteIndex: 1, lineIndex: 0, columnIndex: 1, length: 15));
        }

        [Fact]
        public void TagHelperPrefixDirective_StartQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@tagHelperPrefix \"Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo")
                        .AsTagHelperPrefixDirective("\"Foo")),
                new RazorError(
                    LegacyResources.ParseError_Unterminated_String_Literal,
                    absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 1),
                new RazorError(
                    LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(
                        SyntaxConstants.CSharp.TagHelperPrefixKeyword),
                        absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 4));
        }

        [Fact]
        public void TagHelperPrefixDirective_EndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@tagHelperPrefix Foo   \"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo   \"")
                        .AsTagHelperPrefixDirective("Foo   \"")),
                new RazorError(
                    LegacyResources.ParseError_Unterminated_String_Literal,
                    absoluteIndex: 23, lineIndex: 0, columnIndex: 23, length: 1),
                new RazorError(
                    LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(
                        SyntaxConstants.CSharp.TagHelperPrefixKeyword),
                        absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 7));
        }

        [Fact]
        public void RemoveTagHelperDirective_NoValue_Succeeds()
        {
            ParseBlockTest("@removeTagHelper \"\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"\"")
                        .AsRemoveTagHelper(string.Empty)));
        }

        [Fact]
        public void RemoveTagHelperDirective_Succeeds()
        {
            ParseBlockTest("@removeTagHelper Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo")
                        .AsRemoveTagHelper("Foo")));
        }

        [Fact]
        public void RemoveTagHelperDirective_WithQuotes_Succeeds()
        {
            ParseBlockTest("@removeTagHelper \"Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo\"")
                        .AsRemoveTagHelper("Foo")));
        }

        [Fact]
        public void RemoveTagHelperDirective_SupportsSpaces()
        {
            ParseBlockTest("@removeTagHelper     Foo,   Bar    ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + "     ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo,   Bar    ")
                        .AsRemoveTagHelper("Foo,   Bar")
                        .Accepts(AcceptedCharacters.AnyExceptNewline)));
        }

        [Fact]
        public void RemoveTagHelperDirective_RequiresValue()
        {
            ParseBlockTest("@removeTagHelper ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.EmptyCSharp()
                        .AsRemoveTagHelper(string.Empty)
                        .Accepts(AcceptedCharacters.AnyExceptNewline)),
                 new RazorError(
                    LegacyResources.FormatParseError_DirectiveMustHaveValue(
                        SyntaxConstants.CSharp.RemoveTagHelperKeyword),
                    absoluteIndex: 1, lineIndex: 0, columnIndex: 1, length: 15));
        }

        [Fact]
        public void RemoveTagHelperDirective_StartQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@removeTagHelper \"Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo")
                        .AsRemoveTagHelper("\"Foo")),
                 new RazorError(
                     LegacyResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 1),
                 new RazorError(
                     LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(
                        SyntaxConstants.CSharp.RemoveTagHelperKeyword),
                        absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 4));
        }

        [Fact]
        public void RemoveTagHelperDirective_EndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@removeTagHelper Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo\"")
                        .AsRemoveTagHelper("Foo\"")
                        .Accepts(AcceptedCharacters.AnyExceptNewline)),
                 new RazorError(
                     LegacyResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 20, lineIndex: 0, columnIndex: 20, length: 1),
                 new RazorError(
                     LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(
                        SyntaxConstants.CSharp.RemoveTagHelperKeyword),
                        absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 4));
        }

        [Fact]
        public void AddTagHelperDirective_NoValue_Succeeds()
        {
            ParseBlockTest("@addTagHelper \"\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"\"")
                        .AsAddTagHelper(string.Empty)));
        }

        [Fact]
        public void AddTagHelperDirective_Succeeds()
        {
            ParseBlockTest("@addTagHelper Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo")
                        .AsAddTagHelper("Foo")));
        }

        [Fact]
        public void AddTagHelperDirective_WithQuotes_Succeeds()
        {
            ParseBlockTest("@addTagHelper \"Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo\"")
                        .AsAddTagHelper("Foo")));
        }

        [Fact]
        public void AddTagHelperDirectiveSupportsSpaces()
        {
            ParseBlockTest("@addTagHelper     Foo,   Bar    ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + "     ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo,   Bar    ")
                        .AsAddTagHelper("Foo,   Bar")));
        }

        [Fact]
        public void AddTagHelperDirectiveRequiresValue()
        {
            ParseBlockTest("@addTagHelper ",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.EmptyCSharp()
                        .AsAddTagHelper(string.Empty)
                        .Accepts(AcceptedCharacters.AnyExceptNewline)),
                 new RazorError(
                    LegacyResources.FormatParseError_DirectiveMustHaveValue(SyntaxConstants.CSharp.AddTagHelperKeyword),
                    absoluteIndex: 1, lineIndex: 0, columnIndex: 1, length: 12));
        }

        [Fact]
        public void AddTagHelperDirective_StartQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@addTagHelper \"Foo",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"Foo")
                        .AsAddTagHelper("\"Foo")),
                 new RazorError(
                     LegacyResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 14, lineIndex: 0, columnIndex: 14, length: 1),
                 new RazorError(
                     LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(
                        SyntaxConstants.CSharp.AddTagHelperKeyword),
                        absoluteIndex: 14, lineIndex: 0, columnIndex: 14, length: 4));
        }

        [Fact]
        public void AddTagHelperDirective_EndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@addTagHelper Foo\"",
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.AddTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("Foo\"")
                        .AsAddTagHelper("Foo\"")
                        .Accepts(AcceptedCharacters.AnyExceptNewline)),
                 new RazorError(
                     LegacyResources.ParseError_Unterminated_String_Literal,
                     absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 1),
                 new RazorError(
                     LegacyResources.FormatParseError_IncompleteQuotesAroundDirective(
                        SyntaxConstants.CSharp.AddTagHelperKeyword),
                        absoluteIndex: 14, lineIndex: 0, columnIndex: 14, length: 4));
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
        public void FunctionsDirective()
        {
            ParseBlockTest("@functions { foo(); bar(); }",
                new FunctionsBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.FunctionsKeyword + " {")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code(" foo(); bar(); ")
                           .AsFunctionsBody()
                           .AutoCompleteWith(autoCompleteString: null),
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
                        .AsFunctionsBody()
                        .AutoCompleteWith(autoCompleteString: null),
                    Factory.MetaCode("}")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void SectionDirective()
        {
            ParseBlockTest("@section Header { <p>F{o}o</p> }",
                new SectionBlock(new SectionChunkGenerator("Header"),
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
    }
}
