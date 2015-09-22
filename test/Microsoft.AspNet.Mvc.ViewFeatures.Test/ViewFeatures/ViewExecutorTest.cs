// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Routing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class ViewExecutorTest
    {
        public static TheoryData<MediaTypeHeaderValue, string, string, byte[]> ViewExecutorSetsContentTypeAndEncodingData
        {
            get
            {
                return new TheoryData<MediaTypeHeaderValue, string, string, byte[]>
                {
                    {
                        null,
                        null,
                        "text/html; charset=utf-8",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        new MediaTypeHeaderValue("text/foo"),
                        null,
                        "text/foo; charset=utf-8",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; p1=p1-value"),
                        null,
                        "text/foo; p1=p1-value; charset=utf-8",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Charset = "us-ascii" },
                        null,
                        "text/foo; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        null,
                        "text/bar",
                        "text/bar",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        null,
                        "application/xml; charset=us-ascii",
                        "application/xml; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        null,
                        "Invalid content type",
                        "Invalid content type",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Charset = "us-ascii" },
                        "text/bar",
                        "text/foo; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewExecutorSetsContentTypeAndEncodingData))]
        public async Task ExecuteAsync_SetsContentTypeAndEncoding(
            MediaTypeHeaderValue contentType,
            string responseContentType,
            string expectedContentType,
            byte[] expectedContentData)
        {
            // Arrange
            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                 .Callback((ViewContext v) =>
                 {
                     v.Writer.Write("abcd");
                 })
                 .Returns(Task.FromResult(0));

            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            context.Response.ContentType = responseContentType;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            // Act
            await ViewExecutor.ExecuteAsync(
                view.Object,
                actionContext,
                viewData,
                null,
                new HtmlHelperOptions(),
                contentType);

            // Assert
            Assert.Equal(expectedContentType, context.Response.ContentType);
            Assert.Equal(expectedContentData, memoryStream.ToArray());
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

            // Act
            await Record.ExceptionAsync(
                () => ViewExecutor.ExecuteAsync(
                    view.Object,
                    actionContext,
                    viewData,
                    null,
                    new HtmlHelperOptions(),
                    contentType: null));

            // Assert
            Assert.Equal(expectedLength, memoryStream.Length);
        }

        [Theory]
        [InlineData(HttpResponseStreamWriter.DefaultBufferSize - 1)]
        [InlineData(HttpResponseStreamWriter.DefaultBufferSize + 1)]
        [InlineData(2 * HttpResponseStreamWriter.DefaultBufferSize + 4)]
        public async Task ExecuteAsync_AsynchronouslyFlushesToTheResponseStream_PriorToDispose(int writeLength)
        {
            // Arrange
            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Returns((ViewContext v) =>
                    Task.Run(async () =>
                    {
                        var text = new string('a', writeLength);
                        await v.Writer.WriteAsync(text);
                    }));

            var context = new DefaultHttpContext();
            var stream = new Mock<Stream>();
            context.Response.Body = stream.Object;

            var actionContext = new ActionContext(
                context,
                new RouteData(),
                new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            // Act
            await ViewExecutor.ExecuteAsync(
                view.Object,
                actionContext,
                viewData,
                Mock.Of<ITempDataDictionary>(),
                new HtmlHelperOptions(),
                ViewExecutor.DefaultContentType);

            // Assert
            stream.Verify(s => s.FlushAsync(It.IsAny<CancellationToken>()), Times.Once());
            stream.Verify(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }
    }
}