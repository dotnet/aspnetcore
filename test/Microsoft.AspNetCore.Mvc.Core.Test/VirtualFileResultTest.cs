// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        [Fact]
        public async Task ExecuteResultAsync_FallsBackToWebRootFileProvider_IfNoFileProviderIsPresent()
        {
            // Arrange
            var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");
            var result = new TestVirtualFileResult(path, "text/plain");

            var appEnvironment = new Mock<IHostingEnvironment>();
            appEnvironment.Setup(app => app.WebRootFileProvider)
                .Returns(GetFileProvider(path));

            var httpContext = GetHttpContext();
            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(appEnvironment.Object)
                .AddTransient<TestVirtualFileResultExecutor>()
                .AddTransient<ILoggerFactory, LoggerFactory>()
                .BuildServiceProvider();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            Assert.NotNull(httpContext.Response.Body);
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents¡", contents);
        }

        [Fact]
        public async Task ExecuteResultAsync_FallsbackToStreamCopy_IfNoIHttpSendFilePresent()
        {
            // Arrange
            var path = Path.Combine("TestFiles", "FilePathResultTestFile.txt");
            var result = new TestVirtualFileResult(path, "text/plain")
            {
                FileProvider = GetFileProvider(path),
            };

            var httpContext = GetHttpContext();
            httpContext.Response.Body = new MemoryStream();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            Assert.NotNull(httpContext.Response.Body);
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents¡", contents);
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

            var sendFileMock = new Mock<IHttpSendFileFeature>();
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

        [Fact]
        public async Task ExecuteResultAsync_SetsSuppliedContentTypeAndEncoding()
        {
            // Arrange
            var expectedContentType = "text/foo; charset=us-ascii";
            var result = new TestVirtualFileResult(
                "FilePathResultTestFile_ASCII.txt", expectedContentType)
            {
                FileProvider = GetFileProvider("FilePathResultTestFile_ASCII.txt"),
                IsAscii = true,
            };

            var httpContext = GetHttpContext();
            var memoryStream = new MemoryStream();
            httpContext.Response.Body = memoryStream;
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            var contents = Encoding.ASCII.GetString(memoryStream.ToArray());
            Assert.Equal("FilePathResultTestFile contents ASCII encoded", contents);
            Assert.Equal(expectedContentType, httpContext.Response.ContentType);
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

            var httpContext = GetHttpContext();
            httpContext.Response.Body = new MemoryStream();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            Assert.NotNull(httpContext.Response.Body);
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents¡", contents);
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
            var httpContext = GetHttpContext();
            var memoryStream = new MemoryStream();
            httpContext.Response.Body = memoryStream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents¡", contents);
            Mock.Get(result.FileProvider).Verify();
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
            var httpContext = GetHttpContext();
            var memoryStream = new MemoryStream();
            httpContext.Response.Body = memoryStream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(context);
            httpContext.Response.Body.Position = 0;

            // Assert
            var contents = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
            Assert.Equal("FilePathResultTestFile contents¡", contents);
            Mock.Get(result.FileProvider).Verify();
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

            var hostingEnvironment = new Mock<IHostingEnvironment>();

            services.AddSingleton(executorType ?? typeof(TestVirtualFileResultExecutor));
            services.AddSingleton<IHostingEnvironment>(hostingEnvironment.Object);
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
            fileInfo.SetupGet(fi => fi.Exists).Returns(true);
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
                var executor = context.HttpContext.RequestServices.GetRequiredService<TestVirtualFileResultExecutor>();
                executor.IsAscii = IsAscii;
                return executor.ExecuteAsync(context, this);
            }

            public bool IsAscii { get; set; } = false;
        }

        private class TestVirtualFileResultExecutor : VirtualFileResultExecutor
        {
            public TestVirtualFileResultExecutor(ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment)
                : base(loggerFactory,hostingEnvironment)
            {
            }

            public bool IsAscii { get; set; }

            protected override Stream GetFileStream(IFileInfo fileInfo)
            {
                if (IsAscii)
                {
                    return new MemoryStream(Encoding.ASCII.GetBytes("FilePathResultTestFile contents ASCII encoded"));
                }
                else
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes("FilePathResultTestFile contents¡"));
                }
            }
        }
    }
}