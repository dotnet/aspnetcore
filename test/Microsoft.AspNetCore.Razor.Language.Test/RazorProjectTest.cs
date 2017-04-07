// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorProjectTest
    {
        [Fact]
        public void NormalizeAndEnsureValidPath_DoesNotModifyPath()
        {
            // Arrange
            var project = new TestRazorProject();

            // Act
            var path = project.NormalizeAndEnsureValidPath("/Views/Home/Index.cshtml");

            // Assert
            Assert.Equal("/Views/Home/Index.cshtml", path);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NormalizeAndEnsureValidPath_ThrowsIfPathIsNullOrEmpty(string path)
        {
            // Arrange
            var project = new TestRazorProject();

            // Act and Assert
            ExceptionAssert.ThrowsArgumentNullOrEmptyString(() => project.NormalizeAndEnsureValidPath(path), "path");
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("~/foo")]
        [InlineData("\\foo")]
        public void NormalizeAndEnsureValidPath_ThrowsIfPathDoesNotStartWithForwardSlash(string path)
        {
            // Arrange
            var project = new TestRazorProject();

            // Act and Assert
            ExceptionAssert.ThrowsArgument(
                () => project.NormalizeAndEnsureValidPath(path),
                "path",
                "Path must begin with a forward slash '/'.");
        }

        [Fact]
        public void FindHierarchicalItems_ReturnsEmptySequenceIfPathIsAtRoot()
        {
            // Arrange
            var project = new TestRazorProject();

            // Act
            var result = project.FindHierarchicalItems("/", "File.cshtml");

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData("_ViewStart.cshtml")]
        [InlineData("_ViewImports.cshtml")]
        public void FindHierarchicalItems_ReturnsItemsForPath(string fileName)
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem($"/{fileName}"),
                CreateProjectItem($"/Views/{fileName}"),
                CreateProjectItem($"/Views/Home/{fileName}")
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(path, $"{fileName}");

            // Assert
            Assert.Collection(
                result,
                item => Assert.Equal($"/Views/Home/{fileName}", item.Path),
                item => Assert.Equal($"/Views/{fileName}", item.Path),
                item => Assert.Equal($"/{fileName}", item.Path));
        }

        [Fact]
        public void FindHierarchicalItems_ReturnsItemsForPathAtRoot()
        {
            // Arrange
            var path = "/Index.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem("/File.cshtml")
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(path, "File.cshtml");

            // Assert
            Assert.Collection(
                result,
                item => Assert.Equal("/File.cshtml", item.Path));
        }

        [Fact]
        public void FindHierarchicalItems_DoesNotIncludePassedInItem()
        {
            // Arrange
            var path = "/Areas/MyArea/Views/Home/File.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem("/Areas/MyArea/Views/Home/File.cshtml"),
                CreateProjectItem("/Areas/MyArea/Views/File.cshtml"),
                CreateProjectItem("/Areas/MyArea/File.cshtml"),
                CreateProjectItem("/Areas/File.cshtml"),
                CreateProjectItem("/File.cshtml"),
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(path, "File.cshtml");

            // Assert
            Assert.Collection(
                result,
                item => Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.Path),
                item => Assert.Equal("/Areas/MyArea/File.cshtml", item.Path),
                item => Assert.Equal("/Areas/File.cshtml", item.Path),
                item => Assert.Equal("/File.cshtml", item.Path));
        }

        [Fact]
        public void FindHierarchicalItems_ReturnsEmptySequenceIfPassedInItemWithFileNameIsAtRoot()
        {
            // Arrange
            var path = "/File.cshtml";
            var items = new List<RazorProjectItem>
            {
                 CreateProjectItem("/File.cshtml")
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(path, "File.cshtml");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FindHierarchicalItems_IncludesNonExistentFiles()
        {
            // Arrange
            var path = "/Areas/MyArea/Views/Home/Test.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem("/Areas/MyArea/File.cshtml"),
                CreateProjectItem("/File.cshtml")
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(path, "File.cshtml");

            // Assert
            Assert.Collection(
                result,
                item =>
                {
                    Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/Areas/MyArea/File.cshtml", item.Path);
                    Assert.True(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/Areas/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/File.cshtml", item.Path);
                    Assert.True(item.Exists);
                });
        }

        [Theory]
        [InlineData("/Areas")]
        [InlineData("/Areas/")]
        public void FindHierarchicalItems_WithBasePath(string basePath)
        {
            // Arrange
            var path = "/Areas/MyArea/Views/Home/Test.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem("/Areas/MyArea/File.cshtml"),
                CreateProjectItem("/File.cshtml")
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

            // Assert
            Assert.Collection(
                result,
                item =>
                {
                    Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/Areas/MyArea/File.cshtml", item.Path);
                    Assert.True(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/Areas/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                });
        }

        [Theory]
        [InlineData("/Areas/MyArea/Views")]
        [InlineData("/Areas/MyArea/Views/")]
        public void FindHierarchicalItems_WithNestedBasePath(string basePath)
        {
            // Arrange
            var path = "/Areas/MyArea/Views/Home/Test.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem("/Areas/MyArea/File.cshtml"),
                CreateProjectItem("/File.cshtml")
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

            // Assert
            Assert.Collection(
                result,
                item =>
                {
                    Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                },
                item =>
                {
                    Assert.Equal("/Areas/MyArea/Views/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                });
        }

        [Theory]
        [InlineData("/Areas/MyArea/Views/Home")]
        [InlineData("/Areas/MyArea/Views/Home/")]
        public void FindHierarchicalItems_WithFileAtBasePath(string basePath)
        {
            // Arrange
            var path = "/Areas/MyArea/Views/Home/Test.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem("/Areas/MyArea/File.cshtml"),
                CreateProjectItem("/File.cshtml"),
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

            // Assert
            Assert.Collection(
                result,
                item =>
                {
                    Assert.Equal("/Areas/MyArea/Views/Home/File.cshtml", item.Path);
                    Assert.False(item.Exists);
                });
        }

        [Fact]
        public void FindHierarchicalItems_ReturnsEmptySequenceIfPathIsNotASubPathOfBasePath()
        {
            // Arrange
            var basePath = "/Pages";
            var path = "/Areas/MyArea/Views/Home/Test.cshtml";
            var items = new List<RazorProjectItem>
            {
                CreateProjectItem("/Areas/MyArea/File.cshtml"),
                CreateProjectItem("/File.cshtml"),
            };
            var project = new TestRazorProject(items);

            // Act
            var result = project.FindHierarchicalItems(basePath, path, "File.cshtml");

            // Assert
            Assert.Empty(result);
        }

        private RazorProjectItem CreateProjectItem(string path)
        {
            var projectItem = new Mock<RazorProjectItem>();
            projectItem.SetupGet(f => f.Path).Returns(path);
            projectItem.SetupGet(f => f.Exists).Returns(true);
            return projectItem.Object;
        }
    }
}
