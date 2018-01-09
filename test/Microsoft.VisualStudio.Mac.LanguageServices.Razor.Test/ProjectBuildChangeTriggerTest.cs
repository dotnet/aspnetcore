// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Projects;
using Moq;
using Xunit;
using Project = Microsoft.CodeAnalysis.Project;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    public class ProjectBuildChangeTriggerTest : ForegroundDispatcherTestBase
    {
        [ForegroundFact]
        public void ProjectOperations_EndBuild_Invokes_ProjectBuildComplete()
        {
            // Arrange
            var args = new BuildEventArgs(monitor: null, success: true);
            var expectedProjectName = "Test1";
            var expectedProjectPath = "Path/To/Project";
            var projectService = CreateProjectService(expectedProjectName, expectedProjectPath);
            var workspace = TestWorkspace.Create(ws =>
            {
                CreateProjectInWorkspace(ws, expectedProjectName, expectedProjectPath);
                CreateProjectInWorkspace(ws, "Test2", "Path/To/AnotherProject");
            });

            var projectManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectManager.SetupGet(p => p.Workspace).Returns(workspace);
            projectManager
                .Setup(p => p.ProjectBuildComplete(It.IsAny<Project>()))
                .Callback<Project>(c => Assert.Equal(expectedProjectName, c.Name));
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService, projectManager.Object);

            // Act
            trigger.ProjectOperations_EndBuild(null, args);

            // Assert
            projectManager.VerifyAll();
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_UntrackedProject_Noops()
        {
            // Arrange
            var args = new BuildEventArgs(monitor: null, success: true);
            var projectService = CreateProjectService("Test1", "Path/To/Project");
            var workspace = TestWorkspace.Create(ws =>
            {
                CreateProjectInWorkspace(ws, "Test2", "Path/To/AnotherProject");
            });
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(workspace);
            projectManager
                .Setup(p => p.ProjectBuildComplete(It.IsAny<Project>()))
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

        private static TextBufferProjectService CreateProjectService(string projectName, string projectPath)
        {
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectName(null)).Returns(projectName);
            projectService.Setup(p => p.GetProjectPath(null)).Returns(projectPath);
            projectService.Setup(p => p.IsSupportedProject(null)).Returns(true);
            return projectService.Object;
        }

        private static AdhocWorkspace CreateProjectInWorkspace(AdhocWorkspace workspace, string name, string path)
        {
            workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), new VersionStamp(), name, "TestAssembly", LanguageNames.CSharp, filePath: path));
            return workspace;
        }
    }
}
