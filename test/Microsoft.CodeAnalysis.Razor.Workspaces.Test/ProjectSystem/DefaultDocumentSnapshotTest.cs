// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultDocumentSnapshotTest
    {
        public DefaultDocumentSnapshotTest()
        {
            var services = TestServices.Create(
                new[] { new TestProjectSnapshotProjectEngineFactory() },
                new[] { new TestTagHelperResolver() });
            Workspace = TestWorkspace.Create(services);
            var hostProject = new HostProject("C:/some/path/project.csproj", RazorConfiguration.Default);
            var projectState = ProjectState.Create(Workspace.Services, hostProject);
            var project = new DefaultProjectSnapshot(projectState);
            HostDocument = new HostDocument("C:/some/path/file.cshtml", "C:/some/path/file.cshtml");
            SourceText = Text.SourceText.From("<p>Hello World</p>");
            Version = VersionStamp.Default.GetNewerVersion();
            var textAndVersion = TextAndVersion.Create(SourceText, Version);
            var documentState = DocumentState.Create(Workspace.Services, HostDocument, () => Task.FromResult(textAndVersion));
            Document = new DefaultDocumentSnapshot(project, documentState);

        }

        private Workspace Workspace { get; }

        private SourceText SourceText { get; }

        private VersionStamp Version { get; }

        private HostDocument HostDocument { get; }

        private DefaultDocumentSnapshot Document { get; }

        [Fact]
        public async Task GetGeneratedOutputAsync_SetsHostDocumentOutput()
        {
            // Act
            await Document.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(HostDocument.GeneratedCodeContainer.Output);
            Assert.Same(SourceText, HostDocument.GeneratedCodeContainer.Source);
        }

        [Fact]
        public async Task GetGeneratedOutputAsync_OnlySetsOutputIfDocumentNewer()
        {
            // Arrange
            var newSourceText = SourceText.From("NEW!");
            var newDocumentState = Document.State.WithText(newSourceText, Version.GetNewerVersion());
            var newDocument = new DefaultDocumentSnapshot(Document.Project, newDocumentState);

            // Force the output to be the new output
            await newDocument.GetGeneratedOutputAsync();

            // Act
            await Document.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(HostDocument.GeneratedCodeContainer.Output);
            Assert.Same(newSourceText, HostDocument.GeneratedCodeContainer.Source);
        }
    }
}
