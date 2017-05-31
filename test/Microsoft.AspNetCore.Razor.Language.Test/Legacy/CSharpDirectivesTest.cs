// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpDirectivesTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void DirectiveDescriptor_CanHandleEOFIncompleteNamespaceTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddNamespaceToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System.",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                new RazorError(
                    LegacyResources.FormatDirectiveExpectsNamespace("custom"),
                    8, 0, 8, 7));
        }

        [Fact]
        public void DirectiveDescriptor_CanHandleEOFInvalidNamespaceTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddNamespaceToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System<",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                new RazorError(
                    LegacyResources.FormatDirectiveExpectsNamespace("custom"),
                    8, 0, 8, 7));
        }
        [Fact]
        public void DirectiveDescriptor_CanHandleIncompleteNamespaceTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddNamespaceToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System." + Environment.NewLine,
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                new RazorError(
                    LegacyResources.FormatDirectiveExpectsNamespace("custom"),
                    8, 0, 8, 7));
        }

        [Fact]
        public void DirectiveDescriptor_CanHandleInvalidNamespaceTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddNamespaceToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System<" + Environment.NewLine,
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                new RazorError(
                    LegacyResources.FormatDirectiveExpectsNamespace("custom"),
                    8, 0, 8, 7));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsTypeTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddTypeToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System.Text.Encoding.ASCIIEncoding",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Text.Encoding.ASCIIEncoding", markup: false).AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsMemberTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddMemberToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom Some_Member",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Some_Member", markup: false).AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void Parser_ParsesNamespaceDirectiveToken_WithSingleSegment()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddNamespaceToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom BaseNamespace",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "BaseNamespace", markup: false).AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void Parser_ParsesNamespaceDirectiveToken_WithMultipleSegments()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddNamespaceToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom BaseNamespace.Foo.Bar",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "BaseNamespace.Foo.Bar", markup: false).AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsStringTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"AString\"",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"AString\"", markup: false).AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForUnquotedValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            var expectedError = new RazorError(
                LegacyResources.FormatDirectiveExpectsQuotedStringLiteral("custom"),
                new SourceLocation(8, 0, 8),
                length: 7);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom AString",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)), expectedError);
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForNonStringValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            var expectedError = new RazorError(
                LegacyResources.FormatDirectiveExpectsQuotedStringLiteral("custom"),
                new SourceLocation(8, 0, 8),
                length: 1);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom {foo?}",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)), expectedError);
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForSingleQuotedValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            var expectedError = new RazorError(
                LegacyResources.FormatDirectiveExpectsQuotedStringLiteral("custom"),
                new SourceLocation(8, 0, 8),
                length: 9);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom 'AString'",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)), expectedError);
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForPartialQuotedValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            var expectedError = new RazorError(
                LegacyResources.FormatDirectiveExpectsQuotedStringLiteral("custom"),
                new SourceLocation(8, 0, 8),
                length: 7);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom AString\"",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                expectedError);
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsMultipleTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddTypeToken().AddMemberToken().AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom System.Text.Encoding.ASCIIEncoding Some_Member \"AString\"",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),

                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Text.Encoding.ASCIIEncoding", markup: false).AsDirectiveToken(descriptor.Tokens[0]),

                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Some_Member", markup: false).AsDirectiveToken(descriptor.Tokens[1]),

                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"AString\"", markup: false).AsDirectiveToken(descriptor.Tokens[2])));
        }

        [Fact]
        public void DirectiveDescriptor_UnderstandsRazorBlocks()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.RazorBlock,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Header\" { <p>F{o}o</p> }",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"Header\"", markup: false).AsDirectiveToken(descriptor.Tokens[0]),
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
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.CodeBlock,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Name\" { foo(); bar(); }",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"Name\"", markup: false).AsDirectiveToken(descriptor.Tokens[0]),
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
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddTypeToken().AddMemberToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom    System.Text.Encoding.ASCIIEncoding       Some_Member    ",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),

                    Factory.Span(SpanKind.Code, "    ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.Text.Encoding.ASCIIEncoding", markup: false).AsDirectiveToken(descriptor.Tokens[0]),

                    Factory.Span(SpanKind.Code, "       ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "Some_Member", markup: false).AsDirectiveToken(descriptor.Tokens[1]),

                    Factory.Span(SpanKind.Markup, "    ", markup: false)
                        .Accepts(AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsForInvalidMemberTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddMemberToken());

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
        public void DirectiveDescriptor_NoErrorsSemicolonAfterDirective()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"hello\" ;  ",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"hello\"", markup: false).AsDirectiveToken(descriptor.Tokens[0]),
                    Factory.Span(SpanKind.Markup, " ;  ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)));
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsExtraContentAfterDirective()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            var expectedErorr = new RazorError(
                LegacyResources.FormatUnexpectedDirectiveLiteral("custom", "line break"),
                new SourceLocation(16, 0, 16),
                length: 7);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"hello\" \"world\"",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"hello\"", markup: false).AsDirectiveToken(descriptor.Tokens[0]),

                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace)),
                expectedErorr);
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenExtraContentBeforeBlockStart()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.CodeBlock,
                b => b.AddStringToken());

            var expectedErorr = new RazorError(
                LegacyResources.FormatUnexpectedDirectiveLiteral("custom", "{"),
                new SourceLocation(16, 0, 16),
                length: 5);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Hello\" World { foo(); bar(); }",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"Hello\"", markup: false).AsDirectiveToken(descriptor.Tokens[0]),

                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.AllWhiteSpace)),
                expectedErorr);
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenEOFBeforeDirectiveBlockStart()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.CodeBlock,
                b => b.AddStringToken());

            var expectedErorr = new RazorError(
                LegacyResources.FormatUnexpectedEOFAfterDirective("custom", "{"),
                new SourceLocation(15, 0, 15),
                length: 1);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Hello\"",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"Hello\"", markup: false).AsDirectiveToken(descriptor.Tokens[0])),
                expectedErorr);
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenMissingEndBrace()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.CodeBlock,
                b => b.AddStringToken());

            var expectedErorr = new RazorError(
                LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("custom", "}", "{"),
                new SourceLocation(16, 0, 16),
                length: 1);

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Hello\" {",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"Hello\"", markup: false).AsDirectiveToken(descriptor.Tokens[0]),
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
                    Factory.Span(SpanKind.Code, "System.Web.WebPages.WebPage", markup: false).AsDirectiveToken(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First())));
        }

        [Fact]
        public void InheritsDirectiveSupportsArrays()
        {
            ParseBlockTest("@inherits string[[]][]",
                new DirectiveBlock(new DirectiveChunkGenerator(CSharpCodeParser.InheritsDirectiveDescriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("inherits").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "string[[]][]", markup: false).AsDirectiveToken(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First())));
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
                        .AsDirectiveToken(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First())));
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
                        .AsDirectiveToken(CSharpCodeParser.InheritsDirectiveDescriptor.Tokens.First())));
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
                        .AsDirectiveToken(CSharpCodeParser.SectionDirectiveDescriptor.Tokens.First()),
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
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddOptionalStringToken());

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
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddOptionalStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"simple-value\"",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"simple-value\"", markup: false)
                        .AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void OptionalDirectiveTokens_WithBraces_AreParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddOptionalStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"{formaction}?/{id}?\"",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"{formaction}?/{id}?\"", markup: false)
                        .AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void OptionalDirectiveTokens_WithMultipleOptionalTokens_AreParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddOptionalStringToken().AddOptionalTypeToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"{formaction}?/{id}?\" System.String",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("custom").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Markup, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "\"{formaction}?/{id}?\"", markup: false).AsDirectiveToken(descriptor.Tokens[0]),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "System.String", markup: false).AsDirectiveToken(descriptor.Tokens.Last())));
        }

        [Fact]
        public void OptionalMemberTokens_WithMissingMember_IsParsed()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "TestDirective",
                DirectiveKind.SingleLine,
                b => b.AddOptionalMemberToken().AddOptionalStringToken());

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
            var descriptor = DirectiveDescriptor.CreateDirective(
                "TestDirective",
                DirectiveKind.SingleLine,
                b => b.AddOptionalMemberToken().AddOptionalStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@TestDirective PropertyName",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("TestDirective").Accepts(AcceptedCharacters.None),
                    Factory.Span(SpanKind.Code, " ", markup: false).Accepts(AcceptedCharacters.WhiteSpace),
                    Factory.Span(SpanKind.Code, "PropertyName", markup: false).AsDirectiveToken(descriptor.Tokens[0])));
        }

        [Fact]
        public void Directives_CanUseReservedWord_Class()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "class",
                DirectiveKind.SingleLine);

            // Act & Assert
            ParseCodeBlockTest(
                "@class",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("class").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void Directives_CanUseReservedWord_Namespace()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "namespace",
                DirectiveKind.SingleLine);

            // Act & Assert
            ParseCodeBlockTest(
                "@namespace",
                new[] { descriptor },
                new DirectiveBlock(
                    new DirectiveChunkGenerator(descriptor),
                    Factory.CodeTransition(),
                    Factory.MetaCode("namespace").Accepts(AcceptedCharacters.None)));
        }


        internal virtual void ParseCodeBlockTest(
            string document,
            IEnumerable<DirectiveDescriptor> descriptors,
            Block expected,
            params RazorError[] expectedErrors)
        {
            var result = ParseCodeBlock(document, descriptors, designTime: false);

            EvaluateResults(result, expected, expectedErrors.Select(error => RazorDiagnostic.Create(error)).ToList());
        }
    }
}
