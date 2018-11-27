// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class CompositeRazorProjectFileSystemTest
    {
        [Fact]
        public void EnumerateItems_ReturnsResultsFromAllFileSystems()
        {
            // Arrange
            var basePath = "base-path";
            var file1 = new TestRazorProjectItem("file1");
            var file2 = new TestRazorProjectItem("file2");
            var file3 = new TestRazorProjectItem("file3");
            var fileSystem1 = Mock.Of<RazorProjectFileSystem>(
                f => f.EnumerateItems(basePath) == new[] { file1 });
            var fileSystem2 = Mock.Of<RazorProjectFileSystem>(
                f => f.EnumerateItems(basePath) == Enumerable.Empty<RazorProjectItem>());
            var fileSystem3 = Mock.Of<RazorProjectFileSystem>(
                f => f.EnumerateItems(basePath) == new[] { file2, file3, });

            var compositeRazorProjectFileSystem = new CompositeRazorProjectFileSystem(new[] { fileSystem1, fileSystem2, fileSystem3 });

            // Act
            var result = compositeRazorProjectFileSystem.EnumerateItems(basePath);

            // Assert
            Assert.Equal(new[] { file1, file2, file3 }, result);
        }

        [Fact]
        public void EnumerateItems_ReturnsEmptySequence_IfNoFileSystemReturnsResults()
        {
            // Arrange
            var basePath = "base-path";
            var fileSystem1 = Mock.Of<RazorProjectFileSystem>(
                f => f.EnumerateItems(basePath) == Enumerable.Empty<RazorProjectItem>());
            var fileSystem2 = Mock.Of<RazorProjectFileSystem>(
                f => f.EnumerateItems(basePath) == Enumerable.Empty<RazorProjectItem>());

            var compositeRazorProjectFileSystem = new CompositeRazorProjectFileSystem(new[] { fileSystem1, fileSystem2 });

            // Act
            var result = compositeRazorProjectFileSystem.EnumerateItems(basePath);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetItem_ReturnsFirstInstanceThatExists()
        {
            // Arrange
            var basePath = "base-path";
            var filePath = "file-path";
            var file1 = new NotFoundProjectItem(basePath, filePath);
            var file2 = new TestRazorProjectItem(filePath);
            RazorProjectItem nullItem = null;
            var fileSystem1 = Mock.Of<RazorProjectFileSystem>(
                f => f.GetItem(filePath) == file1);
            var fileSystem2 = Mock.Of<RazorProjectFileSystem>(
                f => f.GetItem(filePath) == nullItem);
            var fileSystem3 = Mock.Of<RazorProjectFileSystem>(
                f => f.GetItem(filePath) == file2);

            var compositeRazorProjectFileSystem = new CompositeRazorProjectFileSystem(new[] { fileSystem1, fileSystem2, fileSystem3 });

            // Act
            var result = compositeRazorProjectFileSystem.GetItem(filePath);

            // Assert
            Assert.Same(file2, result);
        }
    }
}
