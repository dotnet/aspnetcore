// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Internal.Common.FileProviders;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Common.Test
{
    public class CompositeMountedFileProviderTest
    {
        private (string, byte[]) TestItem(string name) => (name, Array.Empty<byte>());
        private (string, byte[]) TestItem(string name, string data) => (name, Encoding.UTF8.GetBytes(data));
        private IFileProvider TestFileProvider(params string[] paths) => new InMemoryFileProvider(paths.Select(TestItem));

        [Fact]
        public void MountPointsMustStartWithSlash()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new CompositeMountedFileProvider(
                    ("test", TestFileProvider("/something.txt")));
            });
        }

        [Fact]
        public void NonRootMountPointsMustNotEndWithSlash()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new CompositeMountedFileProvider(
                    ("/test/", TestFileProvider("/something.txt")));
            });
        }

        [Fact]
        public void MountedFilePathsMustStartWithSlash()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new CompositeMountedFileProvider(
                    ("/test", TestFileProvider("something.txt")));
            });
        }

        [Fact]
        public void CanMountFileProviderAtRoot()
        {
            // Arrange
            IFileProvider childProvider = new InMemoryFileProvider(new[]
            {
                TestItem("/rootitem.txt", "Root item contents"),
                TestItem("/subdir/another", "Another test item"),
            });
            var instance = new CompositeMountedFileProvider(("/", childProvider));

            // Act
            var rootContents = instance.GetDirectoryContents(string.Empty);
            var subdirContents = instance.GetDirectoryContents("/subdir");

            // Assert
            Assert.Collection(rootContents,
                item =>
                {
                    Assert.Equal("/rootitem.txt", item.PhysicalPath);
                    Assert.False(item.IsDirectory);
                    Assert.Equal("Root item contents", new StreamReader(item.CreateReadStream()).ReadToEnd());
                },
                item =>
                {
                    Assert.Equal("/subdir", item.PhysicalPath);
                    Assert.True(item.IsDirectory);
                });

            Assert.Collection(subdirContents,
                item =>
                {
                    Assert.Equal("/subdir/another", item.PhysicalPath);
                    Assert.False(item.IsDirectory);
                    Assert.Equal("Another test item", new StreamReader(item.CreateReadStream()).ReadToEnd());
                });
        }

        [Fact]
        public void CanMountFileProvidersAtSubPaths()
        {
            // Arrange
            var instance = new CompositeMountedFileProvider(
                ("/dir", TestFileProvider("/first", "/A/second", "/A/third")),
                ("/dir/sub", TestFileProvider("/X", "/B/Y", "/B/Z")),
                ("/other", TestFileProvider("/final")));

            // Act
            var rootContents = instance.GetDirectoryContents("/");
            var rootDirContents = instance.GetDirectoryContents("/dir");
            var rootDirAContents = instance.GetDirectoryContents("/dir/A");
            var rootDirSubContents = instance.GetDirectoryContents("/dir/sub");
            var rootDirSubBContents = instance.GetDirectoryContents("/dir/sub/B");
            var otherContents = instance.GetDirectoryContents("/other");

            // Assert
            Assert.Collection(rootContents,
                item => Assert.Equal("/dir", item.PhysicalPath),
                item => Assert.Equal("/other", item.PhysicalPath));
            Assert.Collection(rootDirContents,
                item => Assert.Equal("/dir/first", item.PhysicalPath),
                item => Assert.Equal("/dir/A", item.PhysicalPath),
                item => Assert.Equal("/dir/sub", item.PhysicalPath));
            Assert.Collection(rootDirAContents,
                item => Assert.Equal("/dir/A/second", item.PhysicalPath),
                item => Assert.Equal("/dir/A/third", item.PhysicalPath));
            Assert.Collection(rootDirSubContents,
                item => Assert.Equal("/dir/sub/X", item.PhysicalPath),
                item => Assert.Equal("/dir/sub/B", item.PhysicalPath));
            Assert.Collection(rootDirSubBContents,
                item => Assert.Equal("/dir/sub/B/Y", item.PhysicalPath),
                item => Assert.Equal("/dir/sub/B/Z", item.PhysicalPath));
            Assert.Collection(otherContents,
                item => Assert.Equal("/other/final", item.PhysicalPath));
        }

        [Fact]
        public void CanMountMultipleFileProvidersAtSameLocation()
        {
            // Arrange
            var instance = new CompositeMountedFileProvider(
                ("/dir", TestFileProvider("/first")),
                ("/dir", TestFileProvider("/second")));

            // Act
            var contents = instance.GetDirectoryContents("/dir");

            // Assert
            Assert.Collection(contents,
                item => Assert.Equal("/dir/first", item.PhysicalPath),
                item => Assert.Equal("/dir/second", item.PhysicalPath));
        }

        [Fact]
        public void DisallowsOverlappingFiles()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new CompositeMountedFileProvider(
                    ("/dir", TestFileProvider("/file")),
                    ("/", TestFileProvider("/dir/file")));
            });
        }
    }
}
