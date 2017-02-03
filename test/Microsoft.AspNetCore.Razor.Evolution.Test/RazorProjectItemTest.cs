// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorProjectItemTest
    {
        [Fact]
        public void CombinedPath_ReturnsPathIfBasePathIsEmpty()
        {
            // Arrange
            var emptyBasePath = "/";
            var path = "/foo/bar.cshtml";
            var projectItem = new TestRazorProjectItem(emptyBasePath, path);

            // Act
            var combinedPath = projectItem.CombinedPath;

            // Assert
            Assert.Equal(path, combinedPath);
        }

        [Theory]
        [InlineData("/root", "/root/foo/bar.cshtml")]
        [InlineData("root/subdir", "root/subdir/foo/bar.cshtml")]
        public void CombinedPath_ConcatsPaths(string basePath, string expected)
        {
            // Arrange
            var path = "/foo/bar.cshtml";
            var projectItem = new TestRazorProjectItem(basePath, path);

            // Act
            var combinedPath = projectItem.CombinedPath;

            // Assert
            Assert.Equal(expected, combinedPath);
        }

        [Theory]
        [InlineData("/Home/Index")]
        [InlineData("EditUser")]
        public void Extension_ReturnsNullIfFileDoesNotHaveExtension(string path)
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/views", path);

            // Act
            var extension = projectItem.Extension;

            // Assert
            Assert.Null(extension);
        }

        [Theory]
        [InlineData("/Home/Index.cshtml", ".cshtml")]
        [InlineData("/Home/Index.en-gb.cshtml", ".cshtml")]
        [InlineData("EditUser.razor", ".razor")]
        public void Extension_ReturnsFileExtension(string path, string expected)
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/views", path);

            // Act
            var extension = projectItem.Extension;

            // Assert
            Assert.Equal(expected, extension);
        }

        [Theory]
        [InlineData("Home/Index.cshtml", "Index.cshtml")]
        [InlineData("/Accounts/Customers/Manage-en-us.razor", "Manage-en-us.razor")]
        public void FileName_ReturnsFileNameWithExtension(string path, string expected)
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/", path);

            // Act
            var fileName = projectItem.Filename;

            // Assert
            Assert.Equal(expected, fileName);
        }

        [Theory]
        [InlineData("Home/Index", "Home/Index")]
        [InlineData("Home/Index.cshtml", "Home/Index")]
        [InlineData("/Accounts/Customers/Manage.en-us.razor", "/Accounts/Customers/Manage.en-us")]
        [InlineData("/Accounts/Customers/Manage-en-us.razor", "/Accounts/Customers/Manage-en-us")]
        public void PathWithoutExtension_ExcludesExtension(string path, string expected)
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/", path);

            // Act
            var fileName = projectItem.PathWithoutExtension;

            // Assert
            Assert.Equal(expected, fileName);
        }


        private class TestRazorProjectItem : RazorProjectItem
        {
            public TestRazorProjectItem(string basePath, string path)
            {
                BasePath = basePath;
                Path = path;
            }

            public override string BasePath { get; }

            public override string Path { get; }

            public override string PhysicalPath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool Exists => true;

            public override Stream Read()
            {
                throw new NotImplementedException();
            }
        }
    }
}
