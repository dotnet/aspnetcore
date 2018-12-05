// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotTest : WorkspaceTestBase
    {
        public DefaultProjectSnapshotTest()
        {
            TagHelperResolver = new TestTagHelperResolver();

            HostProject = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_2_0);
            HostProjectWithConfigurationChange = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_1_0);

            var projectId = ProjectId.CreateNewId("Test");
            var solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                "Test",
                "Test",
                LanguageNames.CSharp,
                TestProjectData.SomeProject.FilePath));
            WorkspaceProject = solution.GetProject(projectId);

            SomeTagHelpers = new List<TagHelperDescriptor>
            {
                TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build()
            };

            Documents = new HostDocument[]
            {
                TestProjectData.SomeProjectFile1,
                TestProjectData.SomeProjectFile2,

                // linked file
                TestProjectData.AnotherProjectNestedFile3,
            };
        }

        private HostDocument[] Documents { get; }

        private HostProject HostProject { get; }

        private HostProject HostProjectWithConfigurationChange { get; }

        private Project WorkspaceProject { get; }

        private TestTagHelperResolver TagHelperResolver { get; }

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        private void Configure(RazorProjectEngineBuilder builder)
        {
            builder.Features.Remove(builder.Features.OfType<IImportProjectFeature>().Single());
            builder.Features.Add(new TestImportProjectFeature());
        }

        protected override void ConfigureLanguageServices(List<ILanguageService> services)
        {
            services.Add(TagHelperResolver);
        }

        protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        {
            builder.Features.Remove(builder.Features.OfType<IImportProjectFeature>().Single());
            builder.Features.Add(new TestImportProjectFeature());
        }

        [Fact]
        public void ProjectSnapshot_CachesDocumentSnapshots()
        {
            // Arrange
            var state = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader);
            var snapshot = new DefaultProjectSnapshot(state);

            // Act
            var documents = snapshot.DocumentFilePaths.ToDictionary(f => f, f => snapshot.GetDocument(f));

            // Assert
            Assert.Collection(
                documents,
                d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)),
                d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)),
                d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)));
        }

        [Fact]
        public void IsImportDocument_NonImportDocument_ReturnsFalse()
        {
            // Arrange
            var state = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader);
            var snapshot = new DefaultProjectSnapshot(state);

            var document = snapshot.GetDocument(Documents[0].FilePath);

            // Act
            var result = snapshot.IsImportDocument(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsImportDocument_ImportDocument_ReturnsTrue()
        {
            // Arrange
            var state = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);
            var snapshot = new DefaultProjectSnapshot(state);

            var document = snapshot.GetDocument(TestProjectData.SomeProjectImportFile.FilePath);

            // Act
            var result = snapshot.IsImportDocument(document);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetRelatedDocuments_NonImportDocument_ReturnsEmpty()
        {
            // Arrange
            var state = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader);
            var snapshot = new DefaultProjectSnapshot(state);

            var document = snapshot.GetDocument(Documents[0].FilePath);

            // Act
            var documents = snapshot.GetRelatedDocuments(document);

            // Assert
            Assert.Empty(documents);
        }

        [Fact]
        public void GetRelatedDocuments_ImportDocument_ReturnsRelated()
        {
            // Arrange
            var state = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);
            var snapshot = new DefaultProjectSnapshot(state);

            var document = snapshot.GetDocument(TestProjectData.SomeProjectImportFile.FilePath);

            // Act
            var documents = snapshot.GetRelatedDocuments(document);

            // Assert
            Assert.Collection(
                documents.OrderBy(d => d.FilePath),
                d => Assert.Equal(Documents[0].FilePath, d.FilePath),
                d => Assert.Equal(Documents[1].FilePath, d.FilePath));
        }
    }
}
