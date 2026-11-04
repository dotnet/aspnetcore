// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class ViewViewComponentResultTest
{
    private readonly ITempDataDictionary _tempDataDictionary =
        new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

    [Fact]
    public void Execute_RendersPartialViews()
    {
        // Arrange
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Returns(Task.FromResult(result: true))
            .Verifiable();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Components/Invoke/some-view", view.Object))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view.Object, viewData);

        // Act
        result.Execute(viewComponentContext);

        // Assert
        viewEngine.Verify();
        view.Verify();
    }

    [Fact]
    public void Execute_ResolvesView_WithDefaultAsViewName()
    {
        // Arrange
        var view = new Mock<IView>(MockBehavior.Strict);
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Returns(Task.FromResult(result: true))
            .Verifiable();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/Default", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Components/Invoke/Default", view.Object))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view.Object, viewData);

        // Act
        result.Execute(viewComponentContext);

        // Assert
        viewEngine.Verify();
        view.Verify();
    }

    [Fact]
    public void Execute_ResolvesView_AndWritesDiagnosticListener()
    {
        // Arrange
        var view = new Mock<IView>(MockBehavior.Strict);
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Returns(Task.FromResult(result: true))
            .Verifiable();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/Default", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Components/Invoke/Default", view.Object))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var adapter = new TestDiagnosticListener();

        var viewComponentContext = GetViewComponentContext(view.Object, viewData, adapter);

        // Act
        result.Execute(viewComponentContext);

        // Assert
        viewEngine.Verify();
        view.Verify();

        Assert.NotNull(adapter.ViewComponentBeforeViewExecute?.ActionDescriptor);
        Assert.NotNull(adapter.ViewComponentBeforeViewExecute?.ViewComponentContext);
        Assert.NotNull(adapter.ViewComponentBeforeViewExecute?.View);
        Assert.NotNull(adapter.ViewComponentAfterViewExecute?.ActionDescriptor);
        Assert.NotNull(adapter.ViewComponentAfterViewExecute?.ViewComponentContext);
        Assert.NotNull(adapter.ViewComponentAfterViewExecute?.View);
    }

    [Fact]
    public void Execute_ThrowsIfPartialViewCannotBeFound_MessageUsesGetViewLocations()
    {
        // Arrange
        var expected = string.Join(Environment.NewLine,
            "The view 'Components/Invoke/some-view' was not found. The following locations were searched:",
            "location1",
            "location2");

        var view = Mock.Of<IView>();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", new[] { "location1", "location2" }))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("Components/Invoke/some-view", Enumerable.Empty<string>()))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view, viewData);

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => result.Execute(viewComponentContext));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void Execute_ThrowsIfPartialViewCannotBeFound_MessageUsesFindViewLocations()
    {
        // Arrange
        var expected = string.Join(Environment.NewLine,
            "The view 'Components/Invoke/some-view' was not found. The following locations were searched:",
            "location1",
            "location2");

        var view = Mock.Of<IView>();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("Components/Invoke/some-view", new[] { "location1", "location2" }))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view, viewData);

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => result.Execute(viewComponentContext));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void Execute_ThrowsIfPartialViewCannotBeFound_MessageUsesAllLocations()
    {
        // Arrange
        var expected = string.Join(Environment.NewLine,
            "The view 'Components/Invoke/some-view' was not found. The following locations were searched:",
            "location1",
            "location2",
            "location3",
            "location4");

        var view = Mock.Of<IView>();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", new[] { "location1", "location2" }))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("Components/Invoke/some-view", new[] { "location3", "location4" }))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view, viewData);

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => result.Execute(viewComponentContext));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void Execute_DoesNotWrapThrownExceptionsInAggregateExceptions()
    {
        // Arrange
        var expected = new ArgumentOutOfRangeException();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Throws(expected)
            .Verifiable();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Components/Invoke/some-view", view.Object))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

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
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Components/Invoke/some-view", view))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view, viewData);

        // Act
        await result.ExecuteAsync(viewComponentContext);

        // Assert
        viewEngine.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesViewEngineFromServiceProvider_IfNoViewEngineIsExplicitlyProvided()
    {
        // Arrange
        var view = Mock.Of<IView>();

        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Components/Invoke/some-view", view))
            .Verifiable();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(p => p.GetService(typeof(ICompositeViewEngine)))
            .Returns(viewEngine.Object);
        serviceProvider.Setup(p => p.GetService(typeof(DiagnosticListener)))
            .Returns(new DiagnosticListener("Test"));

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view, viewData);
        viewComponentContext.ViewContext.HttpContext.RequestServices = serviceProvider.Object;

        // Act
        await result.ExecuteAsync(viewComponentContext);

        // Assert
        viewEngine.Verify();
        serviceProvider.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsIfPartialViewCannotBeFound()
    {
        // Arrange
        var expected = string.Join(Environment.NewLine,
            "The view 'Components/Invoke/some-view' was not found. The following locations were searched:",
            "view-location1",
            "view-location2",
            "view-location3",
            "view-location4");

        var view = Mock.Of<IView>();

        var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, "some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("some-view", new[] { "view-location1", "view-location2" }))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "Components/Invoke/some-view", /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(
                "Components/Invoke/some-view",
                new[] { "view-location3", "view-location4" }))
            .Verifiable();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view, viewData);

        // Act and Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => result.ExecuteAsync(viewComponentContext));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_IfNoViewEngineCanBeResolved()
    {
        // Arrange
        var expected = $"No service for type '{typeof(ICompositeViewEngine).FullName}'" +
            " has been registered.";

        var view = Mock.Of<IView>();

        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());

        var result = new ViewViewComponentResult
        {
            ViewName = "some-view",
            ViewData = viewData,
            TempData = _tempDataDictionary,
        };

        var viewComponentContext = GetViewComponentContext(view, viewData);
        viewComponentContext.ViewContext.HttpContext.RequestServices = serviceProvider;

        // Act and Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => result.ExecuteAsync(viewComponentContext));
        Assert.Equal(expected, ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Execute_CallsFindView_WithExpectedPath_WhenViewNameIsNullOrEmpty(string viewName)
    {
        // Arrange
        var shortName = "SomeShortName";
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        var componentContext = GetViewComponentContext(new Mock<IView>().Object, viewData);
        componentContext.ViewComponentDescriptor.ShortName = shortName;
        var expectedViewName = $"Components/{shortName}/Default";
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), expectedViewName, /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found(expectedViewName, new Mock<IView>().Object))
            .Verifiable();

        var componentResult = new ViewViewComponentResult();
        componentResult.ViewEngine = viewEngine.Object;
        componentResult.ViewData = viewData;
        componentResult.ViewName = viewName;
        componentResult.TempData = _tempDataDictionary;

        // Act & Assert
        componentResult.Execute(componentContext);
        viewEngine.Verify();
    }

    [Theory]
    [InlineData("~/Home/Index/MyViewComponent1.cshtml")]
    [InlineData("~MyViewComponent2.cshtml")]
    [InlineData("/MyViewComponent3.cshtml")]
    public void Execute_CallsFindView_WithExpectedPath_WhenViewNameIsSpecified(string viewName)
    {
        // Arrange
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, viewName, /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found(viewName, new Mock<IView>().Object))
            .Verifiable();
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        var componentContext = GetViewComponentContext(new Mock<IView>().Object, viewData);
        var componentResult = new ViewViewComponentResult
        {
            ViewEngine = viewEngine.Object,
            ViewData = viewData,
            ViewName = viewName,
            TempData = _tempDataDictionary,
        };

        // Act & Assert
        componentResult.Execute(componentContext);
        viewEngine.Verify();
    }

    private static ViewComponentContext GetViewComponentContext(
        IView view,
        ViewDataDictionary viewData,
        object diagnosticListener = null)
    {
        var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
        if (diagnosticListener == null)
        {
            diagnosticListener = new TestDiagnosticListener();
        }

        diagnosticSource.SubscribeWithAdapter(diagnosticListener);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(s => s.GetService(typeof(DiagnosticListener))).Returns(diagnosticSource);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider.Object;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewContext = new ViewContext(
            actionContext,
            view,
            viewData,
            new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            TextWriter.Null,
            new HtmlHelperOptions());

        var viewComponentDescriptor = new ViewComponentDescriptor()
        {
            ShortName = "Invoke",
            TypeInfo = typeof(object).GetTypeInfo(),
            MethodInfo = typeof(object).GetTypeInfo().DeclaredMethods.First()
        };

        var viewComponentContext = new ViewComponentContext(
            viewComponentDescriptor,
            new Dictionary<string, object>(),
            new HtmlTestEncoder(),
            viewContext,
            TextWriter.Null);

        return viewComponentContext;
    }
}
