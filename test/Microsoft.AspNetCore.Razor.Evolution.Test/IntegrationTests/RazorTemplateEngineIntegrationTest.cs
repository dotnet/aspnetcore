// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
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
            var project = new TestRazorProject(new[] { projectItem });
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
    }
}
