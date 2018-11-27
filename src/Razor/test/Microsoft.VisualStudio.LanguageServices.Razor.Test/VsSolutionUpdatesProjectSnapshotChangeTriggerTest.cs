// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
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
                Mock.Of<ProjectSnapshot>(p => p.FilePath == expectedProjectPath && p.HostProject == new HostProject(expectedProjectPath, RazorConfiguration.Default)),
                Mock.Of<ProjectSnapshot>(p => p.FilePath == "Test2.csproj" && p.HostProject == new HostProject("Test2.csproj", RazorConfiguration.Default)),
            };

            var called = false;
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Projects).Returns(projectSnapshots);
            projectManager
                .Setup(p => p.HostProjectBuildComplete(It.IsAny<HostProject>()))
                .Callback<HostProject>(c =>
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
        public void UpdateProjectCfg_Done_UnknownProject_DoesNotInvoke_ProjectBuildComplete()
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
                Mock.Of<ProjectSnapshot>(p => p.FilePath == "Path/To/AnotherProject" && p.HostProject == new HostProject("Path/To/AnotherProject", RazorConfiguration.Default)),
                Mock.Of<ProjectSnapshot>(p => p.FilePath == "Path/To/DifferenProject" && p.HostProject == new HostProject("Path/To/DifferenProject", RazorConfiguration.Default)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Projects).Returns(projectSnapshots);
            projectManager
                .Setup(p => p.HostProjectBuildComplete(It.IsAny<HostProject>()))
                .Callback<HostProject>(c =>
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
