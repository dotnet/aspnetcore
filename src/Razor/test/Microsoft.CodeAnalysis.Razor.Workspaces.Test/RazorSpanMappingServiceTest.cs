// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class RazorSpanMappingServiceTest : WorkspaceTestBase
    {
        public RazorSpanMappingServiceTest()
        {
            HostProject = TestProjectData.SomeProject;
            HostDocument = TestProjectData.SomeProjectFile1;
        }

        private HostProject HostProject { get; }
        private HostDocument HostDocument { get; }

        protected override void ConfigureLanguageServices(List<ILanguageService> services)
        {
            services.Add(new TestTagHelperResolver());
        }

        [Fact]
        public async Task TryGetLinePositionSpan_SpanMatchesSourceMapping_ReturnsTrue()
        {
            // Arrange
            var sourceText = SourceText.From(@"
@SomeProperty
");

            var project = new DefaultProjectSnapshot(
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Create()));
                }));

            var document = project.GetDocument(HostDocument.FilePath);
            var service = new RazorSpanMappingService(document);

            var output = await document.GetGeneratedOutputAsync();
            var generated = output.GetCSharpDocument();

            var symbol = "SomeProperty";
            var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol), symbol.Length);

            // Act
            var result = RazorSpanMappingService.TryGetLinePositionSpan(span, await document.GetTextAsync(), generated, out var mapped);
            
            // Assert
            Assert.True(result);
            Assert.Equal(new LinePositionSpan(new LinePosition(1, 1), new LinePosition(1, 13)), mapped);
        }

        [Fact]
        public async Task TryGetLinePositionSpan_SpanMatchesSourceMappingAndPosition_ReturnsTrue()
        {
            // Arrange
            var sourceText = SourceText.From(@"
@SomeProperty
@SomeProperty
@SomeProperty
");

            var project = new DefaultProjectSnapshot(
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Create()));
                }));

            var document = project.GetDocument(HostDocument.FilePath);
            var service = new RazorSpanMappingService(document);

            var output = await document.GetGeneratedOutputAsync();
            var generated = output.GetCSharpDocument();

            var symbol = "SomeProperty";
            // Second occurrence
            var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol, generated.GeneratedCode.IndexOf(symbol) + symbol.Length), symbol.Length);

            // Act
            var result = RazorSpanMappingService.TryGetLinePositionSpan(span, await document.GetTextAsync(), generated, out var mapped);

            // Assert
            Assert.True(result);
            Assert.Equal(new LinePositionSpan(new LinePosition(2, 1), new LinePosition(2, 13)), mapped);
        }

        [Fact]
        public async Task TryGetLinePositionSpan_SpanWithinSourceMapping_ReturnsTrue()
        {
            // Arrange
            var sourceText = SourceText.From(@"
@{
    var x = SomeClass.SomeProperty;
}
");

            var project = new DefaultProjectSnapshot(
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Create()));
                }));

            var document = project.GetDocument(HostDocument.FilePath);
            var service = new RazorSpanMappingService(document);

            var output = await document.GetGeneratedOutputAsync();
            var generated = output.GetCSharpDocument();

            var symbol = "SomeProperty";
            var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol), symbol.Length);

            // Act
            var result = RazorSpanMappingService.TryGetLinePositionSpan(span, await document.GetTextAsync(), generated, out var mapped);

            // Assert
            Assert.True(result);
            Assert.Equal(new LinePositionSpan(new LinePosition(2, 22), new LinePosition(2, 34)), mapped);
        }

        [Fact]
        public async Task TryGetLinePositionSpan_SpanOutsideSourceMapping_ReturnsFalse()
        {
            // Arrange
            var sourceText = SourceText.From(@"
@{
    var x = SomeClass.SomeProperty;
}
");

            var project = new DefaultProjectSnapshot(
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Create()));
                }));

            var document = project.GetDocument(HostDocument.FilePath);
            var service = new RazorSpanMappingService(document);

            var output = await document.GetGeneratedOutputAsync();
            var generated = output.GetCSharpDocument();

            var symbol = "ExecuteAsync";
            var span = new TextSpan(generated.GeneratedCode.IndexOf(symbol), symbol.Length);

            // Act
            var result = RazorSpanMappingService.TryGetLinePositionSpan(span, await document.GetTextAsync(), generated, out var mapped);

            // Assert
            Assert.False(result);
        }
    }
}