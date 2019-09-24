// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        [Fact]
        public void Process_WithImportsAndTagHelpers_SetsOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");
            var importItem = new TestRazorProjectItem("_import.cshtml");
            var expectedImports = new[] { RazorSourceDocument.ReadFrom(importItem) };
            var expectedTagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build(),
                TagHelperDescriptorBuilder.Create("Test2TagHelper", "TestAssembly").Build(),
            };

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), expectedImports, expectedTagHelpers);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Same(expectedTagHelpers, tagHelpers);
            Assert.Equal(expectedImports, codeDocument.Imports);
        }

        [Fact]
        public void Process_WithNullTagHelpers_SetsOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), Array.Empty<RazorSourceDocument>(), tagHelpers: null);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Null(tagHelpers);
        }

        [Fact]
        public void Process_SetsNullTagHelpersOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Null(tagHelpers);
        }

        [Fact]
        public void Process_WithNullImports_SetsEmptyListOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), importSources: null, tagHelpers: null);

            // Assert
            Assert.Empty(codeDocument.Imports);
        }

        [Fact]
        public void ProcessDesignTime_WithImportsAndTagHelpers_SetsOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");
            var importItem = new TestRazorProjectItem("_import.cshtml");
            var expectedImports = new[] { RazorSourceDocument.ReadFrom(importItem) };
            var expectedTagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build(),
                TagHelperDescriptorBuilder.Create("Test2TagHelper", "TestAssembly").Build(),
            };

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), expectedImports, expectedTagHelpers);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Same(expectedTagHelpers, tagHelpers);
            Assert.Equal(expectedImports, codeDocument.Imports);
        }

        [Fact]
        public void ProcessDesignTime_WithNullTagHelpers_SetsOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), Array.Empty<RazorSourceDocument>(), tagHelpers: null);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Null(tagHelpers);
        }

        [Fact]
        public void ProcessDesignTime_SetsNullTagHelpersOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(projectItem);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Null(tagHelpers);
        }

        [Fact]
        public void ProcessDesignTime_WithNullImports_SetsEmptyListOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), importSources: null, tagHelpers: null);

            // Assert
            Assert.Empty(codeDocument.Imports);
        }
    }
}
