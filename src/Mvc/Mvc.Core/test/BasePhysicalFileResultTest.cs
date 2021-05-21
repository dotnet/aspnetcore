// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class BasePhysicalFileResultTest
    {
        public static async Task WriteFileAsync_WritesRangeRequested(
            long? start,
            long? end,
            string expectedString,
            long contentLength,
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            result.EnableRangeProcessing = true;
            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            requestHeaders.Range = new RangeHeaderValue(start, end);
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var startResult = start ?? 34 - end;
            var endResult = startResult + contentLength - 1;
            var httpResponse = actionContext.HttpContext.Response;
            var contentRange = new ContentRangeHeaderValue(startResult.Value, endResult.Value, 34);
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Equal(contentLength, httpResponse.ContentLength);
            Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
            Assert.Equal(startResult, sendFile.Offset);
            Assert.Equal((long?)contentLength, sendFile.Length);
        }

        public static async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            var entityTag = result.EntityTag = new EntityTagHeaderValue("\"Etag\"");
            result.EnableRangeProcessing = true;
            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            requestHeaders.Range = new RangeHeaderValue(0, 3);
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            var contentRange = new ContentRangeHeaderValue(0, 3, 34);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal(4, httpResponse.ContentLength);
            Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
            Assert.Equal(0, sendFile.Offset);
            Assert.Equal(4, sendFile.Length);
        }

        public static async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            var entityTag = result.EntityTag = new EntityTagHeaderValue("\"Etag\"");
            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            requestHeaders.Range = new RangeHeaderValue(0, 3);
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
            Assert.Equal(0, sendFile.Offset);
            Assert.Null(sendFile.Length);
        }

        public static async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            result.EnableRangeProcessing = true;
            var entityTag = result.EntityTag = new EntityTagHeaderValue("\"Etag\"");
            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            requestHeaders.Range = new RangeHeaderValue(0, 3);
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
            Assert.Equal(0, sendFile.Offset);
            Assert.Null(sendFile.Length);
        }

        public static async Task WriteFileAsync_RangeHeaderMalformed_RangeRequestIgnored(
            string rangeString,
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            httpContext.Request.Headers.Range = rangeString;
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Empty(httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
            Assert.Equal(0, sendFile.Offset);
            Assert.Null(sendFile.Length);
        }

        public static async Task WriteFileAsync_RangeRequestedNotSatisfiable(
            string rangeString,
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            result.EnableRangeProcessing = true;
            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            httpContext.Request.Headers.Range = rangeString;
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            var contentRange = new ContentRangeHeaderValue(34);
            Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Equal(0, httpResponse.ContentLength);
            Assert.Empty(body);
        }

        public static async Task WriteFileAsync_RangeRequested_PreconditionFailed(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            result.EnableRangeProcessing = true;
            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue;
            httpContext.Request.Headers.Range = "bytes = 0-6";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(StatusCodes.Status412PreconditionFailed, httpResponse.StatusCode);
            Assert.Null(httpResponse.ContentLength);
            Assert.Empty(httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Empty(body);
        }

        public static async Task WriteFileAsync_RangeRequested_NotModified(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            result.EnableRangeProcessing = true;
            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(1);
            httpContext.Request.Headers.Range = "bytes = 0-6";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
            Assert.Null(httpResponse.ContentLength);
            Assert.Empty(httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
            Assert.Empty(body);
        }

        public static async Task ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            var sendFileMock = new Mock<IHttpResponseBodyFeature>();
            sendFileMock
                .Setup(s => s.SendFileAsync(path, 0, null, CancellationToken.None))
                .Returns(Task.FromResult<int>(0));

            var httpContext = GetHttpContext();
            httpContext.Features.Set(sendFileMock.Object);
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? context : httpContext);

            // Assert
            sendFileMock.Verify();
        }

        public static async Task ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(
            long? start,
            long? end,
            long contentLength,
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");
            result.EnableRangeProcessing = true;
            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.Range = new RangeHeaderValue(start, end);
            requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            start = start ?? 34 - end;
            end = start + contentLength - 1;
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
            Assert.Equal(start, sendFile.Offset);
            Assert.Equal(contentLength, sendFile.Length);
            Assert.Equal(CancellationToken.None, sendFile.Token);
            var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, 34);
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Equal(contentLength, httpResponse.ContentLength);
        }

        public static async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            var path = Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile_ASCII.txt"));
            var result = new TestPhysicalFileResult(path, expectedContentType);
            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? context : httpContext);

            // Assert
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
            Assert.Equal(Path.GetFullPath(Path.Combine("TestFiles", "FilePathResultTestFile_ASCII.txt")), sendFile.Name);
            Assert.Equal(0, sendFile.Offset);
            Assert.Null(sendFile.Length);
            Assert.Equal(CancellationToken.None, sendFile.Token);
        }

        public static async Task ExecuteResultAsync_WorksWithAbsolutePaths(
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var path = Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile.txt"));
            var result = new TestPhysicalFileResult(path, "text/plain");

            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await function(result, action == "ActionContext" ? context : httpContext);

            // Assert
            Assert.Equal(Path.GetFullPath(Path.Combine(".", "TestFiles", "FilePathResultTestFile.txt")), sendFile.Name);
            Assert.Equal(0, sendFile.Offset);
            Assert.Null(sendFile.Length);
            Assert.Equal(CancellationToken.None, sendFile.Token);
        }

        public static async Task ExecuteAsync_ThrowsNotSupported_ForNonRootedPaths(
            string path,
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var result = new TestPhysicalFileResult(path, "text/plain");
            var context = new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());
            var expectedMessage = $"Path '{path}' was not rooted.";

            // Act
            var ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => function(result, action == "ActionContext" ? context : context.HttpContext));

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        public static void ExecuteAsync_ThrowsDirectoryNotFound_IfItCanNotFindTheDirectory_ForRootPaths(
            string path,
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var result = new TestPhysicalFileResult(path, "text/plain");
            var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

            // Act & Assert
            Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => function(result, action == "ActionContext" ? context : context.HttpContext));
        }

        public static void ExecuteAsync_ThrowsFileNotFound_WhenFileDoesNotExist_ForRootPaths(
            string path,
            string action,
            Func<PhysicalFileResult, object, Task> function)
        {
            // Arrange
            var result = new TestPhysicalFileResult(path, "text/plain");
            var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

            // Act & Assert
            Assert.ThrowsAsync<FileNotFoundException>(
                () => function(result, action == "ActionContext" ? context : context.HttpContext));
        }

        private class TestPhysicalFileResult : PhysicalFileResult, IResult
        {
            public TestPhysicalFileResult(string filePath, string contentType)
                : base(filePath, contentType)
            {
            }

            public override Task ExecuteResultAsync(ActionContext context)
            {
                var executor = context.HttpContext.RequestServices.GetRequiredService<TestPhysicalFileResultExecutor>();
                return executor.ExecuteAsync(context, this);
            }

            Task IResult.ExecuteAsync(HttpContext httpContext)
            {
                var lastModified = DateTimeOffset.MinValue.AddDays(1);
                var fileLastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
                return ExecuteAsyncInternal(httpContext, this, fileLastModified, 34);
            }
        }

        private class TestPhysicalFileResultExecutor : PhysicalFileResultExecutor
        {
            public TestPhysicalFileResultExecutor(ILoggerFactory loggerFactory)
                : base(loggerFactory)
            {
            }

            protected override FileMetadata GetFileInfo(string path)
            {
                var lastModified = DateTimeOffset.MinValue.AddDays(1);
                return new FileMetadata
                {
                    Exists = true,
                    Length = 34,
                    LastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0))
                };
            }
        }

        private class TestSendFileFeature : IHttpResponseBodyFeature
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
            services.AddSingleton<TestPhysicalFileResultExecutor>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
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
}
