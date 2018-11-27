// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotManagerTest : ForegroundDispatcherTestBase
    {
        public DefaultProjectSnapshotManagerTest()
        {
            HostProject = new HostProject("Test.csproj", FallbackRazorConfiguration.MVC_2_0);

            Workspace = TestWorkspace.Create();
            ProjectManager = new TestProjectSnapshotManager(Dispatcher, Enumerable.Empty<ProjectSnapshotChangeTrigger>(), Workspace);

            var projectId = ProjectId.CreateNewId("Test");
            var solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                "Test",
                "Test",
                LanguageNames.CSharp,
                "Test.csproj"));
            WorkspaceProject = solution.GetProject(projectId);

            var vbProjectId = ProjectId.CreateNewId("VB");
            solution = solution.AddProject(ProjectInfo.Create(
                vbProjectId,
                VersionStamp.Default,
                "VB",
                "VB",
                LanguageNames.VisualBasic,
                "VB.vbproj"));
            VBWorkspaceProject = solution.GetProject(vbProjectId);

            var projectWithoutFilePathId = ProjectId.CreateNewId("NoFile");
            solution = solution.AddProject(ProjectInfo.Create(
                projectWithoutFilePathId,
                VersionStamp.Default,
                "NoFile",
                "NoFile",
                LanguageNames.CSharp));
            WorkspaceProjectWithoutFilePath = solution.GetProject(projectWithoutFilePathId);

            // Approximates a project with multi-targeting
            var projectIdWithDifferentTfm = ProjectId.CreateNewId("TestWithDifferentTfm");
            solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                projectIdWithDifferentTfm,
                VersionStamp.Default,
                "Test (Different TFM)",
                "Test",
                LanguageNames.CSharp,
                "Test.csproj"));
            WorkspaceProjectWithDifferentTfm = solution.GetProject(projectIdWithDifferentTfm);
        }

        private HostProject HostProject { get; }

        private Project WorkspaceProject { get; }

        private Project WorkspaceProjectWithDifferentTfm { get; }

        private Project WorkspaceProjectWithoutFilePath { get; }

        private Project VBWorkspaceProject { get; }

        private TestProjectSnapshotManager ProjectManager { get; }

        private Workspace Workspace { get; }

        [ForegroundFact]
        public void HostProjectBuildComplete_FindsChangedWorkspaceProject_AndStartsBackgroundWorker()
        {
            // Arrange
            Assert.True(Workspace.TryApplyChanges(WorkspaceProject.Solution));
            ProjectManager.HostProjectAdded(HostProject);
            var project = WorkspaceProject.WithAssemblyName("Test1"); // Simulate a project change
            ProjectManager.WorkspaceProjectAdded(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.HostProjectBuildComplete(HostProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.True(snapshot.IsDirty);
            Assert.True(snapshot.IsInitialized);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void HostProjectAdded_WithoutWorkspaceProject_NotifiesListeners()
        {
            // Arrange

            // Act
            ProjectManager.HostProjectAdded(HostProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.True(snapshot.IsDirty);
            Assert.False(snapshot.IsInitialized);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void HostProjectAdded_FindsWorkspaceProject_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            Assert.True(Workspace.TryApplyChanges(WorkspaceProject.Solution));

            // Act
            ProjectManager.HostProjectAdded(HostProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.True(snapshot.IsDirty);
            Assert.True(snapshot.IsInitialized);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void HostProjectChanged_WithoutWorkspaceProject_NotifiesListeners_AndDoesNotStartBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.Reset();

            var project = new HostProject(HostProject.FilePath, FallbackRazorConfiguration.MVC_1_0); // Simulate a project change

            // Act
            ProjectManager.HostProjectChanged(project);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.True(snapshot.IsDirty);
            Assert.False(snapshot.IsInitialized);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void HostProjectChanged_WithWorkspaceProject_RetainsComputedState_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Adding some computed state
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();
            ProjectManager.ProjectUpdated(updateContext);
            ProjectManager.Reset();

            var project = new HostProject(HostProject.FilePath, FallbackRazorConfiguration.MVC_1_0); // Simulate a project change

            // Act
            ProjectManager.HostProjectChanged(project);

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.True(snapshot.IsDirty);
            Assert.True(snapshot.IsInitialized);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void HostProjectChanged_IgnoresUnknownProject()
        {
            // Arrange

            // Act
            ProjectManager.HostProjectChanged(HostProject);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void HostProjectRemoved_RemovesProject_NotifiesListeners()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.HostProjectRemoved(HostProject);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void ProjectUpdated_WithComputedState_IgnoresUnknownProject()
        {
            // Arrange

            // Act
            ProjectManager.ProjectUpdated(new ProjectSnapshotUpdateContext("Test", HostProject, WorkspaceProject, VersionStamp.Default));

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void ProjectUpdated_WhenHostProjectChanged_MadeClean_NotifiesListeners_AndDoesNotStartBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            var project = new HostProject(HostProject.FilePath, FallbackRazorConfiguration.MVC_1_0); // Simulate a project change
            ProjectManager.HostProjectChanged(project);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();

            // Act
            ProjectManager.ProjectUpdated(updateContext);

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.False(snapshot.IsDirty);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void ProjectUpdated_WhenWorkspaceProjectChanged_MadeClean_NotifiesListeners_AndDoesNotStartBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            var project = WorkspaceProject.WithAssemblyName("Test1"); // Simulate a project change
            ProjectManager.WorkspaceProjectChanged(project);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            var updateContext = snapshot.CreateUpdateContext();

            // Act
            ProjectManager.ProjectUpdated(updateContext);

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.False(snapshot.IsDirty);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void ProjectUpdated_WhenHostProjectChanged_StillDirty_WithSignificantChanges_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();

            var project = new HostProject(HostProject.FilePath, FallbackRazorConfiguration.MVC_1_0); // Simulate a project change
            ProjectManager.HostProjectChanged(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(updateContext);

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.True(snapshot.IsDirty);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectChanged_BackgroundUpdate_StillDirty_WithSignificantChanges_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            var updateContext = snapshot.CreateUpdateContext();

            var project = WorkspaceProject.WithAssemblyName("Test1"); // Simulate a project change
            ProjectManager.WorkspaceProjectChanged(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(updateContext);

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.True(snapshot.IsDirty);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [Fact(Skip = "We no longer have any background-computed state")]
        public void ProjectUpdated_WhenHostProjectChanged_StillDirty_WithoutSignificantChanges_DoesNotNotifyListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate an update based on the original state
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();
            ProjectManager.ProjectUpdated(updateContext);
            ProjectManager.Reset();

            var project = new HostProject(HostProject.FilePath, FallbackRazorConfiguration.MVC_1_0); // Simulate a project change
            ProjectManager.HostProjectChanged(project);
            ProjectManager.Reset();

            // Now start computing another update
            snapshot = ProjectManager.GetSnapshot(HostProject);
            updateContext = snapshot.CreateUpdateContext();

            project = new HostProject(HostProject.FilePath, FallbackRazorConfiguration.MVC_1_1); // Simulate a project change
            ProjectManager.HostProjectChanged(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(updateContext); // Still dirty because the project changed while computing the update

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.True(snapshot.IsDirty);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [Fact(Skip = "We no longer have any background-computed state")]
        public void ProjectUpdated_WhenWorkspaceProjectChanged_StillDirty_WithoutSignificantChanges_DoesNotNotifyListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate an update based on the original state
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();
            ProjectManager.ProjectUpdated(updateContext);
            ProjectManager.Reset();

            var project = WorkspaceProject.WithAssemblyName("Test1"); // Simulate a project change
            ProjectManager.WorkspaceProjectChanged(project);
            ProjectManager.Reset();

            // Now start computing another update
            snapshot = ProjectManager.GetSnapshot(HostProject);
            updateContext = snapshot.CreateUpdateContext();

            project = project.WithAssemblyName("Test2"); // Simulate a project change
            ProjectManager.WorkspaceProjectChanged(project);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(updateContext); // Still dirty because the project changed while computing the update

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.True(snapshot.IsDirty);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void ProjectUpdated_WhenHostProjectRemoved_DiscardsUpdate()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();

            ProjectManager.HostProjectRemoved(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(updateContext);

            // Assert
            snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Null(snapshot);
        }

        [ForegroundFact]
        public void ProjectUpdated_WhenWorkspaceProjectRemoved_DiscardsUpdate()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            var updateContext = snapshot.CreateUpdateContext();

            ProjectManager.WorkspaceProjectRemoved(WorkspaceProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectUpdated(updateContext);

            // Assert
            snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.True(snapshot.IsDirty);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void ProjectUpdated_BackgroundUpdate_MadeClean_WithSignificantChanges_NotifiesListeners_AndDoesNotStartBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();

            // Act
            ProjectManager.ProjectUpdated(updateContext);

            // Assert
            snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.False(snapshot.IsDirty);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectAdded_WithoutHostProject_IgnoresWorkspaceProject()
        {
            // Arrange

            // Act
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectAdded_IgnoresNonCSharpProject()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectAdded(VBWorkspaceProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.False(snapshot.IsInitialized);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectAdded_IgnoresSecondProjectWithSameFilePath()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectAdded(WorkspaceProjectWithDifferentTfm);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.Same(WorkspaceProject, snapshot.WorkspaceProject);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectAdded_IgnoresProjectWithoutFilePath()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectAdded(WorkspaceProjectWithoutFilePath);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.False(snapshot.IsInitialized);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectAdded_WithHostProject_NotifiesListenters_AndStartsBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.True(snapshot.IsDirty);
            Assert.True(snapshot.IsInitialized);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectChanged_WithoutHostProject_IgnoresWorkspaceProject()
        {
            // Arrange
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            var project = WorkspaceProject.WithAssemblyName("Test1"); // Simulate a project change

            // Act
            ProjectManager.WorkspaceProjectChanged(project);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectChanged_IgnoresNonCSharpProject()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(VBWorkspaceProject);
            ProjectManager.Reset();

            var project = VBWorkspaceProject.WithAssemblyName("Test1"); // Simulate a project change

            // Act
            ProjectManager.WorkspaceProjectChanged(project);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.False(snapshot.IsInitialized);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }


        [ForegroundFact]
        public void WorkspaceProjectChanged_IgnoresProjectWithoutFilePath()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProjectWithoutFilePath);
            ProjectManager.Reset();

            var project = WorkspaceProjectWithoutFilePath.WithAssemblyName("Test1"); // Simulate a project change

            // Act
            ProjectManager.WorkspaceProjectChanged(project);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.False(snapshot.IsInitialized);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectChanged_IgnoresSecondProjectWithSameFilePath()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectChanged(WorkspaceProjectWithDifferentTfm);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.Same(WorkspaceProject, snapshot.WorkspaceProject);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectChanged_MadeDirty_RetainsComputedState_NotifiesListeners_AndStartsBackgroundWorker()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Generate the update
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var updateContext = snapshot.CreateUpdateContext();
            ProjectManager.ProjectUpdated(updateContext);
            ProjectManager.Reset();

            var project = WorkspaceProject.WithAssemblyName("Test1"); // Simulate a project change

            // Act
            ProjectManager.WorkspaceProjectChanged(project);

            // Assert
            snapshot = ProjectManager.GetSnapshot(project);
            Assert.True(snapshot.IsDirty);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectRemoved_WithHostProject_DoesNotRemoveProject()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectRemoved(WorkspaceProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.True(snapshot.IsDirty);
            Assert.False(snapshot.IsInitialized);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectRemoved_WithHostProject_FallsBackToSecondProject()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Sets up a solution where the which has WorkspaceProjectWithDifferentTfm but not WorkspaceProject
            // This will enable us to fall back and find the WorkspaceProjectWithDifferentTfm 
            Assert.True(Workspace.TryApplyChanges(WorkspaceProjectWithDifferentTfm.Solution));

            // Act
            ProjectManager.WorkspaceProjectRemoved(WorkspaceProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.True(snapshot.IsDirty);
            Assert.True(snapshot.IsInitialized);
            Assert.Equal(WorkspaceProjectWithDifferentTfm.Id, snapshot.WorkspaceProject.Id);

            Assert.True(ProjectManager.ListenersNotified);
            Assert.True(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectRemoved_IgnoresSecondProjectWithSameFilePath()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectRemoved(WorkspaceProjectWithDifferentTfm);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.Same(WorkspaceProject, snapshot.WorkspaceProject);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectRemoved_IgnoresNonCSharpProject()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(VBWorkspaceProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectRemoved(VBWorkspaceProject);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.False(snapshot.IsInitialized);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectRemoved_IgnoresProjectWithoutFilePath()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProjectWithoutFilePath);
            ProjectManager.Reset();

            // Act
            ProjectManager.WorkspaceProjectRemoved(WorkspaceProjectWithoutFilePath);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(WorkspaceProject);
            Assert.False(snapshot.IsInitialized);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        [ForegroundFact]
        public void WorkspaceProjectRemoved_IgnoresUnknownProject()
        {
            // Arrange

            // Act
            ProjectManager.WorkspaceProjectRemoved(WorkspaceProject);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.False(ProjectManager.ListenersNotified);
            Assert.False(ProjectManager.WorkerStarted);
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(ForegroundDispatcher dispatcher, IEnumerable<ProjectSnapshotChangeTrigger> triggers, Workspace workspace)
                : base(dispatcher, Mock.Of<ErrorReporter>(), Mock.Of<ProjectSnapshotWorker>(), triggers, workspace)
            {
            }

            public bool ListenersNotified { get; private set; }

            public bool WorkerStarted { get; private set; }

            public DefaultProjectSnapshot GetSnapshot(HostProject hostProject)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.FilePath == hostProject.FilePath);
            }

            public DefaultProjectSnapshot GetSnapshot(Project workspaceProject)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.FilePath == workspaceProject.FilePath);
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

            protected override void NotifyBackgroundWorker(ProjectSnapshotUpdateContext context)
            {
                Assert.NotNull(context.HostProject);
                Assert.NotNull(context.WorkspaceProject);

                WorkerStarted = true;
            }
        }
    }
}
