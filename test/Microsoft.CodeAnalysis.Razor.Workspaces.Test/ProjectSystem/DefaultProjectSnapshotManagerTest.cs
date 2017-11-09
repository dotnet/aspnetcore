// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotManagerTest
    {
        public DefaultProjectSnapshotManagerTest()
        {
            Workspace = new AdhocWorkspace();
            ProjectManager = new TestProjectSnapshotManager(Enumerable.Empty<ProjectSnapshotChangeTrigger>(), Workspace);
        }

        private TestProjectSnapshotManager ProjectManager { get; }

        private Workspace Workspace { get; }

        [Fact]
        public void ProjectAdded_AddsProject_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);

            // Act
            ProjectManager.ProjectAdded(project);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(project.Id);
            Assert.True(snapshot.IsDirty);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectChanged_MadeDirty_RetainsComputedState_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);
            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            // Adding some computed state
            var configuration = Mock.Of<ProjectExtensibilityConfiguration>();
            ProjectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(project) { Configuration = configuration });
            ProjectManager.Reset();

            project = project.WithAssemblyName("Test1"); // Simulate a project change

            // Act
            ProjectManager.ProjectChanged(project);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(project.Id);
            Assert.True(snapshot.IsDirty);
            Assert.Same(configuration, snapshot.Configuration);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectChanged_BackgroundUpdate_MadeClean_WithSignificantChanges_NotifiesListeners_AndDoesNotStartBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);
            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            var configuration = Mock.Of<ProjectExtensibilityConfiguration>();
            
            // Act
            ProjectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(project) { Configuration = configuration });

            // Assert
            var snapshot = ProjectManager.GetSnapshot(project.Id);
            Assert.False(snapshot.IsDirty);
            Assert.Same(configuration, snapshot.Configuration);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectChanged_BackgroundUpdate_MadeClean_WithoutSignificantChanges_NotifiesListeners_AndDoesNotStartBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);
            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            var configuration = Mock.Of<ProjectExtensibilityConfiguration>();
            ProjectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(project) { Configuration = configuration });
            ProjectManager.Reset();

            project = project.WithAssemblyName("Test1"); // Simulate a project change
            ProjectManager.ProjectChanged(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(project) { Configuration = configuration });

            // Assert
            var snapshot = ProjectManager.GetSnapshot(project.Id);
            Assert.False(snapshot.IsDirty);
            Assert.Same(configuration, snapshot.Configuration);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectChanged_BackgroundUpdate_StillDirty_WithSignificantChanges_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);
            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            var configuration = Mock.Of<ProjectExtensibilityConfiguration>();

            // Compute an update for "Test"
            var update = new ProjectSnapshotUpdateContext(project) { Configuration = configuration };

            project = project.WithAssemblyName("Test1"); // Simulate a project change
            ProjectManager.ProjectChanged(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(update);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(project.Id);
            Assert.True(snapshot.IsDirty);
            Assert.Same(configuration, snapshot.Configuration);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectChanged_BackgroundUpdate_StillDirty_WithoutSignificantChanges_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);
            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            var configuration = Mock.Of<ProjectExtensibilityConfiguration>();
            ProjectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(project) { Configuration = configuration });

            project = project.WithAssemblyName("Test1"); // Simulate a project change
            ProjectManager.ProjectChanged(project);
            ProjectManager.Reset();

            // Compute an update for "Test1"
            var update = new ProjectSnapshotUpdateContext(project) { Configuration = configuration };

            project = project.WithAssemblyName("Test2"); // Simulate a project change
            ProjectManager.ProjectChanged(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(update); // Still dirty because the project changed while computing the update

            // Assert
            var snapshot = ProjectManager.GetSnapshot(project.Id);
            Assert.True(snapshot.IsDirty);
            Assert.Same(configuration, snapshot.Configuration);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectChanged_IgnoresUnknownProject()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);

            // Act
            ProjectManager.ProjectChanged(project);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectChanged_WithComputedState_IgnoresUnknownProject()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);

            // Act
            ProjectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(project));

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectBuildComplete_KnownProject_NotifiesBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);
            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectBuildComplete(project);

            // Assert
            Assert.False(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectBuildComplete_IgnoresUnknownProject()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);

            // Act
            ProjectManager.ProjectBuildComplete(project);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectRemoved_RemovesProject_NotifiesListeners_DoesNotStartBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);

            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectRemoved(project);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectRemoved_IgnoresUnknownProject()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);

            // Act
            ProjectManager.ProjectRemoved(project);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [Fact]
        public void ProjectsCleared_RemovesProject_NotifiesListeners_DoesNotStartBackgroundWorker()
        {
            // Arrange
            var project = Workspace.CurrentSolution.AddProject("Test", "Test", LanguageNames.CSharp);

            ProjectManager.ProjectAdded(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectsCleared();

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(IEnumerable<ProjectSnapshotChangeTrigger> triggers, Workspace workspace) 
                : base(Mock.Of<ForegroundDispatcher>(), Mock.Of<ErrorReporter>(), Mock.Of<ProjectSnapshotWorker>(), triggers, workspace)
            {
            }

            public bool ListenersNotified { get; private set; }

            public bool WorkerStarted { get; private set; }

            public DefaultProjectSnapshot GetSnapshot(ProjectId id)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.UnderlyingProject.Id == id);
            }

            public void Reset()
            {
                ListenersNotified = false;
                WorkerStarted = false;
            }

            protected override void NotifyListeners(ProjectChangeEventArgs e)
            {
                ListenersNotified = true;
            }

            protected override void NotifyBackgroundWorker(Project project)
            {
                WorkerStarted = true;
            }
        }
    }
}
