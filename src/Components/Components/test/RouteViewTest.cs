// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Test;

public class RouteViewTest
{
    private readonly TestRenderer _renderer;
    private readonly RouteViewTestNavigationManager _navigationManager;
    private readonly RouteView _routeViewComponent;
    private readonly int _routeViewComponentId;

    public RouteViewTest()
    {
        var serviceCollection = new ServiceCollection();
        _navigationManager = new RouteViewTestNavigationManager();
        serviceCollection.AddSingleton<NavigationManager>(_navigationManager);
        var services = serviceCollection.BuildServiceProvider();
        _renderer = new TestRenderer(services);

        var componentFactory = new ComponentFactory(new DefaultComponentActivator(services), new DefaultComponentPropertyActivator(), _renderer);
        _routeViewComponent = (RouteView)componentFactory.InstantiateComponent(services, typeof(RouteView), null, null);

        _routeViewComponentId = _renderer.AssignRootComponentId(_routeViewComponent);
    }

    [Fact]
    public void ThrowsIfNoRouteDataSupplied()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            // Throws synchronously, so no need to await
            _ = _routeViewComponent.SetParametersAsync(ParameterView.Empty);
        });

        Assert.Equal($"The {nameof(RouteView)} component requires a non-null value for the parameter {nameof(RouteView.RouteData)}.", ex.Message);
    }

    [Fact]
    public void RendersPageInsideLayoutView()
    {
        // Arrange
        var routeParams = new Dictionary<string, object>
            {
                { nameof(ComponentWithLayout.Message), "Test message" }
            };
        var routeData = new RouteData(typeof(ComponentWithLayout), routeParams);

        // Act
        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
            })));

        // Assert: RouteView renders LayoutView
        var batch = _renderer.Batches.Single();
        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)typeof(TestLayout), sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));

        // Assert: LayoutView renders TestLayout
        var layoutViewComponentId = batch.GetComponentFrames<LayoutView>().Single().ComponentId;
        var layoutViewFrames = _renderer.GetCurrentRenderTreeFrames(layoutViewComponentId).AsEnumerable();
        Assert.Collection(layoutViewFrames,
            frame => AssertFrame.Component<TestLayout>(frame, subtreeLength: 2, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutComponentBase.Body), sequence: 1));

        // Assert: TestLayout renders page
        var testLayoutComponentId = batch.GetComponentFrames<TestLayout>().Single().ComponentId;
        var testLayoutFrames = _renderer.GetCurrentRenderTreeFrames(testLayoutComponentId).AsEnumerable();
        Assert.Collection(testLayoutFrames,
            frame => AssertFrame.Text(frame, "Layout starts here", sequence: 0),
            frame => AssertFrame.Region(frame, subtreeLength: 4),
            frame => AssertFrame.Region(frame, subtreeLength: 3),
            frame => AssertFrame.Component<ComponentWithLayout>(frame, sequence: 0, subtreeLength: 2),
            frame => AssertFrame.Attribute(frame, nameof(ComponentWithLayout.Message), "Test message", sequence: 1),
            frame => AssertFrame.Text(frame, "Layout ends here", sequence: 2));

        // Assert: page itself is rendered, having received parameters from the original route data
        var pageComponentId = batch.GetComponentFrames<ComponentWithLayout>().Single().ComponentId;
        var pageFrames = _renderer.GetCurrentRenderTreeFrames(pageComponentId).AsEnumerable();
        Assert.Collection(pageFrames,
            frame => AssertFrame.Text(frame, "Hello from the page with message 'Test message'", sequence: 0));

        // Assert: nothing else was rendered
        Assert.Equal(4, batch.DiffsInOrder.Count);
    }

    [Fact]
    public void UsesDefaultLayoutIfNoneSetOnPage()
    {
        // Arrange
        var routeParams = new Dictionary<string, object>();
        var routeData = new RouteData(typeof(ComponentWithoutLayout), routeParams);

        // Act
        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
                { nameof(RouteView.DefaultLayout), typeof(OtherLayout) },
            })));

        // Assert: uses default layout
        // Not asserting about what else gets rendered as that's covered by other tests
        var batch = _renderer.Batches.Single();
        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)typeof(OtherLayout), sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));
    }

    [Fact]
    public void UsesNoLayoutIfNoneSetOnPageAndNoDefaultSet()
    {
        // Arrange
        var routeParams = new Dictionary<string, object>();
        var routeData = new RouteData(typeof(ComponentWithoutLayout), routeParams);

        // Act
        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
            })));

        // Assert: uses no layout
        // Not asserting about what else gets rendered as that's covered by other tests
        var batch = _renderer.Batches.Single();
        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)null, sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));
    }

    [Fact]
    public void PageLayoutSupersedesDefaultLayout()
    {
        // Arrange
        var routeParams = new Dictionary<string, object>();
        var routeData = new RouteData(typeof(ComponentWithLayout), routeParams);

        // Act
        _renderer.Dispatcher.InvokeAsync(() => _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(RouteView.RouteData), routeData },
                { nameof(RouteView.DefaultLayout), typeof(OtherLayout) },
            })));

        // Assert: uses layout specified by page
        // Not asserting about what else gets rendered as that's covered by other tests
        var batch = _renderer.Batches.Single();
        var routeViewFrames = _renderer.GetCurrentRenderTreeFrames(_routeViewComponentId).AsEnumerable();
        Assert.Collection(routeViewFrames,
            frame => AssertFrame.Component<LayoutView>(frame, subtreeLength: 3, sequence: 0),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.Layout), (object)typeof(TestLayout), sequence: 1),
            frame => AssertFrame.Attribute(frame, nameof(LayoutView.ChildContent), sequence: 2));
    }

    [Fact]
    public async Task ClearsBodyWhenLayoutBecomesRoutedPage()
    {
        var childRoute = new RouteData(typeof(ComponentWithRoutableLayout), new Dictionary<string, object>());
        var parentRoute = new RouteData(typeof(RoutableLayout), new Dictionary<string, object>
        {
            [nameof(RoutableLayout.Message)] = "Parent page",
        });

        await SetRouteAsync(childRoute);
        var parentFrame = _renderer.Batches.Single().GetComponentFrames<RoutableLayout>().Single();
        var parent = (RoutableLayout)parentFrame.Component;
        Assert.NotNull(parent.Body);

        await SetRouteAsync(parentRoute);

        var parentPageFrame = _renderer.Batches.Last().GetComponentFrames<RoutableLayout>().Single();
        var parentPage = (RoutableLayout)parentPageFrame.Component;
        Assert.NotEqual(parentFrame.ComponentId, parentPageFrame.ComponentId);
        Assert.Null(parentPage.Body);
        Assert.Equal("Parent page", parentPage.Message);
        var outerLayoutId = _renderer.Batches.First().GetComponentFrames<TestLayout>().Single().ComponentId;
        var renderedParentPageFrame = _renderer.GetCurrentRenderTreeFrames(outerLayoutId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(RoutableLayout));
        Assert.Equal(parentPageFrame.ComponentId, renderedParentPageFrame.ComponentId);

        await SetRouteAsync(childRoute);

        var returnedLayoutFrame = _renderer.Batches.Last().GetComponentFrames<RoutableLayout>().Single();
        Assert.NotEqual(parentPageFrame.ComponentId, returnedLayoutFrame.ComponentId);
        Assert.NotNull(((RoutableLayout)returnedLayoutFrame.Component).Body);
        Assert.Equal(returnedLayoutFrame.ComponentId, _renderer.GetCurrentRenderTreeFrames(outerLayoutId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(RoutableLayout)).ComponentId);
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task RendersLayoutPageWithoutBody()
    {
        await SetRouteAsync(new RouteData(typeof(RoutableLayout), new Dictionary<string, object>()));

        var parentFrame = _renderer.Batches.Single().GetComponentFrames<RoutableLayout>().Single();
        Assert.Null(((RoutableLayout)parentFrame.Component).Body);
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task PreservesConstructorInitializedBodyOnDirectArrival()
    {
        await SetRouteAsync(new RouteData(typeof(InitializedBodyPage), new Dictionary<string, object>()));

        var pageFrame = _renderer.Batches.Single().GetComponentFrames<InitializedBodyPage>().Single();
        var page = (InitializedBodyPage)pageFrame.Component;
        Assert.NotNull(page.Body);
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(pageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Region(frame, subtreeLength: 2),
            frame => AssertFrame.Text(frame, "Default body", sequence: 0));
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Theory]
    [InlineData(nameof(LayoutComponentBase.Body))]
    [InlineData("body")]
    public async Task ClearsPreviouslySuppliedBodyOnLayoutPage(string parameterName)
    {
        RenderFragment body = builder => builder.AddContent(0, "Previous body");
        await SetRouteAsync(new RouteData(typeof(RoutableLayout), new Dictionary<string, object>
        {
            [parameterName] = body,
        }));
        var parentFrame = _renderer.Batches.Single().GetComponentFrames<RoutableLayout>().Single();
        var parent = (RoutableLayout)parentFrame.Component;
        Assert.Same(body, parent.Body);

        await SetRouteAsync(new RouteData(typeof(RoutableLayout), new Dictionary<string, object>()));

        var newParentFrame = _renderer.Batches.Last().GetComponentFrames<RoutableLayout>().Single();
        Assert.NotEqual(parentFrame.ComponentId, newParentFrame.ComponentId);
        Assert.Null(((RoutableLayout)newParentFrame.Component).Body);
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Theory]
    [InlineData(nameof(LayoutComponentBase.Body))]
    [InlineData("body")]
    public async Task PreservesExplicitBodyOnLayoutPage(string parameterName)
    {
        RenderFragment body = builder => builder.AddContent(0, "Explicit body");
        await SetRouteAsync(new RouteData(typeof(RoutableLayout), new Dictionary<string, object>
        {
            [parameterName] = body,
        }));

        var parentFrame = _renderer.Batches.Single().GetComponentFrames<RoutableLayout>().Single();
        Assert.Same(body, ((RoutableLayout)parentFrame.Component).Body);
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task RecreatesCustomLayoutWhenItBecomesRoutedPage()
    {
        var parentRoute = new RouteData(typeof(CustomRoutableLayout), new Dictionary<string, object>());
        var childRoute = new RouteData(typeof(ComponentWithCustomRoutableLayout), new Dictionary<string, object>());

        await SetRouteAsync(childRoute);

        var layoutFrame = _renderer.Batches.Single().GetComponentFrames<CustomRoutableLayout>().Single();
        var outerLayoutId = _renderer.Batches.Single().GetComponentFrames<TestLayout>().Single().ComponentId;
        Assert.NotNull(((CustomRoutableLayout)layoutFrame.Component).Body);

        await SetRouteAsync(parentRoute);

        var pageFrame = _renderer.GetCurrentRenderTreeFrames(outerLayoutId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(CustomRoutableLayout));
        Assert.NotEqual(layoutFrame.ComponentId, pageFrame.ComponentId);
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(pageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Region(frame, subtreeLength: 2),
            frame => AssertFrame.Text(frame, "Default body", sequence: 0));

        await SetRouteAsync(childRoute);

        var returnedLayoutFrame = _renderer.GetCurrentRenderTreeFrames(outerLayoutId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(CustomRoutableLayout));
        Assert.NotEqual(pageFrame.ComponentId, returnedLayoutFrame.ComponentId);
        Assert.NotNull(((CustomRoutableLayout)returnedLayoutFrame.Component).Body);
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task PreservesCustomLayoutConstructorInitializedBodyOnDirectArrival()
    {
        await SetRouteAsync(new RouteData(typeof(CustomRoutableLayout), new Dictionary<string, object>()));

        var pageFrame = _renderer.Batches.Single().GetComponentFrames<CustomRoutableLayout>().Single();
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(pageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Region(frame, subtreeLength: 2),
            frame => AssertFrame.Text(frame, "Default body", sequence: 0));
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Theory]
    [InlineData(nameof(LayoutComponentBase.Body))]
    [InlineData("body")]
    public async Task RestoresCustomLayoutDefaultWhenExplicitBodyIsOmitted(string parameterName)
    {
        RenderFragment body = builder => builder.AddContent(0, "Explicit body");
        await SetRouteAsync(new RouteData(typeof(CustomRoutableLayout), new Dictionary<string, object>
        {
            [parameterName] = body,
        }));

        var pageFrame = _renderer.Batches.Single().GetComponentFrames<CustomRoutableLayout>().Single();
        var outerLayoutId = _renderer.Batches.Single().GetComponentFrames<TestLayout>().Single().ComponentId;
        Assert.Same(body, ((CustomRoutableLayout)pageFrame.Component).Body);

        await SetRouteAsync(new RouteData(typeof(CustomRoutableLayout), new Dictionary<string, object>()));

        var newPageFrame = _renderer.GetCurrentRenderTreeFrames(outerLayoutId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(CustomRoutableLayout));
        Assert.NotEqual(pageFrame.ComponentId, newPageFrame.ComponentId);
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(newPageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Region(frame, subtreeLength: 2),
            frame => AssertFrame.Text(frame, "Default body", sequence: 0));
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Fact]
    public async Task PreservesPageInstanceWhenRouteParametersChange()
    {
        await SetRouteAsync(new RouteData(typeof(ComponentWithoutLayout), new Dictionary<string, object>
        {
            [nameof(ComponentWithoutLayout.Message)] = "First",
        }));
        var pageFrame = _renderer.Batches.Single().GetComponentFrames<ComponentWithoutLayout>().Single();
        var layoutViewId = _renderer.Batches.Single().GetComponentFrames<LayoutView>().Single().ComponentId;

        await SetRouteAsync(new RouteData(typeof(ComponentWithoutLayout), new Dictionary<string, object>
        {
            [nameof(ComponentWithoutLayout.Message)] = "Second",
        }));

        var newPageFrame = _renderer.GetCurrentRenderTreeFrames(layoutViewId).AsEnumerable()
            .Single(frame => frame.ComponentType == typeof(ComponentWithoutLayout));
        Assert.Equal(pageFrame.ComponentId, newPageFrame.ComponentId);
        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(newPageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Text(frame, "Hello from the page with message 'Second'", sequence: 0));
        Assert.Empty(_renderer.HandledExceptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RendersPageWhenLayoutInvokesBodyDirectlyMoreThanOnce(bool supplyBody)
    {
        var routeValues = new Dictionary<string, object>();
        if (supplyBody)
        {
            routeValues[nameof(LayoutComponentBase.Body)] = (RenderFragment)(builder => builder.AddContent(0, "Explicit body"));
        }

        var routeData = new RouteData(typeof(InitializedBodyPage), routeValues);
        await _renderer.Dispatcher.InvokeAsync(() =>
            _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(RouteView.RouteData)] = routeData,
                [nameof(RouteView.DefaultLayout)] = typeof(RepeatingBodyLayout),
            })));
        var layoutId = _renderer.Batches.Single().GetComponentFrames<RepeatingBodyLayout>().Single().ComponentId;
        var originalPages = _renderer.GetCurrentRenderTreeFrames(layoutId).AsEnumerable()
            .Where(frame => frame.ComponentType == typeof(InitializedBodyPage)).ToArray();
        Assert.Equal(2, originalPages.Length);

        await SetRouteAsync(new RouteData(typeof(InitializedBodyPage), routeValues));

        Assert.Empty(_renderer.HandledExceptions);
        var currentPages = _renderer.GetCurrentRenderTreeFrames(layoutId).AsEnumerable()
            .Where(frame => frame.ComponentType == typeof(InitializedBodyPage)).ToArray();
        Assert.Equal(originalPages.Select(frame => frame.ComponentId), currentPages.Select(frame => frame.ComponentId));
        foreach (var page in currentPages)
        {
            Assert.Collection(
                _renderer.GetCurrentRenderTreeFrames(page.ComponentId).AsEnumerable(),
                frame => AssertFrame.Region(frame, subtreeLength: 2),
                frame => AssertFrame.Text(frame, supplyBody ? "Explicit body" : "Default body", sequence: 0));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UsesStablePageKeyAcrossSerializedBodyReplay(bool supplyBody)
    {
        var routeValues = new Dictionary<string, object>();
        if (supplyBody)
        {
            routeValues[nameof(LayoutComponentBase.Body)] = (RenderFragment)(_ => { });
        }

        await SetRouteAsync(new RouteData(typeof(InitializedBodyPage), routeValues));

        var childContent = (RenderFragment)_renderer.GetCurrentRenderTreeFrames(_routeViewComponentId)
            .AsEnumerable()
            .Single(frame => frame.AttributeName == nameof(LayoutView.ChildContent))
            .AttributeValue;
        using var builder = new RenderTreeBuilder();
        childContent(builder);
        var frames = builder.GetFrames();
        var pageKey = Assert.IsType<string>(frames.Array
            .Take(frames.Count)
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component)
            .ComponentKey);

        var json = JsonSerializer.Serialize(pageKey);
        var firstReplayKey = JsonSerializer.Deserialize(json, typeof(string));
        var secondReplayKey = JsonSerializer.Deserialize(json, typeof(string));

        Assert.Equal(pageKey, firstReplayKey);
        Assert.Equal(firstReplayKey, secondReplayKey);
    }

    [Theory]
    [InlineData("Body", "First")]
    [InlineData("body", "First")]
    [InlineData("Body", null)]
    [InlineData("body", null)]
    public async Task PreservesPageInstanceWhenOrdinaryBodyParameterIsSuppliedOrOmitted(string parameterName, string initialBody)
    {
        var routeValues = new Dictionary<string, object> { [parameterName] = initialBody };
        await SetRouteAsync(new RouteData(typeof(PageWithStringBody), routeValues));
        var pageFrame = _renderer.Batches.Single().GetComponentFrames<PageWithStringBody>().Single();
        var page = (PageWithStringBody)pageFrame.Component;
        var layoutViewId = _renderer.Batches.Single().GetComponentFrames<LayoutView>().Single().ComponentId;
        page.LocalState = "Preserved state";

        foreach (var values in new[] { routeValues, new Dictionary<string, object>(), new Dictionary<string, object> { [parameterName] = "Second" } })
        {
            await SetRouteAsync(new RouteData(typeof(PageWithStringBody), values));

            var currentPage = _renderer.GetCurrentRenderTreeFrames(layoutViewId).AsEnumerable()
                .Single(frame => frame.ComponentType == typeof(PageWithStringBody));
            Assert.Equal(pageFrame.ComponentId, currentPage.ComponentId);
            Assert.Same(page, currentPage.Component);
            Assert.Equal(values.TryGetValue(parameterName, out var body) ? body : initialBody, page.Body);
            Assert.Equal("Preserved state", page.LocalState);
            Assert.Empty(_renderer.HandledExceptions);
        }

        Assert.Collection(
            _renderer.GetCurrentRenderTreeFrames(pageFrame.ComponentId).AsEnumerable(),
            frame => AssertFrame.Text(frame, "Preserved state: Second", sequence: 0));
    }

    private Task SetRouteAsync(RouteData routeData) => _renderer.Dispatcher.InvokeAsync(() =>
        _routeViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(RouteView.RouteData)] = routeData,
        })));

    [Layout(typeof(TestLayout))]
    private class RoutableLayout : LayoutComponentBase
    {
        [Parameter] public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Message);
            builder.AddContent(1, Body);
        }
    }

    private class InitializedBodyPage : LayoutComponentBase
    {
        public InitializedBodyPage()
        {
            Body = builder => builder.AddContent(0, "Default body");
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }

    [Layout(typeof(TestLayout))]
    private class CustomRoutableLayout : ComponentBase
    {
        public CustomRoutableLayout()
        {
            Body = builder => builder.AddContent(0, "Default body");
        }

        [Parameter] public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }

    [Layout(typeof(CustomRoutableLayout))]
    private class ComponentWithCustomRoutableLayout : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Child page");
        }
    }

    [Layout(typeof(RoutableLayout))]
    private class ComponentWithRoutableLayout : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Child page");
        }
    }

    private class RepeatingBodyLayout : ComponentBase
    {
        [Parameter] public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Body(builder);
            Body(builder);
        }
    }

    private class PageWithStringBody : ComponentBase
    {
        [Parameter] public string Body { get; set; }

        public string LocalState { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"{LocalState}: {Body}");
        }
    }

    private class RouteViewTestNavigationManager : NavigationManager
    {
        public RouteViewTestNavigationManager() =>
            Initialize("https://www.example.com/subdir/", "https://www.example.com/subdir/");

        public void NotifyLocationChanged(string uri)
        {
            Uri = uri;
            NotifyLocationChanged(false);
        }
    }

    private class ComponentWithoutLayout : AutoRenderComponent
    {
        [Parameter] public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"Hello from the page with message '{Message}'");
        }
    }

    [Layout(typeof(TestLayout))]
    private class ComponentWithLayout : AutoRenderComponent
    {
        [Parameter] public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"Hello from the page with message '{Message}'");
        }
    }

    private class TestLayout : AutoRenderComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Layout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "Layout ends here");
        }
    }

    private class OtherLayout : AutoRenderComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "OtherLayout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "OtherLayout ends here");
        }
    }
}
