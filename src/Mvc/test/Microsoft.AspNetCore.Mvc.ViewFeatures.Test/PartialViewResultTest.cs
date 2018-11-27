// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    // These tests cover the logic included in PartialViewResult.ExecuteResultAsync - see PartialViewResultExecutorTest
    // and ViewExecutorTest for more comprehensive tests.
    public class PartialViewResultTest
    {
        [Fact]
        public void Model_ExposesViewDataModel()
        {
            // Arrange
            var customModel = new object();
            var viewResult = new PartialViewResult
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
                {
                    Model = customModel
                },
            };

            // Act & Assert
            Assert.Same(customModel, viewResult.Model);
        }

        [Fact]
        public async Task ExecuteResultAsync_Throws_IfViewCouldNotBeFound_MessageUsesGetViewLocations()
        {
            // Arrange
            var viewName = "MyView";
            var actionContext = GetActionContext();
            var expected = string.Join(
                Environment.NewLine,
                $"The view '{viewName}' was not found. The following locations were searched:",
                "Location1",
                "Location2");

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "Location1", "Location2" }))
                .Verifiable();

            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = viewName,
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
            var viewName = "MyView";
            var actionContext = GetActionContext();
            var expected = string.Join(
                Environment.NewLine,
                $"The view '{viewName}' was not found. The following locations were searched:",
                "Location1",
                "Location2");

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()))
                .Verifiable();

            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "Location1", "Location2" }))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = viewName,
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
            var viewName = "MyView";
            var actionContext = GetActionContext();
            var expected = string.Join(
                Environment.NewLine,
                $"The view '{viewName}' was not found. The following locations were searched:",
                "Location1",
                "Location2",
                "Location3",
                "Location4");

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "Location1", "Location2" }))
                .Verifiable();

            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "Location3", "Location4" }))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = viewName,
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
            var viewName = "MyView";
            var actionContext = GetActionContext();

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
                .Returns($"{viewName}.cshtml");

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()))
                .Verifiable();

            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found(viewName, view.Object))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await viewResult.ExecuteResultAsync(actionContext);

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
            var options = Options.Create(new MvcViewOptions());

            var viewExecutor = new PartialViewResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                new CompositeViewEngine(options),
                new TempDataDictionaryFactory(new SessionStateTempDataProvider()),
                new DiagnosticListener("Microsoft.AspNetCore"),
                NullLoggerFactory.Instance,
                new EmptyModelMetadataProvider());

            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<PartialViewResult>>(viewExecutor);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services.BuildServiceProvider();
            return httpContext;
        }
    }
}
