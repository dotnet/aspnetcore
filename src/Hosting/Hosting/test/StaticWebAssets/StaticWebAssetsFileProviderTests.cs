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
