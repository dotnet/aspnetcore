// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ViewResultBaseTest
    {
        // The buffer size of the StreamWriter used in ViewResult.
        private const int ViewResultStreamWriterBufferSize = 1024;

        [Fact]
        public async Task ExecuteResultAsync_ReturnsError_IfViewCouldNotBeFound()
        {
            // Arrange
            var expected = string.Join(Environment.NewLine,
                                       "The view 'MyView' was not found. The following locations were searched:",
                                       "Location1",
                                       "Location2.");
            var actionContext = new ActionContext(new DefaultHttpContext(),
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var viewEngine = Mock.Of<IViewEngine>();

            var mockResult = new Mock<ViewResultBase> { CallBase = true };
            mockResult.Protected()
                  .Setup<ViewEngineResult>("FindView", viewEngine, actionContext, ItExpr.IsAny<string>())
                  .Returns(ViewEngineResult.NotFound("MyView", new[] { "Location1", "Location2" }))
                  .Verifiable();

            var viewResult = mockResult.Object;
            viewResult.ViewEngine = viewEngine;
            viewResult.ViewName = "MyView";

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => viewResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, ex.Message);
            mockResult.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_UsesActionDescriptorName_IfViewNameIsNullOrEmpty()
        {
            // Arrange
            var viewName = "some-view-name";
            var actionContext = new ActionContext(new DefaultHttpContext(),
                                                  new RouteData(),
                                                  new ActionDescriptor { Name = viewName });
            var viewEngine = Mock.Of<IViewEngine>();

            var mockResult = new Mock<ViewResultBase> { CallBase = true };
            mockResult.Protected()
                  .Setup<ViewEngineResult>("FindView", viewEngine, actionContext, viewName)
                  .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
                  .Verifiable();

            var viewResult = mockResult.Object;
            viewResult.ViewEngine = viewEngine;

            // Act
            await viewResult.ExecuteResultAsync(actionContext);

            // Assert
            mockResult.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_WritesOutputWithoutBOM()
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

            var viewEngine = Mock.Of<ICompositeViewEngine>();
            var routeDictionary = new Dictionary<string, object>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(ICompositeViewEngine)))
                           .Returns(viewEngine);

            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object,
            };
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(context,
                                                  new RouteData { Values = routeDictionary },
                                                  new ActionDescriptor());

            var viewResult = new Mock<ViewResultBase> { CallBase = true };


            var result = new Mock<ViewResultBase> { CallBase = true };
            result.Protected()
                  .Setup<ViewEngineResult>("FindView", viewEngine, actionContext, ItExpr.IsAny<string>())
                  .Returns(ViewEngineResult.Found("MyView", view.Object));

            // Act
            await result.Object.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
        }

        [Fact]
        public async Task ExecuteResultAsync_UsesProvidedViewEngine()
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

            var routeDictionary = new Dictionary<string, object>();

            var goodViewEngine = Mock.Of<IViewEngine>();
            var badViewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(ICompositeViewEngine)))
                           .Returns(badViewEngine.Object);

            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object,
            };
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(context,
                                                  new RouteData { Values = routeDictionary },
                                                  new ActionDescriptor());

            var result = new Mock<ViewResultBase> { CallBase = true };
            result.Protected()
                  .Setup<ViewEngineResult>("FindView", goodViewEngine, actionContext, ItExpr.IsAny<string>())
                  .Returns(ViewEngineResult.Found("MyView", view.Object))
                  .Verifiable();

            result.Object.ViewEngine = goodViewEngine;

            // Act
            await result.Object.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
            result.Verify();
        }

        public static IEnumerable<object[]> ExecuteResultAsync_DoesNotWriteToResponse_OnceExceptionIsThrownData
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
        [MemberData(nameof(ExecuteResultAsync_DoesNotWriteToResponse_OnceExceptionIsThrownData))]
        public async Task ExecuteResultAsync_DoesNotWriteToResponse_OnceExceptionIsThrown(int writtenLength, int expectedLength)
        {
            // Arrange
            var longString = new string('a', writtenLength);

            var routeDictionary = new Dictionary<string, object>();

            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                 .Callback((ViewContext v) =>
                 {
                     view.ToString();
                     v.Writer.Write(longString);
                     throw new Exception();
                 });

            var viewEngine = Mock.Of<ICompositeViewEngine>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(ICompositeViewEngine)))
                           .Returns(viewEngine);

            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object,
            };
            var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            var actionContext = new ActionContext(context,
                                                  new RouteData { Values = routeDictionary },
                                                  new ActionDescriptor());

            var result = new Mock<ViewResultBase> { CallBase = true };
            result.Protected()
                  .Setup<ViewEngineResult>("FindView", viewEngine, actionContext, ItExpr.IsAny<string>())
                  .Returns(ViewEngineResult.Found("MyView", view.Object))
                  .Verifiable();

            // Act
            await Record.ExceptionAsync(() => result.Object.ExecuteResultAsync(actionContext));

            // Assert
            Assert.Equal(expectedLength, memoryStream.Length);
        }
    }
}