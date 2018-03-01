// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
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

            HostProject_For_2_1 = new HostProject(
                "/TestPath/SomePath/Test.csproj", 
                new ProjectSystemRazorConfiguration(RazorLanguageVersion.Version_2_1, "MVC-2.1", Array.Empty<RazorExtension>()));
            HostProject_For_UnknownConfiguration = new HostProject(
                "/TestPath/SomePath/Test.csproj",
                new ProjectSystemRazorConfiguration(RazorLanguageVersion.Version_2_1, "Blazor-0.1", Array.Empty<RazorExtension>()));


            CustomFactories = new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[]
            {
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new LegacyProjectEngineFactory_1_0(),
                    typeof(LegacyProjectEngineFactory_1_0).GetCustomAttribute<ExportCustomProjectEngineFactoryAttribute>()),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new LegacyProjectEngineFactory_1_1(),
                    typeof(LegacyProjectEngineFactory_1_1).GetCustomAttribute<ExportCustomProjectEngineFactoryAttribute>()),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new LegacyProjectEngineFactory_2_0(),
                    typeof(LegacyProjectEngineFactory_2_0).GetCustomAttribute<ExportCustomProjectEngineFactoryAttribute>()),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new LegacyProjectEngineFactory_2_1(),
                    typeof(LegacyProjectEngineFactory_2_1).GetCustomAttribute<ExportCustomProjectEngineFactoryAttribute>()),
            };

            FallbackFactory = new FallbackProjectEngineFactory();
        }

        private Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[] CustomFactories { get; }

        private IFallbackProjectEngineFactory FallbackFactory { get; }

        private HostProject HostProject_For_1_0 { get; }

        private HostProject HostProject_For_1_1 { get; }

        private HostProject HostProject_For_2_0 { get; }

        private HostProject HostProject_For_2_1 { get; }

        private HostProject HostProject_For_UnknownConfiguration { get; }

        // We don't actually look at the project, we rely on the ProjectStateManager
        private Project WorkspaceProject { get; }

        private Workspace Workspace { get; }

        [Fact]
        public void Create_CreatesDesignTimeTemplateEngine_ForVersion2_1()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.HostProjectAdded(HostProject_For_2_1);
            projectManager.WorkspaceProjectAdded(WorkspaceProject);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager, FallbackFactory, CustomFactories);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperDescriptorProvider>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_CreatesDesignTimeTemplateEngine_ForVersion2_0()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.HostProjectAdded(HostProject_For_2_0);
            projectManager.WorkspaceProjectAdded(WorkspaceProject);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager, FallbackFactory, CustomFactories);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperDescriptorProvider>());
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

            var factoryService = new DefaultProjectEngineFactoryService(projectManager, FallbackFactory, CustomFactories);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<Mvc1_X.ViewComponentTagHelperDescriptorProvider>());
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

            var factoryService = new DefaultProjectEngineFactoryService(projectManager, FallbackFactory, CustomFactories);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Empty(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperDescriptorProvider>());
            Assert.Empty(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Empty(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_UnknownProject_UsesVersion2_0()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager, FallbackFactory, CustomFactories);

            // Act
            var engine = factoryService.Create("/TestPath/DifferentPath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<DefaultTagHelperDescriptorProvider>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperDescriptorProvider>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_ForUnknownConfiguration_UsesFallbackFactory()
        {
            // Arrange
            var projectManager = new TestProjectSnapshotManager(Workspace);
            projectManager.HostProjectAdded(HostProject_For_UnknownConfiguration);
            projectManager.WorkspaceProjectAdded(WorkspaceProject);

            var factoryService = new DefaultProjectEngineFactoryService(projectManager, FallbackFactory, CustomFactories);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Empty(engine.Engine.Features.OfType<DefaultTagHelperDescriptorProvider>());
            Assert.Empty(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperDescriptorProvider>());
            Assert.Empty(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Empty(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
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