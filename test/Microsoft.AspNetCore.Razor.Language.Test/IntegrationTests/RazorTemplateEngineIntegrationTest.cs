// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class RazorTemplateEngineIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void GenerateCodeWithDefaults()
        {
            // Arrange
            var filePath = Path.Combine(TestProjectRoot, $"{Filename}.cshtml");
            var content = File.ReadAllText(filePath);
            var projectItem = new TestRazorProjectItem($"{Filename}.cshtml", "")
            {
                Content = content,
            };
            var project = new TestRazorProject(new[]{ projectItem });
            var razorEngine = RazorEngine.Create();
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act
            var resultcSharpDocument = templateEngine.GenerateCode(projectItem.Path);

            // Assert
            AssertCSharpDocumentMatchesBaseline(resultcSharpDocument);
        }

        [Fact]
        public void GenerateCodeWithBaseType()
        {
            // Arrange
            var filePath = Path.Combine(TestProjectRoot, $"{Filename}.cshtml");
            var content = File.ReadAllText(filePath);
            var projectItem = new TestRazorProjectItem($"{Filename}.cshtml", "")
            {
                Content = content,
            };
            var project = new TestRazorProject(new[] { projectItem });
            var razorEngine = RazorEngine.Create(engine => engine.SetBaseType("MyBaseType"));
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act
            var cSharpDocument = templateEngine.GenerateCode(projectItem.Path);

            // Assert
            AssertCSharpDocumentMatchesBaseline(cSharpDocument);
        }

        [Fact]
        public void GenerateCodeWithConfigureClass()
        {
            // Arrange
            var filePath = Path.Combine(TestProjectRoot, $"{Filename}.cshtml");
            var content = File.ReadAllText(filePath);
            var projectItem = new TestRazorProjectItem($"{Filename}.cshtml", "")
            {
                Content = content,
            };
            var project = new TestRazorProject(new[] { projectItem });
            var razorEngine = RazorEngine.Create(engine =>
            {
                engine.ConfigureClass((document, @class) =>
                {
                    @class.Name = "MyClass";
                    @class.AccessModifier = "protected internal";
                });

                engine.ConfigureClass((document, @class) =>
                {
                    @class.Interfaces = new[] { "global::System.IDisposable" };
                    @class.BaseType = "CustomBaseType";
                });
            });
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act
            var cSharpDocument = templateEngine.GenerateCode(projectItem.Path);

            // Assert
            AssertCSharpDocumentMatchesBaseline(cSharpDocument);
        }

        [Fact]
        public void GenerateCodeWithSetNamespace()
        {
            // Arrange
            var filePath = Path.Combine(TestProjectRoot, $"{Filename}.cshtml");
            var content = File.ReadAllText(filePath);
            var projectItem = new TestRazorProjectItem($"{Filename}.cshtml", "")
            {
                Content = content,
            };
            var project = new TestRazorProject(new[] { projectItem });
            var razorEngine = RazorEngine.Create(engine =>
            {
                engine.SetNamespace("MyApp.Razor.Views");
            });
            var templateEngine = new RazorTemplateEngine(razorEngine, project);

            // Act
            var cSharpDocument = templateEngine.GenerateCode(projectItem.Path);

            // Assert
            AssertCSharpDocumentMatchesBaseline(cSharpDocument);
        }
    }
}
