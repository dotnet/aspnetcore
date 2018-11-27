// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultDocumentSnapshotTest : WorkspaceTestBase
    {
        public DefaultDocumentSnapshotTest()
        {
            var projectState = ProjectState.Create(Workspace.Services, TestProjectData.SomeProject);
            var project = new DefaultProjectSnapshot(projectState);
            HostDocument = new HostDocument(TestProjectData.SomeProjectFile1.FilePath, TestProjectData.SomeProjectFile1.TargetPath);
            SourceText = SourceText.From("<p>Hello World</p>");
            Version = VersionStamp.Create();
            var textAndVersion = TextAndVersion.Create(SourceText, Version);
            var documentState = DocumentState.Create(Workspace.Services, HostDocument, () => Task.FromResult(textAndVersion));
            Document = new DefaultDocumentSnapshot(project, documentState);
        }

        private SourceText SourceText { get; }

        private VersionStamp Version { get; }

        private HostDocument HostDocument { get; }

        private DefaultDocumentSnapshot Document { get; }

        protected override void ConfigureLanguageServices(List<ILanguageService> services)
        {
            services.Add(new TestTagHelperResolver());
        }

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
        public async Task GetGeneratedOutputAsync_SetsOutputWhenDocumentIsNewer()
        {
            // Arrange
            var newSourceText = SourceText.From("NEW!");
            var newDocumentState = Document.State.WithText(newSourceText, Version.GetNewerVersion());
            var newDocument = new DefaultDocumentSnapshot(Document.ProjectInternal, newDocumentState);

            // Force the output to be the new output
            await Document.GetGeneratedOutputAsync();

            // Act
            await newDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(HostDocument.GeneratedCodeContainer.Output);
            Assert.Same(newSourceText, HostDocument.GeneratedCodeContainer.Source);
        }

        [Fact]
        public async Task GetGeneratedOutputAsync_OnlySetsOutputIfDocumentNewer()
        {
            // Arrange
            var newSourceText = SourceText.From("NEW!");
            var newDocumentState = Document.State.WithText(newSourceText, Version.GetNewerVersion());
            var newDocument = new DefaultDocumentSnapshot(Document.ProjectInternal, newDocumentState);

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
