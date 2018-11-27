// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DocumentStateTest
    {
        public DocumentStateTest()
        {
            TagHelperResolver = new TestTagHelperResolver();

            HostServices = TestServices.Create(
                new IWorkspaceService[]
                {
                    new TestProjectSnapshotProjectEngineFactory(),
                },
                new ILanguageService[]
                {
                    TagHelperResolver,
                });

            HostProject = new HostProject("c:\\MyProject\\Test.csproj", FallbackRazorConfiguration.MVC_2_0);
            HostProjectWithConfigurationChange = new HostProject("c:\\MyProject\\Test.csproj", FallbackRazorConfiguration.MVC_1_0);

            Workspace = TestWorkspace.Create(HostServices);

            var projectId = ProjectId.CreateNewId("Test");
            var solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                "Test",
                "Test",
                LanguageNames.CSharp,
                "c:\\MyProject\\Test.csproj"));
            WorkspaceProject = solution.GetProject(projectId);

            SomeTagHelpers = new List<TagHelperDescriptor>();
            SomeTagHelpers.Add(TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build());

            Document = new HostDocument("c:\\MyProject\\File.cshtml", "File.cshtml");

            Text = SourceText.From("Hello, world!");
            TextLoader = () => Task.FromResult(TextAndVersion.Create(Text, VersionStamp.Create()));
        }

        private HostDocument Document { get; }

        private HostProject HostProject { get; }

        private HostProject HostProjectWithConfigurationChange { get; }

        private Project WorkspaceProject { get; }

        private TestTagHelperResolver TagHelperResolver { get; }

        private HostServices HostServices { get; }

        private Workspace Workspace { get; }

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        private Func<Task<TextAndVersion>> TextLoader { get; }

        private SourceText Text { get; }

        [Fact]
        public async Task DocumentState_CreatedNew_HasEmptyText()
        {
            // Arrange & Act
            var state = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader);
            
            // Assert
            var text = await state.GetTextAsync();
            Assert.Equal(0, text.Length);
        }

        [Fact]
        public async Task DocumentState_WithText_CreatesNewState()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader);

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
            var original = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader);

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
            var original = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader)
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
            var original = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader)
                .WithTextLoader(TextLoader);

            await original.GetTextAsync();

            // Act
            var state = original.WithConfigurationChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }

        [Fact]
        public void DocumentState_WithWorkspaceProjectChange_CachesSnapshotText()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader)
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
            var original = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader)
                .WithTextLoader(TextLoader);

            await original.GetTextAsync();

            // Act
            var state = original.WithWorkspaceProjectChange();

            // Assert
            Assert.True(state.TryGetText(out _));
            Assert.True(state.TryGetTextVersion(out _));
        }

        [Fact]
        public void DocumentState_WithWorkspaceProjectChange_TriesToCacheGeneratedOutput()
        {
            // Arrange
            var original = DocumentState.Create(Workspace.Services, Document, DocumentState.EmptyLoader);

            GC.KeepAlive(original.GeneratedOutput);

            // Act
            var state = original.WithWorkspaceProjectChange();

            // Assert
            Assert.Same(state.GeneratedOutput.Older, original.GeneratedOutput);
        }
    }
}
