// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorProjectEngineIntegrationTest
    {
        [Fact]
        public void Process_SetsOptions_Runtime()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var parserOptions = codeDocument.GetParserOptions();
            Assert.False(parserOptions.DesignTime);

            var codeGenerationOptions = codeDocument.GetCodeGenerationOptions();
            Assert.False(codeGenerationOptions.DesignTime);
            Assert.False(codeGenerationOptions.SuppressChecksum);
            Assert.False(codeGenerationOptions.SuppressMetadataAttributes);
        }

        [Fact]
        public void ProcessDesignTime_SetsOptions_DesignTime()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(projectItem);

            // Assert
            var parserOptions = codeDocument.GetParserOptions();
            Assert.True(parserOptions.DesignTime);

            var codeGenerationOptions = codeDocument.GetCodeGenerationOptions();
            Assert.True(codeGenerationOptions.DesignTime);
            Assert.True(codeGenerationOptions.SuppressChecksum);
            Assert.True(codeGenerationOptions.SuppressMetadataAttributes);
        }

        [Fact]
        public void Process_GetsImportsFromFeature()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var testImport = Mock.Of<RazorProjectItem>(i => i.Read() == new MemoryStream() && i.FilePath == "testvalue" && i.Exists == true);
            var importFeature = new Mock<IImportProjectFeature>();
            importFeature
                .Setup(feature => feature.GetImports(It.IsAny<RazorProjectItem>()))
                .Returns(new[] { testImport });

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty, builder =>
            {
                builder.SetImportFeature(importFeature.Object);
            });

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var import = Assert.Single(codeDocument.Imports);
            Assert.Equal("testvalue", import.FilePath);
        }

        [Fact]
        public void Process_GeneratesCodeDocumentWithValidCSharpDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var csharpDocument = codeDocument.GetCSharpDocument();
            Assert.NotNull(csharpDocument);
            Assert.Empty(csharpDocument.Diagnostics);
        }
    }
}
