// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorProjectEngineIntegrationTest
    {
        [Fact]
        public void Process_GetsImportsFromFeature()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");
            var testImport = TestRazorSourceDocument.Create();
            var importFeature = new Mock<IRazorImportFeature>();
            importFeature.Setup(feature => feature.GetImports(It.IsAny<RazorProjectItem>()))
                .Returns(new[] { testImport });
            var projectEngine = RazorProjectEngine.Create(TestRazorProjectFileSystem.Empty, builder =>
            {
                builder.SetImportFeature(importFeature.Object);
            });

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var import = Assert.Single(codeDocument.Imports);
            Assert.Same(testImport, import);
        }

        [Fact]
        public void Process_GeneratesCodeDocumentWithValidCSharpDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");
            var projectEngine = RazorProjectEngine.Create(TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var csharpDocument = codeDocument.GetCSharpDocument();
            Assert.NotNull(csharpDocument);
            Assert.Empty(csharpDocument.Diagnostics);
        }
    }
}
