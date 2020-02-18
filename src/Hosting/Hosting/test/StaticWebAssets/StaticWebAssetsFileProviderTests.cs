// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    public class StaticWebAssetsFileProviderTests
    {
        [Fact]
        public void StaticWebAssetsFileProvider_ConstructorThrows_WhenPathIsNotFound()
        {
            // Arrange, Act & Assert
            var provider = Assert.Throws<DirectoryNotFoundException>(() => new StaticWebAssetsFileProvider("/prefix", "/nonexisting"));
        }

        [Fact]
        public void StaticWebAssetsFileProvider_Constructor_PrependsPrefixWithSlashIfMissing()
        {
            // Arrange & Act
            var provider = new StaticWebAssetsFileProvider("_content", AppContext.BaseDirectory);

            // Assert
            Assert.Equal("/_content", provider.BasePath);
        }

        [Fact]
        public void StaticWebAssetsFileProvider_Constructor_DoesNotPrependPrefixWithSlashIfPresent()
        {
            // Arrange & Act
            var provider = new StaticWebAssetsFileProvider("/_content", AppContext.BaseDirectory);

            // Assert
            Assert.Equal("/_content", provider.BasePath);
        }

        [Theory]
        [InlineData("\\", "_content")]
        [InlineData("\\_content\\RazorClassLib\\Dir", "Castle.Core.dll")]
        [InlineData("", "_content")]
        [InlineData("/", "_content")]
        [InlineData("/_content", "RazorClassLib")]
        [InlineData("/_content/RazorClassLib", "Dir")]
        [InlineData("/_content/RazorClassLib/Dir", "Microsoft.AspNetCore.Hosting.Tests.dll")]
        [InlineData("/_content/RazorClassLib/Dir/testroot/", "TextFile.txt")]
        [InlineData("/_content/RazorClassLib/Dir/testroot/wwwroot/", "README")]
        public void GetDirectoryContents_WalksUpContentRoot(string searchDir, string expected)
        {
            // Arrange
            var provider = new StaticWebAssetsFileProvider("/_content/RazorClassLib/Dir", AppContext.BaseDirectory);

            // Act
            var directory = provider.GetDirectoryContents(searchDir);

            // Assert
            Assert.NotEmpty(directory);
            Assert.Contains(directory, file => string.Equals(file.Name, expected));
        }

        [Fact]
        public void GetDirectoryContents_DoesNotFindNonExistentFiles()
        {
            // Arrange
            var provider = new StaticWebAssetsFileProvider("/_content/RazorClassLib/", AppContext.BaseDirectory);

            // Act
            var directory = provider.GetDirectoryContents("/_content/RazorClassLib/False");

            // Assert
            Assert.Empty(directory);
        }

        [Theory]
        [InlineData("/False/_content/RazorClassLib/")]
        [InlineData("/_content/RazorClass")]
        public void GetDirectoryContents_PartialMatchFails(string requestedUrl)
        {
            // Arrange
            var provider = new StaticWebAssetsFileProvider("/_content/RazorClassLib", AppContext.BaseDirectory);

            // Act
            var directory = provider.GetDirectoryContents(requestedUrl);

            // Assert
            Assert.Empty(directory);
        }

        [Fact]
        public void GetDirectoryContents_HandlesWhitespaceInBase()
        {
            // Arrange
            var provider = new StaticWebAssetsFileProvider("/_content/Static Web Assets",
                Path.Combine(AppContext.BaseDirectory, "testroot", "wwwroot"));

            // Act
            var directory = provider.GetDirectoryContents("/_content/Static Web Assets/Static Web/");

            // Assert
            Assert.Collection(directory,
                file =>
                {
                    Assert.Equal("Static Web.txt", file.Name);
                });
        }

        [Fact]
        public void StaticWebAssetsFileProvider_FindsFileWithSpaces()
        {
            // Arrange & Act
            var provider = new StaticWebAssetsFileProvider("/_content",
                Path.Combine(AppContext.BaseDirectory, "testroot", "wwwroot"));

            // Assert
            Assert.True(provider.GetFileInfo("/_content/Static Web Assets.txt").Exists);
        }

        [Fact]
        public void GetDirectoryContents_HandlesEmptyBasePath()
        {
            // Arrange
            var provider = new StaticWebAssetsFileProvider("/",
                Path.Combine(AppContext.BaseDirectory, "testroot", "wwwroot"));

            // Act
            var directory = provider.GetDirectoryContents("/Static Web/");

            // Assert
            Assert.Collection(directory,
                file =>
                {
                    Assert.Equal("Static Web.txt", file.Name);
                });
        }

        [Fact]
        public void StaticWebAssetsFileProviderWithEmptyBasePath_FindsFile()
        {
            // Arrange & Act
            var provider = new StaticWebAssetsFileProvider("/",
                Path.Combine(AppContext.BaseDirectory, "testroot", "wwwroot"));

            // Assert
            Assert.True(provider.GetFileInfo("/Static Web Assets.txt").Exists);
        }

        [Fact]
        public void GetFileInfo_DoesNotMatch_IncompletePrefixSegments()
        {
            // Arrange
            var expectedResult = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var provider = new StaticWebAssetsFileProvider(
                "_cont",
                Path.GetDirectoryName(new Uri(typeof(StaticWebAssetsFileProviderTests).Assembly.CodeBase).LocalPath));

            // Act
            var file = provider.GetFileInfo("/_content/Microsoft.AspNetCore.TestHost.StaticWebAssets.xml");

            // Assert
            Assert.False(file.Exists, "File exists");
        }

        [Fact]
        public void GetFileInfo_Prefix_RespectsOsCaseSensitivity()
        {
            // Arrange
            var expectedResult = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var provider = new StaticWebAssetsFileProvider(
                "_content",
                Path.GetDirectoryName(new Uri(typeof(StaticWebAssetsFileProviderTests).Assembly.CodeBase).LocalPath));

            // Act
            var file = provider.GetFileInfo("/_CONTENT/Microsoft.AspNetCore.Hosting.StaticWebAssets.xml");

            // Assert
            Assert.Equal(expectedResult, file.Exists);
        }

        [Fact]
        public void GetDirectoryContents_Prefix_RespectsOsCaseSensitivity()
        {
            // Arrange
            var expectedResult = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var provider = new StaticWebAssetsFileProvider(
                "_content",
                Path.GetDirectoryName(new Uri(typeof(StaticWebAssetsFileProviderTests).Assembly.CodeBase).LocalPath));

            // Act
            var directory = provider.GetDirectoryContents("/_CONTENT");

            // Assert
            Assert.Equal(expectedResult, directory.Exists);
        }
    }
}
