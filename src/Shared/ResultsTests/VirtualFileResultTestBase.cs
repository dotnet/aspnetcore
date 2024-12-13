// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Internal;

public abstract class VirtualFileResultTestBase
{
    protected abstract Task ExecuteAsync(
        HttpContext httpContext,
        string path,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false);

    [Theory]
    [InlineData(0L, 3L, 4L)]
    [InlineData(8L, 13L, 6L)]
    [InlineData(null, 4L, 4L)]
    [InlineData(8L, null, 25L)]
    public async Task WriteFileAsync_WritesRangeRequested(
        long? start,
        long? end,
        long contentLength)
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
            .Returns(GetFileProvider(path));

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.Range = new RangeHeaderValue(start, end);
        requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, contentType, enableRangeProcessing: true);

        // Assert
        var startResult = start ?? 33 - end;
        var endResult = startResult + contentLength - 1;
        var httpResponse = httpContext.Response;
        var contentRange = new ContentRangeHeaderValue(startResult.Value, endResult.Value, 33);
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(contentLength, httpResponse.ContentLength);
        Assert.Equal(path, sendFileFeature.Name);
        Assert.Equal(startResult, sendFileFeature.Offset);
        Assert.Equal((long?)contentLength, sendFileFeature.Length);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
            .Returns(GetFileProvider(path));

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        requestHeaders.Range = new RangeHeaderValue(0, 3);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, contentType, entityTag: entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        var contentRange = new ContentRangeHeaderValue(0, 3, 33);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal(4, httpResponse.ContentLength);
        Assert.Equal(path, sendFileFeature.Name);
        Assert.Equal(0, sendFileFeature.Offset);
        Assert.Equal(4, sendFileFeature.Length);
    }

    [Fact]
    public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
            .Returns(GetFileProvider(path));

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        requestHeaders.Range = new RangeHeaderValue(0, 3);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, contentType, entityTag: entityTag);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal(path, sendFileFeature.Name);
        Assert.Equal(0, sendFileFeature.Offset);
        Assert.Null(sendFileFeature.Length);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
            .Returns(GetFileProvider(path));

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        requestHeaders.Range = new RangeHeaderValue(0, 3);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, contentType, entityTag: entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal(path, sendFileFeature.Name);
        Assert.Equal(0, sendFileFeature.Offset);
        Assert.Null(sendFileFeature.Length);
    }

    [Theory]
    [InlineData("0-5")]
    [InlineData("bytes = ")]
    [InlineData("bytes = 1-4, 5-11")]
    public async Task WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(string rangeString)
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        var requestHeaders = httpContext.Request.GetTypedHeaders();
        httpContext.Request.Headers.Range = rangeString;
        requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, contentType, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(0, httpResponse.Headers.ContentRange.Count);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(path, sendFileFeature.Name);
        Assert.Equal(0, sendFileFeature.Offset);
        Assert.Null(sendFileFeature.Length);
    }

    [Theory]
    [InlineData("bytes = 35-36")]
    [InlineData("bytes = -0")]
    public async Task WriteFileAsync_RangeRequestedNotSatisfiable(string rangeString)
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Response.Body = new MemoryStream();

        var requestHeaders = httpContext.Request.GetTypedHeaders();
        httpContext.Request.Headers.Range = rangeString;
        requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, path, contentType, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        var contentRange = new ContentRangeHeaderValue(33);
        Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(0, httpResponse.ContentLength);
        Assert.Empty(body);
    }

    [Fact]
    public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
            .Returns(GetFileProvider(path));

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue;
        httpContext.Request.Headers.Range = "bytes = 0-6";
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, contentType, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status412PreconditionFailed, httpResponse.StatusCode);
        Assert.Null(httpResponse.ContentLength);
        Assert.Equal(0, httpResponse.Headers.ContentRange.Count);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Null(sendFileFeature.Name); // Not called
    }

    [Fact]
    public async Task WriteFileAsync_RangeRequested_NotModified()
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
            .Returns(GetFileProvider(path));

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(1);
        httpContext.Request.Headers.Range = "bytes = 0-6";
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, contentType, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
        Assert.Null(httpResponse.ContentLength);
        Assert.Equal(0, httpResponse.Headers.ContentRange.Count);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
        Assert.Null(sendFileFeature.Name); // Not called
    }

    [Theory]
    [InlineData(0L, 3L, 4L)]
    [InlineData(8L, 13L, 6L)]
    [InlineData(null, 3L, 3L)]
    [InlineData(8L, null, 25L)]
    public async Task ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(long? start, long? end, long contentLength)
    {
        // Arrange
        var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");

        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
        var appEnvironment = new Mock<IWebHostEnvironment>();
        appEnvironment.Setup(app => app.WebRootFileProvider)
            .Returns(GetFileProvider(path));

        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.Range = new RangeHeaderValue(start, end);
        requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", enableRangeProcessing: true);

        // Assert
        start = start ?? 33 - end;
        end = start + contentLength - 1;
        var httpResponse = httpContext.Response;
        Assert.Equal(Path.Combine("TestFiles", "FilePathResultTestFile.txt"), sendFile.Name);
        Assert.Equal(start, sendFile.Offset);
        Assert.Equal(contentLength, sendFile.Length);
        Assert.Equal(CancellationToken.None, sendFile.Token);
        var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, 33);
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(contentLength, httpResponse.ContentLength);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
    {
        // Arrange
        var expectedContentType = "text/foo; charset=us-ascii";

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider("FilePathResultTestFile_ASCII.txt"));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        // Act
        await ExecuteAsync(httpContext, "FilePathResultTestFile_ASCII.txt", expectedContentType);

        // Assert
        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        Assert.Equal("FilePathResultTestFile_ASCII.txt", sendFileFeature.Name);
    }

    [Fact]
    public async Task ExecuteResultAsync_ReturnsFileContentsForRelativePaths()
    {
        // Arrange
        var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");

        var sendFileFeature = new TestSendFileFeature();
        var httpContext = GetHttpContext(GetFileProvider(path));
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        // Act
        await ExecuteAsync(httpContext, path, "text/plain");

        // Assert
        Assert.Equal(path, sendFileFeature.Name);
    }

    [Theory]
    [InlineData("FilePathResultTestFile.txt")]
    [InlineData("TestFiles/FilePathResultTestFile.txt")]
    [InlineData("TestFiles/../FilePathResultTestFile.txt")]
    [InlineData("TestFiles\\FilePathResultTestFile.txt")]
    [InlineData("TestFiles\\..\\FilePathResultTestFile.txt")]
    [InlineData(@"\\..//?><|""&@#\c:\..\? /..txt")]
    public async Task ExecuteResultAsync_ReturnsFiles_ForDifferentPaths(string path)
    {
        // Arrange
        var sendFileFeature = new TestSendFileFeature();
        var webRootFileProvider = GetFileProvider(path);
        var httpContext = GetHttpContext(webRootFileProvider);
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        // Act
        await ExecuteAsync(httpContext, path, "text/plain");

        // Assert
        Mock.Get(webRootFileProvider).Verify();
        Assert.Equal(path, sendFileFeature.Name);
    }

    [Theory]
    [InlineData("~/FilePathResultTestFile.txt")]
    [InlineData("~/TestFiles/FilePathResultTestFile.txt")]
    [InlineData("~/TestFiles/../FilePathResultTestFile.txt")]
    [InlineData("~/TestFiles\\..\\FilePathResultTestFile.txt")]
    [InlineData(@"~~~~\\..//?>~<|""&@#\c:\..\? /..txt~~~")]
    public async Task ExecuteResultAsync_TrimsTilde_BeforeInvokingFileProvider(string path)
    {
        // Arrange
        var expectedPath = path.Substring(1);
        var sendFileFeature = new TestSendFileFeature();
        var webRootFileProvider = GetFileProvider(expectedPath);
        var httpContext = GetHttpContext(webRootFileProvider);
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

        // Act
        await ExecuteAsync(httpContext, path, "text/plain");

        // Assert
        Mock.Get(webRootFileProvider).Verify();
        Assert.Equal(expectedPath, sendFileFeature.Name);
    }

    [Fact]
    public async Task ExecuteResultAsync_WorksWithNonDiskBasedFiles()
    {
        // Arrange
        var expectedData = "This is an embedded resource";
        var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedData));

        var nonDiskFileInfo = new Mock<IFileInfo>();
        nonDiskFileInfo.SetupGet(fi => fi.Exists).Returns(true);
        nonDiskFileInfo.SetupGet(fi => fi.PhysicalPath).Returns(() => null); // set null to indicate non-disk file
        nonDiskFileInfo.Setup(fi => fi.CreateReadStream()).Returns(sourceStream);
        var nonDiskFileProvider = new Mock<IFileProvider>();
        nonDiskFileProvider.Setup(fp => fp.GetFileInfo(It.IsAny<string>())).Returns(nonDiskFileInfo.Object);

        var httpContext = GetHttpContext(nonDiskFileProvider.Object);
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, "/SampleEmbeddedFile.txt", "text/plain");

        // Assert
        httpContext.Response.Body.Position = 0;
        var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Equal(expectedData, contents);
    }

    [Fact]
    public async Task ExecuteResultAsync_ThrowsFileNotFound_IfFileProviderCanNotFindTheFile()
    {
        // Arrange
        var path = "TestPath.txt";
        var fileInfo = new Mock<IFileInfo>();
        fileInfo.SetupGet(f => f.Exists).Returns(false);
        var fileProvider = new Mock<IFileProvider>();
        fileProvider.Setup(f => f.GetFileInfo(path)).Returns(fileInfo.Object);

        var expectedMessage = $"Could not find file: {path}.";
        var httpContext = GetHttpContext(fileProvider.Object);

        // Act
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => ExecuteAsync(httpContext, path, "text/plain"));

        // Assert
        Assert.Equal(expectedMessage, ex.Message);
        Assert.Equal(path, ex.FileName);
    }

    private static IServiceCollection CreateServices(IFileProvider webRootFileProvider)
    {
        var services = new ServiceCollection();

        var hostingEnvironment = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == webRootFileProvider);

        services.AddSingleton<IWebHostEnvironment>(hostingEnvironment);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        return services;
    }

    private static HttpContext GetHttpContext(IFileProvider webRootFileProvider)
    {
        var services = CreateServices(webRootFileProvider);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }

    protected static IFileProvider GetFileProvider(string path)
    {
        var fileInfo = new Mock<IFileInfo>();
        fileInfo.SetupGet(fi => fi.Length).Returns(33);
        fileInfo.SetupGet(fi => fi.Exists).Returns(true);
        var lastModified = DateTimeOffset.MinValue.AddDays(1);
        lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
        fileInfo.SetupGet(fi => fi.LastModified).Returns(lastModified);
        fileInfo.SetupGet(fi => fi.PhysicalPath).Returns(path);
        var fileProvider = new Mock<IFileProvider>();
        fileProvider.Setup(fp => fp.GetFileInfo(path))
            .Returns(fileInfo.Object)
            .Verifiable();

        return fileProvider.Object;
    }

    private sealed class TestSendFileFeature : IHttpResponseBodyFeature
    {
        public string Name { get; set; }
        public long Offset { get; set; }
        public long? Length { get; set; }
        public CancellationToken Token { get; set; }

        public Stream Stream => throw new NotImplementedException();

        public PipeWriter Writer => throw new NotImplementedException();

        public Task CompleteAsync()
        {
            throw new NotImplementedException();
        }

        public void DisableBuffering()
        {
            throw new NotImplementedException();
        }

        public Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            Name = path;
            Offset = offset;
            Length = length;
            Token = cancellation;

            return Task.FromResult(0);
        }

        public Task StartAsync(CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
    }
}
