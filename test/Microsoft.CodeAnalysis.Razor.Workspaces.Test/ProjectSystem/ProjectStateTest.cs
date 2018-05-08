// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Moq;
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
        }

        private HostDocument[] Documents { get; }

        private HostProject HostProject { get; }

        private HostProject HostProjectWithConfigurationChange { get; }

        private Project WorkspaceProject { get; }

        private TestTagHelperResolver TagHelperResolver { get; }

        private HostServices HostServices { get; }

        private Workspace Workspace { get; }

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        [Fact]
        public void ProjectState_ConstructedNew()
        {
            // Arrange
             
            // Act
            var state = new ProjectState(Workspace.Services, HostProject, WorkspaceProject);

            // Assert
            Assert.Empty(state.Documents);
            Assert.NotEqual(VersionStamp.Default, state.Version);
        }

        [Fact] // There's no magic in the constructor.
        public void ProjectState_ConstructedFromCopy()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject);

            // Act
            var state = new ProjectState(original, ProjectDifference.None, HostProject, WorkspaceProject, original.Documents);
            
            // Assert
            Assert.Same(original.Documents, state.Documents);
            Assert.NotEqual(original.Version, state.Version);
        }

        [Fact]
        public void ProjectState_AddHostDocument_ToEmpty()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject);

            // Act
            var state = original.WithAddedHostDocument(Documents[0]);

            // Assert
            Assert.NotEqual(original.Version, state.Version);

            Assert.Collection(
                state.Documents.OrderBy(kvp => kvp.Key),
                d => Assert.Same(Documents[0], d.Value.HostDocument));
        }

        [Fact]
        public void ProjectState_AddHostDocument_ToProjectWithDocuments()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

            // Act
            var state = original.WithAddedHostDocument(Documents[0]);

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
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithAddedHostDocument(Documents[0]);

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
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

            // Act
            var state = original.WithAddedHostDocument(new HostDocument(Documents[1].FilePath, "SomePath.cshtml"));

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public void ProjectState_RemoveHostDocument_FromProjectWithDocuments()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

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
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

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
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

            // Act
            var state = original.WithRemovedHostDocument(Documents[0]);

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public void ProjectState_WithHostProject_ConfigurationChange_UpdatesComputedState()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

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
            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
        }

        [Fact]
        public void ProjectState_WithHostProject_NoConfigurationChange_Noops()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

            // Force init
            GC.KeepAlive(original.ProjectEngine);
            GC.KeepAlive(original.TagHelpers);

            // Act
            var state = original.WithHostProject(HostProject);

            // Assert
            Assert.Same(original, state);
        }

        [Fact]
        public void ProjectState_WithWorkspaceProject_Removed()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

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
            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
        }

        [Fact]
        public void ProjectState_WithWorkspaceProject_Added()
        {
            // Arrange
            var original = new ProjectState(Workspace.Services, HostProject, null)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

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
            var original = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .WithAddedHostDocument(Documents[2])
                .WithAddedHostDocument(Documents[1]);

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
            Assert.NotSame(original.Documents[Documents[1].FilePath], state.Documents[Documents[1].FilePath]);
        }
    }
}
