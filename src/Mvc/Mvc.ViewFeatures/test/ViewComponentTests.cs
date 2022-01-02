// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class ViewComponentTests
{
    [Fact]
    public void ViewComponent_ViewBag_UsesViewData()
    {
        // Arrange
        var viewComponent = new TestViewComponent();

        // Act
        viewComponent.ViewBag.A = "Alice";
        viewComponent.ViewBag.B = "Bob";

        // Assert
        Assert.Equal(2, viewComponent.ViewData.Count);
        Assert.Equal("Alice", viewComponent.ViewData["A"]);
        Assert.Equal("Bob", viewComponent.ViewData["B"]);
    }

    [Fact]
    public void ViewComponent_ViewData_StoresDataForViewBag()
    {
        // Arrange
        var viewComponent = new TestViewComponent();

        // Act
        viewComponent.ViewData["A"] = "Alice";
        viewComponent.ViewData["B"] = "Bob";

        // Assert
        Assert.Equal(2, viewComponent.ViewData.Count);
        Assert.Equal("Alice", viewComponent.ViewBag.A);
        Assert.Equal("Bob", viewComponent.ViewBag.B);
    }

    [Fact]
    public void ViewComponent_Content_SetsResultContentAndEncodedContent()
    {
        // Arrange
        var viewComponent = new TestViewComponent();
        var expectedContent = "TestContent&";
        var expectedEncodedContent = new HtmlString(HtmlEncoder.Default.Encode(expectedContent));

        // Act
        var actualResult = viewComponent.Content(expectedContent);

        // Assert
        Assert.IsType<ContentViewComponentResult>(actualResult);
        Assert.Same(expectedContent, actualResult.Content);
    }

    [Fact]
    public void ViewComponent_View_WithEmptyParameter_SetsResultViewWithDefaultViewName()
    {
        // Arrange
        var viewComponent = new TestViewComponent();

        // Act
        var actualResult = viewComponent.View();

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Null(actualResult.ViewData.Model);
        Assert.Null(actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_View_WithEmptyParameter_SetsViewDataModelWithDefaultViewName()
    {
        // Arrange
        var viewComponent = new TestViewComponent();
        var model = new object();
        viewComponent.ViewData.Model = model;

        // Act
        var actualResult = viewComponent.View();

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Same(model, actualResult.ViewData.Model);
        Assert.Null(actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_View_WithViewNameParameter_SetsResultViewWithCustomViewName()
    {
        // Arrange
        var viewComponent = new TestViewComponent();

        // Act
        var actualResult = viewComponent.View("CustomViewName");

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Null(actualResult.ViewData.Model);
        Assert.Equal("CustomViewName", actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_View_WithModelParameter_SetsResultViewWithDefaultViewNameAndModel()
    {
        // Arrange
        var viewComponent = new TestViewComponent();

        var model = new object();

        // Act
        var actualResult = viewComponent.View(model);

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Same(model, actualResult.ViewData.Model);
        Assert.Null(actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_View_WithNullModelParameter_SetsResultViewWithDefaultViewNameAndNullModel()
    {
        // Arrange
        var viewComponent = new TestViewComponent();
        viewComponent.ViewData.Model = new object();
        object model = null;

        // Act
        var actualResult = viewComponent.View(model: model);

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Null(actualResult.ViewData.Model);
        Assert.Null(actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_View_WithViewNameAndNullModelParameter_SetsResultViewWithViewNameAndNullModel()
    {
        // Arrange
        var viewComponent = new TestViewComponent();
        viewComponent.ViewData.Model = new object();

        // Act
        var actualResult = viewComponent.View<object>("CustomViewName", model: null);

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Null(actualResult.ViewData.Model);
        Assert.Equal("CustomViewName", actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_View_WithViewNameAndNonObjectNullModelParameter_SetsResultViewWithViewNameAndNullModel()
    {
        // Arrange
        var viewComponent = new TestViewComponent();
        viewComponent.ViewData.Model = "Hello World!";

        // Act
        var actualResult = viewComponent.View<string>("CustomViewName", model: null);

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.IsType<ViewDataDictionary<string>>(actualResult.ViewData);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<string>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Null(actualResult.ViewData.Model);
        Assert.Equal("CustomViewName", actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_View_WithViewNameAndModelParameters_SetsResultViewWithCustomViewNameAndModel()
    {
        // Arrange
        var viewComponent = new TestViewComponent();

        var model = new object();

        // Act
        var actualResult = viewComponent.View("CustomViewName", model);

        // Assert
        Assert.IsType<ViewViewComponentResult>(actualResult);
        Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
        Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
        Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
        Assert.Same(model, actualResult.ViewData.Model);
        Assert.Equal("CustomViewName", actualResult.ViewName);
    }

    [Fact]
    public void ViewComponent_ViewContext_ViewData_ReturnsDefaultInstanceIfNull()
    {
        // Arrange && Act
        var viewComponent = new TestViewComponent();

        // Assert
        // ViewComponent.ViewContext returns the default instance for the unit test scenarios
        Assert.NotNull(viewComponent.ViewContext);
        Assert.NotNull(viewComponent.ViewContext.ViewData);
        Assert.Null(viewComponent.ViewContext.TempData);

        // ViewComponent.ViewData returns the default instance for the unit test scenarios
        Assert.Empty(viewComponent.ViewContext.ViewData);
        Assert.NotNull(viewComponent.ViewData);
        Assert.Empty(viewComponent.ViewData);
        Assert.Same(viewComponent.ViewData, viewComponent.ViewContext.ViewData);
    }

    [Fact]
    public void ViewComponent_ViewContext_TempData_ReturnsDefaultInstanceIfSessionActive()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<ISessionFeature>(new SessionFeature() { Session = new TestSession() });
        var viewContext = new ViewContext();
        viewContext.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewComponentContext = new ViewComponentContext();
        viewComponentContext.ViewContext = viewContext;

        // Act
        var viewComponent = new TestViewComponent();
        viewComponent.ViewComponentContext = viewComponentContext;

        // Assert
        Assert.NotNull(viewComponent.ViewContext.TempData);
        Assert.Empty(viewComponent.ViewContext.TempData);
        Assert.Same(viewComponent.TempData, viewComponent.ViewContext.TempData);
    }

    private class TestViewComponent : ViewComponent
    {
    }

    private class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; }
    }

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _innerDictionary = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys { get { return _innerDictionary.Keys; } }

        public string Id => "TestId";

        public bool IsAvailable { get; } = true;

        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public void Clear()
        {
            _innerDictionary.Clear();
        }

        public void Remove(string key)
        {
            _innerDictionary.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _innerDictionary[key] = value.ToArray();
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }
    }
}
