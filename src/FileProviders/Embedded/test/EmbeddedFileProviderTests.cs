// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Embedded.Tests
{
    public class EmbeddedFileProviderTests
    {
        private static readonly string Namespace = typeof(EmbeddedFileProviderTests).Namespace;

        [Fact]
        public void ConstructorWithNullAssemblyThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => new EmbeddedFileProvider(null));
        }

        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_IfFileDoesNotExist()
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly);

            // Act
            var fileInfo = provider.GetFileInfo("DoesNotExist.Txt");

            // Assert
            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.Exists);
        }

        [Theory]
        [InlineData("File.txt")]
        [InlineData("/File.txt")]
        public void GetFileInfo_ReturnsFilesAtRoot(string filePath)
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly);
            var expectedFileLength = 8;

            // Act
            var fileInfo = provider.GetFileInfo(filePath);

            // Assert
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
            Assert.NotEqual(default(DateTimeOffset), fileInfo.LastModified);
            Assert.Equal(expectedFileLength, fileInfo.Length);
            Assert.False(fileInfo.IsDirectory);
            Assert.Null(fileInfo.PhysicalPath);
            Assert.Equal("File.txt", fileInfo.Name);
        }

        [Fact]
        public void GetFileInfo_ReturnsNotFoundFileInfo_IfFileDoesNotExistUnderSpecifiedNamespace()
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace + ".SubNamespace");

            // Act
            var fileInfo = provider.GetFileInfo("File.txt");

            // Assert
            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.Exists);
        }

        [Fact]
        public void GetFileInfo_ReturnsNotFoundIfPathStartsWithBackSlash()
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly);

            // Act
            var fileInfo = provider.GetFileInfo("\\File.txt");

            // Assert
            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.Exists);
        }

        public static TheoryData GetFileInfo_LocatesFilesUnderSpecifiedNamespaceData
        {
            get
            {
                var theoryData = new TheoryData<string>
                {
                    "ResourcesInSubdirectory/File3.txt"
                };

                if (TestPlatformHelper.IsWindows)
                {
                    theoryData.Add("ResourcesInSubdirectory\\File3.txt");
                }

                return theoryData;
            }
        }

        [Theory]
        [MemberData(nameof(GetFileInfo_LocatesFilesUnderSpecifiedNamespaceData))]
        public void GetFileInfo_LocatesFilesUnderSpecifiedNamespace(string path)
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace + ".Resources");

            // Act
            var fileInfo = provider.GetFileInfo(path);

            // Assert
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
            Assert.NotEqual(default(DateTimeOffset), fileInfo.LastModified);
            Assert.True(fileInfo.Length > 0);
            Assert.False(fileInfo.IsDirectory);
            Assert.Null(fileInfo.PhysicalPath);
            Assert.Equal("File3.txt", fileInfo.Name);
        }

        public static TheoryData GetFileInfo_LocatesFilesUnderSubDirectoriesData
        {
            get
            {
                var theoryData = new TheoryData<string>
                {
                    "Resources/File.txt"
                };

                if (TestPlatformHelper.IsWindows)
                {
                    theoryData.Add("Resources\\File.txt");
                }

                return theoryData;
            }
        }

        [Theory]
        [MemberData(nameof(GetFileInfo_LocatesFilesUnderSubDirectoriesData))]
        public void GetFileInfo_LocatesFilesUnderSubDirectories(string path)
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly);

            // Act
            var fileInfo = provider.GetFileInfo(path);

            // Assert
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
            Assert.NotEqual(default(DateTimeOffset), fileInfo.LastModified);
            Assert.True(fileInfo.Length > 0);
            Assert.False(fileInfo.IsDirectory);
            Assert.Null(fileInfo.PhysicalPath);
            Assert.Equal("File.txt", fileInfo.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        public void GetDirectoryContents_ReturnsAllFilesInFileSystem(string path)
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace + ".Resources");

            // Act
            var files = provider.GetDirectoryContents(path);

            // Assert
            Assert.Collection(files.OrderBy(f => f.Name, StringComparer.Ordinal),
                file => Assert.Equal("File.txt", file.Name),
                file => Assert.Equal("ResourcesInSubdirectory.File3.txt", file.Name));

            Assert.False(provider.GetDirectoryContents("file").Exists);
            Assert.False(provider.GetDirectoryContents("file/").Exists);
            Assert.False(provider.GetDirectoryContents("file.txt").Exists);
            Assert.False(provider.GetDirectoryContents("file/txt").Exists);
        }

        [Fact]
        public void GetDirectoryContents_ReturnsEmptySequence_IfResourcesDoNotExistUnderNamespace()
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, "Unknown.Namespace");

            // Act
            var files = provider.GetDirectoryContents(string.Empty);

            // Assert
            Assert.NotNull(files);
            Assert.True(files.Exists);
            Assert.Empty(files);
        }

        [Theory]
        [InlineData("Resources")]
        [InlineData("/Resources")]
        public void GetDirectoryContents_ReturnsNotFoundDirectoryContents_IfHierarchicalPathIsSpecified(string path)
        {
            // Arrange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly);

            // Act
            var files = provider.GetDirectoryContents(path);

            // Assert
            Assert.NotNull(files);
            Assert.False(files.Exists);
            Assert.Empty(files);
        }

        [Fact]
        public void Watch_ReturnsNoOpTrigger()
        {
            // Arange
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly);

            // Act
            var token = provider.Watch("Resources/File.txt");

            // Assert
            Assert.NotNull(token);
            Assert.False(token.ActiveChangeCallbacks);
            Assert.False(token.HasChanged);
        }
    }
}