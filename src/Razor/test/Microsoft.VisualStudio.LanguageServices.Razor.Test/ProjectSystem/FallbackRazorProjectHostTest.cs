// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Microsoft.VisualStudio.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class FallbackRazorProjectHostTest : ForegroundDispatcherTestBase
    {
        public FallbackRazorProjectHostTest()
        {
            Workspace = new AdhocWorkspace();
            ProjectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);
        }

        private TestProjectSnapshotManager ProjectManager { get; }

        private Workspace Workspace { get; }

        [ForegroundFact]
        public async Task FallbackRazorProjectHost_ForegroundThread_CreateAndDispose_Succeeds()
        {
            // Arrange
            var services = new TestProjectSystemServices("Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);

            // Act & Assert
            await host.LoadAsync();
            Assert.Empty(ProjectManager.Projects);

            await host.DisposeAsync();
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task FallbackRazorProjectHost_BackgroundThread_CreateAndDispose_Succeeds()
        {
            // Arrange
            var services = new TestProjectSystemServices("Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);

            // Act & Assert
            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_ReadsProperties_InitializesProject()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                new TestProjectChangeDescription()
                {
                    RuleName = ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                    After = TestProjectRuleSnapshot.CreateItems(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName, new Dictionary<string, Dictionary<string, string>>()
                    {
                        { "c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll", new Dictionary<string, string>() },
                    }),
                },
            };

            var services = new TestProjectSystemServices("Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager)
            {
                AssemblyVersion = new Version(2, 0), // Mock for reading the assembly's version
            };

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_NoAssemblyFound_DoesNotIniatializeProject()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                new TestProjectChangeDescription()
                {
                    RuleName = ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                    After = TestProjectRuleSnapshot.CreateItems(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName, new Dictionary<string, Dictionary<string, string>>()
                    {
                    }),
                },

            };
            var services = new TestProjectSystemServices("Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_AssemblyFoundButCannotReadVersion_DoesNotIniatializeProject()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                new TestProjectChangeDescription()
                {
                    RuleName = ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                    After = TestProjectRuleSnapshot.CreateItems(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName, new Dictionary<string, Dictionary<string, string>>()
                    {
                        { "c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll", new Dictionary<string, string>() },
                    }),
                },
            };

            var services = new TestProjectSystemServices("Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_UpdateProject_Succeeds()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                new TestProjectChangeDescription()
                {
                    RuleName = ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                    After = TestProjectRuleSnapshot.CreateItems(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName, new Dictionary<string, Dictionary<string, string>>()
                    {
                        { "c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll", new Dictionary<string, string>() },
                    }),
                },
            };

            var services = new TestProjectSystemServices("Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager)
            {
                AssemblyVersion = new Version(2, 0),
            };

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);

            // Act - 2
            host.AssemblyVersion = new Version(1, 0);
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 2
            snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_1_0, snapshot.Configuration);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_VersionRemoved_DeinitializesProject()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                new TestProjectChangeDescription()
                {
                    RuleName = ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                    After = TestProjectRuleSnapshot.CreateItems(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName, new Dictionary<string, Dictionary<string, string>>()
                    {
                        { "c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll", new Dictionary<string, string>() },
                    }),
                },
            };

            var services = new TestProjectSystemServices("Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager)
            {
                AssemblyVersion = new Version(2, 0),
            };

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);

            // Act - 2
            host.AssemblyVersion= null;
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 2
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_AfterDispose_IgnoresUpdate()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                new TestProjectChangeDescription()
                {
                    RuleName = ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                    After = TestProjectRuleSnapshot.CreateItems(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName, new Dictionary<string, Dictionary<string, string>>()
                    {
                        { "c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll", new Dictionary<string, string>() },
                    }),
                },
            };

            var services = new TestProjectSystemServices("Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager)
            {
                AssemblyVersion = new Version(2, 0),
            };

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);

            // Act - 2
            await Task.Run(async () => await host.DisposeAsync());

            // Assert - 2
            Assert.Empty(ProjectManager.Projects);

            // Act - 3
            host.AssemblyVersion = new Version(1, 1);
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 3
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectRenamed_RemovesHostProject_CopiesConfiguration()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                new TestProjectChangeDescription()
                {
                    RuleName = ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName,
                    After = TestProjectRuleSnapshot.CreateItems(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName, new Dictionary<string, Dictionary<string, string>>()
                    {
                        { "c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll", new Dictionary<string, string>() },
                    }),
                },
            };

            var services = new TestProjectSystemServices("Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager)
            {
                AssemblyVersion = new Version(2, 0), // Mock for reading the assembly's version
            };

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);

            // Act - 2
            services.UnconfiguredProject.FullPath = "Test2.csproj";
            await Task.Run(async () => await host.OnProjectRenamingAsync());

            // Assert - 1
            snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("Test2.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        private class TestFallbackRazorProjectHost : FallbackRazorProjectHost
        {
            internal TestFallbackRazorProjectHost(IUnconfiguredProjectCommonServices commonServices, Workspace workspace, ProjectSnapshotManagerBase projectManager) 
                : base(commonServices, workspace, projectManager)
            {
            }

            public Version AssemblyVersion { get; set; }

            protected override Version GetAssemblyVersion(string filePath)
            {
                return AssemblyVersion;
            }
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(ForegroundDispatcher dispatcher, Workspace workspace)
                : base(dispatcher, Mock.Of<ErrorReporter>(), Mock.Of<ProjectSnapshotWorker>(), Array.Empty<ProjectSnapshotChangeTrigger>(), workspace)
            {
            }

            protected override void NotifyBackgroundWorker(ProjectSnapshotUpdateContext context)
            {
            }
        }
    }
}
