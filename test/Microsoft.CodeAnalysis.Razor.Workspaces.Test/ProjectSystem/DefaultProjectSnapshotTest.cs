// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotTest
    {
        [Fact]
        public void WithProjectChange_WithProject_CreatesSnapshot_UpdatesUnderlyingProject()
        {
            // Arrange
            var underlyingProject = GetProject("Test1");
            var original = new DefaultProjectSnapshot(underlyingProject);

            var anotherProject = GetProject("Test1");

            // Act
            var snapshot = original.WithProjectChange(anotherProject);

            // Assert
            Assert.Same(anotherProject, snapshot.UnderlyingProject);
            Assert.Equal(original.ComputedVersion, snapshot.ComputedVersion);
            Assert.Equal(original.Configuration, snapshot.Configuration);
            Assert.Equal(original.TagHelpers, snapshot.TagHelpers);
        }

        [Fact]
        public void WithProjectChange_WithProject_CreatesSnapshot_UpdatesValues()
        {
            // Arrange
            var underlyingProject = GetProject("Test1");
            var original = new DefaultProjectSnapshot(underlyingProject);

            var anotherProject = GetProject("Test1");
            var update = new ProjectSnapshotUpdateContext(anotherProject)
            {
                Configuration = Mock.Of<ProjectExtensibilityConfiguration>(),
                TagHelpers = Array.Empty<TagHelperDescriptor>(),
            };

            // Act
            var snapshot = original.WithProjectChange(update);

            // Assert
            Assert.Same(original.UnderlyingProject, snapshot.UnderlyingProject);
            Assert.Equal(update.UnderlyingProject.Version, snapshot.ComputedVersion);
            Assert.Same(update.Configuration, snapshot.Configuration);
            Assert.Same(update.TagHelpers, snapshot.TagHelpers);
        }

        [Fact]
        public void HaveTagHelpersChanged_NoUpdatesToTagHelpers_ReturnsFalse()
        {
            // Arrange
            var underlyingProject = GetProject("Test1");
            var original = new DefaultProjectSnapshot(underlyingProject);

            var anotherProject = GetProject("Test1");
            var update = new ProjectSnapshotUpdateContext(anotherProject);
            var snapshot = original.WithProjectChange(update);

            // Act
            var result = snapshot.HaveTagHelpersChanged(original);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HaveTagHelpersChanged_TagHelpersUpdated_ReturnsTrue()
        {
            // Arrange
            var underlyingProject = GetProject("Test1");
            var original = new DefaultProjectSnapshot(underlyingProject);

            var anotherProject = GetProject("Test1");
            var update = new ProjectSnapshotUpdateContext(anotherProject)
            {
                TagHelpers = new[]
                {
                    TagHelperDescriptorBuilder.Create("One", "TestAssembly").Build(),
                    TagHelperDescriptorBuilder.Create("Two", "TestAssembly").Build(),
                },
            };
            var snapshot = original.WithProjectChange(update);

            // Act
            var result = snapshot.HaveTagHelpersChanged(original);

            // Assert
            Assert.True(result);
        }

        private Project GetProject(string name)
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
