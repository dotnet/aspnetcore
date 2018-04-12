// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Projects;
using Moq;
using Xunit;
using Project = Microsoft.CodeAnalysis.Project;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    public class ProjectBuildChangeTriggerTest : ForegroundDispatcherTestBase
    {
        public ProjectBuildChangeTriggerTest()
        {
            SomeProject = new HostProject("c:\\SomeProject\\SomeProject.csproj", FallbackRazorConfiguration.MVC_1_0);
            SomeOtherProject = new HostProject("c:\\SomeOtherProject\\SomeOtherProject.csproj", FallbackRazorConfiguration.MVC_2_0);

            Workspace = TestWorkspace.Create(w =>
            {
                SomeWorkspaceProject = w.AddProject(ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "SomeProject",
                    "SomeProject",
                    LanguageNames.CSharp,
                    filePath: SomeProject.FilePath));

                SomeOtherWorkspaceProject = w.AddProject(ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "SomeOtherProject",
                    "SomeOtherProject",
                    LanguageNames.CSharp,
                    filePath: SomeOtherProject.FilePath));
            });
        }

        private HostProject SomeProject { get; }

        private HostProject SomeOtherProject { get; }

        private Project SomeWorkspaceProject { get; set; }

        private Project SomeOtherWorkspaceProject { get; set; }

        private Workspace Workspace { get; }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_Invokes_WorkspaceProjectChanged()
        {
            // Arrange
            var expectedProjectPath = SomeProject.FilePath;
            var projectService = CreateProjectService(expectedProjectPath);

            var args = new BuildEventArgs(monitor: null, success: true);

            var projectSnapshots = new[]
            {
                new DefaultProjectSnapshot(new ProjectState(Workspace.Services, SomeProject, SomeWorkspaceProject)),
                new DefaultProjectSnapshot(new ProjectState(Workspace.Services, SomeOtherProject, SomeOtherWorkspaceProject)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(SomeProject.FilePath))
                .Returns(projectSnapshots[0]);
            projectManager
                .Setup(p => p.WorkspaceProjectChanged(It.IsAny<Project>()))
                .Callback<Project>(c => Assert.Equal(expectedProjectPath, c.FilePath));

            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService, projectManager.Object);

            // Act
            trigger.ProjectOperations_EndBuild(null, args);

            // Assert
            projectManager.VerifyAll();
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_ProjectWithoutWorkspaceProject_Noops()
        {
            // Arrange
            var projectService = CreateProjectService(SomeProject.FilePath);

            var args = new BuildEventArgs(monitor: null, success: true);
            var projectSnapshots = new[]
            {
                new DefaultProjectSnapshot(new ProjectState(Workspace.Services, SomeProject, null)),
                new DefaultProjectSnapshot(new ProjectState(Workspace.Services, SomeOtherProject, SomeOtherWorkspaceProject)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(SomeProject.FilePath))
                .Returns(projectSnapshots[0]);
            projectManager
                .Setup(p => p.WorkspaceProjectChanged(It.IsAny<Project>()))
                .Throws<InvalidOperationException>();

            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService, projectManager.Object);

            // Act & Assert
            trigger.ProjectOperations_EndBuild(null, args);
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_UntrackedProject_Noops()
        {
            // Arrange
            var projectService = CreateProjectService("Path/To/Project");

            var args = new BuildEventArgs(monitor: null, success: true);
            var projectSnapshots = new[]
            {
                new DefaultProjectSnapshot(new ProjectState(Workspace.Services, SomeProject, null)),
                new DefaultProjectSnapshot(new ProjectState(Workspace.Services, SomeOtherProject, SomeOtherWorkspaceProject)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(SomeProject.FilePath))
                .Returns(projectSnapshots[0]);
            projectManager
                .Setup(p => p.WorkspaceProjectChanged(It.IsAny<Project>()))
                .Throws<InvalidOperationException>();

            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService, projectManager.Object);

            // Act & Assert
            trigger.ProjectOperations_EndBuild(null, args);
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_BuildFailed_Noops()
        {
            // Arrange
            var args = new BuildEventArgs(monitor: null, success: false);
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.IsSupportedProject(null)).Throws<InvalidOperationException>();
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Throws<InvalidOperationException>();
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService.Object, projectManager.Object);

            // Act & Assert
            trigger.ProjectOperations_EndBuild(null, args);
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_UnsupportedProject_Noops()
        {
            // Arrange
            var args = new BuildEventArgs(monitor: null, success: true);
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.IsSupportedProject(null)).Returns(false);
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Throws<InvalidOperationException>();
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService.Object, projectManager.Object);

            // Act & Assert
            trigger.ProjectOperations_EndBuild(null, args);
        }

        private static TextBufferProjectService CreateProjectService(string projectPath)
        {
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(null)).Returns(projectPath);
            projectService.Setup(p => p.IsSupportedProject(null)).Returns(true);
            return projectService.Object;
        }
    }
}
