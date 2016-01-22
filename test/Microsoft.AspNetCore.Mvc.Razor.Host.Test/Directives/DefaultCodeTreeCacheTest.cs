// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Directives;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Host.Directives
{
    public class ChunkTreeCacheTest
    {
        [Fact]
        public void GetOrAdd_ReturnsCachedEntriesOnSubsequentCalls()
        {
            // Arrange
            var path = @"Views\_ViewStart.cshtml";
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            fileProvider.AddFile(path, "test content");
            using (var chunkTreeCache = new DefaultChunkTreeCache(fileProvider))
            {
                var expected = new ChunkTree();

                // Act
                var result1 = chunkTreeCache.GetOrAdd(path, fileInfo => expected);
                var result2 = chunkTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

                // Assert
                Assert.Same(expected, result1);
                Assert.Same(expected, result2);
                mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
            }
        }

        [Fact]
        public void GetOrAdd_ReturnsNullValues_IfFileDoesNotExistInFileProvider()
        {
            // Arrange
            var path = @"Views\_ViewStart.cshtml";
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            using (var chunkTreeCache = new DefaultChunkTreeCache(fileProvider))
            {
                var expected = new ChunkTree();

                // Act
                var result1 = chunkTreeCache.GetOrAdd(path, fileInfo => expected);
                var result2 = chunkTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

                // Assert
                Assert.Null(result1);
                Assert.Null(result2);
                mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
            }
        }

        [Fact]
        public void GetOrAdd_UpdatesCache_IfFileExpirationTriggerExpires()
        {
            // Arrange
            var path = @"Views\Home\_ViewStart.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "test content");
            using (var chunkTreeCache = new DefaultChunkTreeCache(fileProvider))
            {
                var expected1 = new ChunkTree();
                var expected2 = new ChunkTree();

                // Act 1
                var result1 = chunkTreeCache.GetOrAdd(path, fileInfo => expected1);

                // Assert 1
                Assert.Same(expected1, result1);

                // Act 2
                fileProvider.GetChangeToken(path).HasChanged = true;
                var result2 = chunkTreeCache.GetOrAdd(path, fileInfo => expected2);

                // Assert 2
                Assert.Same(expected2, result2);
            }
        }

        [Fact]
        public void GetOrAdd_UpdatesCacheWithNullValue_IfFileWasDeleted()
        {
            // Arrange
            var path = @"Views\Home\_ViewStart.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "test content");
            using (var chunkTreeCache = new DefaultChunkTreeCache(fileProvider))
            {
                var expected1 = new ChunkTree();

                // Act 1
                var result1 = chunkTreeCache.GetOrAdd(path, fileInfo => expected1);

                // Assert 1
                Assert.Same(expected1, result1);

                // Act 2
                fileProvider.DeleteFile(path);
                fileProvider.GetChangeToken(path).HasChanged = true;
                var result2 = chunkTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

                // Assert 2
                Assert.Null(result2);
            }
        }

        [Fact]
        public void GetOrAdd_UpdatesCacheWithValue_IfFileWasAdded()
        {
            // Arrange
            var path = @"Views\Home\_ViewStart.cshtml";
            var fileProvider = new TestFileProvider();
            using (var chunkTreeCache = new DefaultChunkTreeCache(fileProvider))
            {
                var expected = new ChunkTree();

                // Act 1
                var result1 = chunkTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

                // Assert 1
                Assert.Null(result1);

                // Act 2
                fileProvider.AddFile(path, "test content");
                fileProvider.GetChangeToken(path).HasChanged = true;
                var result2 = chunkTreeCache.GetOrAdd(path, fileInfo => expected);

                // Assert 2
                Assert.Same(expected, result2);
            }
        }

        [Fact]
        public void GetOrAdd_ExpiresEntriesAfterOneMinute()
        {
            // Arrange
            var path = @"Views\Home\_ViewStart.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "some content");
            var clock = new Mock<ISystemClock>();
            var utcNow = DateTimeOffset.UtcNow;
            clock.SetupGet(c => c.UtcNow)
                 .Returns(() => utcNow);
            var options = new MemoryCacheOptions { Clock = clock.Object };
            using (var chunkTreeCache = new DefaultChunkTreeCache(fileProvider, options))
            {
                var chunkTree1 = new ChunkTree();
                var chunkTree2 = new ChunkTree();

                // Act 1
                var result1 = chunkTreeCache.GetOrAdd(path, fileInfo => chunkTree1);

                // Assert 1
                Assert.Same(chunkTree1, result1);

                // Act 2
                utcNow = utcNow.AddSeconds(59);
                var result2 = chunkTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

                // Assert 2
                Assert.Same(chunkTree1, result2);

                // Act 3
                utcNow = utcNow.AddSeconds(65);
                var result3 = chunkTreeCache.GetOrAdd(path, fileInfo => chunkTree2);

                // Assert 3
                Assert.Same(chunkTree2, result3);
            }
        }
    }
}