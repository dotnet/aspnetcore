// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    public class CodeGenerationIntegrationTest : IntegrationTestBase
    {
        #region Runtime
        [Fact]
        public void UnfinishedExpressionInCode_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Templates_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void StringLiterals_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void SimpleUnspacedIf_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Sections_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void RazorComments_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ParserError_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void OpenedIf_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void NullConditionalExpressions_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void NoLinePragmas_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void NestedCSharp_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void NestedCodeBlocks_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void MarkupInCodeBlock_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Instrumented_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InlineBlocks_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Inherits_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Imports_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ImplicitExpressionAtEOF_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ImplicitExpression_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void HtmlCommentWithQuote_Double_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void HtmlCommentWithQuote_Single_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void HiddenSpansInCode_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void FunctionsBlock_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void FunctionsBlockMinimal_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ExpressionsInCode_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ExplicitExpressionWithMarkup_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ExplicitExpressionAtEOF_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ExplicitExpression_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void EmptyImplicitExpressionInCode_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void EmptyImplicitExpression_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void EmptyExplicitExpression_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void EmptyCodeBlock_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ConditionalAttributes_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void CodeBlockWithTextElement_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void CodeBlockAtEOF_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void CodeBlock_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Blocks_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Await_Runtime()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }
        #endregion

        #region DesignTime
        [Fact]
        public void UnfinishedExpressionInCode_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void Templates_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void StringLiterals_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void SimpleUnspacedIf_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void Sections_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void RazorComments_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ParserError_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void OpenedIf_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void NullConditionalExpressions_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void NoLinePragmas_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void NestedCSharp_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void NestedCodeBlocks_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void MarkupInCodeBlock_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void Instrumented_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void InlineBlocks_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void Inherits_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void Imports_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ImplicitExpressionAtEOF_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ImplicitExpression_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void HtmlCommentWithQuote_Double_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void HtmlCommentWithQuote_Single_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void HiddenSpansInCode_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void FunctionsBlock_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void FunctionsBlockMinimal_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ExpressionsInCode_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ExplicitExpressionWithMarkup_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ExplicitExpressionAtEOF_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ExplicitExpression_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void EmptyImplicitExpressionInCode_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void EmptyImplicitExpression_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void EmptyExplicitExpression_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void EmptyCodeBlock_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void DesignTime_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void ConditionalAttributes_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void CodeBlockWithTextElement_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void CodeBlockAtEOF_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void CodeBlock_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void Blocks_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }

        [Fact]
        public void Await_DesignTime()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDesignTimeDocumentMatchBaseline(document);
        }
        #endregion

        protected override RazorCodeDocument CreateCodeDocument()
        {
            if (Filename == null)
            {
                var message = $"{nameof(CreateCodeDocument)} should only be called from an integration test ({nameof(Filename)} is null).";
                throw new InvalidOperationException(message);
            }

            var normalizedFileName = Filename.Substring(0, Filename.LastIndexOf("_"));
            var sourceFilename = Path.ChangeExtension(normalizedFileName, ".cshtml");
            var testFile = TestFile.Create(sourceFilename);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {sourceFilename} was not found.");
            }

            return RazorCodeDocument.Create(TestRazorSourceDocument.CreateResource(sourceFilename));
        }

        private class ApiSetsIRTestAdapter : RazorIRPassBase
        {
            public override DocumentIRNode ExecuteCore(DocumentIRNode irDocument)
            {
                var walker = new ApiSetsIRWalker();
                walker.Visit(irDocument);

                return irDocument;
            }

            private class ApiSetsIRWalker : RazorIRNodeWalker
            {
                public override void VisitClass(ClassDeclarationIRNode node)
                {
                    node.Name = Filename.Replace('/', '_');
                    node.AccessModifier = "public";

                    VisitDefault(node);
                }

                public override void VisitNamespace(NamespaceDeclarationIRNode node)
                {
                    node.Content = typeof(CodeGenerationIntegrationTest).Namespace + ".TestFiles";

                    VisitDefault(node);
                }

                public override void VisitRazorMethodDeclaration(RazorMethodDeclarationIRNode node)
                {
                    node.AccessModifier = "public";
                    node.Modifiers = new[] { "async" };
                    node.ReturnType = typeof(Task).FullName;
                    node.Name = "ExecuteAsync";

                    VisitDefault(node);
                }
            }
        }
    }
}
