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
        public void Execute_RendersPartialViews()
        {
            // Arrange
            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Returns(Task.FromResult(result: true))
                .Verifiable();
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.Found("some-view", view.Object))
                      .Verifiable();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var result = new ViewViewComponentResult(viewEngine.Object, "some-view", viewData);
            var viewComponentContext = GetViewComponentContext(view.Object, viewData);

            // Act
            result.Execute(viewComponentContext);

            // Assert
            viewEngine.Verify();
            view.Verify();
        }

        [Fact]
        public void Execute_ThrowsIfPartialViewCannotBeFound()
        {
            // Arrange
            var expected = string.Join(Environment.NewLine,
                        "The view 'Components/Object/some-view' was not found. The following locations were searched:",
                        "location1",
                        "location2.");
            var view = Mock.Of<IView>();
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("some-view", new[] { "location1", "location2" }))
                      .Verifiable();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var result = new ViewViewComponentResult(viewEngine.Object, "some-view", viewData);
            var viewComponentContext = GetViewComponentContext(view, viewData);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => result.Execute(viewComponentContext));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void Execute_DoesNotWrapThrownExceptionsInAggregateExceptions()
        {
            // Arrange
            var expected = new IndexOutOfRangeException();
            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Throws(expected)
                .Verifiable();
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.Found("some-view", view.Object))
                      .Verifiable();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var result = new ViewViewComponentResult(viewEngine.Object, "some-view", viewData);
            var viewComponentContext = GetViewComponentContext(view.Object, viewData);

            // Act
            var actual = Record.Exception(() => result.Execute(viewComponentContext));

            // Assert
            Assert.Same(expected, actual);
            view.Verify();
        }

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