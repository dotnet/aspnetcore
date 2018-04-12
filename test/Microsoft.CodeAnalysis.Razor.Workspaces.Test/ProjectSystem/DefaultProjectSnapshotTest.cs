// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotTest
    {
        public DefaultProjectSnapshotTest()
        {
            TagHelperResolver = new TestTagHelperResolver();

            HostServices = TestServices.Create(
                new IWorkspaceService[]
                {
                    new TestProjectSnapshotProjectEngineFactory(),
                },
                new ILanguageService[]
                {
                    TagHelperResolver,
                });

            HostProject = new HostProject("c:\\MyProject\\Test.csproj", FallbackRazorConfiguration.MVC_2_0);
            HostProjectWithConfigurationChange = new HostProject("c:\\MyProject\\Test.csproj", FallbackRazorConfiguration.MVC_1_0);

            Workspace = TestWorkspace.Create(HostServices);

            var projectId = ProjectId.CreateNewId("Test");
            var solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                "Test",
                "Test",
                LanguageNames.CSharp,
                "c:\\MyProject\\Test.csproj"));
            WorkspaceProject = solution.GetProject(projectId);

            SomeTagHelpers = new List<TagHelperDescriptor>();
            SomeTagHelpers.Add(TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build());

            Documents = new HostDocument[]
            {
                new HostDocument("c:\\MyProject\\File.cshtml", "File.cshtml"),
                new HostDocument("c:\\MyProject\\Index.cshtml", "Index.cshtml"),

                // linked file
                new HostDocument("c:\\SomeOtherProject\\Index.cshtml", "Pages\\Index.cshtml"),
            };
        }

        private HostDocument[] Documents { get; }

        private HostProject HostProject { get; }

        private HostProject HostProjectWithConfigurationChange { get; }

        private Project WorkspaceProject { get; }

        private TestTagHelperResolver TagHelperResolver { get; }

        private HostServices HostServices { get; }

        private Workspace Workspace { get; }

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        [Fact]
        public void ProjectSnapshot_CachesDocumentSnapshots()
        {
            // Arrange
            var state = new ProjectState(Workspace.Services, HostProject, WorkspaceProject)
                .AddHostDocument(Documents[0])
                .AddHostDocument(Documents[1])
                .AddHostDocument(Documents[2]);
            var snapshot = new DefaultProjectSnapshot(state);

            // Act
            var documents = snapshot.DocumentFilePaths.ToDictionary(f => f, f => snapshot.GetDocument(f));

            // Assert
            Assert.Collection(
                documents,
                d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)),
                d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)),
                d => Assert.Same(d.Value, snapshot.GetDocument(d.Key)));
        }

        [Fact]
        public void ProjectSnapshot_CachesTagHelperTask()
        {
            // Arrange
            TagHelperResolver.CompletionSource = new TaskCompletionSource<TagHelperResolutionResult>();

            try
            {
                var state = new ProjectState(Workspace.Services, HostProject, WorkspaceProject);
                var snapshot = new DefaultProjectSnapshot(state);

                // Act
                var task1 = snapshot.GetTagHelpersAsync();
                var task2 = snapshot.GetTagHelpersAsync();

                // Assert
                Assert.Same(task1, task2);
            }
            finally
            {
                TagHelperResolver.CompletionSource.SetCanceled();
            }
        }
    }
}
