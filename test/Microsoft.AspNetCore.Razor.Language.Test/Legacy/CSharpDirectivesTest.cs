// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpDirectivesTest : CsHtmlCodeParserTestBase
    {
        public CSharpDirectivesTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void DirectiveDescriptor_FileScopedMultipleOccurring_CanHaveDuplicates()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                    builder.AddTypeToken();
                });

            // Act & Assert
            ParseDocumentTest(
@"@custom System.Text.Encoding.ASCIIEncoding
@custom System.Text.Encoding.UTF8Encoding",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_FileScopedSinglyOccurring_ErrorsIfDuplicate()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                    builder.AddTypeToken();
                });

            // Act & Assert
            ParseDocumentTest(
@"@custom System.Text.Encoding.ASCIIEncoding
@custom System.Text.Encoding.UTF8Encoding",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_FileScoped_CanBeBeneathOtherDirectives()
        {
            // Arrange
            var customDescriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                    builder.AddTypeToken();
                });
            var somethingDescriptor = DirectiveDescriptor.CreateDirective(
                "something",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                    builder.AddMemberToken();
                });

            // Act & Assert
            ParseDocumentTest(
@"@custom System.Text.Encoding.ASCIIEncoding
@something Else",
                new[] { customDescriptor, somethingDescriptor });
        }

        [Fact]
        public void DirectiveDescriptor_FileScoped_CanBeBeneathOtherWhiteSpaceCommentsAndDirectives()
        {
            // Arrange
            var customDescriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                    builder.AddTypeToken();
                });
            var somethingDescriptor = DirectiveDescriptor.CreateDirective(
                "something",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                    builder.AddMemberToken();
                });

            // Act & Assert
            ParseDocumentTest(
@"@* There are two directives beneath this *@
@custom System.Text.Encoding.ASCIIEncoding

@something Else

<p>This is extra</p>",
                new[] { customDescriptor, somethingDescriptor });
        }

        [Fact]
        public void DirectiveDescriptor_TokensMustBeSeparatedBySpace()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken().AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"string1\"\"string2\"",
                new[] { descriptor });
        }

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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
        }
        
        [Fact]
        public void ExtensibleDirectiveDoesNotErorrIfNotAtStartOfLineBecauseOfWhitespace()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddTypeToken());

            // Act & Assert
            ParseCodeBlockTest(Environment.NewLine + "  @custom System.Text.Encoding.ASCIIEncoding",
                new[] { descriptor });
        }

        [Fact]
        public void BuiltInDirectiveDoesNotErorrIfNotAtStartOfLineBecauseOfWhitespace()
        {
            // Act & Assert
            ParseCodeBlockTest(Environment.NewLine + "  @addTagHelper \"*, Foo\"");
        }

        [Fact]
        public void BuiltInDirectiveErrorsIfNotAtStartOfLine()
        {
            // Act & Assert
            ParseCodeBlockTest("{  @addTagHelper \"*, Foo\"" + Environment.NewLine + "}");
        }

        [Fact]
        public void ExtensibleDirectiveErrorsIfNotAtStartOfLine()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddTypeToken());

            // Act & Assert
            ParseCodeBlockTest(
                "{  @custom System.Text.Encoding.ASCIIEncoding" + Environment.NewLine + "}",
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForUnquotedValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom AString",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForNonStringValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom {foo?}",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForSingleQuotedValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom 'AString'",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_StringToken_ParserErrorForPartialQuotedValue()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom AString\"",
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsForInvalidMemberTokens()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddMemberToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom -Some_Member",
                new[] { descriptor });
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
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_AllowsNullableTypes()
        {
            // Arrange
            var variants = new[]
            {
                "string?",
                "string?[]",
                "global::System.Int32?",
                "KeyValuePair<string, string>?",
                "KeyValuePair<string, string>?[]",
                "global::System.Collections.Generic.KeyValuePair<string, string>?[]",
            };

            var directiveName = "custom";
            var source = $"@{directiveName}";
            var descriptor = DirectiveDescriptor.CreateDirective(
                directiveName,
                DirectiveKind.SingleLine,
                b =>
                {
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                });

            for (var i = 0; i < variants.Length; i++)
            {
                source += $" {variants[i]}";
            }

            // Act & Assert
            ParseCodeBlockTest(source, new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_AllowsTupleTypes()
        {
            // Arrange
            var variants = new[]
            {
                "(bool, int)",
                "(int aa, string bb)?",
                "(  int?   q   ,  bool   w   )",
                "( int  ?  q, bool ?w ,(long ?  [])) ?",
                "(List<(int, string)?> aa, string bb)",
                "(string ss, (int u, List<(string, int)> k, (Char c, bool b, List<int> l)), global::System.Int32[] a)",
            };

            var directiveName = "custom";
            var source = $"@{directiveName}";
            var descriptor = DirectiveDescriptor.CreateDirective(
                directiveName,
                DirectiveKind.SingleLine,
                b =>
                {
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                    b.AddTypeToken();
                });

            for (var i = 0; i < variants.Length; i++)
            {
                source += $" {variants[i]}";
            }

            // Act & Assert
            ParseCodeBlockTest(source, new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_AllowsTupleTypes_IgnoresTrailingWhitespace()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddTypeToken());

            // Act & Assert
            ParseCodeBlockTest(
                $"@custom (bool, int?)   ",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsExtraContentAfterDirective()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.SingleLine,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"hello\" \"world\"",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenExtraContentBeforeBlockStart()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.CodeBlock,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Hello\" World { foo(); bar(); }",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenEOFBeforeDirectiveBlockStart()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.CodeBlock,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Hello\"",
                new[] { descriptor });
        }

        [Fact]
        public void DirectiveDescriptor_ErrorsWhenMissingEndBrace()
        {
            // Arrange
            var descriptor = DirectiveDescriptor.CreateDirective(
                "custom",
                DirectiveKind.CodeBlock,
                b => b.AddStringToken());

            // Act & Assert
            ParseCodeBlockTest(
                "@custom \"Hello\" {",
                new[] { descriptor });
        }

        [Fact]
        public void TagHelperPrefixDirective_NoValueSucceeds()
        {
            ParseBlockTest("@tagHelperPrefix \"\"");
        }

        [Fact]
        public void TagHelperPrefixDirective_Succeeds()
        {
            ParseBlockTest("@tagHelperPrefix Foo");
        }

        [Fact]
        public void TagHelperPrefixDirective_WithQuotes_Succeeds()
        {
            ParseBlockTest("@tagHelperPrefix \"Foo\"");
        }

        [Fact]
        public void TagHelperPrefixDirective_RequiresValue()
        {
            ParseBlockTest("@tagHelperPrefix ");
        }

        [Fact]
        public void TagHelperPrefixDirective_StartQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@tagHelperPrefix \"Foo");
        }

        [Fact]
        public void TagHelperPrefixDirective_EndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@tagHelperPrefix Foo   \"");
        }

        [Fact]
        public void RemoveTagHelperDirective_NoValue_Invalid()
        {
            ParseBlockTest("@removeTagHelper \"\"");
        }

        [Fact]
        public void RemoveTagHelperDirective_InvalidLookupText_AddsError()
        {
            ParseBlockTest("@removeTagHelper Foo");
        }

        [Fact]
        public void RemoveTagHelperDirective_SingleQuotes_AddsError()
        {
            ParseBlockTest("@removeTagHelper '*, Foo'");
        }

        [Fact]
        public void RemoveTagHelperDirective_WithQuotes_InvalidLookupText_AddsError()
        {
            ParseBlockTest("@removeTagHelper \"Foo\"");
        }

        [Fact]
        public void RemoveTagHelperDirective_SupportsSpaces()
        {
            ParseBlockTest("@removeTagHelper     Foo,   Bar    ");
        }

        [Fact]
        public void RemoveTagHelperDirective_RequiresValue()
        {
            ParseBlockTest("@removeTagHelper ");
        }

        [Fact]
        public void RemoveTagHelperDirective_StartQuoteRequiresDoubleQuotesAroundValue()
        {
            // Arrange
            ParseBlockTest("@removeTagHelper \"Foo");
        }

        [Fact]
        public void RemoveTagHelperDirective_EndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@removeTagHelper Foo\"");
        }

        [Fact]
        public void AddTagHelperDirective_NoValue_Invalid()
        {
            ParseBlockTest("@addTagHelper \"\"");
        }

        [Fact]
        public void AddTagHelperDirective_InvalidLookupText_AddsError()
        {
            ParseBlockTest("@addTagHelper Foo");
        }

        [Fact]
        public void AddTagHelperDirective_WithQuotes_InvalidLookupText_AddsError()
        {
            ParseBlockTest("@addTagHelper \"Foo\"");
        }

        [Fact]
        public void AddTagHelperDirective_SingleQuotes_AddsError()
        {
            ParseBlockTest("@addTagHelper '*, Foo'");
        }

        [Fact]
        public void AddTagHelperDirective_SupportsSpaces()
        {
            ParseBlockTest("@addTagHelper     Foo,   Bar    ");
        }

        [Fact]
        public void AddTagHelperDirective_RequiresValue()
        {
            ParseBlockTest("@addTagHelper ");
        }

        [Fact]
        public void AddTagHelperDirective_StartQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@addTagHelper \"Foo");
        }

        [Fact]
        public void AddTagHelperDirective_EndQuoteRequiresDoubleQuotesAroundValue()
        {
            ParseBlockTest("@addTagHelper Foo\"");
        }

        [Fact]
        public void InheritsDirectiveSupportsArrays()
        {
            ParseDocumentTest(
                "@inherits string[[]][]",
                new[] { InheritsDirective.Directive, });
        }

        [Fact]
        public void InheritsDirectiveSupportsNestedGenerics()
        {
            ParseDocumentTest(
                "@inherits System.Web.Mvc.WebViewPage<IEnumerable<MvcApplication2.Models.RegisterModel>>",
                new[] { InheritsDirective.Directive, });
        }

        [Fact]
        public void InheritsDirectiveSupportsTypeKeywords()
        {
            ParseDocumentTest(
                "@inherits string",
                new[] { InheritsDirective.Directive, });
        }

        [Fact]
        public void Parse_FunctionsDirective()
        {
            ParseCodeBlockTest(
                "@functions { foo(); bar(); }",
                new[] { FunctionsDirective.Directive, });
        }

        [Fact]
        public void EmptyFunctionsDirective()
        {
            ParseCodeBlockTest(
                "@functions { }",
                new[] { FunctionsDirective.Directive, });
        }

        [Fact]
        public void Parse_SectionDirective()
        {
            ParseCodeBlockTest(
                "@section Header { <p>F{o}o</p> }",
                new[] { SectionDirective.Directive, });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
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
                new[] { descriptor });
        }

        internal virtual void ParseCodeBlockTest(string document)
        {
            ParseCodeBlockTest(document, Array.Empty<DirectiveDescriptor>());
        }

        internal virtual void ParseCodeBlockTest(
            string document,
            IEnumerable<DirectiveDescriptor> descriptors,
            Block expected = null,
            params RazorDiagnostic[] expectedErrors)
        {
            var result = ParseCodeBlock(RazorLanguageVersion.Latest, document, descriptors, designTime: false);

            if (UseBaselineTests && !IsTheory)
            {
                SyntaxTreeVerifier.Verify(result);
                AssertSyntaxTreeNodeMatchesBaseline(result);
                return;
            }

            EvaluateResults(result, expected, expectedErrors);
        }
    }
}
