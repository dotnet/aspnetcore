// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class ViewExecutorTest
    {
        public static TheoryData<MediaTypeHeaderValue, string, string> ViewExecutorSetsContentTypeAndEncodingData
        {
            get
            {
                return new TheoryData<MediaTypeHeaderValue, string, string>
                {
                    {
                        null,
                        null,
                        "text/html; charset=utf-8"
                    },
                    {
                        new MediaTypeHeaderValue("text/foo"),
                        null,
                        "text/foo"
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; charset=us-ascii"),
                        null,
                        "text/foo; charset=us-ascii"
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; p1=p1-value"),
                        null,
                        "text/foo; p1=p1-value"
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; p1=p1-value; charset=us-ascii"),
                        null,
                        "text/foo; p1=p1-value; charset=us-ascii"
                    },
                    {
                        null,
                        "text/bar",
                        "text/bar"
                    },
                    {
                        null,
                        "text/bar; p1=p1-value",
                        "text/bar; p1=p1-value"
                    },
                                        {
                        null,
                        "text/bar; p1=p1-value; charset=us-ascii",
                        "text/bar; p1=p1-value; charset=us-ascii"
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; charset=us-ascii"),
                        "text/bar",
                        "text/foo; charset=us-ascii"
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; charset=us-ascii"),
                        "text/bar; charset=utf-8",
                        "text/foo; charset=us-ascii"
                    }
                };
            }
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionInSyncContext()
        {
            // Arrange
            var view = CreateView((v) =>
            {
                v.Writer.Write("xyz");
                throw new NotImplementedException("This should be raw!");
            });

            var context = new DefaultHttpContext();
            var stream = new Mock<Stream>();
            stream.Setup(s => s.CanWrite).Returns(true);

            context.Response.Body = stream.Object;
            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            var viewExecutor = CreateViewExecutor();

            // Act
            var exception = await Assert.ThrowsAsync<NotImplementedException>(async () => await viewExecutor.ExecuteAsync(
                actionContext,
                view,
                viewData,
                Mock.Of<ITempDataDictionary>(),
                contentType: null,
                statusCode: null)
            );

            // Assert
            Assert.Equal("This should be raw!", exception.Message);
            stream.Verify(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(ViewExecutorSetsContentTypeAndEncodingData))]
        public async Task ExecuteAsync_SetsContentTypeAndEncoding(
            MediaTypeHeaderValue contentType,
            string responseContentType,
            string expectedContentType)
        {
            // Arrange
            var view = CreateView(async (v) =>
            {
                await v.Writer.WriteAsync("abcd");
            });

            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            context.Response.ContentType = responseContentType;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            var viewExecutor = CreateViewExecutor();

            // Act
            await viewExecutor.ExecuteAsync(
                actionContext,
                view,
                viewData,
                Mock.Of<ITempDataDictionary>(),
                contentType?.ToString(),
                statusCode: null);

            // Assert
            MediaTypeAssert.Equal(expectedContentType, context.Response.ContentType);
            Assert.Equal("abcd", Encoding.UTF8.GetString(memoryStream.ToArray()));
        }

        private static IServiceProvider GetServiceProvider()
        {
            var httpContext = new DefaultHttpContext();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IModelMetadataProvider>(new EmptyModelMetadataProvider());
            var tempDataProvider = Mock.Of<ITempDataProvider>();
            serviceCollection.AddSingleton<ITempDataDictionary>(new TempDataDictionary(httpContext, tempDataProvider));

            return serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public async Task ExecuteAsync_ViewResultAllowNull()
        {
            // Arrange
            var tempDataNull = false;
            var viewDataNull = false;
            var delegateHit = false;

            var view = CreateView(async (v) =>
            {
                delegateHit = true;
                tempDataNull = v.TempData == null;
                viewDataNull = v.ViewData == null;

                await v.Writer.WriteAsync("abcd");
            });
            var context = new DefaultHttpContext();

            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());

            context.RequestServices = GetServiceProvider();
            var viewExecutor = CreateViewExecutor();

            // Act
            await viewExecutor.ExecuteAsync(
                actionContext,
                view,
                null,
                null,
                contentType: null,
                statusCode: 200);

            // Assert
            Assert.Equal(200, context.Response.StatusCode);
            Assert.True(delegateHit);
            Assert.False(viewDataNull);
            Assert.False(tempDataNull);
        }

        [Fact]
        public async Task ExecuteAsync_SetsStatusCode()
        {
            // Arrange
            var view = CreateView(async (v) =>
            {
                await v.Writer.WriteAsync("abcd");
            });

            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            var viewExecutor = CreateViewExecutor();

            // Act
            await viewExecutor.ExecuteAsync(
                actionContext,
                view,
                viewData,
                Mock.Of<ITempDataDictionary>(),
                contentType: null,
                statusCode: 500);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("abcd", Encoding.UTF8.GetString(memoryStream.ToArray()));
        }

        [Fact]
        public async Task ExecuteAsync_WritesDiagnostic()
        {
            // Arrange
            var view = CreateView(async (v) =>
            {
                await v.Writer.WriteAsync("abcd");
            });

            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            var adapter = new TestDiagnosticListener();

            var diagnosticListener = new DiagnosticListener("Test");
            diagnosticListener.SubscribeWithAdapter(adapter);

            var viewExecutor = CreateViewExecutor(diagnosticListener);

            // Act
            await viewExecutor.ExecuteAsync(
                actionContext,
                view,
                viewData,
                Mock.Of<ITempDataDictionary>(),
                contentType: null,
                statusCode: null);

            // Assert
            Assert.Equal("abcd", Encoding.UTF8.GetString(memoryStream.ToArray()));

            Assert.NotNull(adapter.BeforeView?.View);
            Assert.NotNull(adapter.BeforeView?.ViewContext);
            Assert.NotNull(adapter.AfterView?.View);
            Assert.NotNull(adapter.AfterView?.ViewContext);
        }

        [Fact]
        public async Task ExecuteAsync_DoesNotWriteToResponse_OnceExceptionIsThrown()
        {
            // Arrange
            var expectedLength = 0;

            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                 .Callback((ViewContext v) =>
                 {
                     throw new Exception();
                 });

            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            var viewExecutor = CreateViewExecutor();

            // Act
            await Record.ExceptionAsync(() => viewExecutor.ExecuteAsync(
                actionContext,
                view.Object,
                viewData,
                Mock.Of<ITempDataDictionary>(),
                contentType: null,
                statusCode: null));

            // Assert
            Assert.Equal(expectedLength, memoryStream.Length);
        }

        [Theory]
        [InlineData(TestHttpResponseStreamWriterFactory.DefaultBufferSize - 1)]
        [InlineData(TestHttpResponseStreamWriterFactory.DefaultBufferSize + 1)]
        [InlineData(2 * TestHttpResponseStreamWriterFactory.DefaultBufferSize + 4)]
        public async Task ExecuteAsync_AsynchronouslyFlushesToTheResponseStream_PriorToDispose(int writeLength)
        {
            // Arrange
            var view = CreateView(async (v) =>
            {
                var text = new string('a', writeLength);
                await v.Writer.WriteAsync(text);
            });

            var expectedWriteCallCount = Math.Ceiling((double)writeLength / TestHttpResponseStreamWriterFactory.DefaultBufferSize);

            var context = new DefaultHttpContext();
            var stream = new Mock<Stream>();
            stream.SetupGet(s => s.CanWrite).Returns(true);
            context.Response.Body = stream.Object;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            var viewExecutor = CreateViewExecutor();

            // Act
            await viewExecutor.ExecuteAsync(
                actionContext,
                view,
                viewData,
                Mock.Of<ITempDataDictionary>(),
                contentType: null,
                statusCode: null);

            // Assert
            stream.Verify(s => s.FlushAsync(It.IsAny<CancellationToken>()), Times.Never());
            stream.Verify(
                s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Exactly((int)expectedWriteCallCount));
            stream.Verify(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }

        private IView CreateView(Func<ViewContext, Task> action)
        {
            var view = new Mock<IView>(MockBehavior.Strict);
            view
                .Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Returns(action);

            return view.Object;
        }

        private ViewExecutor CreateViewExecutor(DiagnosticListener diagnosticListener = null)
        {
            if (diagnosticListener == null)
            {
                diagnosticListener = new DiagnosticListener("Test");
            }

            return new ViewExecutor(
                Options.Create(new MvcViewOptions()),
                new TestHttpResponseStreamWriterFactory(),
                new Mock<ICompositeViewEngine>(MockBehavior.Strict).Object,
                new TempDataDictionaryFactory(Mock.Of<ITempDataProvider>()),
                diagnosticListener,
                new EmptyModelMetadataProvider());
        }
    }
}
