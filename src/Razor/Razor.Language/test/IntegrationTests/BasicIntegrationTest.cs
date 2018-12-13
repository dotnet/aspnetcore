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
            var projectEngine = CreateProjectEngine();
            var projectItem = CreateProjectItem();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
        }

        [Fact]
        public void HelloWorld()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var projectItem = CreateProjectItem();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
        }

        [Fact]
        public void CustomDirective()
        {
            // Arrange
            var projectEngine = CreateProjectEngine(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine));
            });

            var projectItem = CreateProjectItem();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
        }

        [Fact]
        public void BuildEngine_CallProcess()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            Assert.NotNull(codeDocument.GetSyntaxTree());
            Assert.NotNull(codeDocument.GetDocumentIntermediateNode());
        }

        [Fact]
        public void CSharpDocument_Runtime_PreservesParserErrors()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var projectItem = new TestRazorProjectItem("test.cshtml")
            {
                Content = "@!!!"
            };

            var expected = RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation("test.cshtml", 1, 0, 1), contentLength: 1),
                                "!");

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var csharpDocument = codeDocument.GetCSharpDocument();
            var error = Assert.Single(csharpDocument.Diagnostics);
            Assert.Equal(expected, error);
        }

        [Fact]
        public void CSharpDocument_DesignTime_PreservesParserErrors()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var projectItem = new TestRazorProjectItem("test.cshtml")
            {
                Content = "@{"
            };

            var expected = RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                new SourceSpan(new SourceLocation("test.cshtml", 1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{");

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(projectItem);

            // Assert
            var csharpDocument = codeDocument.GetCSharpDocument();
            var error = Assert.Single(csharpDocument.Diagnostics);
            Assert.Equal(expected, error);
        }
    }
}
