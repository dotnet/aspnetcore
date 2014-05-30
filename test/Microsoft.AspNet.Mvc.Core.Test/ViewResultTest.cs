// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class ViewResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_WritesOutputWithoutBOM()
        {
            // Arrange
            var expected = new byte[] { 97, 98, 99, 100 };
            var memoryStream = new MemoryStream();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Body)
                   .Returns(memoryStream);
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Response)
                   .Returns(response.Object);
            var routeDictionary = new Dictionary<string, object>();
            var actionContext = new ActionContext(context.Object,
                                                  Mock.Of<IRouter>(),
                                                  routeDictionary,
                                                  new ActionDescriptor());
            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                 .Callback((ViewContext v) =>
                 {
                     v.Writer.Write("abcd");
                 })
                 .Returns(Task.FromResult(0));
            var serviceProvider = Mock.Of<IServiceProvider>();
            var viewEngine = new Mock<IViewEngine>();
            viewEngine.Setup(v => v.FindView(routeDictionary, It.IsAny<string>()))
                      .Returns(ViewEngineResult.Found("MyView", view.Object));
            var viewResult = new ViewResult(serviceProvider, viewEngine.Object);

            // Act
            await viewResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expected, memoryStream.ToArray());
        }
    }
}