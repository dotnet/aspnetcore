// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class VirtualFileResultTest
    {
        [Fact]
        public void Constructor_SetsFileName()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");

            // Act
            var result = new TestVirtualFileResult(path, "text/plain");

            // Assert
            Assert.Equal(path, result.FileName);
        }

        [Fact]
        public void Constructor_SetsContentTypeAndParameters()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var expectedMediaType = contentType;

            // Act
            var result = new TestVirtualFileResult(path, contentType);

            // Assert
            Assert.Equal(path, result.FileName);
            MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
        }

        [Theory]
        [InlineData(0, 3, "File", 4)]
        [InlineData(8, 13, "Result", 6)]
        [InlineData(null, 4, "ts¡", 4)]
        [InlineData(8, null, "ResultTestFile contents¡", 25)]
        public async Task WriteFileAsync_WritesRangeRequested(long? start, long? end, string expectedString, long contentLength)
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var result = new TestVirtualFileResult(path, contentType);
            result.EnableRangeProcessing = true;
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();

            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.Range = new RangeHeaderValue(start, end);
            requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var startResult = start ?? 33 - end;
            var endResult = startResult + contentLength - 1;
            var httpResponse = actionContext.HttpContext.Response;
            var contentRange = new ContentRangeHeaderValue(startResult.Value, endResult.Value, 33);
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers[HeaderNames.AcceptRanges]);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers[HeaderNames.ContentRange]);
            Assert.NotEmpty(httpResponse.Headers[HeaderNames.LastModified]);
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
            var result = new TestVirtualFileResult(path, contentType);
            result.EnableRangeProcessing = true;
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();

            var entityTag = result.EntityTag = new EntityTagHeaderValue("\"Etag\"");
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            requestHeaders.Range = new RangeHeaderValue(0, 3);
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers[HeaderNames.AcceptRanges]);
            var contentRange = new ContentRangeHeaderValue(0, 3, 33);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers[HeaderNames.ContentRange]);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers[HeaderNames.ETag]);
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
            var result = new TestVirtualFileResult(path, contentType);
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();

            var entityTag = result.EntityTag = new EntityTagHeaderValue("\"Etag\"");
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            requestHeaders.Range = new RangeHeaderValue(0, 3);
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers[HeaderNames.ETag]);
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
            var result = new TestVirtualFileResult(path, contentType);
            result.EnableRangeProcessing = true;
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();

            var entityTag = result.EntityTag = new EntityTagHeaderValue("\"Etag\"");
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            requestHeaders.Range = new RangeHeaderValue(0, 3);
            requestHeaders.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Equal(entityTag.ToString(), httpResponse.Headers[HeaderNames.ETag]);
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
            var result = new TestVirtualFileResult(path, contentType);
            result.EnableRangeProcessing = true;
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                    .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                    .AddSingleton(appEnvironment.Object)
                    .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                    .AddTransient<ILoggerFactory, LoggerFactory>()
                    .BuildServiceProvider();

            var requestHeaders = httpContext.Request.GetTypedHeaders();
            httpContext.Request.Headers[HeaderNames.Range] = rangeString;
            requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status200OK, httpResponse.StatusCode);
            Assert.Empty(httpResponse.Headers[HeaderNames.ContentRange]);
            Assert.NotEmpty(httpResponse.Headers[HeaderNames.LastModified]);
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
            var result = new TestVirtualFileResult(path, contentType);
            result.EnableRangeProcessing = true;
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                    .Returns(GetFileProvider(path));

            var httpContext = GetHttpContext();
            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = new ServiceCollection()
                    .AddSingleton(appEnvironment.Object)
                    .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                    .AddTransient<ILoggerFactory, LoggerFactory>()
                    .BuildServiceProvider();

            var requestHeaders = httpContext.Request.GetTypedHeaders();
            httpContext.Request.Headers[HeaderNames.Range] = rangeString;
            requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
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
            var contentRange = new ContentRangeHeaderValue(33);
            Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers[HeaderNames.AcceptRanges]);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers[HeaderNames.ContentRange]);
            Assert.NotEmpty(httpResponse.Headers[HeaderNames.LastModified]);
            Assert.Equal(0, httpResponse.ContentLength);
            Assert.Empty(body);
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_PreconditionFailed()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var result = new TestVirtualFileResult(path, contentType);
            result.EnableRangeProcessing = true;
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();

            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue;
            httpContext.Request.Headers[HeaderNames.Range] = "bytes = 0-6";
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status412PreconditionFailed, httpResponse.StatusCode);
            Assert.Null(httpResponse.ContentLength);
            Assert.Empty(httpResponse.Headers[HeaderNames.ContentRange]);
            Assert.NotEmpty(httpResponse.Headers[HeaderNames.LastModified]);
            Assert.Null(sendFileFeature.Name); // Not called
        }

        [Fact]
        public async Task WriteFileAsync_RangeRequested_NotModified()
        {
            // Arrange
            var path = Path.GetFullPath("helllo.txt");
            var contentType = "text/plain; charset=us-ascii; p1=p1-value";
            var result = new TestVirtualFileResult(path, contentType);
            result.EnableRangeProcessing = true;
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();

            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(1);
            httpContext.Request.Headers[HeaderNames.Range] = "bytes = 0-6";
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(StatusCodes.Status304NotModified, httpResponse.StatusCode);
            Assert.Null(httpResponse.ContentLength);
            Assert.Empty(httpResponse.Headers[HeaderNames.ContentRange]);
            Assert.NotEmpty(httpResponse.Headers[HeaderNames.LastModified]);
            Assert.False(httpResponse.Headers.ContainsKey(HeaderNames.ContentType));
            Assert.Null(sendFileFeature.Name); // Not called
        }

        [Fact]
        public async Task ExecuteResultAsync_FallsBackToWebRootFileProvider_IfNoFileProviderIsPresent()
        {
            // Arrange
            var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");
            var result = new TestVirtualFileResult(path, "text/plain");

            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(path, sendFileFeature.Name);
            Assert.Equal(0, sendFileFeature.Offset);
            Assert.Null(sendFileFeature.Length);
        }

        [Fact]
        public async Task ExecuteResultAsync_CallsSendFileAsync_IfIHttpSendFilePresent()
        {
            // Arrange
            var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");
            var result = new TestVirtualFileResult(path, "text/plain")
            {
                FileProvider = GetFileProvider(path),
            };

            var sendFileMock = new Mock<IHttpResponseBodyFeature>();
            sendFileMock
                .Setup(s => s.SendFileAsync(path, 0, null, CancellationToken.None))
                .Returns(Task.FromResult<int>(0));

            var httpContext = GetHttpContext();
            httpContext.Features.Set(sendFileMock.Object);
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            sendFileMock.Verify();
        }

        [Theory]
        [InlineData(0, 3, "File", 4)]
        [InlineData(8, 13, "Result", 6)]
        [InlineData(null, 3, "ts¡", 3)]
        [InlineData(8, null, "ResultTestFile contents¡", 25)]
        public async Task ExecuteResultAsync_CallsSendFileAsyncWithRequestedRange_IfIHttpSendFilePresent(long? start, long? end, string expectedString, long contentLength)
        {
            // Arrange
            var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");
            var result = new TestVirtualFileResult(path, "text/plain")
            {
                FileProvider = GetFileProvider(path),
                EnableRangeProcessing = true,
            };

            var sendFile = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFile);
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var appEnvironment = new Mock<IWebHostEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();

            var requestHeaders = httpContext.Request.GetTypedHeaders();
            requestHeaders.Range = new RangeHeaderValue(start, end);
            requestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue.AddDays(1);
            httpContext.Request.Method = HttpMethods.Get;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            start = start ?? 33 - end;
            end = start + contentLength - 1;
            var httpResponse = actionContext.HttpContext.Response;
            Assert.Equal(Path.Combine("TestFiles", "FilePathResultTestFile.txt"), sendFile.Name);
            Assert.Equal(start, sendFile.Offset);
            Assert.Equal(contentLength, sendFile.Length);
            Assert.Equal(CancellationToken.None, sendFile.Token);
            var contentRange = new ContentRangeHeaderValue(start.Value, end.Value, 33);
            Assert.Equal(StatusCodes.Status206PartialContent, httpResponse.StatusCode);
            Assert.Equal("bytes", httpResponse.Headers[HeaderNames.AcceptRanges]);
            Assert.Equal(contentRange.ToString(), httpResponse.Headers[HeaderNames.ContentRange]);
            Assert.NotEmpty(httpResponse.Headers[HeaderNames.LastModified]);
            Assert.Equal(contentLength, httpResponse.ContentLength);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            var result = new TestVirtualFileResult(
                "FilePathResultTestFile_ASCII.txt", expectedContentType)
            {
                FileProvider = GetFileProvider("FilePathResultTestFile_ASCII.txt"),
            };

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
            Assert.Equal("FilePathResultTestFile_ASCII.txt", sendFileFeature.Name);
        }

        [Fact]
        public async Task ExecuteResultAsync_ReturnsFileContentsForRelativePaths()
        {
            // Arrange
            var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");
            var result = new TestVirtualFileResult(path, "text/plain")
            {
                FileProvider = GetFileProvider(path),
            };

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

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
            var result = new TestVirtualFileResult(path, "text/plain")
            {
                FileProvider = GetFileProvider(path),
            };

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Mock.Get(result.FileProvider).Verify();
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
            var result = new TestVirtualFileResult(path, "text/plain")
            {
                FileProvider = GetFileProvider(expectedPath),
            };

            var sendFileFeature = new TestSendFileFeature();
            var httpContext = GetHttpContext();
            httpContext.Features.Set<IHttpResponseBodyFeature>(sendFileFeature);

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Mock.Get(result.FileProvider).Verify();
            Assert.Equal(expectedPath, sendFileFeature.Name);
        }

        [Fact]
        public async Task ExecuteResultAsync_WorksWithNonDiskBasedFiles()
        {
            // Arrange
            var httpContext = GetHttpContext(typeof(VirtualFileResultExecutor));
            httpContext.Response.Body = new MemoryStream();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var expectedData = "This is an embedded resource";
            var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedData));

            var nonDiskFileInfo = new Mock<IFileInfo>();
            nonDiskFileInfo.SetupGet(fi => fi.Exists).Returns(true);
            nonDiskFileInfo.SetupGet(fi => fi.PhysicalPath).Returns(() => null); // set null to indicate non-disk file
            nonDiskFileInfo.Setup(fi => fi.CreateReadStream()).Returns(sourceStream);
            var nonDiskFileProvider = new Mock<IFileProvider>();
            nonDiskFileProvider.Setup(fp => fp.GetFileInfo(It.IsAny<string>())).Returns(nonDiskFileInfo.Object);

            var filePathResult = new VirtualFileResult("/SampleEmbeddedFile.txt", "text/plain")
            {
                FileProvider = nonDiskFileProvider.Object
            };

            // Act
            await filePathResult.ExecuteResultAsync(actionContext);

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
            var filePathResult = new TestVirtualFileResult(path, "text/plain")
            {
                FileProvider = fileProvider.Object,
            };

            var expectedMessage = "Could not find file: " + path;
            var context = new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());

            // Act
            var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => filePathResult.ExecuteResultAsync(context));

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
            Assert.Equal(path, ex.FileName);
        }

        private static IServiceCollection CreateServices(Type executorType)
        {
            var services = new ServiceCollection();

            var hostingEnvironment = new Mock<IWebHostEnvironment>();

            services.AddSingleton<IActionResultExecutor<VirtualFileResult>, TestVirtualFileResultExecutor>();
            if (executorType != null)
            {
                services.AddSingleton(typeof(IActionResultExecutor<VirtualFileResult>), executorType);
            }

            services.AddSingleton<IWebHostEnvironment>(hostingEnvironment.Object);
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            return services;
        }

        private static HttpContext GetHttpContext(Type executorType = null)
        {
            var services = CreateServices(executorType);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }

        private static IFileProvider GetFileProvider(string path)
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

        private class TestVirtualFileResult : VirtualFileResult
        {
            public TestVirtualFileResult(string filePath, string contentType)
                : base(filePath, contentType)
            {
            }

            public override Task ExecuteResultAsync(ActionContext context)
            {
                var executor = (TestVirtualFileResultExecutor)context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<VirtualFileResult>>();
                return executor.ExecuteAsync(context, this);
            }
        }

        private class TestVirtualFileResultExecutor : VirtualFileResultExecutor
        {
            public TestVirtualFileResultExecutor(ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnvironment)
                : base(loggerFactory, hostingEnvironment)
            {
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

                return Task.FromResult(0);
            }

            public Task StartAsync(CancellationToken cancellation = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}