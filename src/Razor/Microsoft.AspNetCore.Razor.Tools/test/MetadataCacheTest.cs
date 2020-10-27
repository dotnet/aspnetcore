// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class MetadataCacheTest
    {
        [Fact]
        public void GetMetadata_AddsToCache()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var metadataCache = new MetadataCache();
                var assemblyFilePath = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");

                // Act
                var result = metadataCache.GetMetadata(assemblyFilePath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(1, metadataCache.Cache.Count);
            }
        }

        [Fact]
        public void GetMetadata_UsesCache()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var metadataCache = new MetadataCache();
                var assemblyFilePath = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");

                // Act 1
                var result = metadataCache.GetMetadata(assemblyFilePath);

                // Assert 1
                Assert.NotNull(result);
                Assert.Equal(1, metadataCache.Cache.Count);

                // Act 2
                var cacheResult = metadataCache.GetMetadata(assemblyFilePath);

                // Assert 2
                Assert.Same(result, cacheResult);
                Assert.Equal(1, metadataCache.Cache.Count);
            }
        }

        [Fact]
        public void GetMetadata_MultipleFiles_ReturnsDifferentResultsAndAddsToCache()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var metadataCache = new MetadataCache();
                var assemblyFilePath1 = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");
                var assemblyFilePath2 = LoaderTestResources.Gamma.WriteToFile(directory.DirectoryPath, "Gamma.dll");

                // Act
                var result1 = metadataCache.GetMetadata(assemblyFilePath1);
                var result2 = metadataCache.GetMetadata(assemblyFilePath2);

                // Assert
                Assert.NotSame(result1, result2);
                Assert.Equal(2, metadataCache.Cache.Count);
            }
        }

        [Fact]
        public void GetMetadata_ReplacesCache_IfFileTimestampChanged()
        {
            using (var directory = TempDirectory.Create())
            {
                // Arrange
                var metadataCache = new MetadataCache();
                var assemblyFilePath = LoaderTestResources.Delta.WriteToFile(directory.DirectoryPath, "Delta.dll");

                // Act 1
                var result = metadataCache.GetMetadata(assemblyFilePath);

                // Assert 1
                Assert.NotNull(result);
                var entry = Assert.Single(metadataCache.Cache.TestingEnumerable);
                Assert.Same(result, entry.Value.Metadata);

                // Act 2
                // Update the timestamp of the file
                File.SetLastWriteTimeUtc(assemblyFilePath, File.GetLastWriteTimeUtc(assemblyFilePath).AddSeconds(1));
                var cacheResult = metadataCache.GetMetadata(assemblyFilePath);

                // Assert 2
                Assert.NotSame(result, cacheResult);
                entry = Assert.Single(metadataCache.Cache.TestingEnumerable);
                Assert.Same(cacheResult, entry.Value.Metadata);
            }
        }
    }
}
