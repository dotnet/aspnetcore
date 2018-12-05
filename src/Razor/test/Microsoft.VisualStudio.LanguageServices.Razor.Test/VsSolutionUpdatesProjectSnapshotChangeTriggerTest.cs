// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class VsSolutionUpdatesProjectSnapshotChangeTriggerTest
    {
        public VsSolutionUpdatesProjectSnapshotChangeTriggerTest()
        {
            SomeProject = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_1_0);
            SomeOtherProject = new HostProject(TestProjectData.AnotherProject.FilePath, FallbackRazorConfiguration.MVC_2_0);

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
        public void UpdateProjectCfg_Done_KnownProject_Invokes_WorkspaceProjectChanged()
        {
            // Arrange
            var expectedProjectPath = SomeProject.FilePath;

            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var projectSnapshots = new[]
            {
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeProject, SomeWorkspaceProject)),
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeOtherProject, SomeOtherWorkspaceProject)),
            };

            var called = false;
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(expectedProjectPath))
                .Returns(projectSnapshots[0]);
            projectManager
                .Setup(p => p.WorkspaceProjectChanged(It.IsAny<Project>()))
                .Callback<Project>(c =>
                {
                    called = true;
                    Assert.Equal(expectedProjectPath, c.FilePath);
                });

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object);
            trigger.Initialize(projectManager.Object);

            // Act
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void UpdateProjectCfg_Done_WithoutWorkspaceProject_DoesNotInvoke_WorkspaceProjectChanged()
        {
            // Arrange
            var expectedProjectPath = SomeProject.FilePath;

            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var projectSnapshots = new[]
            {
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeProject, null)),
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeOtherProject, SomeOtherWorkspaceProject)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(expectedProjectPath))
                .Returns(projectSnapshots[0]);
            projectManager
                .Setup(p => p.WorkspaceProjectChanged(It.IsAny<Project>()))
                .Callback<Project>(c =>
                {
                    throw new InvalidOperationException("This should not be called.");
                });

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object);
            trigger.Initialize(projectManager.Object);

            // Act & Assert - Does not throw
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);
        }

        [Fact]
        public void UpdateProjectCfg_Done_UnknownProject_DoesNotInvoke_WorkspaceProjectChanged()
        {
            // Arrange
            var expectedProjectPath = "Path/To/Project";

            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var projectSnapshots = new[]
            {
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeProject, SomeWorkspaceProject)),
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeOtherProject, SomeOtherWorkspaceProject)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(expectedProjectPath))
                .Returns((ProjectSnapshot)null);
            projectManager
                .Setup(p => p.WorkspaceProjectChanged(It.IsAny<Project>()))
                .Callback<Project>(c =>
                {
                    throw new InvalidOperationException("This should not be called.");
                });

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object);
            trigger.Initialize(projectManager.Object);

            // Act & Assert - Does not throw
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);
        }
    }
}
