// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorTemplateEngineTest
    {
        [Fact]
        public void GenerateCode_ThrowsIfItemCannotBeFound()
        {
            // Arrange
            var project = new TestRazorProject(new RazorProjectItem[] { });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => templateEngine.GenerateCode("/does-not-exist.cstml"));
            Assert.Equal("The item '/does-not-exist.cstml' could not be found.", ex.Message);
        }

        [Fact]
        public void SettingOptions_ThrowsIfValueIsNull()
        {
            // Arrange
            var project = new TestRazorProject(new RazorProjectItem[] { });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNull(
                () => templateEngine.Options = null,
                "value");
        }

        [Fact]
        public void GenerateCode_WithPath()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var projectItem = new TestRazorProjectItem(path);
            var project = new TestRazorProject(new[] { projectItem });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act
            var cSharpDocument = templateEngine.GenerateCode(path);

            // Assert
            Assert.NotNull(cSharpDocument);
            Assert.NotEmpty(cSharpDocument.GeneratedCode);
            Assert.Empty(cSharpDocument.Diagnostics);
        }

        [Fact]
        public void GenerateCode_ThrowsIfProjectItemCannotBeFound()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var project = new TestRazorProject(new RazorProjectItem[] { });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => templateEngine.GenerateCode(path));
            Assert.Equal($"The item '{path}' could not be found.", ex.Message);
        }

        [Fact]
        public void GenerateCode_WithProjectItem()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var projectItem = new TestRazorProjectItem(path);
            var project = new TestRazorProject(new[] { projectItem });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act
            var cSharpDocument = templateEngine.GenerateCode(projectItem);

            // Assert
            Assert.NotNull(cSharpDocument);
            Assert.NotEmpty(cSharpDocument.GeneratedCode);
            Assert.Empty(cSharpDocument.Diagnostics);
        }

        [Fact]
        public void GenerateCode_WithCodeDocument()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var projectItem = new TestRazorProjectItem(path);
            var project = new TestRazorProject(new[] { projectItem });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act - 1
            var codeDocument = templateEngine.CreateCodeDocument(path);

            // Assert - 1
            Assert.NotNull(codeDocument);

            // Act - 2
            var cSharpDocument = templateEngine.GenerateCode(codeDocument);

            // Assert
            Assert.NotNull(cSharpDocument);
            Assert.NotEmpty(cSharpDocument.GeneratedCode);
            Assert.Empty(cSharpDocument.Diagnostics);
        }

        [Fact]
        public void CreateCodeDocument_ThrowsIfPathCannotBeFound()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/Views/Home/Index.cshtml");
            var project = new TestRazorProject(new[] { projectItem });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project)
            {
                Options =
                {
                    ImportsFileName = "MyImport.cshtml",
                }
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => templateEngine.CreateCodeDocument("/DoesNotExist.cshtml"));

            // Assert
            Assert.Equal("The item '/DoesNotExist.cshtml' could not be found.", ex.Message);
        }

        [Fact]
        public void CreateCodeDocument_IncludesImportsIfFileIsPresent()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/Views/Home/Index.cshtml");
            var import1 = new TestRazorProjectItem("/MyImport.cshtml");
            var import2 = new TestRazorProjectItem("/Views/Home/MyImport.cshtml");
            var project = new TestRazorProject(new[] { import1, import2, projectItem });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project)
            {
                Options =
                {
                    ImportsFileName = "MyImport.cshtml",
                }
            };

            // Act
            var codeDocument = templateEngine.CreateCodeDocument("/Views/Home/Index.cshtml");

            // Assert
            Assert.Collection(codeDocument.Imports,
                import => Assert.Equal("/MyImport.cshtml", import.Filename),
                import => Assert.Equal("/Views/Home/MyImport.cshtml", import.Filename));
        }

        [Fact]
        public void CreateCodeDocument_IncludesDefaultImportIfNotNull()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/Views/Home/Index.cshtml");
            var import1 = new TestRazorProjectItem("/MyImport.cshtml");
            var import2 = new TestRazorProjectItem("/Views/Home/MyImport.cshtml");
            var project = new TestRazorProject(new[] { import1, import2, projectItem });
            var razorEngine = RazorEngine.Create();
            var defaultImport = RazorSourceDocument.ReadFrom(new MemoryStream(), "Default.cshtml");
            var templateEngine = new RazorTemplateEngine(razorEngine, project)
            {
                Options =
                {
                    ImportsFileName = "MyImport.cshtml",
                    DefaultImports = defaultImport,
                }
            };

            // Act
            var codeDocument = templateEngine.CreateCodeDocument(projectItem);

            // Assert
            Assert.Collection(codeDocument.Imports,
                import => Assert.Same(defaultImport, import),
                import => Assert.Equal("/MyImport.cshtml", import.Filename),
                import => Assert.Equal("/Views/Home/MyImport.cshtml", import.Filename));
        }
    }
}
