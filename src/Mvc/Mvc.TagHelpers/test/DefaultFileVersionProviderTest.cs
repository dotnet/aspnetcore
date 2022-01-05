// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class DefaultFileVersionProviderTest
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
        var fileVersionProvider = GetFileVersionProvider(fileProvider);
        var requestPath = GetRequestPathBase();

        // Act
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, filePath);

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
        var fileVersionProvider = GetFileVersionProvider(fileProvider);
        var mockFileProvider = Mock.Get(fileProvider);
        var requestPath = GetRequestPathBase();

        // Act 1
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, path);

        // Assert 1
        Assert.Equal(path, result);
        mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
        mockFileProvider.Verify(f => f.Watch(It.IsAny<string>()), Times.Once());

        // Act 2
        result = fileVersionProvider.AddFileVersionToPath(requestPath, path);

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
        var fileVersionProvider = GetFileVersionProvider(fileProvider);
        var mockFileProvider = Mock.Get(fileProvider);
        var requestPath = GetRequestPathBase();

        // Act 1
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, path);

        // Assert 1
        Assert.Equal($"{path}?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
        mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
        mockFileProvider.Verify(f => f.Watch(It.IsAny<string>()), Times.Once());

        // Act 2
        result = fileVersionProvider.AddFileVersionToPath(requestPath, path);

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
        var fileVersionProvider = GetFileVersionProvider(fileProvider);
        var requestPath = GetRequestPathBase();

        // Act 1 - File does not exist
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, "file.txt");

        // Assert 1
        Assert.Equal("file.txt", result);

        // Act 2 - File gets added
        fileProvider.AddFile("file.txt", "Hello World!");
        fileProvider.GetChangeToken("file.txt").HasChanged = true;
        result = fileVersionProvider.AddFileVersionToPath(requestPath, "file.txt");

        // Assert 2
        Assert.Equal("file.txt?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
    }

    [Fact]
    public void AddFileVersionToPath_UpdatesEntryWhenCacheExpires_ForExistingFile()
    {
        // Arrange
        var fileProvider = new TestFileProvider();
        var requestPath = GetRequestPathBase();
        var fileVersionProvider = GetFileVersionProvider(fileProvider);
        fileProvider.AddFile("file.txt", "Hello World!");

        // Act 1 - File exists
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, "file.txt");

        // Assert 1
        Assert.Equal("file.txt?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);

        // Act 2
        fileProvider.DeleteFile("file.txt");
        fileProvider.GetChangeToken("file.txt").HasChanged = true;
        result = fileVersionProvider.AddFileVersionToPath(requestPath, "file.txt");

        // Assert 2
        Assert.Equal("file.txt", result);
    }

    [Fact]
    public void AddFileVersionToPath_UpdatesEntryWhenCacheExpires_ForExistingFile_WithRequestPathBase()
    {
        // Arrange
        var fileProvider = new TestFileProvider();
        var requestPath = GetRequestPathBase("/wwwroot/");
        var fileVersionProvider = GetFileVersionProvider(fileProvider);
        fileProvider.AddFile("file.txt", "Hello World!");

        // Act 1 - File exists
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, "/wwwroot/file.txt");

        // Assert 1
        Assert.Equal("/wwwroot/file.txt?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);

        // Act 2
        fileProvider.DeleteFile("file.txt");
        fileProvider.GetChangeToken("file.txt").HasChanged = true;
        result = fileVersionProvider.AddFileVersionToPath(requestPath, "/wwwroot/file.txt");

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
        var requestPath = GetRequestPathBase();
        var fileVersionProvider = GetFileVersionProvider(fileProvider);

        // Act
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, "/hello/world");

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
        var requestPath = GetRequestPathBase(requestPathBase);
        var fileVersionProvider = GetFileVersionProvider(fileProvider);

        // Act
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, filePath);

        // Assert
        Assert.Equal(filePath + "?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", result);
    }

    [Fact]
    public void DoesNotAddVersion_IfFileNotFound()
    {
        // Arrange
        var filePath = "http://contoso.com/hello/world";
        var fileProvider = GetMockFileProvider(filePath, false, true);
        var requestPath = GetRequestPathBase();
        var fileVersionProvider = GetFileVersionProvider(fileProvider);

        // Act
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, filePath);

        // Assert
        Assert.Equal("http://contoso.com/hello/world", result);
    }

    [Fact]
    public void ReturnsValueFromCache()
    {
        // Arrange
        var filePath = "/hello/world";
        var fileProvider = GetMockFileProvider(filePath);
        var fileVersionProvider = GetFileVersionProvider(fileProvider);
        var cacheEntryOptions = new MemoryCacheEntryOptions();
        cacheEntryOptions.SetSize(1);
        fileVersionProvider.Cache.Set(filePath, "FromCache", cacheEntryOptions);
        var requestPath = GetRequestPathBase();

        // Act
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, filePath);

        // Assert
        Assert.Equal("FromCache", result);
    }

    [Fact]
    public void AddFileVersionToPath_CachesEntry() => AddFileVersionToPath("/hello/world", "/hello/world", null);

    [Fact]
    public void AddFileVersionToPath_WithRequestPathBase_CachesEntry() => AddFileVersionToPath("/testApp/hello/world", "/hello/world", "/testApp");

    private static void AddFileVersionToPath(string filePath, string watchPath, string requestPathBase)
    {
        // Arrange
        var expected = filePath + "?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk";
        var expectedSize = expected.Length * sizeof(char);
        var changeToken = Mock.Of<IChangeToken>();

        var fileProvider = GetMockFileProvider(filePath, requestPathBase != null);
        Mock.Get(fileProvider)
            .Setup(f => f.Watch(watchPath)).Returns(changeToken);

        var cacheEntry = Mock.Of<ICacheEntry>(c => c.ExpirationTokens == new List<IChangeToken>());
        var cache = new Mock<IMemoryCache>();

        cache.Setup(c => c.CreateEntry(filePath))
            .Returns(cacheEntry)
            .Verifiable();

        var requestPath = GetRequestPathBase(requestPathBase);

        var fileVersionProvider = GetFileVersionProvider(fileProvider, cache.Object);

        // Act
        var result = fileVersionProvider.AddFileVersionToPath(requestPath, filePath);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expected, cacheEntry.Value);
        Assert.Equal(expectedSize, cacheEntry.Size);
        cache.VerifyAll();
    }

    private static DefaultFileVersionProvider GetFileVersionProvider(
        IFileProvider fileProvider,
        IMemoryCache memoryCache = null)
    {
        var hostingEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider);
        var cacheProvider = new TagHelperMemoryCacheProvider();
        if (memoryCache != null)
        {
            cacheProvider.Cache = memoryCache;
        }

        return new DefaultFileVersionProvider(hostingEnv, cacheProvider);
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
