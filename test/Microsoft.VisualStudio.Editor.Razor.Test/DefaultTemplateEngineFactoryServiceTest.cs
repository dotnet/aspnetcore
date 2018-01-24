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
    public class DefaultTemplateEngineFactoryServiceTest
    {
        public DefaultTemplateEngineFactoryServiceTest()
        {
            Project project = null;

            Workspace = TestWorkspace.Create(workspace =>
            {
                var info = ProjectInfo.Create(ProjectId.CreateNewId("Test"), VersionStamp.Default, "Test", "Test", LanguageNames.CSharp, filePath: "/TestPath/SomePath/Test.csproj");
                project = workspace.CurrentSolution.AddProject(info).GetProject(info.Id);
            });

            Project = project;
        }

        // We don't actually look at the project, we rely on the ProjectStateManager
        public Project Project { get; }

        public Workspace Workspace { get; }

        [Fact]
        public void Create_CreatesDesignTimeTemplateEngine_ForLatest()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.ProjectAdded(Project);
            projectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(Project)
            {
                Configuration = new MvcExtensibilityConfiguration(
                    RazorLanguageVersion.Version_2_0,
                    ProjectExtensibilityConfigurationKind.ApproximateMatch,
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("2.0.0.0"))),
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("2.0.0.0")))),
            });

            var factoryService = new DefaultTemplateEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_CreatesDesignTimeTemplateEngine_ForVersion1_1()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.ProjectAdded(Project);
            projectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(Project)
            {
                Configuration = new MvcExtensibilityConfiguration(
                    RazorLanguageVersion.Version_1_1,
                    ProjectExtensibilityConfigurationKind.ApproximateMatch,
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("1.1.3.0"))),
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("1.1.3.0")))),
            });

            var factoryService = new DefaultTemplateEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
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
            projectManager.ProjectAdded(Project);
            projectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(Project)
            {
                Configuration = new MvcExtensibilityConfiguration(
                    RazorLanguageVersion.Version_1_0,
                    ProjectExtensibilityConfigurationKind.ApproximateMatch,
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("1.0.0.0"))),
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("1.0.0.0")))),
            });

            var factoryService = new DefaultTemplateEngineFactoryService(projectManager);

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
        public void Create_HigherMvcVersion_UsesLatest()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.ProjectAdded(Project);
            projectManager.ProjectUpdated(new ProjectSnapshotUpdateContext(Project)
            {
                Configuration = new MvcExtensibilityConfiguration(
                    RazorLanguageVersion.Latest,
                    ProjectExtensibilityConfigurationKind.ApproximateMatch,
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("3.0.0.0"))),
                    new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Razor", new Version("3.0.0.0")))),
            });

            var factoryService = new DefaultTemplateEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_UnknownProjectPath_UsesLatest()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);

            var factoryService = new DefaultTemplateEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/DifferentPath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
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
            projectManager.ProjectAdded(Project);

            var factoryService = new DefaultTemplateEngineFactoryService(projectManager);

            // Act
            var engine = factoryService.Create("/TestPath/DifferentPath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
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
