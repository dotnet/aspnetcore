// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;
using Mvc1_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;
using MvcLatest = Microsoft.AspNetCore.Mvc.Razor.Extensions;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultProjectEngineFactoryServiceTest
    {
        public DefaultProjectEngineFactoryServiceTest()
        {
            Project project = null;

            Workspace = TestWorkspace.Create(workspace =>
            {
                var info = ProjectInfo.Create(ProjectId.CreateNewId("Test"), VersionStamp.Default, "Test", "Test", LanguageNames.CSharp, filePath: "/TestPath/SomePath/Test.csproj");
                project = workspace.CurrentSolution.AddProject(info).GetProject(info.Id);
            });

            WorkspaceProject = project;

            HostProject_For_1_0 = new HostProject("/TestPath/SomePath/Test.csproj", FallbackRazorConfiguration.MVC_1_0);
            HostProject_For_1_1 = new HostProject("/TestPath/SomePath/Test.csproj", FallbackRazorConfiguration.MVC_1_1);
            HostProject_For_2_0 = new HostProject("/TestPath/SomePath/Test.csproj", FallbackRazorConfiguration.MVC_2_0);
        }

        private HostProject HostProject_For_1_0 { get; }

        private HostProject HostProject_For_1_1 { get; }

        private HostProject HostProject_For_2_0 { get; }

        // We don't actually look at the project, we rely on the ProjectStateManager
        private Project WorkspaceProject { get; }

        private Workspace Workspace { get; }

        [Fact]
        public void Create_CreatesTemplateEngine_ForLatest()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.HostProjectAdded(HostProject_For_2_0);
            projectManager.WorkspaceProjectAdded(WorkspaceProject);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_CreatesTemplateEngine_ForVersion1_1()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.HostProjectAdded(HostProject_For_1_1);
            projectManager.WorkspaceProjectAdded(WorkspaceProject);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<Mvc1_X.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<Mvc1_X.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_DoesNotSupportViewComponentTagHelpers_ForVersion1_0()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.HostProjectAdded(HostProject_For_1_0);
            projectManager.WorkspaceProjectAdded(WorkspaceProject);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<Mvc1_X.MvcViewDocumentClassifierPass>());
            Assert.Empty(engine.Engine.Features.OfType<Mvc1_X.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_UnknownProjectPath_UsesLatest()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/DifferentPath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_MvcReferenceNotFound_UsesLatest()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.HostProjectAdded(HostProject_For_2_0);
            projectManager.WorkspaceProjectAdded(WorkspaceProject);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/DifferentPath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        private class MyCoolNewFeature : IRazorEngineFeature
        {
            public RazorEngine Engine { get; set; }
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(Workspace workspace)
                : base(
                      Mock.Of<ForegroundDispatcher>(),
                      Mock.Of<ErrorReporter>(),
                      Mock.Of<ProjectSnapshotWorker>(),
                      Enumerable.Empty<ProjectSnapshotChangeTrigger>(),
                      workspace)
            {
            }
        }
    }
}