// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Internal
{
    public class FileVersionProviderTest
    {
        [Theory]
        [InlineData("/hello/world", "/hello/world?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk")]
        [InlineData("/hello/world?q=test", "/hello/world?q=test&v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk")]
        [InlineData("/hello/world?q=foo&bar", "/hello/world?q=foo&bar&v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk")]
        [InlineData("/hello/world?q=foo&bar#abc", "/hello/world?q=foo&bar&v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk#abc")]
        [InlineData("/hello/world#somefragment", "/hello/world?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk#somefragment")]
        public void AddFileVersionToPath_WhenCacheIsAbsent(string filePath, string expected)
        {
            // Arrange
            var fileProvider = GetMockFileProvider(filePath);
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase());

            // Act
            var result = fileVersionProvider.AddFileVersionToPath(filePath);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void AddFileVersionToPath_CachesNotFoundResults()
        {
            // Arrange
            var path = "/wwwroot/file.txt";
            var fileProvider = GetMockFileProvider(
                path,
                pathStartsWithAppName: false,
                fileDoesNotExist: true);
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase());
            var mockFileProvider = Mock.Get(fileProvider);

            // Act 1
            var result = fileVersionProvider.AddFileVersionToPath(path);

            // Assert 1
            Assert.Equal(path, result);
            mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
            mockFileProvider.Verify(f => f.Watch(It.IsAny<string>()), Times.Once());

            // Act 2
            result = fileVersionProvider.AddFileVersionToPath(path);

            // Assert 2
            Assert.Equal(path, result);
            mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
            mockFileProvider.Verify(f => f.Watch(It.IsAny<string>()), Times.Once());
        }

        [Theory]
        [InlineData("file.txt", false)]
        [InlineData("/wwwroot/file.txt", true)]
        public void AddFileVersionToPath_CachesFoundResults(string path, bool pathStartsWithAppName)
        {
            // Arrange
            var fileProvider = GetMockFileProvider(
                "file.txt",
                pathStartsWithAppName);
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase());
            var mockFileProvider = Mock.Get(fileProvider);

            // Act 1
            var result = fileVersionProvider.AddFileVersionToPath(path);

            // Assert 1
            Assert.Equal($"{path}?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
            mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
            mockFileProvider.Verify(f => f.Watch(It.IsAny<string>()), Times.Once());

            // Act 2
            result = fileVersionProvider.AddFileVersionToPath(path);

            // Assert 2
            Assert.Equal($"{path}?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
            mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
            mockFileProvider.Verify(f => f.Watch(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public void AddFileVersionToPath_UpdatesEntryWhenCacheExpires_ForNonExistingFile()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase());

            // Act 1 - File does not exist
            var result = fileVersionProvider.AddFileVersionToPath("file.txt");

            // Assert 1
            Assert.Equal("file.txt", result);

            // Act 2 - File gets added
            fileProvider.AddFile("file.txt", "Hello World!");
            fileProvider.GetChangeToken("file.txt").HasChanged = true;
            result = fileVersionProvider.AddFileVersionToPath("file.txt");

            // Assert 2
            Assert.Equal("file.txt?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
        }

        [Fact]
        public void AddFileVersionToPath_UpdatesEntryWhenCacheExpires_ForExistingFile()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase());
            fileProvider.AddFile("file.txt", "Hello World!");

            // Act 1 - File exists
            var result = fileVersionProvider.AddFileVersionToPath("file.txt");

            // Assert 1
            Assert.Equal("file.txt?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);

            // Act 2
            fileProvider.DeleteFile("file.txt");
            fileProvider.GetChangeToken("file.txt").HasChanged = true;
            result = fileVersionProvider.AddFileVersionToPath("file.txt");

            // Assert 2
            Assert.Equal("file.txt", result);
        }

        [Fact]
        public void AddFileVersionToPath_UpdatesEntryWhenCacheExpires_ForExistingFile_WithRequestPathBase()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase("/wwwroot/"));
            fileProvider.AddFile("file.txt", "Hello World!");

            // Act 1 - File exists
            var result = fileVersionProvider.AddFileVersionToPath("/wwwroot/file.txt");

            // Assert 1
            Assert.Equal("/wwwroot/file.txt?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);

            // Act 2
            fileProvider.DeleteFile("file.txt");
            fileProvider.GetChangeToken("file.txt").HasChanged = true;
            result = fileVersionProvider.AddFileVersionToPath("/wwwroot/file.txt");

            // Assert 2
            Assert.Equal("/wwwroot/file.txt", result);
        }

        // Verifies if the stream is closed after reading.
        [Fact]
        public void AddFileVersionToPath_DoesNotLockFileAfterReading()
        {
            // Arrange
            var stream = new TestableMemoryStream(Encoding.UTF8.GetBytes("Hello World!"));
            var mockFile = new Mock<IFileInfo>();
            mockFile.SetupGet(f => f.Exists).Returns(true);
            mockFile
                .Setup(m => m.CreateReadStream())
                .Returns(stream);

            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/hello/world", mockFile.Object);

            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase());

            // Act
            var result = fileVersionProvider.AddFileVersionToPath("/hello/world");

            // Assert
            Assert.True(stream.Disposed);
        }

        [Theory]
        [InlineData("/testApp/hello/world", true, "/testApp")]
        [InlineData("/testApp/foo/bar/hello/world", true, "/testApp/foo/bar")]
        [InlineData("/test/testApp/hello/world", false, "/testApp")]
        public void AddFileVersionToPath_PathContainingAppName(
            string filePath,
            bool pathStartsWithAppBase,
            string requestPathBase)
        {
            // Arrange
            var fileProvider = GetMockFileProvider(filePath, pathStartsWithAppBase);
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase(requestPathBase));

            // Act
            var result = fileVersionProvider.AddFileVersionToPath(filePath);

            // Assert
            Assert.Equal(filePath + "?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
        }

        [Fact]
        public void DoesNotAddVersion_IfFileNotFound()
        {
            // Arrange
            var filePath = "http://contoso.com/hello/world";
            var fileProvider = GetMockFileProvider(filePath, false, true);
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                new MemoryCache(new MemoryCacheOptions()),
                GetRequestPathBase());

            // Act
            var result = fileVersionProvider.AddFileVersionToPath(filePath);

            // Assert
            Assert.Equal("http://contoso.com/hello/world", result);
        }

        [Fact]
        public void ReturnsValueFromCache()
        {
            // Arrange
            var filePath = "/hello/world";
            var fileProvider = GetMockFileProvider(filePath);
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            memoryCache.Set(filePath, "FromCache");
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                memoryCache,
                GetRequestPathBase());

            // Act
            var result = fileVersionProvider.AddFileVersionToPath(filePath);

            // Assert
            Assert.Equal("FromCache", result);
        }

        [Theory]
        [InlineData("/hello/world", "/hello/world", null)]
        [InlineData("/testApp/hello/world", "/hello/world", "/testApp")]
        public void SetsValueInCache(string filePath, string watchPath, string requestPathBase)
        {
            // Arrange
            var changeToken = new Mock<IChangeToken>();
            var fileProvider = GetMockFileProvider(filePath, requestPathBase != null);
            Mock.Get(fileProvider)
                .Setup(f => f.Watch(watchPath)).Returns(changeToken.Object);

            object cacheValue = null;
            var value = new Mock<ICacheEntry>();
            value.Setup(c => c.Value).Returns(cacheValue);
            value.Setup(c => c.ExpirationTokens).Returns(new List<IChangeToken>());
            var cache = new Mock<IMemoryCache>();
            cache.CallBase = true;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cacheValue))
                .Returns(cacheValue != null);
            cache.Setup(c => c.CreateEntry(
                /*key*/ filePath))
                .Returns((object key) => value.Object)
                .Verifiable();
            var fileVersionProvider = new FileVersionProvider(
                fileProvider,
                cache.Object,
                GetRequestPathBase(requestPathBase));

            // Act
            var result = fileVersionProvider.AddFileVersionToPath(filePath);

            // Assert
            Assert.Equal(filePath + "?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
            cache.VerifyAll();
        }

        private static IFileProvider GetMockFileProvider(
            string filePath,
            bool pathStartsWithAppName = false,
            bool fileDoesNotExist = false)
        {
            var existingMockFile = new Mock<IFileInfo>();
            existingMockFile.SetupGet(f => f.Exists).Returns(true);
            existingMockFile
                .Setup(m => m.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Hello World!")));

            var doesNotExistMockFile = new Mock<IFileInfo>();
            doesNotExistMockFile.SetupGet(f => f.Exists).Returns(false);

            var mockFileProvider = new Mock<IFileProvider>();
            if (pathStartsWithAppName)
            {
                mockFileProvider.Setup(fp => fp.GetFileInfo(filePath)).Returns(doesNotExistMockFile.Object);
                mockFileProvider.Setup(fp => fp.GetFileInfo(It.Is<string>(str => str != filePath)))
                    .Returns(existingMockFile.Object);
            }
            else
            {
                mockFileProvider.Setup(fp => fp.GetFileInfo(It.IsAny<string>()))
                    .Returns(fileDoesNotExist ? doesNotExistMockFile.Object : existingMockFile.Object);
            }

            mockFileProvider.Setup(fp => fp.Watch(It.IsAny<string>()))
                .Returns(new TestFileChangeToken());

            return mockFileProvider.Object;
        }

        private static PathString GetRequestPathBase(string requestPathBase = null)
        {
            return new PathString(requestPathBase);
        }

        private class TestableMemoryStream : MemoryStream
        {
            public TestableMemoryStream(byte[] buffer)
                : base(buffer)
            {
            }

            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Disposed = true;
            }
        }
    }
}
