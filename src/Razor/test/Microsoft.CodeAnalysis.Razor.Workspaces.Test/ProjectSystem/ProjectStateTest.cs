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
    public class ProjectStateTest
    {
        public ProjectStateTest()
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

            Documents = new HostDocument[]
            {
                new HostDocument("c:\\MyProject\\File.cshtml", "File.cshtml"),
                new HostDocument("c:\\MyProject\\Index.cshtml", "Index.cshtml"),

                // linked file
                new HostDocument("c:\\SomeOtherProject\\Index.cshtml", "Pages\\Index.cshtml"),
            };

            Text = SourceText.From("Hello, world!");
            TextLoader = () => Task.FromResult(TextAndVersion.Create(Text, VersionStamp.Create()));
        }

        private HostDocument[] Documents { get; }

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
        public void ProjectState_ConstructedNew()
        {
            // Arrange
             
            // Act
            var state = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject);

            // Assert
            Assert.Empty(state.Documents);
            Assert.NotEqual(VersionStamp.Default, state.Version);
        }

        [Fact]
        public void ProjectState_AddHostDocument_ToEmpty()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject);

            // Act
            var state = original.WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader);

            // Assert
            Assert.NotEqual(original.Version, state.Version);

            Assert.Collection(
                state.Documents.OrderBy(kvp => kvp.Key),
                d => Assert.Same(Documents[0], d.Value.HostDocument));
        }

        [Fact] // When we first add a document, we have no way to read the text, so it's empty.
        public async Task ProjectState_AddHostDocument_DocumentIsEmpty()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject);

            // Act
            var state = original.WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader);

            // Assert
            var text = await state.Documents[Documents[0].FilePath].GetTextAsync();
            Assert.Equal(0, text.Length);
        }

        [Fact]
        public void ProjectState_AddHostDocument_ToProjectWithDocuments()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader);

            // Assert
            Assert.NotEqual(original.Version, state.Version);

            Assert.Collection(
                state.Documents.OrderBy(kvp => kvp.Key),
                d => Assert.Same(Documents[0], d.Value.HostDocument),
                d => Assert.Same(Documents[1], d.Value.HostDocument),
                d => Assert.Same(Documents[2], d.Value.HostDocument));
        }

        [Fact]
        public void ProjectState_AddHostDocument_RetainsComputedState()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithAddedHostDocument(Documents[0], DocumentState.EmptyLoader);

            // Assert
            Assert.Same(original.ProjectEngine, state.ProjectEngine);
            Assert.Same(original.TagHelpers, state.TagHelpers);

            Assert.Same(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
            Assert.Same(original.Documents[Documents[2].FilePath], state.Documents[Documents[2].FilePath]);
        }

        [Fact]
        public void ProjectState_AddHostDocument_DuplicateNoops()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithAddedHostDocument(new HostDocument(Documents[1].FilePath, "SomePath.cshtml"), DocumentState.EmptyLoader);

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public async Task ProjectState_WithChangedHostDocument_Loader()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithChangedHostDocument(Documents[1], TextLoader);

            // Assert
            Assert.NotEqual(original.Version, state.Version);

            var text = await state.Documents[Documents[1].FilePath].GetTextAsync();
            Assert.Same(Text, text);
        }

        [Fact]
        public async Task ProjectState_WithChangedHostDocument_Snapshot()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithChangedHostDocument(Documents[1], Text, VersionStamp.Create());

            // Assert
            Assert.NotEqual(original.Version, state.Version);

            var text = await state.Documents[Documents[1].FilePath].GetTextAsync();
            Assert.Same(Text, text);
        }

        [Fact]
        public void ProjectState_WithChangedHostDocument_Loader_RetainsComputedState()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithChangedHostDocument(Documents[1], TextLoader);

            // Assert
            Assert.Same(original.ProjectEngine, state.ProjectEngine);
            Assert.Same(original.TagHelpers, state.TagHelpers);

            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
        }

        [Fact]
        public void ProjectState_WithChangedHostDocument_Snapshot_RetainsComputedState()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithChangedHostDocument(Documents[1], Text, VersionStamp.Create());

            // Assert
            Assert.Same(original.ProjectEngine, state.ProjectEngine);
            Assert.Same(original.TagHelpers, state.TagHelpers);

            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
        }

        [Fact]
        public void ProjectState_WithChangedHostDocument_Loader_NotFoundNoops()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithChangedHostDocument(Documents[0], TextLoader);

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public void ProjectState_WithChangedHostDocument_Snapshot_NotFoundNoops()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithChangedHostDocument(Documents[0], Text, VersionStamp.Create());

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public void ProjectState_RemoveHostDocument_FromProjectWithDocuments()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithRemovedHostDocument(Documents[1]);

            // Assert
            Assert.NotEqual(original.Version, state.Version);

            Assert.Collection(
                state.Documents.OrderBy(kvp => kvp.Key),
                d => Assert.Same(Documents[2], d.Value.HostDocument));
        }

        [Fact]
        public void ProjectState_RemoveHostDocument_RetainsComputedState()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithRemovedHostDocument(Documents[2]);

            // Assert
            Assert.Same(original.ProjectEngine, state.ProjectEngine);
            Assert.Same(original.TagHelpers, state.TagHelpers);

            Assert.Same(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
        }

        [Fact]
        public void ProjectState_RemoveHostDocument_NotFoundNoops()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Act
            var state = original.WithRemovedHostDocument(Documents[0]);

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public void ProjectState_WithHostProject_ConfigurationChange_UpdatesComputedState()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithHostProject(HostProjectWithConfigurationChange);

            // Assert
            Assert.NotEqual(original.Version, state.Version);
            Assert.Same(HostProjectWithConfigurationChange, state.HostProject);

            Assert.NotSame(original.ProjectEngine, state.ProjectEngine);
            Assert.NotSame(original.TagHelpers, state.TagHelpers);

            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
            Assert.NotSame(original.Documents[Documents[2].FilePath], state.Documents[Documents[2].FilePath]);
        }

        [Fact]
        public void ProjectState_WithHostProject_NoConfigurationChange_Noops()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithHostProject(HostProject);

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public void ProjectState_WithHostProject_CallsConfigurationChangeOnDocumentState()
        {
            // Arrange
            var callCount = 0;

            var documents = new Dictionary<string, DocumentState>();
            documents[Documents[1].FilePath] = TestDocumentState.Create(Workspace.Services, Documents[1], onConfigurationChange: () => callCount++);
            documents[Documents[2].FilePath] = TestDocumentState.Create(Workspace.Services, Documents[2], onConfigurationChange: () => callCount++);

            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject);
            original.Documents = documents;

            var changed = WorkspaceProject.WithAssemblyName("Test1");

            // Act
            var state = original.WithHostProject(HostProjectWithConfigurationChange);

            // Assert
            Assert.NotEqual(original.Version, state.Version);
            Assert.Same(HostProjectWithConfigurationChange, state.HostProject);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void ProjectState_WithWorkspaceProject_Removed()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithWorkspaceProject(null);

            // Assert
            Assert.NotEqual(original.Version, state.Version);
            Assert.Null(state.WorkspaceProject);

            Assert.Same(original.ProjectEngine, state.ProjectEngine);
            Assert.NotSame(original.TagHelpers, state.TagHelpers);

            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
            Assert.NotSame(original.Documents[Documents[2].FilePath], state.Documents[Documents[2].FilePath]);
        }

        [Fact]
        public void ProjectState_WithWorkspaceProject_Added()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, null)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithWorkspaceProject(WorkspaceProject);

            // Assert
            Assert.NotEqual(original.Version, state.Version);
            Assert.Same(WorkspaceProject, state.WorkspaceProject);

            Assert.Same(original.ProjectEngine, state.ProjectEngine);
            Assert.NotSame(original.TagHelpers, state.TagHelpers);

            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
        }

        [Fact]
        public void ProjectState_WithWorkspaceProject_Changed()
        {
            // Arrange
            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2], DocumentState.EmptyLoader)
                .WithAddedHostDocument(Documents[1], DocumentState.EmptyLoader);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            var changed = WorkspaceProject.WithAssemblyName("Test1");

            // Act
            var state = original.WithWorkspaceProject(changed);

            // Assert
            Assert.NotEqual(original.Version, state.Version);
            Assert.Same(changed, state.WorkspaceProject);

            Assert.Same(original.ProjectEngine, state.ProjectEngine);
            Assert.NotSame(original.TagHelpers, state.TagHelpers);

            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
            Assert.NotSame(original.Documents[Documents[2].FilePath], state.Documents[Documents[2].FilePath]);
        }

        [Fact]
        public void ProjectState_WithWorkspaceProject_CallsWorkspaceProjectChangeOnDocumentState()
        {
            // Arrange
            var callCount = 0;

            var documents = new Dictionary<string, DocumentState>();
            documents[Documents[1].FilePath] = TestDocumentState.Create(Workspace.Services, Documents[1], onWorkspaceProjectChange: () => callCount++);
            documents[Documents[2].FilePath] = TestDocumentState.Create(Workspace.Services, Documents[2], onWorkspaceProjectChange: () => callCount++);

            var original = ProjectState.Create(Workspace.Services, HostProject, WorkspaceProject);
            original.Documents = documents;

            var changed = WorkspaceProject.WithAssemblyName("Test1");

            // Act
            var state = original.WithWorkspaceProject(changed);

            // Assert
            Assert.NotEqual(original.Version, state.Version);
            Assert.Equal(2, callCount);
        }

        private class TestDocumentState : DocumentState
        {
            public static TestDocumentState Create(
                HostWorkspaceServices services,
                HostDocument hostDocument,
                Func<Task<TextAndVersion>> loader = null,
                Action onTextChange = null,
                Action onTextLoaderChange = null,
                Action onConfigurationChange = null,
                Action onWorkspaceProjectChange = null)
            {
                return new TestDocumentState(services, hostDocument, null, null, loader, onTextChange, onTextLoaderChange, onConfigurationChange, onWorkspaceProjectChange);
            }

            Action _onTextChange;
            Action _onTextLoaderChange;
            Action _onConfigurationChange;
            Action _onWorkspaceProjectChange;

            private TestDocumentState(
                HostWorkspaceServices services,
                HostDocument hostDocument,
                SourceText text,
                VersionStamp? version,
                Func<Task<TextAndVersion>> loader,
                Action onTextChange,
                Action onTextLoaderChange,
                Action onConfigurationChange,
                Action onWorkspaceProjectChange)
                : base(services, hostDocument, text, version, loader)
            {
                _onTextChange = onTextChange;
                _onTextLoaderChange = onTextLoaderChange;
                _onConfigurationChange = onConfigurationChange;
                _onWorkspaceProjectChange = onWorkspaceProjectChange;
            }

            public override DocumentState WithText(SourceText sourceText, VersionStamp version)
            {
                _onTextChange?.Invoke();
                return base.WithText(sourceText, version);
            }

            public override DocumentState WithTextLoader(Func<Task<TextAndVersion>> loader)
            {
                _onTextLoaderChange?.Invoke();
                return base.WithTextLoader(loader);
            }

            public override DocumentState WithConfigurationChange()
            {
                _onConfigurationChange?.Invoke();
                return base.WithConfigurationChange();
            }

            public override DocumentState WithWorkspaceProjectChange()
            {
                _onWorkspaceProjectChange?.Invoke();
                return base.WithWorkspaceProjectChange();
            }
        }
    }
}
