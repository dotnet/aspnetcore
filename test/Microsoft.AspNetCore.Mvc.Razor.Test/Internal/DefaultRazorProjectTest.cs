// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DefaultRazorProjectTest
    {
        [Fact]
        public void EnumerateFiles_ReturnsEmptySequenceIfNoCshtmlFilesArePresent()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("File1.txt", "content");
            var file2 = fileProvider.AddFile("File2.js", "content");
            fileProvider.AddDirectoryContent("/", new IFileInfo[] { file1, file2 });

            var razorProject = new DefaultRazorProject(fileProvider);

            // Act
            var razorFiles = razorProject.EnumerateItems("/");

            // Assert
            Assert.Empty(razorFiles);
        }

        [Fact]
        public void EnumerateFiles_ReturnsCshtmlFiles()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("File1.cshtml", "content");
            var file2 = fileProvider.AddFile("File2.js", "content");
            var file3 = fileProvider.AddFile("File3.cshtml", "content");
            fileProvider.AddDirectoryContent("/", new IFileInfo[] { file1, file2, file3 });

            var razorProject = new DefaultRazorProject(fileProvider);

            // Act
            var razorFiles = razorProject.EnumerateItems("/");

            // Assert
            Assert.Collection(razorFiles.OrderBy(f => f.Path),
                file => Assert.Equal("/File1.cshtml", file.Path),
                file => Assert.Equal("/File3.cshtml", file.Path));
        }

        [Fact]
        public void EnumerateFiles_IteratesOverAllCshtmlUnderRoot()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var directory1 = new TestDirectoryFileInfo
            {
                Name = "Level1-Dir1",
            };
            var file1 = fileProvider.AddFile("File1.cshtml", "content");
            var directory2 = new TestDirectoryFileInfo
            {
                Name = "Level1-Dir2",
            };
            fileProvider.AddDirectoryContent("/", new IFileInfo[] { directory1, file1, directory2 });

            var file2 = fileProvider.AddFile("Level1-Dir1/File2.cshtml", "content");
            var file3 = fileProvider.AddFile("Level1-Dir1/File3.cshtml", "content");
            var file4 = fileProvider.AddFile("Level1-Dir1/File4.txt", "content");
            var directory3 = new TestDirectoryFileInfo
            {
                Name = "Level2-Dir1"
            };
            fileProvider.AddDirectoryContent("/Level1-Dir1", new IFileInfo[] { file2, directory3, file3, file4 });
            var file5 = fileProvider.AddFile("Level1-Dir2/File5.cshtml", "content");
            fileProvider.AddDirectoryContent("/Level1-Dir2", new IFileInfo[] { file5 });
            fileProvider.AddDirectoryContent("/Level1/Level2", new IFileInfo[0]);
            var razorProject = new DefaultRazorProject(fileProvider);

            // Act
            var razorFiles = razorProject.EnumerateItems("/");

            // Assert
            Assert.Collection(razorFiles.OrderBy(f => f.Path),
                file => Assert.Equal("/File1.cshtml", file.Path),
                file => Assert.Equal("/Level1-Dir1/File2.cshtml", file.Path),
                file => Assert.Equal("/Level1-Dir1/File3.cshtml", file.Path),
                file => Assert.Equal("/Level1-Dir2/File5.cshtml", file.Path));
        }

        [Fact]
        public void EnumerateFiles_IteratesOverAllCshtmlUnderPath()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var directory1 = new TestDirectoryFileInfo
            {
                Name = "Level1-Dir1",
            };
            var file1 = fileProvider.AddFile("File1.cshtml", "content");
            var directory2 = new TestDirectoryFileInfo
            {
                Name = "Level1-Dir2",
            };
            fileProvider.AddDirectoryContent("/", new IFileInfo[] { directory1, file1, directory2 });

            var file2 = fileProvider.AddFile("Level1-Dir1/File2.cshtml", "content");
            var file3 = fileProvider.AddFile("Level1-Dir1/File3.cshtml", "content");
            var file4 = fileProvider.AddFile("Level1-Dir1/File4.txt", "content");
            var directory3 = new TestDirectoryFileInfo
            {
                Name = "Level2-Dir1"
            };
            fileProvider.AddDirectoryContent("/Level1-Dir1", new IFileInfo[] { file2, directory3, file3, file4 });
            var file5 = fileProvider.AddFile("Level1-Dir2/File5.cshtml", "content");
            fileProvider.AddDirectoryContent("/Level1-Dir2", new IFileInfo[] { file5 });
            fileProvider.AddDirectoryContent("/Level1/Level2", new IFileInfo[0]);
            var razorProject = new DefaultRazorProject(fileProvider);

            // Act
            var razorFiles = razorProject.EnumerateItems("/Level1-Dir1");

            // Assert
            Assert.Collection(razorFiles.OrderBy(f => f.Path),
                file => Assert.Equal("/File2.cshtml", file.Path),
                file => Assert.Equal("/File3.cshtml", file.Path));
        }
    }
}
