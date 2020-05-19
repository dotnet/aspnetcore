// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    public class ManifestEntryTests
    {
        [Fact]
        public void TraversingAFile_ReturnsUnknownPath()
        {
            // Arrange
            var file = new ManifestFile("a", "a.b.c");

            // Act
            var result = file.Traverse(".");

            // Assert
            Assert.Equal(ManifestEntry.UnknownPath, result);
        }

        [Fact]
        public void TraversingANonExistingFile_ReturnsUnknownPath()
        {
            // Arrange
            var directory = ManifestDirectory.CreateDirectory("a", Array.Empty<ManifestEntry>());

            // Act
            var result = directory.Traverse("missing.txt");

            // Assert
            Assert.Equal(ManifestEntry.UnknownPath, result);
        }

        [Fact]
        public void TraversingWithDot_ReturnsSelf()
        {
            // Arrange
            var directory = ManifestDirectory.CreateDirectory("a", Array.Empty<ManifestEntry>());

            // Act
            var result = directory.Traverse(".");

            // Assert
            Assert.Same(directory, result);
        }

        [Fact]
        public void TraversingWithDotDot_ReturnsParent()
        {
            // Arrange
            var childDirectory = ManifestDirectory.CreateDirectory("b", Array.Empty<ManifestEntry>());
            var directory = ManifestDirectory.CreateDirectory("a", new[] { childDirectory });

            // Act
            var result = childDirectory.Traverse("..");

            // Assert
            Assert.Equal(directory, result);
        }

        [Fact]
        public void TraversingRootDirectoryWithDotDot_ReturnsSinkDirectory()
        {
            // Arrange
            var directory = ManifestDirectory.CreateRootDirectory(Array.Empty<ManifestEntry>());

            // Act
            var result = directory.Traverse("..");

            // Assert
            Assert.Equal(ManifestEntry.UnknownPath, result);
        }

        [Fact]
        public void ScopingAFolderAndTryingToGetAScopedFile_ReturnsSinkDirectory()
        {
            // Arrange
            var directory = ManifestDirectory.CreateRootDirectory(new[] {
                ManifestDirectory.CreateDirectory("a",
                    new[] { new ManifestFile("test1.txt", "text.txt") }),
                ManifestDirectory.CreateDirectory("b",
                    new[] { new ManifestFile("test2.txt", "test2.txt") }) });

            var newRoot = ((ManifestDirectory)directory.Traverse("a")).ToRootDirectory();

            // Act
            var result = newRoot.Traverse("../b/test.txt");

            // Assert
            Assert.Same(ManifestEntry.UnknownPath, result);
        }

        [Theory]
        [InlineData("..")]
        [InlineData(".")]
        [InlineData("file.txt")]
        [InlineData("folder")]
        public void TraversingUnknownPath_ReturnsSinkDirectory(string path)
        {
            // Arrange
            var directory = ManifestEntry.UnknownPath;

            // Act
            var result = directory.Traverse(path);

            // Assert
            Assert.Equal(ManifestEntry.UnknownPath, result);
        }
    }
}
