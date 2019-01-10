// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class CodeGenerationIntegrationTest : IntegrationTestBase
    {
        #region Runtime
        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void IncompleteDirectives_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CSharp7_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void BasicImports_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void UnfinishedExpressionInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Templates_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void StringLiterals_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SimpleUnspacedIf_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Sections_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void RazorComments_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ParserError_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void OpenedIf_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NullConditionalExpressions_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NoLinePragmas_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedCSharp_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedCodeBlocks_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void MarkupInCodeBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Instrumented_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void InlineBlocks_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Inherits_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Usings_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ImplicitExpressionAtEOF_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ImplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void HtmlCommentWithQuote_Double_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void HtmlCommentWithQuote_Single_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void HiddenSpansInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void FunctionsBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void FunctionsBlockMinimal_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExpressionsInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExplicitExpressionWithMarkup_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExplicitExpressionAtEOF_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyImplicitExpressionInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyImplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyExplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyCodeBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ConditionalAttributes_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CodeBlockWithTextElement_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CodeBlockAtEOF_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CodeBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Blocks_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Await_Runtime()
        {
            RunTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SimpleTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithBoundAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithPrefix_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SingleTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SingleTagHelperWithNewlineBeforeAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithWeirdlySpacedAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void IncompleteTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void BasicTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void BasicTagHelpers_Prefixed_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void BasicTagHelpers_RemoveTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CssSelectorTagHelperAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.CssSelectorTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ComplexTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EscapedTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void DuplicateTargetTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DuplicateTargetTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void AttributeTargetingTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.AttributeTargetingTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void PrefixedAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.PrefixedAttributeTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void DuplicateAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void DynamicAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DynamicAttributeTagHelpers_Descriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TransitionsInTagHelperAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void MinimizedTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.MinimizedTagHelpers_Descriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedScriptTagTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SymbolBoundAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SymbolBoundTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EnumTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.EnumTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersInSection_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.TagHelpersInSectionDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithTemplate_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }
        #endregion

        #region DesignTime
        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void IncompleteDirectives_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CSharp7_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void BasicImports_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void UnfinishedExpressionInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Templates_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void StringLiterals_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SimpleUnspacedIf_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Sections_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void RazorComments_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ParserError_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void OpenedIf_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NullConditionalExpressions_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NoLinePragmas_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedCSharp_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedCodeBlocks_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void MarkupInCodeBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Instrumented_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void InlineBlocks_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Inherits_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Usings_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ImplicitExpressionAtEOF_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ImplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void HtmlCommentWithQuote_Double_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void HtmlCommentWithQuote_Single_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void HiddenSpansInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void FunctionsBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void FunctionsBlockMinimal_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExpressionsInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExplicitExpressionWithMarkup_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExplicitExpressionAtEOF_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ExplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyImplicitExpressionInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyImplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyExplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyCodeBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void DesignTime_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ConditionalAttributes_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CodeBlockWithTextElement_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CodeBlockAtEOF_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void CodeBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Blocks_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void Await_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void AddTagHelperDirective_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void RemoveTagHelperDirective_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SimpleTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithBoundAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithPrefix_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SingleTagHelper_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SingleTagHelperWithNewlineBeforeAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithWeirdlySpacedAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void IncompleteTagHelper_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void BasicTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void BasicTagHelpers_Prefixed_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void ComplexTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EmptyAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EscapedTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void DuplicateTargetTagHelper_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DuplicateTargetTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void AttributeTargetingTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.AttributeTargetingTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void PrefixedAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.PrefixedAttributeTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void DuplicateAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void DynamicAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DynamicAttributeTagHelpers_Descriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TransitionsInTagHelperAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void MinimizedTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.MinimizedTagHelpers_Descriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void NestedScriptTagTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void SymbolBoundAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SymbolBoundTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void EnumTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.EnumTagHelperDescriptors);
        }

        [Fact(Skip="https://github.com/aspnet/AspNetCore/issues/6549")]
        public void TagHelpersWithTemplate_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }
        #endregion

        private void DesignTimeTest()
        {
            // Arrange
            var projectEngine = CreateProjectEngine(builder => 
            {
                builder.ConfigureDocumentClassifier();

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var projectItem = CreateProjectItem();

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(codeDocument.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(codeDocument);
        }

        private void RunTimeTest()
        {
            // Arrange
            var projectEngine = CreateProjectEngine(builder =>
            {
                builder.ConfigureDocumentClassifier();

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var projectItem = CreateProjectItem();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(codeDocument.GetCSharpDocument());
        }

        private void RunRuntimeTagHelpersTest(IEnumerable<TagHelperDescriptor> descriptors)
        {
            // Arrange
            var projectEngine = CreateProjectEngine(builder =>
            {
                builder.ConfigureDocumentClassifier();
                builder.AddTagHelpers(descriptors);

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var projectItem = CreateProjectItem();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(codeDocument.GetCSharpDocument());
        }

        private void RunDesignTimeTagHelpersTest(IEnumerable<TagHelperDescriptor> descriptors)
        {
            // Arrange
            var projectEngine = CreateProjectEngine(builder =>
            {
                builder.ConfigureDocumentClassifier();
                builder.AddTagHelpers(descriptors);

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var projectItem = CreateProjectItem();

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(codeDocument.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(codeDocument);
        }
    }
}
