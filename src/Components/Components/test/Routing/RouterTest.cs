// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0618 // Type or member is obsolete

using System.Reflection;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Routing;

public class RouterTest
{
    private readonly Router _router;
    private readonly TestNavigationManager _navigationManager;
    private readonly TestRenderer _renderer;

    public RouterTest()
    {
        var services = new ServiceCollection();
        _navigationManager = new TestNavigationManager();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<NavigationManager>(_navigationManager);
        services.AddSingleton<INavigationInterception, TestNavigationInterception>();
        services.AddSingleton<IScrollToLocationHash, TestScrollToLocationHash>();
        var serviceProvider = services.BuildServiceProvider();

        _renderer = new TestRenderer(serviceProvider);
        _renderer.ShouldHandleExceptions = true;
        _router = (Router)_renderer.InstantiateComponent<Router>();
        _router.AppAssembly = Assembly.GetExecutingAssembly();
        _router.Found = routeData => (builder) => builder.AddContent(0, $"Rendering route matching {routeData.PageType}");
        _renderer.AssignRootComponentId(_router);
    }

    [Fact]
    public async Task CanRunOnNavigateAsync()
    {
        // Arrange
        var called = false;
        Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
        {
            await Task.CompletedTask;
            called = true;
        };
        _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

        // Act
        await _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task CanceledFailedOnNavigateAsyncDoesNothing()
    {
        // Arrange
        var onNavigateInvoked = 0;
        Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
        {
            onNavigateInvoked += 1;
            if (args.Path.EndsWith("jan", StringComparison.Ordinal))
            {
                await Task.Delay(Timeout.Infinite, args.CancellationToken);
                throw new Exception("This is an uncaught exception.");
            }
        };
        var refreshCalled = 0;
        _renderer.OnUpdateDisplay = (renderBatch) =>
        {
            refreshCalled += 1;
            return;
        };
        _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

        // Act
        var janTask = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));
        var febTask = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/feb", false));

        await janTask;
        await febTask;

