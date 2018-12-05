// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DocumentStateTest : WorkspaceTestBase
    {
        public DocumentStateTest()
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

        private TestTagHelperResolver TagHelperResolver { get; }

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        private Func<Task<TextAndVersion>> TextLoader { get; }

        private SourceText Text { get; }

        protected override void ConfigureLanguageServices(List<ILanguageService> services)
        {
            services.Add(TagHelperResolver);
        }

        [Fact]
        public async Task DocumentState_CreatedNew_HasEmptyText()
        {
            // Arrange & Act
            var state = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader);
            
            // Assert
            var text = await state.GetTextAsync();
            Assert.Equal(0, text.Length);
        }

        [Fact]
        public async Task DocumentState_WithText_CreatesNewState()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader);

            // Act
            var state = original.WithText(Text, VersionStamp.Create());

            // Assert
            var text = await state.GetTextAsync();
            Assert.Same(Text, text);
        }

        [Fact]
        public async Task DocumentState_WithTextLoader_CreatesNewState()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader);

            // Act
            var state = original.WithTextLoader(TextLoader);

            // Assert
            var text = await state.GetTextAsync();
            Assert.Same(Text, text);
        }

        [Fact]
        public void DocumentState_WithConfigurationChange_CachesSnapshotText()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader)
                .WithText(Text, VersionStamp.Create());

            // Act
            var state = original.WithConfigurationChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }

        [Fact]
        public async Task DocumentState_WithConfigurationChange_CachesLoadedText()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader)
                .WithTextLoader(TextLoader);

            await original.GetTextAsync();

            // Act
            var state = original.WithConfigurationChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }

        [Fact]
        public void DocumentState_WithImportsChange_CachesSnapshotText()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader)
                .WithText(Text, VersionStamp.Create());

            // Act
            var state = original.WithImportsChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }

        [Fact]
        public async Task DocumentState_WithImportsChange_CachesLoadedText()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader)
                .WithTextLoader(TextLoader);

            await original.GetTextAsync();

            // Act
            var state = original.WithImportsChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }

        [Fact]
        public void DocumentState_WithWorkspaceProjectChange_CachesSnapshotText()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader)
                .WithText(Text, VersionStamp.Create());

            // Act
            var state = original.WithWorkspaceProjectChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }

        [Fact]
        public async Task DocumentState_WithWorkspaceProjectChange_CachesLoadedText()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, HostDocument, DocumentState.EmptyLoader)
                .WithTextLoader(TextLoader);

            await original.GetTextAsync();

            // Act
            var state = original.WithWorkspaceProjectChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }
    }
}
