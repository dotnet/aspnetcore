// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewViewComponentResultTest
    {
        [Fact]
        public async Task ExecuteAsync_RendersPartialViews()
        {
            // Arrange
            var view = Mock.Of<IView>();
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.Found("some-view", view))
                      .Verifiable();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var result = new ViewViewComponentResult(viewEngine.Object, "some-view", viewData);
            var viewComponentContext = GetViewComponentContext(view, viewData);

            // Act
            await result.ExecuteAsync(viewComponentContext);

            // Assert
            viewEngine.Verify();
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsIfPartialViewCannotBeFound()
        {
            // Arrange
            var expected =
@"The view 'Components/Object/some-view' was not found. The following locations were searched:
foo
bar.";
            var view = Mock.Of<IView>();
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("some-view", new[] { "foo", "bar" }))
                      .Verifiable();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var result = new ViewViewComponentResult(viewEngine.Object, "some-view", viewData);
            var viewComponentContext = GetViewComponentContext(view, viewData);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await result.ExecuteAsync(viewComponentContext));
            Assert.Equal(expected, ex.Message);
        }

        private static ViewComponentContext GetViewComponentContext(IView view, ViewDataDictionary viewData)
        {
            var actionContext = new ActionContext(new RouteContext(new DefaultHttpContext()), new ActionDescriptor());
            var viewContext = new ViewContext(actionContext, view, viewData, TextWriter.Null);
            var viewComponentContext = new ViewComponentContext(typeof(object).GetTypeInfo(), viewContext, TextWriter.Null);
            return viewComponentContext;
        }
    }
}