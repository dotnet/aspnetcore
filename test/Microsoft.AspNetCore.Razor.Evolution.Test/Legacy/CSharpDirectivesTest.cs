// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public class CSharpDirectivesTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void DirectiveDescriptor_UnderstandsTypeTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").AddType().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System.Text.Encoding.ASCIIEncoding",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Text.Encoding.ASCIIEncoding", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsMemberTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").AddMember().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom Some_Member",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Some_Member", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsStringTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").AddString().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom AString",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "AString", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsMultipleTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom")
                .AddType()
                .AddMember()
                .AddString()
                .Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System.Text.Encoding.ASCIIEncoding Some_Member AString",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),

                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Text.Encoding.ASCIIEncoding", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),

                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Some_Member", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[1]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),

                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "AString", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[2]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsRazorBlocks()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.CreateRazorBlock("custom").AddString().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom Header { <p>F{o}o</p> }",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "Header", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{")
                        .AutoCompleteWith(null, atEndOfSpan: true)
                        .Accepts(AcceptedCharacters.None),
                    new MarkupBlock(
                        Factory.Markup(" "),
                        new MarkupTagBlock(
                            Factory.Markup("<p>")),
                        Factory.Markup("F", "{", "o", "}", "o"),
                        new MarkupTagBlock(
                            Factory.Markup("</p>")),
                        Factory.Markup(" ")),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsCodeBlocks()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.CreateCodeBlock("custom").AddString().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom Name { foo(); bar(); }",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "Name", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{")
                        .AutoCompleteWith(null, atEndOfSpan: true)
                        .Accepts(AcceptedCharacters.None),
                    Factory.Code(" foo(); bar(); ").AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void DirectiveDescriptor_AllowsWhiteSpaceAroundTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom")
                .AddType()
                .AddMember()
                .Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom    System.Text.Encoding.ASCIIEncoding       Some_Member    ",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),

                    Factory.Span(SpanKind.Code, "    ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Text.Encoding.ASCIIEncoding", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),

                    Factory.Span(SpanKind.Code, "       ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Some_Member", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[1]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),

                    Factory.Span(SpanKind.Markup, "    ", markup: false)
                        .Accepts(AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsForInvalidMemberTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").AddMember().Build();
            var expectedErorr = new RazorError(
                LegacyResources.FormatDirectiveExpectsIdentifier("custom"),
                new SourceLocation(8, 0, 8),
                length: 1);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom -Some_Member",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                expectedErorr);
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsExtraContentAfterDirective()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").AddString().Build();
            var expectedErorr = new RazorError(
                LegacyResources.FormatUnexpectedDirectiveLiteral("custom", Environment.NewLine),
                new SourceLocation(14, 0, 14),
                length: 5);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom hello world",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "hello", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),

                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                expectedErorr);
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenExtraContentBeforeBlockStart()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.CreateCodeBlock("custom").AddString().Build();
            var expectedErorr = new RazorError(
                LegacyResources.FormatUnexpectedDirectiveLiteral("custom", "{"),
                new SourceLocation(14, 0, 14),
                length: 5);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom Hello World { foo(); bar(); }",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "Hello", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),

                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.AllWhiteSpace)),
                expectedErorr);
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenEOFBeforeDirectiveBlockStart()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.CreateCodeBlock("custom").AddString().Build();
            var expectedErorr = new RazorError(
                LegacyResources.FormatUnexpectedEOFAfterDirective("custom", "{"),
                new SourceLocation(13, 0, 13),
                length: 1);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom Hello",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "Hello", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace)),
                expectedErorr);
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenMissingEndBrace()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.CreateCodeBlock("custom").AddString().Build();
            var expectedErorr = new RazorError(
                LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("custom", "}", "{"),
                new SourceLocation(14, 0, 14),
                length: 1);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom Hello {",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "Hello", markup: false)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens[0]))
                        .Accepts(AcceptedCharacters.NonWhiteSpace),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{")
                        .AutoCompleteWith("}", atEndOfSpan: true)
                        .Accepts(AcceptedCharacters.None)),
                expectedErorr);
        }

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
                        .AsTagHelperPrefixDirective("\"\"")));
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
                        .AsTagHelperPrefixDirective("\"Foo\"")));
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
                        .AsRemoveTagHelper("\"\"")));
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
                        .AsRemoveTagHelper("\"Foo\"")));
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
                        .AsAddTagHelper("\"\"")));
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
                        .AsAddTagHelper("\"Foo\"")));
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
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("inherits").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Web.WebPages.WebPage", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First()))));
        }

        [Fact]
        public void InheritsDirectiveSupportsArrays()
        {
            ParseBlockTest("@inherits string[[]][]",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("inherits").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "string[[]][]", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First()))));
        }

        [Fact]
        public void InheritsDirectiveSupportsNestedGenerics()
        {
            ParseBlockTest("@inherits System.Web.Mvc.WebViewPage<IEnumerable<MvcApplication2.Models.RegisterModel>>",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("inherits").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Web.Mvc.WebViewPage<IEnumerable<MvcApplication2.Models.RegisterModel>>", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First()))));
        }

        [Fact]
        public void InheritsDirectiveSupportsTypeKeywords()
        {
            ParseBlockTest("@inherits string",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("inherits").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "string", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First()))));
        }

        [Fact]
        public void FunctionsDirective()
        {
            ParseBlockTest("@functions { foo(); bar(); }",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.FunctionsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("functions").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharacters.None),
                    Factory.Code(" foo(); bar(); ").AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void EmptyFunctionsDirective()
        {
            ParseBlockTest("@functions { }",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.FunctionsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("functions").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharacters.None),
                    Factory.Code(" ").AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void SectionDirective()
        {
            ParseBlockTest("@section Header { <p>F{o}o</p> }",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.SectionDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("section").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Header", CSharpSymbolType.Identifier)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(CSharpCodeParser.SectionDirectiveDescriptor.Tokens.First())),
                    Factory.Span(SpanKind.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharacters.None),
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
        public void OptionalDirectiveTokens_AreSkipped()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").BeginOptionals().AddString().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom ",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void OptionalDirectiveTokens_WithSimpleTokens_AreParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").BeginOptionals().AddString().Build();
            var chunkGenerator = new DirectiveTokenChunkGenerator(descriptor.Tokens.First());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom simple-value",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "simple-value", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(chunkGenerator)));
        }

        [Fact]
        public void OptionalDirectiveTokens_WithBraces_AreParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").BeginOptionals().AddString().Build();
            var chunkGenerator = new DirectiveTokenChunkGenerator(descriptor.Tokens.First());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom {formaction}?/{id}?",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "{formaction}?/{id}?", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(chunkGenerator)));
        }

        [Fact]
        public void OptionalDirectiveTokens_WithMultipleOptionalTokens_AreParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("custom").BeginOptionals().AddString().AddType().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@custom {formaction}?/{id}? System.String",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Markup, "{formaction}?/{id}?", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens.First())),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.String", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens.Last()))));
        }

        [Fact]
        public void OptionalMemberTokens_WithMissingMember_IsParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("TestDirective").BeginOptionals().AddMember().AddString().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@TestDirective ",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("TestDirective").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void OptionalMemberTokens_WithMemberSpecified_IsParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptorBuilder.Create("TestDirective").BeginOptionals().AddMember().AddString().Build();

            // Act & Assert
            ParseCodeBlockTest(
                "@TestDirective PropertyName",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("TestDirective").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "PropertyName", markup: false)
                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                        .With(new DirectiveTokenChunkGenerator(descriptor.Tokens.First()))));
        }

        internal virtual void ParseCodeBlockTest(
            string document,
            IEnumerable<DirectiveDescriptor> descriptors,
            Block expected,
            params RazorError[] expectedErrors)
        {
            var result = ParseCodeBlock(document, descriptors, designTime: false);

            EvaluateResults(result, expected, expectedErrors);
        }
    }
}
