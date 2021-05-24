// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    public class BaseFileStreamResultTest
    {
        public static async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested<TContext>(
            long? start,
            long? end,
            string expectedString,
            long contentLength,
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);
            readStream.SetLength(11);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.Range = new RangeHeaderValue(start, end);
            requestHeaders.IfMatch = new[]
            {
                new EntityTagHeaderValue("\"Etag\""),
            };
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            start = start ?? 11 - end;
            end = start + contentLength - 1;
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, byteArray.Length);
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.Equal(contentLength, httpResponse.ContentLength);
            Assert.Equal(expectedString, body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_IfRangeHeaderValid_WritesRequestedRange<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = DateTimeOffset.MinValue;
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);
            readStream.SetLength(11);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfMatch = new[]
            {
                new EntityTagHeaderValue("\"Etag\""),
            };
            requestHeaders.Range = new RangeHeaderValue(0, 4);
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            var contentRange = new ContentRangeHeaderValue(0, 4, byteArray.Length);
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.Equal(5, httpResponse.ContentLength);
            Assert.Equal("Hello", body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestedIgnored<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = DateTimeOffset.MinValue;
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);
            readStream.SetLength(11);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };

            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfMatch = new[]
            {
                new EntityTagHeaderValue("\"Etag\""),
            };
            requestHeaders.Range = new RangeHeaderValue(0, 4);
            requestHeaders.IfRange = new RangeConditionHeaderValue(DateTimeOffset.MinValue);
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal("Hello World", body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestedIgnored<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = DateTimeOffset.MinValue.AddDays(1);
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);
            readStream.SetLength(11);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfMatch = new[]
            {
                new EntityTagHeaderValue("\"Etag\""),
            };
            requestHeaders.Range = new RangeHeaderValue(0, 4);
            requestHeaders.IfRange = new RangeConditionHeaderValue(DateTimeOffset.MinValue);
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal("Hello World", body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored<TContext>(
            string rangeString,
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange            
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            httpContext.Request.Headers.Range = rangeString;
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Empty(httpResponse.Headers.ContentRange);
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal("Hello World", body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable<TContext>(
            string rangeString,
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange            
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            httpContext.Request.Headers.Range = rangeString;
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            var contentRange = new ContentRangeHeaderValue(byteArray.Length);
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.Equal(0, httpResponse.ContentLength);
            Assert.Empty(body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_RangeRequested_PreconditionFailed<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfMatch = new[]
            {
                new EntityTagHeaderValue("\"NotEtag\""),
            };
            httpContext.Request.Headers.Range = "bytes = 0-6";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

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
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_NotModified_RangeRequestedIgnored<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange       
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");
            var readStream = new MemoryStream(byteArray);

            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfNoneMatch = new[]
            {
                new EntityTagHeaderValue("\"Etag\""),
            };
            httpContext.Request.Headers.Range = "bytes = 0-6";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = await streamReader.ReadToEndAsync();
            Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
            Assert.Null(httpResponse.ContentLength);
            Assert.Empty(httpResponse.Headers.ContentRange);            
            Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Empty(body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_RangeRequested_FileLengthZeroOrNull<TContext>(
            long? fileLength,
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("");
            var readStream = new MemoryStream(byteArray);
            fileLength = fileLength ?? 0L;
            readStream.SetLength(fileLength.Value);
            var result = new FileStreamResult(readStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = true,
            };

            var httpContext = GetHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.Range = new RangeHeaderValue(0, 5);
            requestHeaders.IfMatch = new[]
            {
                new EntityTagHeaderValue("\"Etag\""),
            };
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            var contentRange = new ContentRangeHeaderValue(byteArray.Length);
            Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.Equal(0, httpResponse.ContentLength);
            Assert.Empty(body);
            Assert.False(readStream.CanSeek);
        }

        public static async Task WriteFileAsync_WritesResponse_InChunksOfFourKilobytes<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var mockReadStream = new Mock<Stream>();
            mockReadStream.SetupSequence(s => s.ReadAsync(It.IsAny<byte[]>(), 0, 0x1000, CancellationToken.None))
                .Returns(Task.FromResult(0x1000))
                .Returns(Task.FromResult(0x500))
                .Returns(Task.FromResult(0));

            var mockBodyStream = new Mock<Stream>();
            mockBodyStream
                .Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, 0x1000, CancellationToken.None))
                .Returns(Task.FromResult(0));

            mockBodyStream
                .Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, 0x500, CancellationToken.None))
                .Returns(Task.FromResult(0));

            var result = new FileStreamResult(mockReadStream.Object, "text/plain");

            var httpContext = GetHttpContext();
            httpContext.Response.Body = mockBodyStream.Object;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            mockReadStream.Verify();
            mockBodyStream.Verify();
        }

        public static async Task WriteFileAsync_CopiesProvidedStream_ToOutputStream<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            // Generate an array of bytes with a predictable pattern
            // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, 10, 11, 12, 13
            var originalBytes = Enumerable.Range(0, 0x1234)
                .Select(b => (byte)(b % 20)).ToArray();

            var originalStream = new MemoryStream(originalBytes);

            var httpContext = GetHttpContext();
            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileStreamResult(originalStream, "text/plain");

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var outBytes = outStream.ToArray();
            Assert.True(originalBytes.SequenceEqual(outBytes));
            Assert.False(originalStream.CanSeek);
        }

        public static async Task SetsSuppliedContentTypeAndEncoding<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            // Generate an array of bytes with a predictable pattern
            // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, A, B, C, D, E, F, 10, 11, 12, 13
            var originalBytes = Enumerable.Range(0, 0x1234)
                .Select(b => (byte)(b % 20)).ToArray();

            var originalStream = new MemoryStream(originalBytes);

            var httpContext = GetHttpContext();
            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileStreamResult(originalStream, expectedContentType);

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            var outBytes = outStream.ToArray();
            Assert.True(originalBytes.SequenceEqual(outBytes));
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
            Assert.False(originalStream.CanSeek);
        }

        public static async Task HeadRequest_DoesNotWriteToBody_AndClosesReadStream<TContext>(
            Func<FileStreamResult, TContext, Task> function)
        {
            // Arrange
            var readStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));

            var httpContext = GetHttpContext();
            httpContext.Request.Method = "HEAD";
            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileStreamResult(readStream, "text/plain");

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            Assert.False(readStream.CanSeek);
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.Equal(0, httpContext.Response.Body.Length);
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<FileStreamResult>, FileStreamResultExecutor>();
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
