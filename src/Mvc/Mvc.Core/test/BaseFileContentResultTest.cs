// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
    public class BaseFileContentResultTest
    {
        public static async Task WriteFileAsync_CopiesBuffer_ToOutputStream<TContext>(
            Func<FileContentResult, TContext, Task> function)
        {
            // Arrange
            var buffer = new byte[] { 1, 2, 3, 4, 5 };

            var httpContext = GetHttpContext();

            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileContentResult(buffer, "text/plain");

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            Assert.Equal(buffer, outStream.ToArray());
        }

        public static async Task WriteFileAsync_PreconditionStateShouldProcess_WritesRangeRequested<TContext>(
            long? start,
            long? end,
            string expectedString,
            long contentLength,
            Func<FileContentResult, TContext, Task> function)
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
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

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

        public static async Task WriteFileAsync_IfRangeHeaderValid_WritesRangeRequest<TContext>(
            Func<FileContentResult, TContext, Task> function)
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
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

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

        public static async Task WriteFileAsync_RangeProcessingNotEnabled_RangeRequestIgnored<TContext>(
            Func<FileContentResult, TContext, Task> function)
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
        }

        public static async Task WriteFileAsync_IfRangeHeaderInvalid_RangeRequestIgnored<TContext>(
            Func<FileContentResult, TContext, Task> function)
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
        }

        public static async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestIgnored<TContext>(
            string rangeString,
            Func<FileContentResult, TContext, Task> function)
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
        }

        public static async Task WriteFileAsync_PreconditionStateUnspecified_RangeRequestedNotSatisfiable<TContext>(
            string rangeString,
            Func<FileContentResult, TContext, Task> function)
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
        }

        public static async Task WriteFileAsync_PreconditionFailed_RangeRequestedIgnored<TContext>(
            Func<FileContentResult, TContext, Task> function)
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
        }

        public static async Task WriteFileAsync_NotModified_RangeRequestedIgnored<TContext>(
            Func<FileContentResult, TContext, Task> function)
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
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

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

        public static async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding<TContext>(
            Func<FileContentResult, TContext, Task> function)
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            var buffer = new byte[] { 1, 2, 3, 4, 5 };

            var httpContext = GetHttpContext();

            var outStream = new MemoryStream();
            httpContext.Response.Body = outStream;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var result = new FileContentResult(buffer, expectedContentType);

            // Act
            object context = typeof(TContext) == typeof(HttpContext) ? httpContext : actionContext;
            await function(result, (TContext)context);

            // Assert
            Assert.Equal(buffer, outStream.ToArray());
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
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