        // Assert that we render the second route component and don't throw an exception
        Assert.Empty(_renderer.HandledExceptions);
        Assert.Equal(2, onNavigateInvoked);
        Assert.Equal(2, refreshCalled);
    }

    [Fact]
    public async Task AlreadyCanceledOnNavigateAsyncDoesNothing()
    {
        // Arrange
        var triggerCancel = new TaskCompletionSource();
        Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
        {
            if (args.Path.EndsWith("jan", StringComparison.Ordinal))
            {
                var tcs = new TaskCompletionSource();
                await triggerCancel.Task;
                tcs.TrySetCanceled();
                await tcs.Task;
            }
        };
        var refreshCalled = false;
        _renderer.OnUpdateDisplay = (renderBatch) =>
        {
            if (!refreshCalled)
            {
                Assert.True(true);
                return;
            }
            Assert.Fail("OnUpdateDisplay called more than once.");
        };
        _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

        // Act (start the operations then await them)
        var jan = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));
        var feb = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/feb", false));
        triggerCancel.TrySetResult();

        await jan;
        await feb;
    }

    [Fact]
    public void CanCancelPreviousOnNavigateAsync()
    {
        // Arrange
        var cancelled = "";
        Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
        {
            await Task.CompletedTask;
            args.CancellationToken.Register(() => cancelled = args.Path);
        };
        _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

        // Act
        _ = _router.RunOnNavigateAsync("jan", false);
        _ = _router.RunOnNavigateAsync("feb", false);

        // Assert
        var expected = "jan";
        Assert.Equal(expected, cancelled);
    }

    [Fact]
    public async Task RefreshesOnceOnCancelledOnNavigateAsync()
    {
        // Arrange
        Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
        {
            if (args.Path.EndsWith("jan", StringComparison.Ordinal))
            {
                await Task.Delay(Timeout.Infinite, args.CancellationToken);
            }
        };
        var refreshCalled = false;
        _renderer.OnUpdateDisplay = (renderBatch) =>
        {
            if (!refreshCalled)
            {
                Assert.True(true);
                return;
            }
            Assert.Fail("OnUpdateDisplay called more than once.");
        };
        _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

        // Act
        var jan = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));
        var feb = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/feb", false));

        await jan;
        await feb;
    }

    [Fact]
    public async Task UsesCurrentRouteMatchingIfSpecified()
    {
        // Arrange
        // Current routing prefers exactly-matched patterns over {*someWildcard}, no matter
        // how many segments are in the exact match
        _navigationManager.NotifyLocationChanged("https://www.example.com/subdir/a/b/c", false);
        var parameters = new Dictionary<string, object>
            {
                { nameof(Router.AppAssembly), typeof(RouterTest).Assembly },
                { nameof(Router.NotFound), (RenderFragment)(builder => { }) },
            };

        // Act
        await _renderer.Dispatcher.InvokeAsync(() =>
            _router.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        // Assert
        var renderedFrame = _renderer.Batches.First().ReferenceFrames.First();
        Assert.Equal(RenderTreeFrameType.Text, renderedFrame.FrameType);
        Assert.Equal($"Rendering route matching {typeof(MultiSegmentRouteComponent)}", renderedFrame.TextContent);
    }

    [Fact]
    public async Task SetParametersAsyncRefreshesOnce()
    {
        //Arrange
        var parameters = new Dictionary<string, object>
            {
                { nameof(Router.AppAssembly), typeof(RouterTest).Assembly },
                { nameof(Router.NotFound), (RenderFragment)(builder => { }) },
            };

        var refreshCalled = 0;
        _renderer.OnUpdateDisplay = (renderBatch) =>
        {
            refreshCalled += 1;
            return;
        };

        // Act
        await _renderer.Dispatcher.InvokeAsync(() =>
            _router.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        //Assert
        Assert.Equal(1, refreshCalled);
    }

    [Fact]
    public async Task UsesNotFoundContentIfSpecified()
    {
        // Arrange
        _navigationManager.NotifyLocationChanged("https://www.example.com/subdir/nonexistent", false);
        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly },
            { nameof(Router.NotFound), (RenderFragment)(builder => builder.AddContent(0, "Custom content")) },
        };

        // Act
        await _renderer.Dispatcher.InvokeAsync(() =>
            _router.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        // Assert
        var renderedFrame = _renderer.Batches.First().ReferenceFrames.First();
        Assert.Equal(RenderTreeFrameType.Text, renderedFrame.FrameType);
        Assert.Equal("Custom content", renderedFrame.TextContent);
    }

    [Fact]
    public async Task UsesDefaultNotFoundContentIfNotSpecified()
    {
        // Arrange
        _navigationManager.NotifyLocationChanged("https://www.example.com/subdir/nonexistent", false);
        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly }
        };

        // Act
        await _renderer.Dispatcher.InvokeAsync(() =>
            _router.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        // Assert
        var renderedFrame = _renderer.Batches.First().ReferenceFrames.First();
        Assert.Equal(RenderTreeFrameType.Text, renderedFrame.FrameType);
        Assert.Equal("Not found", renderedFrame.TextContent);
    }

    [Fact]
    public async Task ThrowsExceptionWhenBothNotFoundAndNotFoundPageAreSet()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<NavigationManager>(_navigationManager);
        services.AddSingleton<INavigationInterception, TestNavigationInterception>();
        services.AddSingleton<IScrollToLocationHash, TestScrollToLocationHash>();
        var serviceProvider = services.BuildServiceProvider();

        var renderer = new TestRenderer(serviceProvider);
        renderer.ShouldHandleExceptions = true;
        var router = (Router)renderer.InstantiateComponent<Router>();
        router.AppAssembly = Assembly.GetExecutingAssembly();
        router.Found = routeData => (builder) => builder.AddContent(0, $"Rendering route matching {routeData.PageType}");
        renderer.AssignRootComponentId(router);

        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly },
            { nameof(Router.NotFound), (RenderFragment)(builder => builder.AddContent(0, "Custom not found")) },
            { nameof(Router.NotFoundPage), typeof(NotFoundTestComponent) }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await renderer.Dispatcher.InvokeAsync(() =>
                router.SetParametersAsync(ParameterView.FromDictionary(parameters))));

        Assert.Contains("Setting NotFound and NotFoundPage properties simultaneously is not supported", exception.Message);
        Assert.Contains("Use either NotFound or NotFoundPage", exception.Message);
    }

    [Fact]
    public async Task OnNotFound_WithNotFoundPageSet_UsesNotFoundPage()
    {
        // Create a new router instance for this test to control Attach() timing
        var services = new ServiceCollection();
        var testNavManager = new TestNavigationManager();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<NavigationManager>(testNavManager);
        services.AddSingleton<INavigationInterception, TestNavigationInterception>();
        services.AddSingleton<IScrollToLocationHash, TestScrollToLocationHash>();
        var serviceProvider = services.BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        testRenderer.ShouldHandleExceptions = true;
        var testRouter = (Router)testRenderer.InstantiateComponent<Router>();
        testRouter.AppAssembly = Assembly.GetExecutingAssembly();
        testRouter.Found = routeData => (builder) => builder.AddContent(0, $"Rendering route matching {routeData.PageType}");

        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly },
            { nameof(Router.NotFoundPage), typeof(NotFoundTestComponent) }
        };

        // Assign the root component ID which will call Attach()
        testRenderer.AssignRootComponentId(testRouter);

        // Act
        await testRenderer.Dispatcher.InvokeAsync(() =>
            testRouter.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        // Trigger the NavigationManager's OnNotFound event
        await testRenderer.Dispatcher.InvokeAsync(() => testNavManager.TriggerNotFound());

        // Assert
        var lastBatch = testRenderer.Batches.Last();
        var renderedFrame = lastBatch.ReferenceFrames.First();
        Assert.Equal(RenderTreeFrameType.Component, renderedFrame.FrameType);
        Assert.Equal(typeof(RouteView), renderedFrame.ComponentType);

        // Verify that the RouteData contains the NotFoundTestComponent
        var routeViewFrame = lastBatch.ReferenceFrames.Skip(1).First();
        Assert.Equal(RenderTreeFrameType.Attribute, routeViewFrame.FrameType);
        var routeData = (RouteData)routeViewFrame.AttributeValue;
        Assert.Equal(typeof(NotFoundTestComponent), routeData.PageType);
    }

    [Fact]
    public async Task OnNotFound_WithArgsPathSet_RendersComponentByRoute()
    {
        // Create a new router instance for this test to control Attach() timing
        var services = new ServiceCollection();
        var testNavManager = new TestNavigationManager();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<NavigationManager>(testNavManager);
        services.AddSingleton<INavigationInterception, TestNavigationInterception>();
        services.AddSingleton<IScrollToLocationHash, TestScrollToLocationHash>();
        var serviceProvider = services.BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        testRenderer.ShouldHandleExceptions = true;
        var testRouter = (Router)testRenderer.InstantiateComponent<Router>();
        testRouter.AppAssembly = Assembly.GetExecutingAssembly();
        testRouter.Found = routeData => (builder) => builder.AddContent(0, $"Rendering route matching {routeData.PageType}");

        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly }
        };

        // Subscribe to OnNotFound event BEFORE router attaches and set args.Path
        testNavManager.OnNotFound += (sender, args) =>
        {
            args.Path = "/jan"; // Point to an existing route
        };

        // Assign the root component ID which will call Attach()
        testRenderer.AssignRootComponentId(testRouter);

        // Act
        await testRenderer.Dispatcher.InvokeAsync(() =>
            testRouter.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        // Trigger the NavigationManager's OnNotFound event
        await testRenderer.Dispatcher.InvokeAsync(() => testNavManager.TriggerNotFound());

        // Assert
        var lastBatch = testRenderer.Batches.Last();
        var renderedFrame = lastBatch.ReferenceFrames.First();
        Assert.Equal(RenderTreeFrameType.Component, renderedFrame.FrameType);
        Assert.Equal(typeof(RouteView), renderedFrame.ComponentType);

        // Verify that the RouteData contains the correct component type
        var routeViewFrame = lastBatch.ReferenceFrames.Skip(1).First();
        Assert.Equal(RenderTreeFrameType.Attribute, routeViewFrame.FrameType);
        var routeData = (RouteData)routeViewFrame.AttributeValue;
        Assert.Equal(typeof(JanComponent), routeData.PageType);
    }

    [Fact]
    public async Task OnNotFound_WithBothNotFoundPageAndArgsPath_PreferArgs()
    {
        // Create a new router instance for this test to control Attach() timing
        var services = new ServiceCollection();
        var testNavManager = new TestNavigationManager();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<NavigationManager>(testNavManager);
        services.AddSingleton<INavigationInterception, TestNavigationInterception>();
        services.AddSingleton<IScrollToLocationHash, TestScrollToLocationHash>();
        var serviceProvider = services.BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        testRenderer.ShouldHandleExceptions = true;
        var testRouter = (Router)testRenderer.InstantiateComponent<Router>();
        testRouter.AppAssembly = Assembly.GetExecutingAssembly();
        testRouter.Found = routeData => (builder) => builder.AddContent(0, $"Rendering route matching {routeData.PageType}");

        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly },
            { nameof(Router.NotFoundPage), typeof(NotFoundTestComponent) }
        };

        // Subscribe to OnNotFound event BEFORE router attaches and sets up its own subscription
        testNavManager.OnNotFound += (sender, args) =>
        {
            args.Path = "/jan"; // This should take precedence over NotFoundPage
        };

        // Now assign the root component ID which will call Attach()
        testRenderer.AssignRootComponentId(testRouter);

        await testRenderer.Dispatcher.InvokeAsync(() =>
            testRouter.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        // trigger the NavigationManager's OnNotFound event
        await testRenderer.Dispatcher.InvokeAsync(() => testNavManager.TriggerNotFound());

        // The Router should have rendered using RenderComponentByRoute (args.Path) instead of NotFoundPage
        var lastBatch = testRenderer.Batches.Last();
        var renderedFrame = lastBatch.ReferenceFrames.First();
        Assert.Equal(RenderTreeFrameType.Component, renderedFrame.FrameType);
        Assert.Equal(typeof(RouteView), renderedFrame.ComponentType);

        // Verify that the RouteData contains the JanComponent (from args.Path), not NotFoundTestComponent
        var routeViewFrame = lastBatch.ReferenceFrames.Skip(1).First();
        Assert.Equal(RenderTreeFrameType.Attribute, routeViewFrame.FrameType);
        var routeData = (RouteData)routeViewFrame.AttributeValue;
        Assert.Equal(typeof(JanComponent), routeData.PageType);
    }

    [Fact]
    public async Task FindComponentTypeByRoute_WithValidRoute_ReturnsComponentType()
    {
        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly }
        };

        await _renderer.Dispatcher.InvokeAsync(() =>
            _router.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        var result = _router.FindComponentTypeByRoute("/jan");
        Assert.Equal(typeof(JanComponent), result);
    }

    [Fact]
    public async Task RenderComponentByRoute_WithInvalidRoute_ThrowsException()
    {
        var parameters = new Dictionary<string, object>
        {
            { nameof(Router.AppAssembly), typeof(RouterTest).Assembly }
        };

        await _renderer.Dispatcher.InvokeAsync(() =>
            _router.SetParametersAsync(ParameterView.FromDictionary(parameters)));

        var builder = new RenderTreeBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _router.RenderComponentByRoute(builder, "/nonexistent-route");
        });
        Assert.Contains("No component found for route '/nonexistent-route'", exception.Message);
    }

    internal class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() =>
            Initialize("https://www.example.com/subdir/", "https://www.example.com/subdir/jan");

        public void NotifyLocationChanged(string uri, bool intercepted, string state = null)
        {
            Uri = uri;
            NotifyLocationChanged(intercepted);
        }

        public void TriggerNotFound()
        {
            base.NotFound();
        }
    }

    internal sealed class TestNavigationInterception : INavigationInterception
    {
        public static readonly TestNavigationInterception Instance = new TestNavigationInterception();

        public Task EnableNavigationInterceptionAsync()
        {
            return Task.CompletedTask;
        }
    }

    internal sealed class TestScrollToLocationHash : IScrollToLocationHash
    {
        public Task RefreshScrollPositionForHash(string locationAbsolute)
        {
            return Task.CompletedTask;
        }
    }

    [Route("feb")]
    public class FebComponent : ComponentBase { }

    [Route("jan")]
    public class JanComponent : ComponentBase { }

    [Route("a/{*matchAnything}")]
    public class MatchAnythingComponent : ComponentBase { }

    [Route("a/b/c")]
    public class MultiSegmentRouteComponent : ComponentBase { }

    [Route("not-found")]
    public class NotFoundTestComponent : ComponentBase { }

}
