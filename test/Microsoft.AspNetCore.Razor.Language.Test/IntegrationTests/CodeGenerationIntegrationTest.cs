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
        [Fact]
        public void IncompleteDirectives_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void CSharp7_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void BasicImports_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void UnfinishedExpressionInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void Templates_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void StringLiterals_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void SimpleUnspacedIf_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void Sections_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void RazorComments_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ParserError_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void OpenedIf_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void NullConditionalExpressions_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void NoLinePragmas_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void NestedCSharp_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void NestedCodeBlocks_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void MarkupInCodeBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void Instrumented_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void InlineBlocks_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void Inherits_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void Usings_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ImplicitExpressionAtEOF_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ImplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void HtmlCommentWithQuote_Double_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void HtmlCommentWithQuote_Single_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void HiddenSpansInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void FunctionsBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void FunctionsBlockMinimal_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ExpressionsInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ExplicitExpressionWithMarkup_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ExplicitExpressionAtEOF_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ExplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void EmptyImplicitExpressionInCode_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void EmptyImplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void EmptyExplicitExpression_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void EmptyCodeBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void ConditionalAttributes_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void CodeBlockWithTextElement_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void CodeBlockAtEOF_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void CodeBlock_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void Blocks_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void Await_Runtime()
        {
            RunTimeTest();
        }

        [Fact]
        public void SimpleTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersWithBoundAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersWithPrefix_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void NestedTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void SingleTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void SingleTagHelperWithNewlineBeforeAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersWithWeirdlySpacedAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void IncompleteTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void BasicTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void BasicTagHelpers_Prefixed_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void BasicTagHelpers_RemoveTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void CssSelectorTagHelperAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.CssSelectorTagHelperDescriptors);
        }

        [Fact]
        public void ComplexTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void EmptyAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void EscapedTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void DuplicateTargetTagHelper_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DuplicateTargetTagHelperDescriptors);
        }

        [Fact]
        public void AttributeTargetingTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.AttributeTargetingTagHelperDescriptors);
        }

        [Fact]
        public void PrefixedAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.PrefixedAttributeTagHelperDescriptors);
        }

        [Fact]
        public void DuplicateAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void DynamicAttributeTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DynamicAttributeTagHelpers_Descriptors);
        }

        [Fact]
        public void TransitionsInTagHelperAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void MinimizedTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.MinimizedTagHelpers_Descriptors);
        }

        [Fact]
        public void NestedScriptTagTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void SymbolBoundAttributes_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SymbolBoundTagHelperDescriptors);
        }

        [Fact]
        public void EnumTagHelpers_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.EnumTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersInSection_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.TagHelpersInSectionDescriptors);
        }

        [Fact]
        public void TagHelpersWithTemplate_Runtime()
        {
            // Arrange, Act & Assert
            RunRuntimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }
        #endregion

        #region DesignTime
        [Fact]
        public void IncompleteDirectives_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void CSharp7_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void BasicImports_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void UnfinishedExpressionInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void Templates_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void StringLiterals_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void SimpleUnspacedIf_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void Sections_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void RazorComments_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ParserError_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void OpenedIf_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void NullConditionalExpressions_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void NoLinePragmas_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void NestedCSharp_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void NestedCodeBlocks_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void MarkupInCodeBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void Instrumented_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void InlineBlocks_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void Inherits_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void Usings_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ImplicitExpressionAtEOF_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ImplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void HtmlCommentWithQuote_Double_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void HtmlCommentWithQuote_Single_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void HiddenSpansInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void FunctionsBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void FunctionsBlockMinimal_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ExpressionsInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ExplicitExpressionWithMarkup_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ExplicitExpressionAtEOF_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ExplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void EmptyImplicitExpressionInCode_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void EmptyImplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void EmptyExplicitExpression_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void EmptyCodeBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void DesignTime_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void ConditionalAttributes_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void CodeBlockWithTextElement_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void CodeBlockAtEOF_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void CodeBlock_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void Blocks_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void Await_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void AddTagHelperDirective_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void RemoveTagHelperDirective_DesignTime()
        {
            DesignTimeTest();
        }

        [Fact]
        public void SimpleTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersWithBoundAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersWithPrefix_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void NestedTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }

        [Fact]
        public void SingleTagHelper_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void SingleTagHelperWithNewlineBeforeAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersWithWeirdlySpacedAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void IncompleteTagHelper_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void BasicTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void BasicTagHelpers_Prefixed_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void ComplexTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void EmptyAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void EscapedTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void DuplicateTargetTagHelper_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DuplicateTargetTagHelperDescriptors);
        }

        [Fact]
        public void AttributeTargetingTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.AttributeTargetingTagHelperDescriptors);
        }

        [Fact]
        public void PrefixedAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.PrefixedAttributeTagHelperDescriptors);
        }

        [Fact]
        public void DuplicateAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void DynamicAttributeTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DynamicAttributeTagHelpers_Descriptors);
        }

        [Fact]
        public void TransitionsInTagHelperAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void MinimizedTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.MinimizedTagHelpers_Descriptors);
        }

        [Fact]
        public void NestedScriptTagTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);
        }

        [Fact]
        public void SymbolBoundAttributes_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SymbolBoundTagHelperDescriptors);
        }

        [Fact]
        public void EnumTagHelpers_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.EnumTagHelperDescriptors);
        }

        [Fact]
        public void TagHelpersWithTemplate_DesignTime()
        {
            // Arrange, Act & Assert
            RunDesignTimeTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);
        }
        #endregion

        private void DesignTimeTest()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => 
            {
                builder.ConfigureDocumentClassifier();

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(document);
        }

        private void RunTimeTest()
        {
            // Arrange
            var engine = RazorEngine.Create(builder =>
            {
                builder.ConfigureDocumentClassifier();

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        private void RunRuntimeTagHelpersTest(IEnumerable<TagHelperDescriptor> descriptors)
        {
            // Arrange
            var engine = RazorEngine.Create(builder =>
            {
                builder.ConfigureDocumentClassifier();
                builder.AddTagHelpers(descriptors);

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        private void RunDesignTimeTagHelpersTest(IEnumerable<TagHelperDescriptor> descriptors)
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder =>
            {
                builder.ConfigureDocumentClassifier();
                builder.AddTagHelpers(descriptors);

                // Some of these tests use templates
                builder.AddTargetExtension(new TemplateTargetExtension());

                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(document);
        }
    }
}
