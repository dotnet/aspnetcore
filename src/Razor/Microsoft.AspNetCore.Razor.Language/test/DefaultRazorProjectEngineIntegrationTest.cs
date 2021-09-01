// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        public void Process_GetsImportsFromFeature_MultipleFeatures()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var testImport1 = Mock.Of<RazorProjectItem>(i => i.Read() == new MemoryStream() && i.FilePath == "testvalue1" && i.Exists == true);
            var importFeature1 = new Mock<IImportProjectFeature>();
            importFeature1
                .Setup(feature => feature.GetImports(It.IsAny<RazorProjectItem>()))
                .Returns(new[] { testImport1 });

            var testImport2 = Mock.Of<RazorProjectItem>(i => i.Read() == new MemoryStream() && i.FilePath == "testvalue2" && i.Exists == true);
            var importFeature2 = new Mock<IImportProjectFeature>();
            importFeature2
                .Setup(feature => feature.GetImports(It.IsAny<RazorProjectItem>()))
                .Returns(new[] { testImport2 });

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty, builder =>
            {
                builder.Features.Add(importFeature1.Object);
                builder.Features.Add(importFeature2.Object);
            });

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            Assert.Collection(codeDocument.Imports,
                i => Assert.Equal("testvalue1", i.FilePath),
                i => Assert.Equal("testvalue2", i.FilePath));
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
            var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), "test", expectedImports, expectedTagHelpers);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Same(expectedTagHelpers, tagHelpers);
            Assert.Equal(expectedImports, codeDocument.Imports);
        }

        [Fact]
        public void Process_WithFileKind_SetsOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), "test", Array.Empty<RazorSourceDocument>(), tagHelpers: null);

            // Assert
            var actual = codeDocument.GetFileKind();
            Assert.Equal("test", actual);
        }

        [Fact]
        public void Process_WithNullTagHelpers_SetsOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), "test", Array.Empty<RazorSourceDocument>(), tagHelpers: null);

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
        public void Process_SetsInferredFileKindOnCodeDocument_MvcFile()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var actual = codeDocument.GetFileKind();
            Assert.Same(FileKinds.Legacy, actual);
        }

        [Fact]
        public void Process_SetsInferredFileKindOnCodeDocument_Component()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.razor");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            var actual = codeDocument.GetFileKind();
            Assert.Same(FileKinds.Component, actual);
        }

        [Fact]
        public void Process_WithNullImports_SetsEmptyListOnCodeDocument()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), "test", importSources: null, tagHelpers: null);

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
            var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), "test", expectedImports, expectedTagHelpers);

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
            var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), "test", Array.Empty<RazorSourceDocument>(), tagHelpers: null);

            // Assert
            var tagHelpers = codeDocument.GetTagHelpers();
            Assert.Null(tagHelpers);
        }

        [Fact]
        public void ProcessDesignTime_SetsInferredFileKindOnCodeDocument_MvcFile()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.cshtml");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(projectItem);

            // Assert
            var actual = codeDocument.GetFileKind();
            Assert.Same(FileKinds.Legacy, actual);
        }

        [Fact]
        public void ProcessDesignTime_SetsInferredFileKindOnCodeDocument_Component()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Index.razor");

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, TestRazorProjectFileSystem.Empty);

            // Act
            var codeDocument = projectEngine.ProcessDesignTime(projectItem);

            // Assert
            var actual = codeDocument.GetFileKind();
            Assert.Same(FileKinds.Component, actual);
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
            var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), "test", importSources: null, tagHelpers: null);

            // Assert
            Assert.Empty(codeDocument.Imports);
        }
    }
}
