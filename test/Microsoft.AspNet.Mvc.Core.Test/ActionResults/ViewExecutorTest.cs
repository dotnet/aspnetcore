// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewExecutorTest
    {
        // The buffer size of the StreamWriter used in ViewResult.
        private const int ViewResultStreamWriterBufferSize = 1024;

        [Fact]
        public async Task ExecuteAsync_WritesOutputWithoutBOM()
        {
            // Arrange
            var expected = new byte[] { 97, 98, 99, 100 };

            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                 .Callback((ViewContext v) =>
                 {
                     view.ToString();
                     v.Writer.Write("abcd");
                 })
                 .Returns(Task.FromResult(0));

            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(context,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            // Act
            await ViewExecutor.ExecuteAsync(view.Object, actionContext, viewData, null, contentType: null);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
            Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_UsesSpecifiedContentType()
        {
            // Arrange
            var contentType = "some-content-type";
            var view = Mock.Of<IView>();
            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(context,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            // Act
            await ViewExecutor.ExecuteAsync(view, actionContext, viewData, null, contentType);

            // Assert
            Assert.Equal(contentType, context.Response.ContentType);
        }

        public static IEnumerable<object[]> ExecuteAsync_DoesNotWriteToResponse_OnceExceptionIsThrownData
        {
            get
            {
                yield return new object[] { 30, 0 };

                if (PlatformHelper.IsMono)
                {
                    // The StreamWriter in Mono buffers 2x the buffer size before flushing.
                    yield return new object[] { ViewResultStreamWriterBufferSize * 2 + 30, ViewResultStreamWriterBufferSize };
                }
                else
                {
                    yield return new object[] { ViewResultStreamWriterBufferSize + 30, ViewResultStreamWriterBufferSize };
                }
            }
        }

        // The StreamWriter used by ViewResult an internal buffer and consequently anything written to this buffer
        // prior to it filling up will not be written to the underlying stream once an exception is thrown.
        [Theory]
        [MemberData(nameof(ExecuteAsync_DoesNotWriteToResponse_OnceExceptionIsThrownData))]
        public async Task ExecuteAsync_DoesNotWriteToResponse_OnceExceptionIsThrown(int writtenLength, int expectedLength)
        {
            // Arrange
            var longString = new string('a', writtenLength);

            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                 .Callback((ViewContext v) =>
                 {
                     view.ToString();
                     v.Writer.Write(longString);
                     throw new Exception();
                 });

            var context = new DefaultHttpContext();
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(context,
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

            // Act
            await Record.ExceptionAsync(
                () => ViewExecutor.ExecuteAsync(view.Object, actionContext, viewData, null, contentType: null));

            // Assert
            Assert.Equal(expectedLength, memoryStream.Length);
        }
    }
}