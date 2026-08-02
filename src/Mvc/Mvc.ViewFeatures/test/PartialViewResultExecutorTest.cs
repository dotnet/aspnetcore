// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class PartialViewResultExecutorTest
{
    [Fact]
    public void FindView_UsesViewEngine_FromPartialViewResult()
    {
        // Arrange
        var context = GetActionContext();
        var executor = GetViewExecutor();

        var viewName = "my-view";
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(e => e.GetView(/*executingFilePath*/ null, viewName, /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(e => e.FindView(context, viewName, /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
            .Verifiable();

        var viewResult = new PartialViewResult
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
        var viewName = "some-view-name";
        var context = GetActionContext(viewName);
        var executor = GetViewExecutor();

        var viewResult = new PartialViewResult
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
    [ReplaceCulture("de-CH", "de-CH")]
    public void FindView_UsesActionDescriptorName_IfViewNameIsNull_UsesInvariantCulture()
    {
        // Arrange
        var viewName = "10/31/2018 07:37:38 -07:00";
        var context = GetActionContext(viewName);
        context.RouteData.Values["action"] = new DateTimeOffset(2018, 10, 31, 7, 37, 38, TimeSpan.FromHours(-7));

        var executor = GetViewExecutor();

        var viewResult = new PartialViewResult
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
    public void FindView_ReturnsExpectedNotFoundResult_WithGetViewLocations()
    {
        // Arrange
        var expectedLocations = new[] { "location1", "location2" };
        var context = GetActionContext();
        var executor = GetViewExecutor();

        var viewName = "myview";
        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", expectedLocations))
            .Verifiable();
        viewEngine
            .Setup(e => e.FindView(context, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", Enumerable.Empty<string>()));

        var viewResult = new PartialViewResult
        {
            ViewName = viewName,
            ViewEngine = viewEngine.Object,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            TempData = Mock.Of<ITempDataDictionary>(),
        };

        // Act
        var result = executor.FindView(context, viewResult);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.View);
        Assert.Equal(expectedLocations, result.SearchedLocations);
    }

    [Fact]
    public void FindView_ReturnsExpectedNotFoundResult_WithFindViewLocations()
    {
        // Arrange
        var expectedLocations = new[] { "location1", "location2" };
        var context = GetActionContext();
        var executor = GetViewExecutor();

        var viewName = "myview";
        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(e => e.FindView(context, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", expectedLocations));

        var viewResult = new PartialViewResult
        {
            ViewName = viewName,
            ViewEngine = viewEngine.Object,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            TempData = Mock.Of<ITempDataDictionary>(),
        };

        // Act
        var result = executor.FindView(context, viewResult);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.View);
        Assert.Equal(expectedLocations, result.SearchedLocations);
    }

    [Fact]
    public void FindView_ReturnsExpectedNotFoundResult_WithAllLocations()
    {
        // Arrange
        var expectedLocations = new[] { "location1", "location2", "location3", "location4" };
        var context = GetActionContext();
        var executor = GetViewExecutor();

        var viewName = "myview";
        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", new[] { "location1", "location2" }))
            .Verifiable();
        viewEngine
            .Setup(e => e.FindView(context, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", new[] { "location3", "location4" }));

        var viewResult = new PartialViewResult
        {
            ViewName = viewName,
            ViewEngine = viewEngine.Object,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            TempData = Mock.Of<ITempDataDictionary>(),
        };

        // Act
        var result = executor.FindView(context, viewResult);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.View);
        Assert.Equal(expectedLocations, result.SearchedLocations);
    }

    [Fact]
    public void FindView_WritesDiagnostic_ViewFound()
    {
        // Arrange
        var diagnosticListener = new DiagnosticListener("Test");
        var listener = new TestDiagnosticListener();
        diagnosticListener.SubscribeWithAdapter(listener);

        var context = GetActionContext();
        var executor = GetViewExecutor(diagnosticListener);

        var viewName = "myview";
        var viewResult = new PartialViewResult
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
        Assert.False(listener.ViewFound.IsMainPage);
        Assert.Equal("myview", listener.ViewFound.ViewName);
    }

    [Fact]
    public void FindView_WritesDiagnostic_ViewNotFound()
    {
        // Arrange
        var diagnosticListener = new DiagnosticListener("Test");
        var listener = new TestDiagnosticListener();
        diagnosticListener.SubscribeWithAdapter(listener);

        var context = GetActionContext();
        var executor = GetViewExecutor(diagnosticListener);

        var viewName = "myview";
        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(e => e.FindView(context, "myview", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("myview", new string[] { "location/myview" }));

        var viewResult = new PartialViewResult
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
    public async Task ExecuteAsync_UsesContentType_FromPartialViewResult()
    {
        // Arrange
        var context = GetActionContext();
        var executor = GetViewExecutor();

        var contentType = "application/x-my-content-type";

        var viewResult = new PartialViewResult
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
        Assert.Null(MediaType.GetEncoding(contentType));
    }

    [Fact]
    public async Task ExecuteAsync_UsesStatusCode_FromPartialViewResult()
    {
        // Arrange
        var context = GetActionContext();
        var executor = GetViewExecutor();

        var contentType = MediaTypeHeaderValue.Parse("application/x-my-content-type");

        var viewResult = new PartialViewResult
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

    private ActionContext GetActionContext(string actionName = null)
    {
        var routeData = new RouteData();
        routeData.Values["action"] = actionName;

        return new ActionContext(new DefaultHttpContext(), routeData, new ControllerActionDescriptor() { ActionName = actionName });
    }

    private PartialViewResultExecutor GetViewExecutor(DiagnosticListener diagnosticListener = null)
    {
        if (diagnosticListener == null)
        {
            diagnosticListener = new DiagnosticListener("Test");
        }

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(e => e.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns<string, string, bool>(
                (executing, name, isMainPage) => ViewEngineResult.NotFound(name, Enumerable.Empty<string>()));
        viewEngine
            .Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns<ActionContext, string, bool>(
                (context, name, isMainPage) => ViewEngineResult.Found(name, Mock.Of<IView>()));

        var options = Options.Create(new MvcViewOptions());
        options.Value.ViewEngines.Add(viewEngine.Object);

        var viewExecutor = new PartialViewResultExecutor(
            options,
            new TestHttpResponseStreamWriterFactory(),
            new CompositeViewEngine(options),
            Mock.Of<ITempDataDictionaryFactory>(),
            diagnosticListener,
            NullLoggerFactory.Instance,
            new EmptyModelMetadataProvider());

        return viewExecutor;
    }
}
