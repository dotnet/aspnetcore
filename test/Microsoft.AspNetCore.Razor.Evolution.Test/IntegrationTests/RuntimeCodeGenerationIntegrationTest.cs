// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    public class RuntimeCodeGenerationIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void UnfinishedExpressionInCode()
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
        public void Templates()
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
        public void StringLiterals()
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
        public void SimpleUnspacedIf()
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
        public void Sections()
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
        public void RazorComments()
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
        public void ParserError()
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
        public void OpenedIf()
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
        public void NullConditionalExpressions()
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
        public void NoLinePragmas()
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
        public void NestedCSharp()
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
        public void NestedCodeBlocks()
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
        public void MarkupInCodeBlock()
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
        public void Instrumented()
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
        public void InlineBlocks()
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
        public void Inherits()
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
        public void Imports()
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
        public void ImplicitExpressionAtEOF()
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
        public void ImplicitExpression()
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
        public void HtmlCommentWithQuote_Double()
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
        public void HtmlCommentWithQuote_Single()
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
        public void HiddenSpansInCode()
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
        public void FunctionsBlock()
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
        public void FunctionsBlockMinimal()
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
        public void ExpressionsInCode()
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
        public void ExplicitExpressionWithMarkup()
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
        public void ExplicitExpressionAtEOF()
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
        public void ExplicitExpression()
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
        public void EmptyImplicitExpressionInCode()
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
        public void EmptyImplicitExpression()
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
        public void EmptyExplicitExpression()
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
        public void EmptyCodeBlock()
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
        public void DesignTime()
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
        public void ConditionalAttributes()
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
        public void CodeBlockWithTextElement()
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
        public void CodeBlockAtEOF()
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
        public void CodeBlock()
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
        public void Blocks()
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
        public void Await()
        {
            // Arrange
            var engine = RazorEngine.Create(builder => builder.Features.Add(new ApiSetsIRTestAdapter()));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        private class ApiSetsIRTestAdapter : IRazorIRPass
        {
            public RazorEngine Engine { get; set; }

            public int Order { get; set; }

            public DocumentIRNode Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
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
                    node.Content = typeof(RuntimeCodeGenerationIntegrationTest).Namespace + ".TestFiles";

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
