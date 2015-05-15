// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Caching;
using Microsoft.Framework.Caching.Memory;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    public class FileVersionProviderTest
    {
        [Theory]
        [InlineData("/hello/world", "/hello/world?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk")]
        [InlineData("/hello/world?q=test", "/hello/world?q=test&v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk")]
        [InlineData("/hello/world?q=foo&bar", "/hello/world?q=foo&bar&v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk")]
        public void AddsVersionToFiles_WhenCacheIsAbsent(string filePath, string expected)
        {
            // Arrange
            var hostingEnvironment = GetMockHostingEnvironment(filePath);
            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.WebRootFileProvider,
                GetMockCache(),
                GetRequestPathBase());

            // Act
            var result = fileVersionProvider.AddFileVersionToPath(filePath);

            // Assert
            Assert.Equal(expected, result);
        }

        // Verifies if the stream is closed after reading.
        [Fact]
        public void AddsVersionToFiles_DoesNotLockFileAfterReading()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hello World!"));
            var mockFile = new Mock<IFileInfo>();
            mockFile.SetupGet(f => f.Exists).Returns(true);
            mockFile
                .Setup(m => m.CreateReadStream())
                .Returns(stream);

            var mockFileProvider = new Mock<IFileProvider>();
            mockFileProvider.Setup(fp => fp.GetFileInfo(It.IsAny<string>()))
                .Returns(mockFile.Object);

            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.Setup(h => h.WebRootFileProvider).Returns(mockFileProvider.Object);

            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.Object.WebRootFileProvider,
                GetMockCache(),
                GetRequestPathBase());

            // Act
            var result = fileVersionProvider.AddFileVersionToPath("/hello/world");

            // Assert
            Assert.False(stream.CanRead);
            Assert.Throws<ObjectDisposedException>(() => fileVersionProvider.AddFileVersionToPath("/hello/world"));
        }

        [Theory]
        [InlineData("/testApp/hello/world", true, "/testApp")]
        [InlineData("/testApp/foo/bar/hello/world", true, "/testApp/foo/bar")]
        [InlineData("/test/testApp/hello/world", false, "/testApp")]
        public void AddsVersionToFiles_PathContainingAppName(
            string filePath,
            bool pathStartsWithAppBase,
            string requestPathBase)
        {
            // Arrange
            var hostingEnvironment = GetMockHostingEnvironment(filePath, pathStartsWithAppBase);
            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.WebRootFileProvider,
                GetMockCache(),
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
            var hostingEnvironment = GetMockHostingEnvironment(filePath, false, true);
            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.WebRootFileProvider,
                GetMockCache(),
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
            var hostingEnvironment = GetMockHostingEnvironment(filePath);
            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.WebRootFileProvider,
                GetMockCache("FromCache"),
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
            var trigger = new Mock<IExpirationTrigger>();
            var hostingEnvironment = GetMockHostingEnvironment(filePath, requestPathBase != null);
            Mock.Get(hostingEnvironment.WebRootFileProvider)
                .Setup(f => f.Watch(watchPath)).Returns(trigger.Object);

            object cacheValue = null;
            var cache = new Mock<IMemoryCache>();
            cache.CallBase = true;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cacheValue))
                .Returns(cacheValue != null);
            cache.Setup(c => c.Set(
                /*key*/ filePath,
                /*value*/ It.IsAny<object>(),
                /*options*/ It.IsAny<MemoryCacheEntryOptions>()))
                .Returns(new object())
                .Verifiable();
            var fileVersionProvider = new FileVersionProvider(
                hostingEnvironment.WebRootFileProvider,
                cache.Object,
                GetRequestPathBase(requestPathBase));

            // Act
            var result = fileVersionProvider.AddFileVersionToPath(filePath);

            // Assert
            Assert.Equal(filePath + "?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
            cache.VerifyAll();
        }

        private IHostingEnvironment GetMockHostingEnvironment(
            string filePath,
            bool pathStartsWithAppName = false,
            bool fileDoesNotExist = false)
        {
            var existingMockFile = new Mock<IFileInfo>();
            existingMockFile.SetupGet(f => f.Exists).Returns(true);
            existingMockFile
                .Setup(m => m.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Hello World!")));

            var nonExistingMockFile = new Mock<IFileInfo>();
            nonExistingMockFile.SetupGet(f => f.Exists).Returns(false);
            nonExistingMockFile
                .Setup(m => m.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Hello World!")));

            var mockFileProvider = new Mock<IFileProvider>();
            if (pathStartsWithAppName)
            {
                mockFileProvider.Setup(fp => fp.GetFileInfo(filePath)).Returns(nonExistingMockFile.Object);
                mockFileProvider.Setup(fp => fp.GetFileInfo(It.Is<string>(str => str != filePath)))
                    .Returns(existingMockFile.Object);
            }
            else
            {
                mockFileProvider.Setup(fp => fp.GetFileInfo(It.IsAny<string>()))
                    .Returns(fileDoesNotExist? nonExistingMockFile.Object : existingMockFile.Object);
            }

            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.Setup(h => h.WebRootFileProvider).Returns(mockFileProvider.Object);

            return hostingEnvironment.Object;
        }

        private static IMemoryCache GetMockCache(object result = null)
        {
            var cache = new Mock<IMemoryCache>();
            cache.CallBase = true;
                cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out result))
                    .Returns(result != null);

            cache
                .Setup(
                    c => c.Set(
                        /*key*/ It.IsAny<string>(),
                        /*value*/ It.IsAny<object>(),
                        /*options*/ It.IsAny<MemoryCacheEntryOptions>()))
                .Returns(new object());
            return cache.Object;
        }

        private static PathString GetRequestPathBase(string requestPathBase = null)
        {
            return new PathString(requestPathBase);
        }
    }
}