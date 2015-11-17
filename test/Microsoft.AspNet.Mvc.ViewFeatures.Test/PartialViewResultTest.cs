// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if MOCK_SUPPORT
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    // These tests cover the logic included in PartialViewResult.ExecuteResultAsync - see PartialViewResultExecutorTest
    // and ViewExecutorTest for more comprehensive tests.
    public class PartialViewResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_Throws_IfViewCouldNotBeFound_MessageUsesGetViewLocations()
        {
            // Arrange
            var expected = string.Join(
                Environment.NewLine,
                "The view 'MyView' was not found. The following locations were searched:",
                "Location1",
                "Location2");

            var actionContext = GetActionContext();

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, "MyView", /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("MyView", new[] { "Location1", "Location2" }))
                .Verifiable();
            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), "MyView", /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("MyView", Enumerable.Empty<string>()))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = "MyView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => viewResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, ex.Message);
            viewEngine.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_Throws_IfViewCouldNotBeFound_MessageUsesFindViewLocations()
        {
            // Arrange
            var expected = string.Join(
                Environment.NewLine,
                "The view 'MyView' was not found. The following locations were searched:",
                "Location1",
                "Location2");

            var actionContext = GetActionContext();

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, "MyView", /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("MyView", Enumerable.Empty<string>()))
                .Verifiable();
            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), "MyView", /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("MyView", new[] { "Location1", "Location2" }))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = "MyView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => viewResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, ex.Message);
            viewEngine.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_Throws_IfViewCouldNotBeFound_MessageUsesAllLocations()
        {
            // Arrange
            var expected = string.Join(
                Environment.NewLine,
                "The view 'MyView' was not found. The following locations were searched:",
                "Location1",
                "Location2",
                "Location3",
                "Location4");

            var actionContext = GetActionContext();

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, "MyView", /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("MyView", new[] { "Location1", "Location2" }))
                .Verifiable();
            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), "MyView", /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("MyView", new[] { "Location3", "Location4" }))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = "MyView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => viewResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, ex.Message);
            viewEngine.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_FindsAndExecutesView()
        {
            // Arrange
            var viewName = "myview";
            var context = GetActionContext();

            var view = new Mock<IView>(MockBehavior.Strict);
            view
                .Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            view
                .As<IDisposable>()
                .Setup(v => v.Dispose())
                .Verifiable();

            // Used by logging
            view
                .SetupGet(v => v.Path)
                .Returns("myview.cshtml");

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("myview", Enumerable.Empty<string>()))
                .Verifiable();
            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), "myview", /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found("myview", view.Object))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            view.Verify();
            viewEngine.Verify();
        }

        private ActionContext GetActionContext()
        {
            return new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());
        }

        private HttpContext GetHttpContext()
        {
            var options = new TestOptionsManager<MvcViewOptions>();

            var viewExecutor = new PartialViewResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                new CompositeViewEngine(options),
                new DiagnosticListener("Microsoft.AspNet"),
                NullLoggerFactory.Instance);

            var services = new ServiceCollection();
            services.AddSingleton(viewExecutor);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();
            return httpContext;
        }
    }
}
#endif