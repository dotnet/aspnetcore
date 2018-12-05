// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorProjectFileSystemTest
    {
        private static string TestFolder { get; } = Path.Combine(
            TestProject.GetProjectDirectory(typeof(DefaultRazorProjectFileSystemTest)),
            "TestFiles",
            "DefaultRazorProjectFileSystem");

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NormalizeAndEnsureValidPath_ThrowsIfPathIsNullOrEmpty(string path)
        {
            // Arrange
            var fileSystem = new TestRazorProjectFileSystem("C:/some/test/path/root");

            // Act and Assert
            ExceptionAssert.ThrowsArgumentNullOrEmptyString(() => fileSystem.NormalizeAndEnsureValidPath(path), "path");
        }

        [Fact]
        public void NormalizeAndEnsureValidPath_NormalizesToAbsolutePath()
        {
            // Arrange
            var fileSystem = new TestRazorProjectFileSystem("C:/some/test/path/root");

            // Act
            var absolutePath = fileSystem.NormalizeAndEnsureValidPath("file.cshtml");

            // Assert
            Assert.Equal("C:/some/test/path/root/file.cshtml", absolutePath);
        }

        [Fact]
        public void NormalizeAndEnsureValidPath_NormalizesToAbsolutePathWithoutForwardSlash()
        {
            // Arrange
            var fileSystem = new TestRazorProjectFileSystem("C:/some/test/path/root");

            // Act
            var absolutePath = fileSystem.NormalizeAndEnsureValidPath("/file.cshtml");

            // Assert
            Assert.Equal("C:/some/test/path/root/file.cshtml", absolutePath);
        }

        [Fact]
        public void NormalizeAndEnsureValidPath_NormalizesToForwardSlashes()
        {
            // Arrange
            var fileSystem = new TestRazorProjectFileSystem(@"C:\some\test\path\root");

            // Act
            var absolutePath = fileSystem.NormalizeAndEnsureValidPath(@"something\file.cshtml");

            // Assert
            Assert.Equal("C:/some/test/path/root/something/file.cshtml", absolutePath);
        }

        [Fact]
        public void EnumerateItems_DiscoversAllCshtmlFiles()
        {
            // Arrange
            var fileSystem = new DefaultRazorProjectFileSystem(TestFolder);

            // Act
            var items = fileSystem.EnumerateItems("/");

            // Assert
            Assert.Collection(
                items.OrderBy(f => f.FilePath),
                item =>
                {
                    Assert.Equal("/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal("_ViewImports.cshtml", item.RelativePhysicalPath);

                },
                item =>
                {
                    Assert.Equal("/Home.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Home.cshtml"), item.PhysicalPath);
                    Assert.Equal("Home.cshtml", item.RelativePhysicalPath);

                },
                item =>
                {
                    Assert.Equal("/Views/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Views", "_ViewImports.cshtml"), item.RelativePhysicalPath);

                },
                item =>
                {
                    Assert.Equal("/Views/About/About.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "About", "About.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Views", "About", "About.cshtml"), item.RelativePhysicalPath);
                },
                item =>
                {
                    Assert.Equal("/Views/Home/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "Home", "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Views", "Home", "_ViewImports.cshtml"), item.RelativePhysicalPath);

                },
                item =>
                {
                    Assert.Equal("/Views/Home/Index.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "Home", "Index.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Views", "Home", "Index.cshtml"), item.RelativePhysicalPath);
                });
        }

        [Fact]
        public void EnumerateItems_DiscoversAllCshtmlFiles_UnderSpecifiedBasePath()
        {
            // Arrange
            var fileSystem = new DefaultRazorProjectFileSystem(TestFolder);

            // Act
            var items = fileSystem.EnumerateItems("/Views");

            // Assert
            Assert.Collection(
                items.OrderBy(f => f.FilePath),
                item =>
                {
                    Assert.Equal("/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/Views", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("_ViewImports.cshtml"), item.RelativePhysicalPath);
                },
                item =>
                {
                    Assert.Equal("/About/About.cshtml", item.FilePath);
                    Assert.Equal("/Views", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "About", "About.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("About", "About.cshtml"), item.RelativePhysicalPath);
                },
                item =>
                {
                    Assert.Equal("/Home/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/Views", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "Home", "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Home", "_ViewImports.cshtml"), item.RelativePhysicalPath);
                },
                item =>
                {
                    Assert.Equal("/Home/Index.cshtml", item.FilePath);
                    Assert.Equal("/Views", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "Home", "Index.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Home", "Index.cshtml"), item.RelativePhysicalPath);
                });
        }

        [Fact]
        public void EnumerateItems_ReturnsEmptySequence_WhenBasePathDoesNotExist()
        {
            // Arrange
            var fileSystem = new DefaultRazorProjectFileSystem(TestFolder);

            // Act
            var items = fileSystem.EnumerateItems("/Does-Not-Exist");

            // Assert
            Assert.Empty(items);
        }

        [Fact]
        public void FindHierarchicalItems_FindsItemsWithMatchingNames()
        {
            // Arrange
            var fileSystem = new DefaultRazorProjectFileSystem(TestFolder);

            // Act
            var items = fileSystem.FindHierarchicalItems("/Views/Home/Index.cshtml", "_ViewImports.cshtml");

            // Assert
            Assert.Collection(
                items,
                item =>
                {
                    Assert.Equal("/Views/Home/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "Home", "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Views", "Home", "_ViewImports.cshtml"), item.RelativePhysicalPath);

                },
                item =>
                {
                    Assert.Equal("/Views/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "Views", "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal(Path.Combine("Views", "_ViewImports.cshtml"), item.RelativePhysicalPath);

                },
                item =>
                {
                    Assert.Equal("/_ViewImports.cshtml", item.FilePath);
                    Assert.Equal("/", item.BasePath);
                    Assert.Equal(Path.Combine(TestFolder, "_ViewImports.cshtml"), item.PhysicalPath);
                    Assert.Equal("_ViewImports.cshtml", item.RelativePhysicalPath);

                });
        }

        [Fact]
        public void GetItem_ReturnsFileFromDisk()
        {
            // Arrange
            var filePath = "/Views/About/About.cshtml";
            var fileSystem = new DefaultRazorProjectFileSystem(TestFolder);

            // Act
            var item = fileSystem.GetItem(filePath);

            // Assert
            Assert.True(item.Exists);
            Assert.Equal(filePath, item.FilePath);
            Assert.Equal("/", item.BasePath);
            Assert.Equal(Path.Combine(TestFolder, "Views", "About", "About.cshtml"), item.PhysicalPath);
            Assert.Equal(Path.Combine("Views", "About", "About.cshtml"), item.RelativePhysicalPath);
        }

        [Fact]
        public void GetItem_ReturnsNotFoundResult()
        {
            // Arrange
            var path = "/NotFound.cshtml";
            var fileSystem = new DefaultRazorProjectFileSystem(TestFolder);

            // Act
            var item = fileSystem.GetItem(path);

            // Assert
            Assert.False(item.Exists);
        }

        private class TestRazorProjectFileSystem : DefaultRazorProjectFileSystem
        {
            public TestRazorProjectFileSystem(string root) : base(root)
            {
            }

            public new string NormalizeAndEnsureValidPath(string path) => base.NormalizeAndEnsureValidPath(path);
        }
    }
}
