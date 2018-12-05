// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class ProjectStateGeneratedOutputTest : WorkspaceTestBase
    {
        public ProjectStateGeneratedOutputTest()
        {
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

            SomeTagHelpers = new List<TagHelperDescriptor>();
            SomeTagHelpers.Add(TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build());

            HostDocument = TestProjectData.SomeProjectFile1;

            Text = SourceText.From("Hello, world!");
            TextLoader = () => Task.FromResult(TextAndVersion.Create(Text, VersionStamp.Create()));
        }

        private HostDocument HostDocument { get; }

        private HostProject HostProject { get; }

        private HostProject HostProjectWithConfigurationChange { get; }

        private Project WorkspaceProject { get; }

        private TestTagHelperResolver TagHelperResolver { get; } = new TestTagHelperResolver();

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        private Func<Task<TextAndVersion>> TextLoader { get; }

        private SourceText Text { get; }

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
        public async Task HostDocumentAdded_CachesOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithAddedHostDocument(TestProjectData.AnotherProjectFile1, DocumentState.EmptyLoader);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.Same(originalOutput, actualOutput);
            Assert.Equal(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(await state.GetComputedStateVersionAsync(new DefaultProjectSnapshot(state)), actualOutputVersion);
        }

        [Fact]
        public async Task HostDocumentAdded_Import_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(state.DocumentCollectionVersion, actualInputVersion);
        }

        [Fact]
        public async Task HostDocumentChanged_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var version = VersionStamp.Create();
            var state = original.WithChangedHostDocument(HostDocument, () =>
            {
                return Task.FromResult(TextAndVersion.Create(SourceText.From("@using System"), version));
            });

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.NotEqual(originalOutputVersion, actualOutputVersion);
            Assert.Equal(version, actualInputVersion);
        }

        [Fact]
        public async Task HostDocumentChanged_Import_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var version = VersionStamp.Create();
            var state = original.WithChangedHostDocument(TestProjectData.SomeProjectImportFile, () =>
            {
                return Task.FromResult(TextAndVersion.Create(SourceText.From("@using System"), version));
            });

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.NotEqual(originalOutputVersion, actualOutputVersion);
            Assert.Equal(version, actualInputVersion);
        }

        [Fact]
        public async Task HostDocumentRemoved_Import_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithRemovedHostDocument(TestProjectData.SomeProjectImportFile);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(state.DocumentCollectionVersion, actualInputVersion);
        }

        [Fact]
        public async Task WorkspaceProjectChange_CachesOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithWorkspaceProject(WorkspaceProject.WithAssemblyName("Test2"));

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.Same(originalOutput, actualOutput);
            Assert.Equal(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(await state.GetComputedStateVersionAsync(new DefaultProjectSnapshot(state)), actualInputVersion);
        }

        // The generated code's text doesn't change as a result, so the output version does not change
        [Fact]
        public async Task WorkspaceProjectChange_WithTagHelperChange_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            TagHelperResolver.TagHelpers = SomeTagHelpers;

            // Act
            var state = original.WithWorkspaceProject(WorkspaceProject.WithAssemblyName("Test2"));

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(await state.GetComputedStateVersionAsync(new DefaultProjectSnapshot(state)), actualInputVersion);
        }

        [Fact]
        public async Task ConfigurationChange_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithHostProject(HostProjectWithConfigurationChange);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.NotEqual(originalOutputVersion, actualOutputVersion);
            Assert.Equal(await state.GetComputedStateVersionAsync(new DefaultProjectSnapshot(state)), actualInputVersion);
        }

        private static Task<(RazorCodeDocument, VersionStamp, VersionStamp)> GetOutputAsync(ProjectState project, HostDocument hostDocument)
        {
            var document = project.Documents[hostDocument.FilePath];
            return GetOutputAsync(project, document);
        }

        private static Task<(RazorCodeDocument, VersionStamp, VersionStamp)> GetOutputAsync(ProjectState project, DocumentState document)
        {

            var projectSnapshot = new DefaultProjectSnapshot(project);
            var documentSnapshot = new DefaultDocumentSnapshot(projectSnapshot, document);
            return document.GetGeneratedOutputAndVersionAsync(projectSnapshot, documentSnapshot);
        }
    }
}
