// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class FileContentResultTest
    {
        [Fact]
        public void Constructor_SetsFileContents()
        {
            // Arrange
            var fileContents = new byte[0];

            // Act
            var result = new FileContentResult(fileContents, "text/plain");

            // Assert
            Assert.Same(fileContents, result.FileContents);
        }

        [Fact]
        public void Constructor_SetsContentTypeAndParameters()
        {
            // Arrange
            var fileContents = new byte[0];
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var expectedMediaType = contentType;

            // Act
            var result = new FileContentResult(fileContents, contentType);

            // Assert
            Assert.Same(fileContents, result.FileContents);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
        }

        [Fact]
        public void Constructor_SetsLastModifiedAndEtag()
        {
            // Arrange
            var fileContents = new byte[0];
            var contentType = "text/plain";
            var expectedMediaType = contentType;
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");

            // Act
            var result = new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag
            };

            // Assert
            Assert.Equal(lastModified, result.LastModified);
            Assert.Equal(entityTag, result.EntityTag);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        public async Task WriteFileAsync_CopiesBuffer_ToOutputStream(
            string action,
            Func<FileContentResult, object, Task> function)
        {
            // Arrange
            var buffer = new byte[] { 1, 2, 3, 4, 5 };

            var httpContext = GetHttpContext();

            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileContentResult(buffer, "text/plain");

            // Act
            await function(result, action == "ActionContext" ? context : httpContext);

            // Assert
            Assert.Equal(buffer, outStream.ToArray());
        }

        [Theory]
        [InlineData(0, 4, "Hello", 5)]
        [InlineData(6, 10, "World", 5)]
        [InlineData(null, 5, "World", 5)]
        [InlineData(6, null, "World", 5)]
        public async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
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
            await result.ExecuteResultAsync(actionContext);

            // Assert
            start = start ?? 11 - end;
            end = start + contentLength - 1;
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
            var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, byteArray.Length);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
            Assert.Equal(contentLength, httpResponse.ContentLength);
            Assert.Equal(expectedString, body);
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        public async Task WriteFileAsync_IfRangeHeaderValid_WritesRangeRequest(
            string action,
            Func<FileContentResult, object, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = DateTimeOffset.MinValue;
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
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
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);

            if (result.EnableRangeProcessing)
            {
                Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
                Assert.Equal("bytes", httpResponse.Headers.AcceptRanges);
                var contentRange = new ContentRangeHeaderValue(0, 4, byteArray.Length);
                Assert.Equal(contentRange.ToString(), httpResponse.Headers.ContentRange);
                Assert.Equal(5, httpResponse.ContentLength);
                Assert.Equal("Hello", body);
            }
            else
            {
                Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
                Assert.Equal(11, httpResponse.ContentLength);
                Assert.Equal("Hello World", body);
            }
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        public async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestIgnored(
            string action,
            Func<FileContentResult, object, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = DateTimeOffset.MinValue.AddDays(1);
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag
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
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal("Hello World", body);
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        public async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestIgnored(
            string action,
            Func<FileContentResult, object, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = DateTimeOffset.MinValue.AddDays(1);
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
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
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));
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
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Equal(lastModified.ToString("R"), httpResponse.Headers.LastModified);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers.ETag);
            Assert.Equal("Hello World", body);
        }

        [Theory]
        [InlineData("0-5")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored(string rangeString)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
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
            await result.ExecuteResultAsync(actionContext);

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
        }

        [Theory]
        [InlineData("bytes = 12-13")]
        [InlineData("bytes = -0")]
        public async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable(string rangeString)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
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
            await result.ExecuteResultAsync(actionContext);

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
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        public async Task WriteFileAsync_PreconditionFailed_RangeRequestedIgnored(
            string action,
            Func<FileContentResult, object, Task> function)
        {
            // Arrange
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
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

        [Theory]
        [MemberData(nameof(GetActions))]
        public async Task WriteFileAsync_NotModified_RangeRequestedIgnored(
            string action,
            Func<FileContentResult, object, Task> function)
        {
            // Arrange       
            var contentType = "text/plain";
            var lastModified = new DateTimeOffset();
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            var byteArray = Encoding.ASCII.GetBytes("Hello World");

            var result = new FileContentResult(byteArray, contentType)
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
            await function(result, action == "ActionContext" ? actionContext : httpContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = streamReader.ReadToEndAsync().Result;
            Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
            Assert.Null(httpResponse.ContentLength);
            Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
            Assert.Empty(httpResponse.Headers.ContentRange);
            Assert.NotEmpty(httpResponse.Headers.LastModified);
            Assert.Empty(body);
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding(
            string action,
            Func<FileContentResult, object, Task> function)
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            var buffer = new byte[] { 1, 2, 3, 4, 5 };

            var httpContext = GetHttpContext();

            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileContentResult(buffer, expectedContentType);

            // Act
            await function(result, action == "ActionContext" ? context : httpContext);

            // Assert
            Assert.Equal(buffer, outStream.ToArray());
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        }

        public static IEnumerable<object[]> GetActions()
        {
            return new List<object[]>
            {
                new object[] { "ActionContext", new Func<FileContentResult, object, Task>(async (result, context) => await result.ExecuteResultAsync((ActionContext)context)) },
                new object[] { "HttpContext", new Func<FileContentResult, object, Task>(async (result, context) => await ((IResult)result).ExecuteAsync((HttpContext)context)) },
            };
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<FileContentResult>, FileContentResultExecutor>();
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
