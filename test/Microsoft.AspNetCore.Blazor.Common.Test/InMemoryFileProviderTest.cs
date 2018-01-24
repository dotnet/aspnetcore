// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Internal.Common.FileProviders;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Common.Test
{
    public class InMemoryFileProviderTest
    {
        private (string, byte[]) TestItem(string name) => (name, Array.Empty<byte>());

        [Fact]
        public void RequiresPathsToStartWithSlash()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new InMemoryFileProvider(new[] { TestItem("item") });
            });
        }

        [Fact]
        public void RequiresPathsNotToEndWithSlash()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new InMemoryFileProvider(new[] { TestItem("/item/") });
            });
        }

        [Fact]
        public void ReturnsFileInfosForExistingFiles()
        {
            // Arrange
            var instance = new InMemoryFileProvider(new[]
            {
                ("/dirA/item", Encoding.UTF8.GetBytes("Contents of /dirA/item")),
                ("/dirB/item", Encoding.UTF8.GetBytes("Contents of /dirB/item"))
            });

            // Act
            var dirAItem = instance.GetFileInfo("/dirA/item");
            var dirBItem = instance.GetFileInfo("/dirB/item");

            // Assert
            Assert.Equal(
                "Contents of /dirA/item",
                new StreamReader(dirAItem.CreateReadStream()).ReadToEnd());
            Assert.Equal(
                "Contents of /dirB/item",
                new StreamReader(dirBItem.CreateReadStream()).ReadToEnd());
            Assert.True(dirAItem.Exists);
            Assert.False(dirAItem.IsDirectory);
            Assert.True((DateTime.Now - dirAItem.LastModified).TotalDays < 1); // Exact behaviour doesn't need to be defined (at least not yet) but it should be a valid date
            Assert.Equal(22, dirAItem.Length);
            Assert.Equal("item", dirAItem.Name);
            Assert.Equal("/dirA/item", dirAItem.PhysicalPath);
        }

        [Fact]
        public void ReturnsFileInfosForNonExistingFiles()
        {
            // Arrange
            var instance = new InMemoryFileProvider(new[] { TestItem("/dirA/item") });

            // Act
            var mismatchedCaseItem = instance.GetFileInfo("/dira/item");
            var dirBItem = instance.GetFileInfo("/dirB/item");

            // Assert
            Assert.False(mismatchedCaseItem.Exists);
            Assert.False(dirBItem.Exists);
            Assert.False(dirBItem.IsDirectory);
            Assert.Equal("item", dirBItem.Name);
            Assert.Equal("/dirB/item", dirBItem.PhysicalPath);
        }

        [Fact]
        public void ReturnsDirectoryContentsForExistingDirectory()
        {
            // Arrange
            var instance = new InMemoryFileProvider(new[]
            {
                TestItem("/dir/subdir/item1"),
                TestItem("/dir/subdir/item2"),
                TestItem("/dir/otherdir/item3")
            });

            // Act
            var contents = instance.GetDirectoryContents("/dir/subdir");

            // Assert
            Assert.True(contents.Exists);
            Assert.Collection(contents,
                item => Assert.Equal("/dir/subdir/item1", item.PhysicalPath),
                item => Assert.Equal("/dir/subdir/item2", item.PhysicalPath));
        }

        [Fact]
        public void EmptyStringAndSlashAreBothInterpretedAsRootDir()
        {
            // Technically this test duplicates checking the behavior asserted
            // previously (i.e., trailing slashes are ignored), but it's worth
            // checking that nothing bad happens when the path is an empty string

            // Arrange
            var instance = new InMemoryFileProvider(new[] { TestItem("/item") });

            // Act/Assert
            Assert.Collection(instance.GetDirectoryContents(string.Empty),
                item => Assert.Equal("/item", item.PhysicalPath));
            Assert.Collection(instance.GetDirectoryContents("/"),
                item => Assert.Equal("/item", item.PhysicalPath));
        }

        [Fact]
        public void ReturnsDirectoryContentsIfGivenPathEndsWithSlash()
        {
            // Arrange
            var instance = new InMemoryFileProvider(new[] { TestItem("/dir/subdir/item1") });

            // Act
            var contents = instance.GetDirectoryContents("/dir/subdir/");

            // Assert
            Assert.True(contents.Exists);
            Assert.Collection(contents,
                item => Assert.Equal("/dir/subdir/item1", item.PhysicalPath));
        }

        [Fact]
        public void ReturnsDirectoryContentsForNonExistingDirectory()
        {
            // Arrange
            var instance = new InMemoryFileProvider(new[] { TestItem("/dir/subdir/item1") });

            // Act
            var contents = instance.GetDirectoryContents("/dir/otherdir");

            // Assert
            Assert.False(contents.Exists);
            Assert.Throws<InvalidOperationException>(() => contents.GetEnumerator());
        }

        [Fact]
        public void IncludesSubdirectoriesInDirectoryContents()
        {
            // Arrange
            var instance = new InMemoryFileProvider(new[] {
                TestItem("/dir/sub/item1"),
                TestItem("/dir/sub/item2"),
                TestItem("/dir/sub2/item"),
                TestItem("/unrelated/item")
            });

            // Act
            var contents = instance.GetDirectoryContents("/dir");

            // Assert
            Assert.True(contents.Exists);
            Assert.Collection(contents,
                item =>
                {
                    // For this example, verify all properties. Don't need to do this for all examples.
                    Assert.True(item.Exists);
                    Assert.True(item.IsDirectory);
                    Assert.Equal(default(DateTimeOffset), item.LastModified);
                    Assert.Throws<InvalidOperationException>(() => item.Length);
                    Assert.Throws<InvalidOperationException>(() => item.CreateReadStream());
                    Assert.Equal("sub", item.Name);
                    Assert.Equal("/dir/sub", item.PhysicalPath);
                },
                item =>
                {
                    Assert.Equal("/dir/sub2", item.PhysicalPath);
                    Assert.True(item.IsDirectory);
                });
        }

        [Fact]
        public void HasAllAncestorDirectoriesForDirectory()
        {
            // Arrange
            var instance = new InMemoryFileProvider(new[] { TestItem("/a/b/c") });

            // Act/Assert
            Assert.Collection(instance.GetDirectoryContents("/"),
                item =>
                {
                    Assert.Equal("/a", item.PhysicalPath);
                    Assert.True(item.IsDirectory);
                });
            Assert.Collection(instance.GetDirectoryContents("/a"),
                item =>
                {
                    Assert.Equal("/a/b", item.PhysicalPath);
                    Assert.True(item.IsDirectory);
                });
            Assert.Collection(instance.GetDirectoryContents("/a/b"),
                item =>
                {
                    Assert.Equal("/a/b/c", item.PhysicalPath);
                    Assert.False(item.IsDirectory);
                });
        }
    }
}
