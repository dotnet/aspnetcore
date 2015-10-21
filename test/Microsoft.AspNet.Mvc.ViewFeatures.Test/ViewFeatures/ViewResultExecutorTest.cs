// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if MOCK_SUPPORT
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class ViewResultExecutorTest
    {
        [Fact]
        public void FindView_UsesViewEngine_FromViewResult()
        {
            // Arrange
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var viewName = "my-view";
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(e => e.FindView(context, viewName))
                .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
                .Verifiable();

            var viewResult = new ViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = viewName,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.Equal(viewName, viewEngineResult.ViewName);
            viewEngine.Verify();
        }

        [Fact]
        public void FindView_UsesActionDescriptorName_IfViewNameIsNull()
        {
            // Arrange
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var viewName = "some-view-name";
            context.ActionDescriptor.Name = viewName;

            var viewResult = new ViewResult
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.Equal(viewName, viewEngineResult.ViewName);
        }

        [Fact]
        public void FindView_WritesDiagnostic_ViewFound()
        {
            // Arrange
            var diagnosticSource = new DiagnosticListener("Test");
            var listener = new TestDiagnosticListener();
            diagnosticSource.SubscribeWithAdapter(listener);

            var context = GetActionContext();
            var executor = GetViewExecutor(diagnosticSource);

            var viewName = "myview";
            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.Equal(viewName, viewEngineResult.ViewName);

            Assert.NotNull(listener.ViewFound);
            Assert.NotNull(listener.ViewFound.ActionContext);
            Assert.NotNull(listener.ViewFound.Result);
            Assert.NotNull(listener.ViewFound.View);
            Assert.False(listener.ViewFound.IsPartial);
            Assert.Equal("myview", listener.ViewFound.ViewName);
        }

        [Fact]
        public void FindView_WritesDiagnostic_ViewNotFound()
        {
            // Arrange
            var diagnosticSource = new DiagnosticListener("Test");
            var listener = new TestDiagnosticListener();
            diagnosticSource.SubscribeWithAdapter(listener);

            var context = GetActionContext();
            var executor = GetViewExecutor(diagnosticSource);

            var viewName = "myview";
            var viewEngine = new Mock<IViewEngine>();
            viewEngine
                .Setup(e => e.FindView(context, "myview"))
                .Returns(ViewEngineResult.NotFound("myview", new string[] { "location/myview" }));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.False(viewEngineResult.Success);

            Assert.NotNull(listener.ViewNotFound);
            Assert.NotNull(listener.ViewNotFound.ActionContext);
            Assert.NotNull(listener.ViewNotFound.Result);
            Assert.Equal(new string[] { "location/myview" }, listener.ViewNotFound.SearchedLocations);
            Assert.Equal("myview", listener.ViewNotFound.ViewName);
        }

        [Fact]
        public async Task ExecuteAsync_UsesContentType_FromViewResult()
        {
            // Arrange
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var contentType = MediaTypeHeaderValue.Parse("application/x-my-content-type");

            var viewResult = new ViewResult
            {
                ViewName = "my-view",
                ContentType = contentType,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await executor.ExecuteAsync(context, Mock.Of<IView>(), viewResult);

            // Assert
            Assert.Equal("application/x-my-content-type", context.HttpContext.Response.ContentType);

            // Check if the original instance provided by the user has not changed.
            // Since we do not have access to the new instance created within the view executor,
            // check if at least the content is the same.
            Assert.Null(contentType.Encoding);
        }

        [Fact]
        public async Task ExecuteAsync_UsesStatusCode_FromViewResult()
        {
            // Arrange
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var contentType = MediaTypeHeaderValue.Parse("application/x-my-content-type");

            var viewResult = new ViewResult
            {
                ViewName = "my-view",
                StatusCode = 404,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await executor.ExecuteAsync(context, Mock.Of<IView>(), viewResult);

            // Assert
            Assert.Equal(404, context.HttpContext.Response.StatusCode);
        }

        private ActionContext GetActionContext()
        {
            return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        }

        private ViewResultExecutor GetViewExecutor(DiagnosticListener diagnosticSource = null)
        {
            if (diagnosticSource == null)
            {
                diagnosticSource = new DiagnosticListener("Test");
            }

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns<ActionContext, string>((_, name) => ViewEngineResult.Found(name, Mock.Of<IView>()));

            var options = new TestOptionsManager<MvcViewOptions>();
            options.Value.ViewEngines.Add(viewEngine.Object);

            var viewExecutor = new ViewResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                new CompositeViewEngine(options),
                diagnosticSource,
                NullLoggerFactory.Instance);

            return viewExecutor;
        }
    }
}
#endif
