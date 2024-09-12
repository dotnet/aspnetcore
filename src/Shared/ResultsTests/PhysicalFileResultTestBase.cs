// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Internal;

public abstract class PhysicalFileResultTestBase
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
    [InlineData(null, 5L, 5L)]
    [InlineData(8L, null, 26L)]
    public async Task WriteFileAsync_WritesRangeRequested(long? start, long? end, long contentLength)
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        requestHeaders.Range = new RangeHeaderValue(start, end);
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", enableRangeProcessing: true);

        // Assert
        var startResult = start ?? 34 - end;
        var endResult = startResult + contentLength - 1;
        var httpResponse = httpContext.Response;
        var contentRange = new ContentRangeHeaderValue(startResult.Value, endResult.Value, 34);
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(contentLength, httpResponse.ContentLength);
        Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
        Assert.Equal(startResult, sendFile.Offset);
        Assert.Equal((long?)contentLength, sendFile.Length);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        requestHeaders.Range = new RangeHeaderValue(0, 3);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", entityTag: entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
        Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
        var contentRange = new ContentRangeHeaderValue(0, 3, 34);
        Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
        Assert.Equal(4, httpResponse.ContentLength);
        Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
        Assert.Equal(0, sendFile.Offset);
        Assert.Equal(4, sendFile.Length);
    }

    [Fact]
    public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        requestHeaders.Range = new RangeHeaderValue(0, 3);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", entityTag: entityTag);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
        Assert.Equal(0, sendFile.Offset);
        Assert.Null(sendFile.Length);
    }

    [Fact]
    public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var entityTag = new EntityTagHeaderValue("\"Etag\"");
        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        requestHeaders.Range = new RangeHeaderValue(0, 3);
        requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", entityTag: entityTag, enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
        Assert.Equal(0, sendFile.Offset);
        Assert.Null(sendFile.Length);
    }

    [Theory]
    [InlineData("0-5")]
    [InlineData("bytes = ")]
    [InlineData("bytes = 1-4, 5-11")]
    public async Task WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(string rangeString)
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        httpContext.Request.Headers.Range = rangeString;
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, "text/plain");

        // Assert
        var httpResponse = httpContext.Response;
        Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
        Assert.Equal(0, httpResponse.Headers.ContentRange.Count);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
        Assert.Equal(0, sendFile.Offset);
        Assert.Null(sendFile.Length);
    }

    [Theory]
    [InlineData("bytes = 35-36")]
    [InlineData("bytes = -0")]
    public async Task WriteFileAsync_RangeRequestedNotSatisfiable(string rangeString)
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
        httpContext.Request.Headers.Range = rangeString;
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        var contentRange = new ContentRangeHeaderValue(34);
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
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue;
        httpContext.Request.Headers.Range = "bytes = 0-6";
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(StatusCodes.Status412PreconditionFailed, httpResponse.StatusCode);
        Assert.Null(httpResponse.ContentLength);
        Assert.Equal(0, httpResponse.Headers.ContentRange.Count);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.Empty(body);
    }

    [Fact]
    public async Task WriteFileAsync_RangeRequested_NotModified()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var httpContext = GetHttpContext();
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(1);
        httpContext.Request.Headers.Range = "bytes = 0-6";
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", enableRangeProcessing: true);

        // Assert
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
        Assert.Null(httpResponse.ContentLength);
        Assert.Equal(0, httpResponse.Headers.ContentRange.Count);
        Assert.NotEqual(0, httpResponse.Headers.LastModified.Count);
        Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
        Assert.Empty(body);
    }

    [Fact]
    public async Task ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var sendFileMock = new Mock<IHttpResponseBodyFeature>();
        sendFileMock
            .Setup(s => s.SendFileAsync(path, 0, null, CancellationToken.None))
            .Returns(Task.FromResult<int>(0));

        var httpContext = GetHttpContext();
        httpContext.Features.Set(sendFileMock.Object);

        // Act
        await ExecuteAsync(httpContext, path, "text/plain");

        // Assert
        sendFileMock.Verify();
    }

    [Theory]
    [InlineData(0L, 3L, 4L)]
    [InlineData(8L, 13L, 6L)]
    [InlineData(null, 3L, 3L)]
    [InlineData(8L, null, 26L)]
    public async Task ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(long? start, long? end, long contentLength)
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
        var requestHeaders = httpContext.Request.GetTypedHeaders();
        requestHeaders.Range = new RangeHeaderValue(start, end);
        requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
        httpContext.Request.Method = HttpMethods.Get;

        // Act
        await ExecuteAsync(httpContext, path, "text/plain", enableRangeProcessing: true);

        // Assert
        start ??= 34 - end;
        end = start + contentLength - 1;
        var httpResponse = httpContext.Response;
        Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
        Assert.Equal(start, sendFile.Offset);
        Assert.Equal(contentLength, sendFile.Length);
        Assert.Equal(CancellationToken.None, sendFile.Token);
        var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, 34);
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
        var path = Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile_ASCII.txt"));
        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);

        // Act
        await ExecuteAsync(httpContext, path, expectedContentType);

        // Assert
        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile_ASCII.txt")), sendFile.Name);
        Assert.Equal(0, sendFile.Offset);
        Assert.Null(sendFile.Length);
        Assert.Equal(CancellationToken.None, sendFile.Token);
    }

    [Fact]
    public async Task ExecuteResultAsync_WorksWithAbsolutePaths()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile.txt"));

        var sendFile = new TestSendFileFeature();
        var httpContext = GetHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);

        // Act
        await ExecuteAsync(httpContext, path, "text/plain");

        // Assert
        Assert.Equal(Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
        Assert.Equal(0, sendFile.Offset);
        Assert.Null(sendFile.Length);
        Assert.Equal(CancellationToken.None, sendFile.Token);
    }

    [Theory]
    [InlineData("FilePathResultTestFile.txt")]
    [InlineData("./FilePathResultTestFile.txt")]
    [InlineData(".\\FilePathResultTestFile.txt")]
    [InlineData("~/FilePathResultTestFile.txt")]
    [InlineData("..\\TestFiles/FilePathResultTestFile.txt")]
    [InlineData("..\\TestFiles\\FilePathResultTestFile.txt")]
    [InlineData("..\\TestFiles/SubFolder/SubFolderTestFile.txt")]
    [InlineData("..\\TestFiles\\SubFolder\\SubFolderTestFile.txt")]
    [InlineData("..\\TestFiles/SubFolder\\SubFolderTestFile.txt")]
    [InlineData("..\\TestFiles\\SubFolder/SubFolderTestFile.txt")]
    [InlineData("~/SubFolder/SubFolderTestFile.txt")]
    [InlineData("~/SubFolder\\SubFolderTestFile.txt")]
    public async Task ExecuteAsync_ThrowsNotSupported_ForNonRootedPaths(string path)
    {
        // Arrange
        var expectedMessage = $"Path '{path}' was not rooted.";
        var httpContext = GetHttpContext();

        // Act
        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => ExecuteAsync(httpContext, path, "text/plain"));

        // Assert
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory(Skip = "Throws NotSupportedException instead of DirectoryNotFoundException")]
    [InlineData("/SubFolder/SubFolderTestFile.txt")]
    [InlineData("\\SubFolder\\SubFolderTestFile.txt")]
    [InlineData("/SubFolder\\SubFolderTestFile.txt")]
    [InlineData("\\SubFolder/SubFolderTestFile.txt")]
    [InlineData("./SubFolder/SubFolderTestFile.txt")]
    [InlineData(".\\SubFolder\\SubFolderTestFile.txt")]
    [InlineData("./SubFolder\\SubFolderTestFile.txt")]
    [InlineData(".\\SubFolder/SubFolderTestFile.txt")]
    public async Task ExecuteAsync_ThrowsDirectoryNotFound_IfItCanNotFindTheDirectory_ForRootPaths(string path)
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => ExecuteAsync(httpContext, path, "text/plain"));
    }

    [Theory(Skip = "Throws NotSupportedException instead of FileNotFoundException")]
    [InlineData("/FilePathResultTestFile.txt")]
    [InlineData("\\FilePathResultTestFile.txt")]
    public async Task ExecuteAsync_ThrowsFileNotFound_WhenFileDoesNotExist_ForRootPaths(string path)
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => ExecuteAsync(httpContext, path, "text/plain"));
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
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }
}
