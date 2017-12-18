// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class BasicIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void Empty()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
        }

        [Fact]
        public void HelloWorld()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
        }

        [Fact]
        public void CustomDirective()
        {
            // Arrange
            var engine = RazorEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine));
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
        }

        [Fact]
        public void BuildEngine_CallProcess()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create());

            // Act
            engine.Process(document);

            // Assert
            Assert.NotNull(document.GetSyntaxTree());
            Assert.NotNull(document.GetDocumentIntermediateNode());
        }

        [Fact]
        public void CSharpDocument_Runtime_PreservesParserErrors()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create("@!!!", fileName: "test.cshtml"));

            var expected = RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation("test.cshtml", 1, 0, 1), contentLength: 1),
                                "!");

            // Act
            engine.Process(document);

            // Assert
            var csharpDocument = document.GetCSharpDocument();
            var error = Assert.Single(csharpDocument.Diagnostics);
            Assert.Equal(expected, error);
        }

        [Fact]
        public void CSharpDocument_DesignTime_PreservesParserErrors()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime();

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create("@{", fileName: "test.cshtml"));

            var expected = RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                new SourceSpan(new SourceLocation("test.cshtml", 1, 0, 1), contentLength: 1), LegacyResources.BlockName_Code, "}", "{");

            // Act
            engine.Process(document);

            // Assert
            var csharpDocument = document.GetCSharpDocument();
            var error = Assert.Single(csharpDocument.Diagnostics);
            Assert.Equal(expected, error);
        }
    }
}
