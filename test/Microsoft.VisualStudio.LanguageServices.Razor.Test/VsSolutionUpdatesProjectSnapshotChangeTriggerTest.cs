// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class VsSolutionUpdatesProjectSnapshotChangeTriggerTest
    {
        [Fact]
        public void Initialize_AttachesEventSink()
        {
            // Arrange
            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, Mock.Of<TextBufferProjectService>());

            // Act
            trigger.Initialize(Mock.Of<ProjectSnapshotManagerBase>());

            // Assert
            buildManager.Verify();
        }

        [Fact]
        public void UpdateProjectCfg_Done_KnownProject_Invokes_ProjectBuildComplete()
        {
            // Arrange
            var expectedProjectName = "Test1";
            var expectedProjectPath = "Path/To/Project";

            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectName(It.IsAny<IVsHierarchy>())).Returns(expectedProjectName);
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var workspace = TestWorkspace.Create(ws =>
            {
                CreateProjectInWorkspace(ws, expectedProjectName, expectedProjectPath);
                CreateProjectInWorkspace(ws, "Test2", "Path/To/AnotherProject");
            });

            var called = false;
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(workspace);
            projectManager
                .Setup(p => p.ProjectBuildComplete(It.IsAny<Project>()))
                .Callback<Project>(c =>
                {
                    called = true;
                    Assert.Equal(expectedProjectName, c.Name);
                });

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object);
            trigger.Initialize(projectManager.Object);

            // Act
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void UpdateProjectCfg_Done_UnknownProject_DoesNotInvoke_ProjectBuildComplete()
        {
            // Arrange
            var expectedProjectName = "Test1";
            var expectedProjectPath = "Path/To/Project";

            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectName(It.IsAny<IVsHierarchy>())).Returns(expectedProjectName);
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var workspace = TestWorkspace.Create(ws =>
            {
                CreateProjectInWorkspace(ws, "Test2", "Path/To/AnotherProject");
                CreateProjectInWorkspace(ws, "Test3", "Path/To/DifferenProject");
            });

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(workspace);
            projectManager
                .Setup(p => p.ProjectBuildComplete(It.IsAny<Project>()))
                .Callback<Project>(c =>
                {
                    throw new InvalidOperationException("This should not be called.");
                });

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object);
            trigger.Initialize(projectManager.Object);

            // Act & Assert - Does not throw
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);
        }

        private static AdhocWorkspace CreateProjectInWorkspace(AdhocWorkspace workspace, string name, string path)
        {
            workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), new VersionStamp(), name, "TestAssembly", LanguageNames.CSharp, filePath: path));
            return workspace;
        }
    }
}
