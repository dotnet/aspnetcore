// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotTest
    {
        [Fact]
        public void WithWorkspaceProject_CreatesSnapshot_UpdatesUnderlyingProject()
        {
            // Arrange
            var hostProject = new HostProject("Test.cshtml", FallbackRazorConfiguration.MVC_2_0);
            var workspaceProject = GetWorkspaceProject("Test1");
            var original = new DefaultProjectSnapshot(hostProject, workspaceProject);

            var anotherProject = GetWorkspaceProject("Test1");

            // Act
            var snapshot = original.WithWorkspaceProject(anotherProject);

            // Assert
            Assert.Same(anotherProject, snapshot.WorkspaceProject);
            Assert.Equal(original.ComputedVersion, snapshot.ComputedVersion);
            Assert.Equal(original.Configuration, snapshot.Configuration);
        }

        [Fact]
        public void WithProjectChange_WithProject_CreatesSnapshot_UpdatesValues()
        {
            // Arrange
            var hostProject = new HostProject("Test.cshtml", FallbackRazorConfiguration.MVC_2_0);
            var workspaceProject = GetWorkspaceProject("Test1");
            var original = new DefaultProjectSnapshot(hostProject, workspaceProject);

            var anotherProject = GetWorkspaceProject("Test1");
            var update = new ProjectSnapshotUpdateContext(original.FilePath, hostProject, anotherProject, original.Version);

            // Act
            var snapshot = original.WithComputedUpdate(update);

            // Assert
            Assert.Same(original.WorkspaceProject, snapshot.WorkspaceProject);
        }

        private Project GetWorkspaceProject(string name)
        {
            Project project = null;
            TestWorkspace.Create(workspace =>
            {
                project = workspace.AddProject(name, LanguageNames.CSharp);
            });
            return project;
        }
    }
}
