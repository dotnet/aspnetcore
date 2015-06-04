// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewExecutorTest
    {
        public static TheoryData<MediaTypeHeaderValue, string, byte[]> ViewExecutorSetsContentTypeAndEncodingData
        {
            get
            {
                return new TheoryData<MediaTypeHeaderValue, string, byte[]>
                {
                    {
                        null,
                        "text/html; charset=utf-8",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        new MediaTypeHeaderValue("text/foo"),
                        "text/foo; charset=utf-8",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo; p1=p1-value"),
                        "text/foo; p1=p1-value; charset=utf-8",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Charset = "us-ascii" },
                        "text/foo; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewExecutorSetsContentTypeAndEncodingData))]
        public async Task ExecuteAsync_SetsContentTypeAndEncoding(
            MediaTypeHeaderValue contentType,
            string expectedContentType,
            byte[] expectedContentData)
        {
            // Arrange
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

            var actionContext = new ActionContext(context,
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
    }
}