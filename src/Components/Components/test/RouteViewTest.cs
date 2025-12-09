// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
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

        var componentFactory = new ComponentFactory(new DefaultComponentActivator(services), _renderer);
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
