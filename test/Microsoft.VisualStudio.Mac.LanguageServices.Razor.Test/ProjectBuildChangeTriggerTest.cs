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

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    public class ProjectBuildChangeTriggerTest : ForegroundDispatcherTestBase
    {
        [ForegroundFact]
        public void ProjectOperations_EndBuild_Invokes_ProjectBuildComplete()
        {
            // Arrange
            var args = new BuildEventArgs(monitor: null, success: true);
            var expectedProjectPath = "Path/To/Project";
            var projectService = CreateProjectService(expectedProjectPath);
            var projectSnapshots = new[]
            {
                Mock.Of<ProjectSnapshot>(p => p.FilePath == expectedProjectPath && p.HostProject == new HostProject(expectedProjectPath, RazorConfiguration.Default)),
                Mock.Of<ProjectSnapshot>(p => p.FilePath == "Test2.csproj" && p.HostProject == new HostProject("Test2.csproj", RazorConfiguration.Default)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectManager.SetupGet(p => p.Projects).Returns(projectSnapshots);
            projectManager
                .Setup(p => p.HostProjectBuildComplete(It.IsAny<HostProject>()))
                .Callback<HostProject>(c => Assert.Equal(expectedProjectPath, c.FilePath));
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
            var projectService = CreateProjectService("Path/To/Project");
            var projectSnapshots = new[]
            {
                Mock.Of<ProjectSnapshot>(p => p.FilePath == "Path/To/AnotherProject" && p.HostProject == new HostProject("Path/To/AnotherProject", RazorConfiguration.Default)),
            };
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Projects).Returns(projectSnapshots);
            projectManager
                .Setup(p => p.HostProjectBuildComplete(It.IsAny<HostProject>()))
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

        private static AdhocWorkspace CreateProjectInWorkspace(AdhocWorkspace workspace, string name, string path)
        {
            workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), new VersionStamp(), name, "TestAssembly", LanguageNames.CSharp, filePath: path));
            return workspace;
        }
    }
}
