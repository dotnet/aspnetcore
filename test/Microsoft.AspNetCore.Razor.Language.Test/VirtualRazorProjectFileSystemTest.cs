// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using DirectoryNode = Microsoft.AspNetCore.Razor.Language.VirtualRazorProjectFileSystem.DirectoryNode;
using FileNode = Microsoft.AspNetCore.Razor.Language.VirtualRazorProjectFileSystem.FileNode;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class VirtualRazorProjectFileSystemTest
    {
        [Fact]
        public void GetItem_ReturnsNotFound_IfFileDoesNotExistInRoot()
        {
            // Arrange
            var path = "/root-file.cshtml";
            var projectSystem = new VirtualRazorProjectFileSystem();

            // Act
            projectSystem.Add(new TestRazorProjectItem("/different-file.cshtml"));
            var result = projectSystem.GetItem(path);

            // Assert
            Assert.False(result.Exists);
        }

        [Fact]
        public void GetItem_ReturnsItemAddedToRoot()
        {
            // Arrange
            var path = "/root-file.cshtml";
            var projectSystem = new VirtualRazorProjectFileSystem();
            var projectItem = new TestRazorProjectItem(path);

            // Act
            projectSystem.Add(projectItem);
            var actual = projectSystem.GetItem(path);

            // Assert
            Assert.Same(projectItem, actual);
        }

        [Theory]
        [InlineData("/subDirectory/file.cshtml")]
        [InlineData("/subDirectory/dir2/file.cshtml")]
        [InlineData("/subDirectory/dir2/dir3/file.cshtml")]
        public void GetItem_ReturnsItemAddedToNestedDirectory(string path)
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();
            var projectItem = new TestRazorProjectItem(path);

            // Act
            projectSystem.Add(projectItem);
            var actual = projectSystem.GetItem(path);

            // Assert
            Assert.Same(projectItem, actual);
        }

        [Fact]
        public void GetItem_ReturnsNotFound_WhenNestedDirectoryDoesNotExist()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();

            // Act
            var actual = projectSystem.GetItem("/subDirectory/dir3/file.cshtml");

            // Assert
            Assert.False(actual.Exists);
        }

        [Fact]
        public void GetItem_ReturnsNotFound_WhenNestedDirectoryDoesNotExist_AndPeerDirectoryExists()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();
            var projectItem = new TestRazorProjectItem("/subDirectory/dir2/file.cshtml");

            // Act
            projectSystem.Add(projectItem);
            var actual = projectSystem.GetItem("/subDirectory/dir3/file.cshtml");

            // Assert
            Assert.False(actual.Exists);
        }

        [Fact]
        public void GetItem_ReturnsNotFound_WhenFileDoesNotExistInNestedDirectory()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();
            var projectItem = new TestRazorProjectItem("/subDirectory/dir2/file.cshtml");

            // Act
            projectSystem.Add(projectItem);
            var actual = projectSystem.GetItem("/subDirectory/dir2/file2.cshtml");

            // Assert
            Assert.False(actual.Exists);
        }

        [Fact]
        public void EnumerateItems_AtRoot_ReturnsAllFiles()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();
            var file1 = new TestRazorProjectItem("/subDirectory/dir2/file1.cshtml");
            var file2 = new TestRazorProjectItem("/file2.cshtml");
            var file3 = new TestRazorProjectItem("/dir3/file3.cshtml");
            var file4 = new TestRazorProjectItem("/subDirectory/file4.cshtml");
            projectSystem.Add(file1);
            projectSystem.Add(file2);
            projectSystem.Add(file3);
            projectSystem.Add(file4);

            // Act
            var result = projectSystem.EnumerateItems("/");

            // Assert
            Assert.Equal(new[] { file2, file4, file1, file3 }, result);
        }

        [Fact]
        public void EnumerateItems_AtSubDirectory_ReturnsAllFilesUnderDirectoryHierarchy()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();
            var file1 = new TestRazorProjectItem("/subDirectory/dir2/file1.cshtml");
            var file2 = new TestRazorProjectItem("/file2.cshtml");
            var file3 = new TestRazorProjectItem("/dir3/file3.cshtml");
            var file4 = new TestRazorProjectItem("/subDirectory/file4.cshtml");
            projectSystem.Add(file1);
            projectSystem.Add(file2);
            projectSystem.Add(file3);
            projectSystem.Add(file4);

            // Act
            var result = projectSystem.EnumerateItems("/subDirectory");

            // Assert
            Assert.Equal(new[] { file4, file1 }, result);
        }

        [Fact]
        public void EnumerateItems_WithNoFilesInRoot_ReturnsEmptySequence()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();

            // Act
            var result = projectSystem.EnumerateItems("/");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void EnumerateItems_ForNonExistentDirectory_ReturnsEmptySequence()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();
            projectSystem.Add(new TestRazorProjectItem("/subDirectory/dir2/file1.cshtml"));
            projectSystem.Add(new TestRazorProjectItem("/file2.cshtml"));

            // Act
            var result = projectSystem.EnumerateItems("/dir3");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetHierarchicalItems_Works()
        {
            // Arrange
            var projectSystem = new VirtualRazorProjectFileSystem();
            var viewImport1 = new TestRazorProjectItem("/_ViewImports.cshtml");
            var viewImport2 = new TestRazorProjectItem("/Views/Home/_ViewImports.cshtml");
            projectSystem.Add(viewImport1);
            projectSystem.Add(viewImport2);

            // Act
            var items = projectSystem.FindHierarchicalItems("/", "/Views/Home/Index.cshtml", "_ViewImports.cshtml");

            // Assert
            Assert.Collection(
                items,
                item => Assert.Same(viewImport2, item),
                item => Assert.False(item.Exists),
                item => Assert.Same(viewImport1, item));
        }

        [Fact]
        public void DirectoryNode_GetDirectory_ReturnsRoot()
        {
            // Arrange
            var root = new DirectoryNode("/");

            // Act
            var result = root.GetDirectory("/");

            // Assert
            Assert.Same(root, result);
        }

        [Fact]
        public void DirectoryNode_GetDirectory_ReturnsNull_IfDirectoryDoesNotExist()
        {
            // Arrange
            var root = new DirectoryNode("/");

            // Act
            var result = root.GetDirectory("/does-not/exist");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DirectoryNode_AddFile_CanAddToRoot()
        {
            // Arrange
            var root = new DirectoryNode("/");
            var projectItem = new TestRazorProjectItem("/File.txt");

            // Act
            root.AddFile(new FileNode("/File.txt", projectItem));

            // Assert
            Assert.Empty(root.Directories);
            Assert.Collection(
                root.Files,
                file => Assert.Same(projectItem, file.ProjectItem));
        }

        [Fact]
        public void DirectoryNode_AddFile_CanAddToNestedDirectory()
        {
            // Arrange
            var root = new DirectoryNode("/");
            var projectItem = new TestRazorProjectItem("/Pages/Shared/_Layout.cshtml");

            // Act
            root.AddFile(new FileNode("/Pages/Shared/_Layout.cshtml", projectItem));

            // Assert
            Assert.Collection(
                root.Directories,
                directory =>
                {
                    Assert.Equal("/Pages/", directory.Path);
                    Assert.Empty(directory.Files);

                    Assert.Collection(
                        directory.Directories,
                        subDirectory =>
                        {
                            Assert.Equal("/Pages/Shared/", subDirectory.Path);
                            Assert.Collection(
                                subDirectory.Files,
                                file => Assert.Same(projectItem, file.ProjectItem));
                        });
                });
        }

        [Fact]
        public void DirectoryNode_AddMultipleFiles_ToSameDirectory()
        {
            // Arrange
            var root = new DirectoryNode("/");
            var projectItem1 = new TestRazorProjectItem("/Pages/Shared/_Layout.cshtml");
            var projectItem2 = new TestRazorProjectItem("/Pages/Shared/_Partial.cshtml");

            // Act
            root.AddFile(new FileNode(projectItem1.FilePath, projectItem1));
            root.AddFile(new FileNode(projectItem2.FilePath, projectItem2));

            // Assert
            Assert.Collection(
                root.Directories,
                directory =>
                {
                    Assert.Equal("/Pages/", directory.Path);
                    Assert.Empty(directory.Files);

                    Assert.Collection(
                        directory.Directories,
                        subDirectory =>
                        {
                            Assert.Equal("/Pages/Shared/", subDirectory.Path);
                            Assert.Collection(
                                subDirectory.Files,
                                file => Assert.Same(projectItem1, file.ProjectItem),
                                file => Assert.Same(projectItem2, file.ProjectItem));
                        });
                });
        }

        [Fact]
        public void DirectoryNode_AddsFiles_ToSiblingDirectories()
        {
            // Arrange
            var root = new DirectoryNode("/");
            var projectItem1 = new TestRazorProjectItem("/Pages/Products/Index.cshtml");
            var projectItem2 = new TestRazorProjectItem("/Pages/Accounts/About.cshtml");

            // Act
            root.AddFile(new FileNode(projectItem1.FilePath, projectItem1));
            root.AddFile(new FileNode(projectItem2.FilePath, projectItem2));

            // Assert
            Assert.Collection(
                root.Directories,
                directory =>
                {
                    Assert.Equal("/Pages/", directory.Path);
                    Assert.Empty(directory.Files);

                    Assert.Collection(
                        directory.Directories,
                        subDirectory =>
                        {
                            Assert.Equal("/Pages/Products/", subDirectory.Path);
                            Assert.Collection(
                                subDirectory.Files,
                                file => Assert.Same(projectItem1, file.ProjectItem));
                        },
                        subDirectory =>
                        {
                            Assert.Equal("/Pages/Accounts/", subDirectory.Path);
                            Assert.Collection(
                                subDirectory.Files,
                                file => Assert.Same(projectItem2, file.ProjectItem));
                        });
                });
        }

        [Fact]
        public void DirectoryNode_GetItem_ReturnsItemAtRoot()
        {
            // Arrange
            var root = new DirectoryNode("/");
            var projectItem = new TestRazorProjectItem("/_ViewStart.cshtml");
            root.AddFile(new FileNode(projectItem.FilePath, projectItem));

            // Act
            var result = root.GetItem(projectItem.FilePath);

            // Assert
            Assert.Same(result, projectItem);
        }

        [Fact]
        public void DirectoryNode_GetItem_WhenFilePathSharesSameNameAsSiblingDirectory()
        {
            // Arrange
            var root = new DirectoryNode("/");
            var projectItem1 = new TestRazorProjectItem("/Home.cshtml");
            var projectItem2 = new TestRazorProjectItem("/Home/About.cshtml");
            root.AddFile(new FileNode(projectItem1.FilePath, projectItem1));
            root.AddFile(new FileNode(projectItem2.FilePath, projectItem2));

            // Act
            var result = root.GetItem(projectItem1.FilePath);

            // Assert
            Assert.Same(result, projectItem1);
        }

        [Fact]
        public void DirectoryNode_GetItem_WhenFileNameIsSameAsDirectoryName()
        {
            // Arrange
            var projectItem1 = new TestRazorProjectItem("/Home/Home.cshtml");
            var projectItem2 = new TestRazorProjectItem("/Home/About.cshtml");
            var root = new DirectoryNode("/")
            {
                Directories =
                {
                    new DirectoryNode("/Home/")
                    {
                        Files =
                        {
                            new FileNode(projectItem1.FilePath, projectItem1),
                            new FileNode(projectItem2.FilePath, projectItem2),
                        }
                    }
                },
            };

            // Act
            var result = root.GetItem(projectItem1.FilePath);

            // Assert
            Assert.Same(result, projectItem1);
        }
    }
}
