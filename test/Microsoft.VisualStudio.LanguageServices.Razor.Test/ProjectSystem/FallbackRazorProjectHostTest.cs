// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Moq;
using Xunit;
using ItemReference = Microsoft.CodeAnalysis.Razor.ProjectSystem.ManageProjectSystemSchema.ItemReference;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class FallbackRazorProjectHostTest : ForegroundDispatcherTestBase
    {
        public FallbackRazorProjectHostTest()
        {
            Workspace = new AdhocWorkspace();
            ProjectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);

            ReferenceItems = new ItemCollection(ManageProjectSystemSchema.ResolvedCompilationReference.SchemaName);
            ContentItems = new ItemCollection(ManageProjectSystemSchema.ContentItem.SchemaName);
            NoneItems = new ItemCollection(ManageProjectSystemSchema.NoneItem.SchemaName);
        }

        private ItemCollection ReferenceItems { get; }

        private TestProjectSnapshotManager ProjectManager { get; }

        private ItemCollection ContentItems { get; }

        private ItemCollection NoneItems { get; }

        private Workspace Workspace { get; }

        [Fact]
        public void GetChangedAndRemovedDocuments_ReturnsChangedContentAndNoneItems()
        {
            // Arrange
            var afterChangeContentItems = new ItemCollection(ManageProjectSystemSchema.ContentItem.SchemaName);
            ContentItems.Item("Index.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.LinkPropertyName] = "NewIndex.cshtml",
                [ItemReference.FullPathPropertyName] = "C:\\From\\Index.cshtml",
            });
            var afterChangeNoneItems = new ItemCollection(ManageProjectSystemSchema.NoneItem.SchemaName);
            NoneItems.Item("About.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.LinkPropertyName] = "NewAbout.cshtml",
                [ItemReference.FullPathPropertyName] = "C:\\From\\About.cshtml",
            });
            var services = new TestProjectSystemServices("C:\\To\\Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);
            var changes = new TestProjectChangeDescription[]
            {
                afterChangeContentItems.ToChange(ContentItems.ToSnapshot()),
                afterChangeNoneItems.ToChange(NoneItems.ToSnapshot()),
            };
            var update = services.CreateUpdate(changes).Value;

            // Act
            var result = host.GetChangedAndRemovedDocuments(update);

            // Assert
            Assert.Collection(
                result,
                document =>
                {
                    Assert.Equal("C:\\From\\Index.cshtml", document.FilePath);
                    Assert.Equal("C:\\To\\NewIndex.cshtml", document.TargetPath);
                },
                document =>
                {
                    Assert.Equal("C:\\From\\About.cshtml", document.FilePath);
                    Assert.Equal("C:\\To\\NewAbout.cshtml", document.TargetPath);
                });
        }

        [Fact]
        public void GetCurrentDocuments_ReturnsContentAndNoneItems()
        {
            // Arrange
            ContentItems.Item("Index.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.LinkPropertyName] = "NewIndex.cshtml",
                [ItemReference.FullPathPropertyName] = "C:\\From\\Index.cshtml",
            });
            NoneItems.Item("About.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.LinkPropertyName] = "NewAbout.cshtml",
                [ItemReference.FullPathPropertyName] = "C:\\From\\About.cshtml",
            });
            var services = new TestProjectSystemServices("C:\\To\\Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);
            var changes = new TestProjectChangeDescription[]
            {
                ContentItems.ToChange(),
                NoneItems.ToChange(),
            };
            var update = services.CreateUpdate(changes).Value;

            // Act
            var result = host.GetCurrentDocuments(update);

            // Assert
            Assert.Collection(
                result,
                document =>
                {
                    Assert.Equal("C:\\From\\Index.cshtml", document.FilePath);
                    Assert.Equal("C:\\To\\NewIndex.cshtml", document.TargetPath);
                },
                document =>
                {
                    Assert.Equal("C:\\From\\About.cshtml", document.FilePath);
                    Assert.Equal("C:\\To\\NewAbout.cshtml", document.TargetPath);
                });
        }

        [Fact]
        public void TryGetRazorDocument_NoFilePath_ReturnsFalse()
        {
            // Arrange
            var services = new TestProjectSystemServices("C:\\To\\Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);
            var itemState = new Dictionary<string, string>()
            {
                [ItemReference.LinkPropertyName] = "Index.cshtml",
            }.ToImmutableDictionary();

            // Act
            var result = host.TryGetRazorDocument(itemState, out var document);

            // Assert
            Assert.False(result);
            Assert.Null(document);
        }

        [Fact]
        public void TryGetRazorDocument_NonRazorFilePath_ReturnsFalse()
        {
            // Arrange
            var services = new TestProjectSystemServices("C:\\Path\\Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);
            var itemState = new Dictionary<string, string>()
            {
                [ItemReference.FullPathPropertyName] = "C:\\Path\\site.css",
            }.ToImmutableDictionary();

            // Act
            var result = host.TryGetRazorDocument(itemState, out var document);

            // Assert
            Assert.False(result);
            Assert.Null(document);
        }

        [Fact]
        public void TryGetRazorDocument_NonRazorTargetPath_ReturnsFalse()
        {
            // Arrange
            var services = new TestProjectSystemServices("C:\\Path\\To\\Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);
            var itemState = new Dictionary<string, string>()
            {
                [ItemReference.LinkPropertyName] = "site.html",
                [ItemReference.FullPathPropertyName] = "C:\\Path\\From\\Index.cshtml",
            }.ToImmutableDictionary();

            // Act
            var result = host.TryGetRazorDocument(itemState, out var document);

            // Assert
            Assert.False(result);
            Assert.Null(document);
        }

        [Fact]
        public void TryGetRazorDocument_JustFilePath_ReturnsTrue()
        {
            // Arrange
            var expectedPath = "C:\\Path\\Index.cshtml";
            var services = new TestProjectSystemServices("C:\\Path\\Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);
            var itemState = new Dictionary<string, string>()
            {
                [ItemReference.FullPathPropertyName] = expectedPath,
            }.ToImmutableDictionary();

            // Act
            var result = host.TryGetRazorDocument(itemState, out var document);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedPath, document.FilePath);
            Assert.Equal(expectedPath, document.TargetPath);
        }

        [Fact]
        public void TryGetRazorDocument_LinkedFilepath_ReturnsTrue()
        {
            // Arrange
            var expectedFullPath = "C:\\Path\\From\\Index.cshtml";
            var expectedTargetPath = "C:\\Path\\To\\Index.cshtml";
            var services = new TestProjectSystemServices("C:\\Path\\To\\Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager);
            var itemState = new Dictionary<string, string>()
            {
                [ItemReference.LinkPropertyName] = "Index.cshtml",
                [ItemReference.FullPathPropertyName] = expectedFullPath,
            }.ToImmutableDictionary();

            // Act
            var result = host.TryGetRazorDocument(itemState, out var document);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedFullPath, document.FilePath);
            Assert.Equal(expectedTargetPath, document.TargetPath);
        }

        [ForegroundFact]
        public async Task FallbackRazorProjectHost_ForegroundThread_CreateAndDispose_Succeeds()
        {
            // Arrange
            var services = new TestProjectSystemServices("C:\\To\\Test.csproj");
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

        [ForegroundFact] // This can happen if the .xaml files aren't included correctly.
        public async Task OnProjectChanged_NoRulesDefined()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
            };

            var services = new TestProjectSystemServices("Test.csproj");
            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager)
            {
                AssemblyVersion = new Version(2, 0),
            };

            // Act & Assert
            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_ReadsProperties_InitializesProject()
        {
            // Arrange
            ReferenceItems.Item("c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll");
            ContentItems.Item("Index.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.FullPathPropertyName] = "C:\\Path\\Index.cshtml",
            });
            NoneItems.Item("About.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.FullPathPropertyName] = "C:\\Path\\About.cshtml",
            });

            var changes = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
                ContentItems.ToChange(),
                NoneItems.ToChange(),
            };

            var services = new TestProjectSystemServices("C:\\Path\\Test.csproj");

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
            Assert.Equal("C:\\Path\\Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);

            Assert.Collection(
                snapshot.DocumentFilePaths,
                filePath => Assert.Equal("C:\\Path\\Index.cshtml", filePath),
                filePath => Assert.Equal("C:\\Path\\About.cshtml", filePath));

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_NoAssemblyFound_DoesNotIniatializeProject()
        {
            // Arrange
            var changes = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
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
            ReferenceItems.Item("c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll");

            var changes = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
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
            ReferenceItems.Item("c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll");
            var afterChangeContentItems = new ItemCollection(ManageProjectSystemSchema.ContentItem.SchemaName);
            ContentItems.Item("Index.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.FullPathPropertyName] = "C:\\Path\\Index.cshtml",
            });

            var initialChanges = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
                ContentItems.ToChange(),
            };
            var changes = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
                afterChangeContentItems.ToChange(ContentItems.ToSnapshot()),
            };

            var services = new TestProjectSystemServices("C:\\Path\\Test.csproj");

            var host = new TestFallbackRazorProjectHost(services, Workspace, ProjectManager)
            {
                AssemblyVersion = new Version(2, 0),
            };

            await Task.Run(async () => await host.LoadAsync());
            Assert.Empty(ProjectManager.Projects);

            // Act - 1
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(initialChanges)));

            // Assert - 1
            var snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("C:\\Path\\Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);
            var filePath = Assert.Single(snapshot.DocumentFilePaths);
            Assert.Equal("C:\\Path\\Index.cshtml", filePath);

            // Act - 2
            host.AssemblyVersion = new Version(1, 0);
            await Task.Run(async () => await host.OnProjectChanged(services.CreateUpdate(changes)));

            // Assert - 2
            snapshot = Assert.Single(ProjectManager.Projects);
            Assert.Equal("C:\\Path\\Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_1_0, snapshot.Configuration);
            Assert.Empty(snapshot.DocumentFilePaths);

            await Task.Run(async () => await host.DisposeAsync());
            Assert.Empty(ProjectManager.Projects);
        }

        [ForegroundFact]
        public async Task OnProjectChanged_VersionRemoved_DeinitializesProject()
        {
            // Arrange
            ReferenceItems.Item("c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll");

            var changes = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
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
            host.AssemblyVersion = null;
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
            ReferenceItems.Item("c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll");
            ContentItems.Item("Index.cshtml", new Dictionary<string, string>()
            {
                [ItemReference.FullPathPropertyName] = "C:\\Path\\Index.cshtml",
            });

            var changes = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
                ContentItems.ToChange(),
            };

            var services = new TestProjectSystemServices("C:\\Path\\Test.csproj");

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
            Assert.Equal("C:\\Path\\Test.csproj", snapshot.FilePath);
            Assert.Same(FallbackRazorConfiguration.MVC_2_0, snapshot.Configuration);
            var filePath = Assert.Single(snapshot.DocumentFilePaths);
            Assert.Equal("C:\\Path\\Index.cshtml", filePath);

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
            ReferenceItems.Item("c:\\nuget\\Microsoft.AspNetCore.Mvc.razor.dll");

            var changes = new TestProjectChangeDescription[]
            {
                ReferenceItems.ToChange(),
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
                : base(dispatcher, Mock.Of<ErrorReporter>(), Array.Empty<ProjectSnapshotChangeTrigger>(), workspace)
            {
            }
        }
    }
}
