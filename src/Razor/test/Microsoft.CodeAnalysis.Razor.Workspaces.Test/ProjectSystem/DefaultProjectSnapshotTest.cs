// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
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
            Assert.Equal(original.TagHelpers, snapshot.TagHelpers);
        }

        [Fact]
        public void WithProjectChange_WithProject_CreatesSnapshot_UpdatesValues()
        {
            // Arrange
            var hostProject = new HostProject("Test.cshtml", FallbackRazorConfiguration.MVC_2_0);
            var workspaceProject = GetWorkspaceProject("Test1");
            var original = new DefaultProjectSnapshot(hostProject, workspaceProject);

            var anotherProject = GetWorkspaceProject("Test1");
            var update = new ProjectSnapshotUpdateContext(original.FilePath, hostProject, anotherProject, original.Version)
            {
                TagHelpers = Array.Empty<TagHelperDescriptor>(),
            };

            // Act
            var snapshot = original.WithComputedUpdate(update);

            // Assert
            Assert.Same(original.WorkspaceProject, snapshot.WorkspaceProject);
            Assert.Same(update.TagHelpers, snapshot.TagHelpers);
        }

        [Fact]
        public void HaveTagHelpersChanged_NoUpdatesToTagHelpers_ReturnsFalse()
        {
            // Arrange
            var hostProject = new HostProject("Test1.csproj", RazorConfiguration.Default);
            var workspaceProject = GetWorkspaceProject("Test1");
            var original = new DefaultProjectSnapshot(hostProject, workspaceProject);

            var anotherProject = GetWorkspaceProject("Test1");
            var update = new ProjectSnapshotUpdateContext("Test1.csproj", hostProject, anotherProject, VersionStamp.Default);
            var snapshot = original.WithComputedUpdate(update);

            // Act
            var result = snapshot.HaveTagHelpersChanged(original);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HaveTagHelpersChanged_TagHelpersUpdated_ReturnsTrue()
        {
            // Arrange
            var hostProject = new HostProject("Test1.csproj", RazorConfiguration.Default);
            var workspaceProject = GetWorkspaceProject("Test1");
            var original = new DefaultProjectSnapshot(hostProject, workspaceProject);

            var anotherProject = GetWorkspaceProject("Test1");
            var update = new ProjectSnapshotUpdateContext("Test1.csproj", hostProject, anotherProject, VersionStamp.Default)
            {
                TagHelpers = new[]
                {
                    TagHelperDescriptorBuilder.Create("One", "TestAssembly").Build(),
                    TagHelperDescriptorBuilder.Create("Two", "TestAssembly").Build(),
                },
            };
            var snapshot = original.WithComputedUpdate(update);

            // Act
            var result = snapshot.HaveTagHelpersChanged(original);

            // Assert
            Assert.True(result);
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
