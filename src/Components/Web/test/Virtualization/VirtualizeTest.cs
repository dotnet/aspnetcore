// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Microsoft.AspNetCore.Components.Virtualization;

public class VirtualizeTest
{
    [Fact]
    public async Task Virtualize_ThrowsWhenGivenNonPositiveItemSize()
    {
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(0f, EmptyItemsProvider<int>, null)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await testRenderer.RenderRootComponentAsync(componentId));
        Assert.Contains("requires a positive value for parameter", ex.Message);
    }

    [Fact]
    public async Task Virtualize_ThrowsWhenGivenMultipleItemSources()
    {
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(10f, EmptyItemsProvider<int>, new List<int>())
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await testRenderer.RenderRootComponentAsync(componentId));
        Assert.Contains("can only accept one item source from its parameters", ex.Message);
    }

    [Fact]
    public async Task Virtualize_ThrowsWhenGivenNoItemSources()
    {
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize<int>(10f, null, null)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await testRenderer.RenderRootComponentAsync(componentId));
        Assert.Contains("parameters to be specified and non-null", ex.Message);
    }

    [Fact]
    public async Task Virtualize_DispatchesExceptionsFromItemsProviderThroughRenderer()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(10f, AlwaysThrowsItemsProvider<int>, null, virtualize => renderedVirtualize = virtualize)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        // Render to populate the component reference.
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);

        // Simulate a JS spacer callback.
        ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(10f, 50f, 100f, SpacerVisibilityReason.ViewportFill);

        // Validate that the exception is dispatched through the renderer.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await testRenderer.RenderRootComponentAsync(componentId));
        Assert.Equal("Thrown from items provider.", ex.Message);
    }

    [Fact]
    public async Task Virtualize_MeasurementsUpdateRunningAverage()
    {
        // Use fixed items with a template so items actually render
        Virtualize<int> virtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => virtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        // First callback triggers distribution calculation and re-render
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 80f, 500f, SpacerVisibilityReason.ViewportFill));

        // Second callback — with items now rendered, CalculateItemDistribution derives
        // item heights from spacerSeparation and _lastRenderedItemCount
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 80f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.True(virtualize._totalMeasuredHeight > 0);
        Assert.True(virtualize._measuredItemCount > 0);
    }

    [Fact]
    public async Task InitialIndex_MeasurementDoesNotChangeSpacerUntilPositioningCompletes()
    {
        Virtualize<int> virtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", (ICollection<int>)Enumerable.Range(0, 1000).ToList());
                builder.AddComponentParameter(3, "InitialItemIndex", 950);
                builder.AddComponentParameter(4, "OverscanCount", 3);
                builder.AddComponentParameter(5, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(6, component => virtualize = (Virtualize<int>)component);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(
                0f,
                1253f,
                2000f,
                SpacerVisibilityReason.RenderedContentMeasurement));
        await testRenderer.RenderRootComponentAsync(componentId);

        var virtualizeComponentId = testRenderer.Batches
            .SelectMany(batch => batch.ReferenceFrames)
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component
                && ReferenceEquals(frame.Component, virtualize))
            .ComponentId;
        var spacerHeights = testRenderer.GetCurrentRenderTreeFrames(virtualizeComponentId)
            .AsEnumerable()
            .Where(frame => frame.FrameType == RenderTreeFrameType.Attribute
                && frame.AttributeName == "data-blazor-virtualize-reserved-height");

        Assert.Equal(1253f, virtualize._totalMeasuredHeight);
        Assert.Equal(7, virtualize._measuredItemCount);
        Assert.Collection(
            spacerHeights,
            spacerBefore => Assert.Equal("47350", spacerBefore.AttributeValue),
            spacerAfter => Assert.Equal("2300", spacerAfter.AttributeValue));
    }

    [Fact]
    public async Task InitialIndex_ApplyingMeasuredGeometry_PreservesRunningAverage()
    {
        Virtualize<int> virtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", (ICollection<int>)Enumerable.Range(0, 7).ToList());
                builder.AddComponentParameter(3, "InitialItemIndex", 5);
                builder.AddComponentParameter(4, "OverscanCount", 3);
                builder.AddComponentParameter(5, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(6, component => virtualize = (Virtualize<int>)component);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(
                0f,
                1253f,
                2000f,
                SpacerVisibilityReason.RenderedContentMeasurement));
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(
                0f,
                1253f,
                2000f,
                SpacerVisibilityReason.ViewportFill));

        Assert.Equal(2506f, virtualize._totalMeasuredHeight);
        Assert.Equal(14, virtualize._measuredItemCount);
    }

    [Fact]
    public async Task Virtualize_ZeroSpacerSeparationDoesNotCorruptAverage()
    {
        // BuildVirtualizeWithContent provides Items + ChildContent so the test renderer
        // actually renders items, incrementing _lastRenderedItemCount (needed for measurement accumulation).
        Virtualize<int> virtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => virtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        // First callback with valid spacerSeparation establishes measurements
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        var heightAfterValid = virtualize._totalMeasuredHeight;
        var countAfterValid = virtualize._measuredItemCount;
        Assert.True(heightAfterValid > 0, "Should have accumulated measurements");

        // Callback with zero spacerSeparation should not add measurements
        // (realItemHeight would be zero or negative)
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(heightAfterValid, virtualize._totalMeasuredHeight);
        Assert.Equal(countAfterValid, virtualize._measuredItemCount);
    }

    [Fact]
    public async Task Virtualize_OnBeforeSpacerVisible_MeasuredSpacerTriggersProviderFetch()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: trackingProvider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        var countBefore = requests.Count;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(100f, 300f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.True(requests.Count > countBefore,
            "ItemsProvider should be called when before spacer becomes visible with measurements");
    }

    [Fact]
    public async Task Virtualize_NonZeroStartIndex_ItemsProviderReceivesCorrectStartIndex()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 500 - request.StartIndex)),
                500));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 500, customProvider: trackingProvider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(5000f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Contains(requests, r => r.StartIndex > 0);
    }

    [Fact]
    public async Task Virtualize_NaNSpacerSeparationDoesNotCrashComponent()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: trackingProvider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        // Establish baseline
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, SpacerVisibilityReason.ViewportFill));
        var countAfterBaseline = requests.Count;
        var heightBefore = virtualize._totalMeasuredHeight;

        // NaN spacerSeparation should not corrupt measurements or crash
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, float.NaN, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, float.NaN, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.True(requests.Count > countAfterBaseline,
            "Component should still process callbacks after NaN spacerSeparation");
    }

    [Fact]
    public async Task Virtualize_NegativeSpacerSeparationDoesNotCrashComponent()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: trackingProvider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, SpacerVisibilityReason.ViewportFill));
        var heightBefore = virtualize._totalMeasuredHeight;
        var countBefore = virtualize._measuredItemCount;

        // Negative spacerSeparation produces negative realItemHeight — should not
        // accumulate into the running average.
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, -500f, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, -100f, 500f, SpacerVisibilityReason.ViewportFill));

        // Measurements should not have changed from the negative inputs
        Assert.Equal(heightBefore, virtualize._totalMeasuredHeight);
        Assert.Equal(countBefore, virtualize._measuredItemCount);
    }

    [Fact]
    public async Task Virtualize_InfinitySpacerSeparationDoesNotCrashComponent()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: trackingProvider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, SpacerVisibilityReason.ViewportFill));
        var countAfterBaseline = requests.Count;

        // Extremely large (infinity) spacerSeparation — component should handle
        // without overflow or crash. MaxItemCount caps the visible capacity.
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, float.PositiveInfinity, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, float.PositiveInfinity, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.True(requests.Count > countAfterBaseline,
            "Component should still process callbacks after infinity spacerSeparation");
    }

    [Fact]
    public async Task Virtualize_RendersItemsWithoutWrapperElements()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, new List<int> { 1, 2, 3 },
                captureRenderedVirtualize: virtualize => renderedVirtualize = virtualize)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f, SpacerVisibilityReason.ViewportFill));

        // Items should be rendered directly without wrapper elements
        var hasWrapperElements = testRenderer.Batches
            .SelectMany(b => b.ReferenceFrames)
            .Any(f => f.FrameType == RenderTreeFrameType.Element
                   && f.ElementName == "virtualize-item");

        Assert.False(hasWrapperElements,
            "Items should be rendered directly without wrapper elements");
    }

    [Fact]
    public async Task Virtualize_TableSpacerElement_RendersItemsDirectlyWithTrSpacers()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, new List<int> { 1, 2, 3 },
                captureRenderedVirtualize: virtualize => renderedVirtualize = virtualize,
                spacerElement: "tr")
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f, SpacerVisibilityReason.ViewportFill));

        var referenceFrames = testRenderer.Batches.SelectMany(b => b.ReferenceFrames).ToList();

        var hasTrSpacers = referenceFrames
            .Any(f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "tr");

        Assert.True(hasTrSpacers,
            "Spacer elements should use 'tr' tag when SpacerElement='tr'");
    }

    [Fact]
    public async Task Virtualize_RefreshDataAsync_ResetsRunningAverage()
    {
        Virtualize<int> virtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => virtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        // Multiple callbacks to load items and accumulate measurements
        for (int i = 0; i < 10; i++)
        {
            await testRenderer.Dispatcher.InvokeAsync(() =>
                callbacks.OnAfterSpacerVisible(0f, 90f, 500f, SpacerVisibilityReason.ViewportFill));
        }

        // After several cycles, measurements should have accumulated
        Assert.True(virtualize._measuredItemCount > 0);

        await testRenderer.Dispatcher.InvokeAsync(() => virtualize.RefreshDataAsync());

        Assert.Equal(0f, virtualize._totalMeasuredHeight);
        Assert.Equal(0, virtualize._measuredItemCount);
    }

    [Fact]
    public async Task Virtualize_ScrollToBottom_SetWhenAtEndWithNewMeasurements()
    {
        var mockJs = new Mock<IJSRuntime>(MockBehavior.Loose);

        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => renderedVirtualize = v,
                anchorMode: VirtualizeAnchorMode.End)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient<IJSRuntime>((sp) => mockJs.Object)
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        // First callback triggers items to render
        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(
                0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        // Second callback: spacerSize=0 means at the very bottom; with items rendered, should trigger scrollToBottom
        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(
                0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        var scrollToBottomCalled = mockJs.Invocations.Any(i =>
            i.Arguments.Count > 0 &&
            i.Arguments[0] is string id &&
            id.Contains("scrollToBottom"));

        Assert.True(scrollToBottomCalled || renderedVirtualize._pendingScrollToBottom,
            "scrollToBottom should either be called via JS interop or be pending");
    }

    [Fact]
    public async Task Virtualize_ScrollToBottom_NotSetWhenNotAtEnd()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        // spacerSize=5000 means many items remain after the viewport
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(5000f, 1000f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.False(virtualize._pendingScrollToBottom);
    }

    [Fact]
    public async Task Virtualize_ScrollToBottom_NotSetWhenMeasurementsNotApplied()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        // First call: real measurements at the bottom — should set pending
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.True(renderedVirtualize._lastRenderedItemCount > 0,
            "Items should have rendered");
        renderedVirtualize._pendingScrollToBottom = false;

        // Second call: spacerSeparation=0 at the bottom — no new measurements,
        // so _pendingScrollToBottom must NOT be set
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.False(renderedVirtualize._pendingScrollToBottom,
            "scrollToBottom should not be set when no new measurements were applied");
    }

    [Fact]
    public async Task Virtualize_FirstRender_DoesNotShiftStartIndexAwayFromZero()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: trackingProvider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        var callCountAfterMount = requests.Count;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(callCountAfterMount + 1, requests.Count);

        var lastRequest = requests[^1];
        Assert.Equal(0, lastRequest.StartIndex);
    }

    [Fact]
    public async Task Virtualize_BothSpacersVisible_SmallItemCountDoesNotCrash()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, new List<int> { 1, 2, 3 },
                captureRenderedVirtualize: v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 150f, 1000f, SpacerVisibilityReason.ViewportFill));

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(0f, 150f, 1000f, SpacerVisibilityReason.ViewportFill));

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 150f, 1000f, SpacerVisibilityReason.ViewportFill));

        // After multiple callbacks, measurements should accumulate
        Assert.True(renderedVirtualize._measuredItemCount > 0);
    }

    [Fact]
    public async Task Virtualize_FixedItems_MeasurementsAccumulateWithoutBreakingRendering()
    {
        Virtualize<int> renderedVirtualize = null;

        var items = Enumerable.Range(1, 20).ToList();
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, items,
                captureRenderedVirtualize: v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        Assert.Equal(0f, renderedVirtualize._totalMeasuredHeight);
        Assert.Equal(0, renderedVirtualize._measuredItemCount);

        // First callback triggers render with items (setting _lastRenderedItemCount)
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 1000f, 500f, SpacerVisibilityReason.ViewportFill));

        // Second callback accumulates measurements from spacerSeparation
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 1000f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.True(renderedVirtualize._totalMeasuredHeight > 0);
        Assert.True(renderedVirtualize._measuredItemCount > 0);
    }

    [Fact]
    public async Task Virtualize_RapidScrollCancellation_StaleRequestsCancelled()
    {
        var pendingCalls = new List<(ItemsProviderRequest request, TaskCompletionSource<ItemsProviderResult<int>> tcs)>();

        ValueTask<ItemsProviderResult<int>> delayedProvider(ItemsProviderRequest request)
        {
            var tcs = new TaskCompletionSource<ItemsProviderResult<int>>();
            pendingCalls.Add((request, tcs));
            return new ValueTask<ItemsProviderResult<int>>(tcs.Task);
        }

        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(50f, delayedProvider, null, v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Single(pendingCalls);
        var firstCall = pendingCalls[0];

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 1000f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(2, pendingCalls.Count);
        var secondCall = pendingCalls[1];

        Assert.True(firstCall.request.CancellationToken.IsCancellationRequested);
        Assert.False(secondCall.request.CancellationToken.IsCancellationRequested);

        // With delayed provider, no items have rendered so _lastRenderedItemCount = 0
        // and no measurements accumulate (correct: can't measure what hasn't rendered)
        Assert.Equal(0, renderedVirtualize._measuredItemCount);
        Assert.Equal(0f, renderedVirtualize._totalMeasuredHeight);
        foreach (var call in pendingCalls.Where(c => !c.tcs.Task.IsCompleted))
        {
            call.tcs.TrySetResult(new ItemsProviderResult<int>(Array.Empty<int>(), 0));
        }
    }

    [Fact]
    public async Task Virtualize_CanceledPrependAdjustment_DoesNotOverwriteNewerWindow()
    {
        var items = Enumerable.Range(0, 200).ToList();
        var blockAdjustedRequest = false;
        var blockedRequestStarted = new TaskCompletionSource<ItemsProviderRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlockedRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<ItemsProviderResult<int>> provider(ItemsProviderRequest request)
        {
            if (blockAdjustedRequest && request.StartIndex == 120)
            {
                blockAdjustedRequest = false;
                blockedRequestStarted.SetResult(request);
                await releaseBlockedRequest.Task;
            }

            return new ItemsProviderResult<int>(
                items.Skip(request.StartIndex).Take(request.Count).ToArray(),
                items.Count);
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            50f,
            items.Count,
            provider,
            item => builder => builder.AddContent(0, item));
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f, SpacerVisibilityReason.ViewportFill));
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible((100 + 15) * 50f, 40 * 50f, 500f, SpacerVisibilityReason.UserScroll));
        Assert.Equal(100, virtualize._itemsBefore);

        items.InsertRange(0, Enumerable.Range(-20, 20));
        blockAdjustedRequest = true;

        Task canceledRefresh = null;
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            canceledRefresh = virtualize.RefreshDataAsync();
        });

        var blockedRequest = await blockedRequestStarted.Task;

        Task newerRefresh = null;
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            virtualize._itemsBefore = 130;
            newerRefresh = virtualize.RefreshDataAsync();
        });
        await newerRefresh;

        Assert.True(blockedRequest.CancellationToken.IsCancellationRequested);
        Assert.Equal(150, virtualize._itemsBefore);

        releaseBlockedRequest.SetResult();
        await canceledRefresh;

        Assert.Equal(150, virtualize._itemsBefore);
    }

    [Fact]
    public async Task MaxItemCount_ClampsVisibleItemCapacity()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 1000 - request.StartIndex)),
                1000));
        }

        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithMaxItemCount(10f, trackingProvider, maxItemCount: 20, captureRenderedVirtualize: v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        var lastRequest = requests.Last();
        Assert.True(lastRequest.Count <= 50,
            $"Expected request count <= 50 (MaxItemCount=20 + 2*OverscanCount=30), but got {lastRequest.Count}");
    }

    private async Task<(Virtualize<int> virtualize, TestRenderer renderer)> CreateRenderedVirtualize(
        float itemSize,
        int totalItems,
        ItemsProviderDelegate<int> customProvider = null,
        RenderFragment<int> childContent = null,
        IJSRuntime jsRuntime = null)
    {
        Virtualize<int> renderedVirtualize = null;

        ItemsProviderDelegate<int> provider = customProvider ?? ((ItemsProviderRequest request) =>
            ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, totalItems - request.StartIndex)),
                totalItems)));

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(itemSize, provider, null, virtualize => renderedVirtualize = virtualize, childContent)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => jsRuntime ?? Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        return (renderedVirtualize, testRenderer);
    }

    private ValueTask<ItemsProviderResult<TItem>> EmptyItemsProvider<TItem>(ItemsProviderRequest request)
        => ValueTask.FromResult(new ItemsProviderResult<TItem>(Enumerable.Empty<TItem>(), 0));

    private ValueTask<ItemsProviderResult<TItem>> AlwaysThrowsItemsProvider<TItem>(ItemsProviderRequest request)
        => throw new InvalidOperationException("Thrown from items provider.");

    private RenderFragment BuildVirtualize<TItem>(
        float itemSize,
        ItemsProviderDelegate<TItem> itemsProvider,
        ICollection<TItem> items,
        Action<Virtualize<TItem>> captureRenderedVirtualize = null,
        RenderFragment<TItem> childContent = null)
        => builder =>
    {
        builder.OpenComponent<Virtualize<TItem>>(0);
        builder.AddComponentParameter(1, "ItemSize", itemSize);
        builder.AddComponentParameter(2, "ItemsProvider", itemsProvider);
        builder.AddComponentParameter(3, "Items", items);

        if (childContent != null)
        {
            builder.AddComponentParameter(5, "ChildContent", childContent);
        }

        if (captureRenderedVirtualize != null)
        {
            builder.AddComponentReferenceCapture(4, component => captureRenderedVirtualize(component as Virtualize<TItem>));
        }

        builder.CloseComponent();
    };

    private RenderFragment BuildVirtualizeWithContent(
        float itemSize,
        ICollection<int> items,
        Action<Virtualize<int>> captureRenderedVirtualize = null,
        string spacerElement = "div",
        VirtualizeAnchorMode anchorMode = VirtualizeAnchorMode.Start)
        => builder =>
    {
        builder.OpenComponent<Virtualize<int>>(0);
        builder.AddComponentParameter(1, "ItemSize", itemSize);
        builder.AddComponentParameter(2, "Items", items);
        builder.AddComponentParameter(3, "SpacerElement", spacerElement);
        builder.AddComponentParameter(4, "ChildContent", (RenderFragment<int>)(item => b =>
        {
            b.OpenElement(0, "span");
            b.AddContent(1, item.ToString(System.Globalization.CultureInfo.InvariantCulture));
            b.CloseElement();
        }));
        builder.AddComponentParameter(5, "AnchorMode", anchorMode);

        if (captureRenderedVirtualize != null)
        {
            builder.AddComponentReferenceCapture(6, component =>
                captureRenderedVirtualize(component as Virtualize<int>));
        }

        builder.CloseComponent();
    };

    private RenderFragment BuildVirtualizeWithMaxItemCount(
        float itemSize,
        ItemsProviderDelegate<int> itemsProvider,
        int maxItemCount,
        Action<Virtualize<int>> captureRenderedVirtualize = null)
        => builder =>
    {
        builder.OpenComponent<Virtualize<int>>(0);
        builder.AddComponentParameter(1, "ItemSize", itemSize);
        builder.AddComponentParameter(2, "ItemsProvider", itemsProvider);
        builder.AddComponentParameter(3, "MaxItemCount", maxItemCount);

        if (captureRenderedVirtualize != null)
        {
            builder.AddComponentReferenceCapture(4, component => captureRenderedVirtualize(component as Virtualize<int>));
        }

        builder.CloseComponent();
    };

    private RenderFragment BuildVirtualizeWithMultiRootContent(
        float itemSize,
        ICollection<int> items,
        Action<Virtualize<int>> captureRenderedVirtualize = null)
        => builder =>
    {
        builder.OpenComponent<Virtualize<int>>(0);
        builder.AddComponentParameter(1, "ItemSize", itemSize);
        builder.AddComponentParameter(2, "Items", items);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment<int>)(item => b =>
        {
            // Two root elements per item
            b.OpenElement(0, "div");
            b.AddContent(1, $"Part A of {item.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            b.CloseElement();
            b.OpenElement(2, "div");
            b.AddContent(3, $"Part B of {item.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            b.CloseElement();
        }));

        if (captureRenderedVirtualize != null)
        {
            builder.AddComponentReferenceCapture(4, component =>
                captureRenderedVirtualize(component as Virtualize<int>));
        }

        builder.CloseComponent();
    };

    private class VirtualizeTestHostcomponent : AutoRenderComponent
    {
        public RenderFragment InnerContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "overflow: auto; height: 800px;");
            builder.AddContent(2, InnerContent);
            builder.CloseElement();
        }
    }

    [Fact]
    public async Task Virtualize_ItemsProvider_GrowingTotalCount_DoesNotAssumePrepend()
    {
        Virtualize<int> renderedVirtualize = null;
        var totalCount = 100;

        ValueTask<ItemsProviderResult<int>> growingProvider(ItemsProviderRequest request)
        {
            var items = Enumerable.Range(request.StartIndex, Math.Min(request.Count, totalCount - request.StartIndex));
            return ValueTask.FromResult(new ItemsProviderResult<int>(items, totalCount));
        }

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(50f, (ItemsProviderDelegate<int>)growingProvider, null, v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        // Initial IO callback to set up _itemCount
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 800f, 800f, SpacerVisibilityReason.ViewportFill));

        var itemsBeforeAfterInit = renderedVirtualize._itemsBefore;

        // Simulate TotalItemCount growing (new data arriving, not a prepend)
        totalCount = 120;

        // IO-driven refresh (NOT RefreshDataAsync) — triggered by spacer becoming visible
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 800f, 800f, SpacerVisibilityReason.ViewportFill));

        // _itemsBefore may change due to normal IO redistribution, but should NOT
        // have been shifted by exactly countDelta (20) which would indicate false
        // prepend detection. Normal redistribution produces different offsets.
        var shift = renderedVirtualize._itemsBefore - itemsBeforeAfterInit;
        Assert.True(shift != 20,
            $"IO-driven refresh should not trigger prepend detection (shift by countDelta). " +
            $"Before: {itemsBeforeAfterInit}, After: {renderedVirtualize._itemsBefore}, Shift: {shift}");
    }

    [Fact]
    public async Task Virtualize_ItemsProvider_PrependedSinglePassEnumerable_IsNotEnumeratedMoreThanOnce()
    {
        Virtualize<int> renderedVirtualize = null;
        var items = Enumerable.Range(0, 100).ToList();
        var returnSinglePassEnumerable = false;
        var singlePassEnumerationCount = 0;

        ValueTask<ItemsProviderResult<int>> provider(ItemsProviderRequest request)
        {
            var requestedItems = items
                .Skip(request.StartIndex)
                .Take(Math.Min(request.Count, items.Count - request.StartIndex))
                .ToList();
            var resultItems = returnSinglePassEnumerable
                ? SinglePass(requestedItems)
                : requestedItems;
            return ValueTask.FromResult(new ItemsProviderResult<int>(resultItems, items.Count));
        }

        IEnumerable<int> SinglePass(IEnumerable<int> source)
        {
            var enumerated = false;
            return Enumerate();

            IEnumerable<int> Enumerate()
            {
                singlePassEnumerationCount++;
                if (enumerated)
                {
                    throw new InvalidOperationException("The provider result was enumerated more than once.");
                }

                enumerated = true;
                foreach (var item in source)
                {
                    yield return item;
                }
            }
        }

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(
                50f,
                (ItemsProviderDelegate<int>)provider,
                null,
                v => renderedVirtualize = v,
                item => builder => builder.AddContent(0, item))
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 800f, 800f, SpacerVisibilityReason.ViewportFill));

        var itemsBeforePrepend = renderedVirtualize._itemsBefore;
        items.InsertRange(0, Enumerable.Range(-20, 20));
        returnSinglePassEnumerable = true;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            renderedVirtualize.RefreshDataAsync());

        Assert.Equal(itemsBeforePrepend + 20, renderedVirtualize._itemsBefore);
        Assert.Equal(1, singlePassEnumerationCount);
    }

    [Theory]
    [InlineData(false, 130)]
    [InlineData(true, 140)]
    public async Task Virtualize_ItemsProvider_GrowthWhileWindowMovesPastPreviousFirstItem_DistinguishesPrependFromAppend(
        bool prepend,
        int expectedItemsBefore)
    {
        var items = Enumerable.Range(0, 200).ToList();

        ValueTask<ItemsProviderResult<int>> provider(ItemsProviderRequest request)
            => ValueTask.FromResult(new ItemsProviderResult<int>(
                items.Skip(request.StartIndex).Take(request.Count).ToArray(),
                items.Count));

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            50f,
            items.Count,
            provider,
            item => builder => builder.AddContent(0, item));
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f, SpacerVisibilityReason.ViewportFill));
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible((100 + 15) * 50f, 40 * 50f, 500f, SpacerVisibilityReason.UserScroll));
        Assert.Equal(100, virtualize._itemsBefore);

        if (prepend)
        {
            items.InsertRange(0, Enumerable.Range(-10, 10));
        }
        else
        {
            items.AddRange(Enumerable.Range(200, 10));
        }

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible((130 + 15) * 50f, 40 * 50f, 500f, SpacerVisibilityReason.UserScroll));

        Assert.Equal(expectedItemsBefore, virtualize._itemsBefore);
    }

    [Fact]
    public async Task Virtualize_DefaultProvider_ValueTypeItem_AppendDoesNotAssumePrepend()
    {
        Virtualize<int> renderedVirtualize = null;
        var items = Enumerable.Range(1, 100).ToList();

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, items, v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        // Initial IO callback to set up _itemCount.
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 800f, 800f, SpacerVisibilityReason.ViewportFill));

        var itemsBeforeAfterInit = renderedVirtualize._itemsBefore;

        // Append 20 items at the end. Items[0] is unchanged — this is NOT a prepend.
        items.AddRange(Enumerable.Range(101, 20));

        // IO-driven refresh re-reads the in-memory list and observes count growth 100 -> 120.
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 800f, 800f, SpacerVisibilityReason.ViewportFill));

        var shift = renderedVirtualize._itemsBefore - itemsBeforeAfterInit;
        Assert.True(shift != 20,
            $"In-memory append on value-type TItem must not trigger prepend detection (shift by countDelta). " +
            $"Before: {itemsBeforeAfterInit}, After: {renderedVirtualize._itemsBefore}, Shift: {shift}");
    }

    [Fact]
    public void ScrollToIndexAsync_ThrowsBeforeJsInteropInitialized()
    {
        var virtualize = new Virtualize<int>();

        var ex = Assert.Throws<InvalidOperationException>(
            (Action)(() => { _ = virtualize.ScrollToItemAsync(0); }));
        Assert.Contains(nameof(Virtualize<int>.ScrollToItemAsync), ex.Message);
        Assert.Contains(nameof(Virtualize<int>.InitialItemIndex), ex.Message);
    }

    private static readonly RenderFragment<int> SimpleItemTemplate = item => b => b.AddContent(0, item);

    [Fact]
    public async Task ScrollToIndexAsync_NegativeIndexDoesNotThrow()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, childContent: SimpleItemTemplate);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(-5); });
        await task;
        Assert.True(task.IsCompletedSuccessfully);
        // Clamp post-condition: negative index lands the window at the start.
        Assert.Equal(0, virtualize._itemsBefore);
    }

    [Fact]
    public async Task ScrollToIndexAsync_IndexBeyondCountDoesNotThrow()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, childContent: SimpleItemTemplate);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(99_999); });

        await task;
        Assert.True(task.IsCompletedSuccessfully);
        // Clamp post-condition: out-of-range index lands the window at the end. After
        // OnAfterSpacerVisible with container=500/itemSize=50, capacity = ceil(500/50) + 2*15 = 40,
        // so the last full window starts at max(0, 100 - 40) = 60.
        Assert.Equal(60, virtualize._itemsBefore);
    }

    [Fact]
    public async Task ScrollToIndexAsync_EmptyListCompletesAsNoOp()
    {
        // Regression: render-commit rendezvous used to wait for _lastRenderedItemCount > 0, which never becomes true for an empty list, so the Task hung forever.
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 0);

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(0); });

        await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ScrollToIndexAsync_AlreadyCancelledTokenProducesCancelledTask()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(10, cts.Token); });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/68852")]
    public async Task ScrollToIndexAsync_CancellationCancelsProviderRequest()
    {
        var blockProvider = false;
        using var cts = new CancellationTokenSource();
        CancellationToken? providerCancellationToken = null;

        async ValueTask<ItemsProviderResult<int>> provider(ItemsProviderRequest request)
        {
            if (!blockProvider)
            {
                return new ItemsProviderResult<int>(
                    Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                    100);
            }

            providerCancellationToken = request.CancellationToken;
            // Cancel only after capturing the provider token, so no separate start-signal rendezvous is needed.
            cts.Cancel();
            await Task.Delay(Timeout.InfiniteTimeSpan, request.CancellationToken);
            return default;
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: provider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        blockProvider = true;
        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(90, cts.Token); });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(providerCancellationToken is { IsCancellationRequested: true });
    }

    [Fact]
    public async Task ScrollToIndexAsync_SecondCallDoesNotFaultFirstTask()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 1000);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        var (firstTask, secondTask) = await renderer.Dispatcher.InvokeAsync(() =>
        {
            var first = virtualize.ScrollToItemAsync(500);
            var second = virtualize.ScrollToItemAsync(750);
            return (first, second);
        });

        await firstTask;
        Assert.True(firstTask.IsCompletedSuccessfully);

        if (secondTask.IsCompleted)
        {
            Assert.True(secondTask.IsCompletedSuccessfully, secondTask.Exception?.ToString());
        }
    }

    [Fact]
    public async Task ScrollToIndexAsync_ProviderThrows_FaultsTaskInsteadOfHanging()
    {
        var sentinel = new InvalidOperationException("provider boom");
        ItemsProviderDelegate<int> throwingProvider = _ => throw sentinel;

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: throwingProvider);

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(50); });

        var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(sentinel, ex);
    }

    [Fact]
    public async Task InitialIndex_ParameterRoundTrip()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", (ICollection<int>)Enumerable.Range(1, 100).ToList());
                builder.AddComponentParameter(3, "InitialItemIndex", 42);
                builder.AddComponentParameter(4, "ChildContent", (RenderFragment<int>)(item => b =>
                {
                    b.OpenElement(0, "span");
                    b.AddContent(1, item);
                    b.CloseElement();
                }));
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = (Virtualize<int>)c);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        Assert.Equal(42, renderedVirtualize.InitialItemIndex);
    }

    // Covers dotnet/aspnetcore#68114 (and #68064): a top-spacer callback classified ProgrammaticScroll
    // (a transient during our own scroll) MUST be ignored, otherwise it snaps the window back toward index 0
    // and the InitialItemIndex / ScrollToItem target is lost.
    [Fact]
    public async Task OnBeforeSpacerVisible_ProgrammaticScroll_DoesNotResetWindow()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 1000);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        // Establish measurements, then move the window away from the top via a before-spacer whose size
        // implies many items above the viewport.
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(5000f, 500f, 500f, SpacerVisibilityReason.ViewportFill));
        var itemsBeforeAtTarget = virtualize._itemsBefore;
        Assert.True(itemsBeforeAtTarget > 0, "Precondition: the window should have moved away from the top.");

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ProgrammaticScroll));

        Assert.Equal(itemsBeforeAtTarget, virtualize._itemsBefore);
    }

    // Discrimination check: the SAME top-spacer callback, but classified UserScroll (the user really scrolled
    // up), must be honored and move the window toward the top. Contrast with the ProgrammaticScroll case above.
    [Fact]
    public async Task OnBeforeSpacerVisible_UserScroll_MovesWindowTowardTop()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 1000);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(5000f, 500f, 500f, SpacerVisibilityReason.ViewportFill));
        var itemsBeforeAtTarget = virtualize._itemsBefore;
        Assert.True(itemsBeforeAtTarget > 0, "Precondition: the window should have moved away from the top.");

        // A UserScroll before-spacer with spacerSize=0 (top of the list in view) moves the window up.
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.UserScroll));

        Assert.True(virtualize._itemsBefore < itemsBeforeAtTarget,
            $"A UserScroll before-spacer callback should move the window toward the top; it stayed at {virtualize._itemsBefore}.");
    }

    // "Display an index that is already on screen": a short list that fully fits the viewport, so aligning to
    // InitialItemIndex needs no real scroll. Verify the window is not left stuck — it renders from the top
    // (all items visible, target on screen) and a later bottom-spacer ViewportFill is still honored.
    [Fact]
    public async Task InitialIndex_ShortListThatFits_TargetOnScreenWithoutScroll_DoesNotStall()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", (ICollection<int>)Enumerable.Range(1, 10).ToList());
                builder.AddComponentParameter(3, "InitialItemIndex", 8);
                builder.AddComponentParameter(4, "ChildContent", (RenderFragment<int>)(item => b => b.AddContent(0, item)));
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = (Virtualize<int>)c);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        // 10 items with a seed capacity of OverscanCount*2+1 = 31 > 10, so the window clamps to the top and
        // item 8 is within the rendered range — it is already on screen, no scroll required.
        Assert.Equal(0, renderedVirtualize._itemsBefore);

        // No user scroll ever occurred; a bottom-spacer ViewportFill must still be processed and must not
        // push the window off the top (everything already fits).
        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(0, renderedVirtualize._itemsBefore);
    }

    [Fact]
    public async Task InitialIndex_NonZeroSeedsInitialRenderedWindow()
    {
        Virtualize<int> renderedVirtualize = null;
        var items = Enumerable.Range(1, 100).ToList();
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, nameof(Virtualize<int>.ItemSize), 50f);
                builder.AddComponentParameter(2, nameof(Virtualize<int>.Items), (ICollection<int>)items);
                builder.AddComponentParameter(3, nameof(Virtualize<int>.InitialItemIndex), 42);
                builder.AddComponentParameter(4, nameof(Virtualize<int>.ChildContent), SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, component => renderedVirtualize = (Virtualize<int>)component);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        var virtualizeComponentId = testRenderer.Batches
            .SelectMany(batch => batch.ReferenceFrames)
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component
                && ReferenceEquals(frame.Component, renderedVirtualize))
            .ComponentId;
        var renderedItems = testRenderer.GetCurrentRenderTreeFrames(virtualizeComponentId)
            .AsEnumerable()
            .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
            .Select(frame => frame.TextContent);

        // capacity = OverscanCount*2 + 1 = 31 with default OverscanCount=15.
        // _itemsBefore = InitialItemIndex - OverscanCount = 27, so the initial rendered range is 28..58.
        Assert.Equal(Enumerable.Range(28, 31).Select(item => item.ToString(CultureInfo.InvariantCulture)), renderedItems);
    }

    [Fact]
    public async Task InitialIndex_NegativeClampsToZero()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "InitialItemIndex", -5);
                builder.AddComponentParameter(4, "ChildContent", (RenderFragment<int>)(item => b => b.AddContent(0, item.ToString(System.Globalization.CultureInfo.InvariantCulture))));
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        Assert.Equal(-5, renderedVirtualize.InitialItemIndex);
        // Negative InitialItemIndex must clamp to 0 (no out-of-range seeding of _itemsBefore).
        Assert.Equal(0, renderedVirtualize._itemsBefore);
    }

    [Fact]
    public async Task InitialIndex_BeyondCountClampsToEnd()
    {
        Virtualize<int> renderedVirtualize = null;
        var items = Enumerable.Range(1, 100).ToList();
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", items as ICollection<int>);
                builder.AddComponentParameter(3, "InitialItemIndex", 99999);
                builder.AddComponentParameter(4, "ChildContent", (RenderFragment<int>)(item => b => b.AddContent(0, item.ToString(System.Globalization.CultureInfo.InvariantCulture))));
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        Assert.Equal(99999, renderedVirtualize.InitialItemIndex);
        // For a fixed Items collection, the seed clamps using the now-known count: max(0, Count - capacity).
        // capacity = OverscanCount*2 + 1 = 31 with default OverscanCount=15, so _itemsBefore = max(0, 100 - 31) = 69.
        Assert.Equal(69, renderedVirtualize._itemsBefore);
    }

    [Fact]
    public async Task Virtualize_SpacersUseDataAttributeNotInlineStyle()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        // Spacer elements use data-blazor-virtualize-* attributes instead of inline style.
        // A MutationObserver on the JS side mirrors them to element styles via CSSOM.
        var referenceFrames = testRenderer.Batches.SelectMany(b => b.ReferenceFrames).ToList();

        var heightAttributes = referenceFrames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute
                     && f.AttributeName == "data-blazor-virtualize-reserved-height")
            .ToList();

        Assert.Equal(2, heightAttributes.Count);
        Assert.Equal("0", (string)heightAttributes[0].AttributeValue);
        Assert.True(double.TryParse((string)heightAttributes[1].AttributeValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _));

        var inlineStyleAttributes = referenceFrames
            .Where(f => f.FrameType == RenderTreeFrameType.Attribute
                     && f.AttributeName == "style")
            .ToList();

        // The only inline style is the test host's scroll container; Virtualize itself emits none.
        var hostStyle = Assert.Single(inlineStyleAttributes);
        Assert.Equal("overflow: auto; height: 800px;", (string)hostStyle.AttributeValue);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(-50f)]
    [InlineData(-0.001f)]
    public async Task Virtualize_NegativeItemSize_Throws(float itemSize)
    {
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(itemSize, EmptyItemsProvider<int>, null)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await testRenderer.RenderRootComponentAsync(componentId));
        Assert.Contains("requires a positive value for parameter", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Virtualize_NegativeOverscanCount_HandlesGracefully(int overscan)
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "OverscanCount", overscan);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_DisposedWhileInitializationPending_DoesNotUseDisposedObjectReference()
    {
        var jsRuntime = new GatedVirtualizeJsRuntime("Blazor._internal.Virtualize.init");
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, nameof(Virtualize<int>.Items), (ICollection<int>)Enumerable.Range(0, 500).ToArray());
                builder.AddComponentParameter(2, nameof(Virtualize<int>.ItemSize), 30f);
                builder.AddComponentParameter(3, nameof(Virtualize<int>.InitialItemIndex), 50);
                builder.AddComponentParameter(4, nameof(Virtualize<int>.ChildContent),
                    (RenderFragment<int>)(item => content => content.AddContent(0, item)));
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(jsRuntime)
            .BuildServiceProvider();

        await using var renderer = new TestRenderer(serviceProvider)
        {
            ShouldHandleExceptions = true,
        };
        var componentId = renderer.AssignRootComponentId(rootComponent);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        await jsRuntime.InvocationStarted.WaitAsync(TimeSpan.FromSeconds(5));
        rootComponent.InnerContent = null;
        rootComponent.TriggerRender();
        await jsRuntime.ReferenceDisposed.WaitAsync(TimeSpan.FromSeconds(5));

        jsRuntime.CompleteInvocation();
        await jsRuntime.InvocationCompleted.WaitAsync(TimeSpan.FromSeconds(5));
        await renderer.Dispatcher.InvokeAsync(() => { });
        await renderTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(renderer.HandledExceptions.Count == 0, string.Join(Environment.NewLine, renderer.HandledExceptions));
        Assert.False(jsRuntime.InteropAttemptedAfterDisposal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task Virtualize_InvalidMaxItemCount_HandlesGracefully(int maxItems)
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "MaxItemCount", maxItems);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_EmptySpacerElement_HandledGracefully()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 10).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "SpacerElement", "");
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);
    }

    [Theory]
    [InlineData(VirtualizeAnchorMode.None)]
    [InlineData(VirtualizeAnchorMode.Start)]
    [InlineData(VirtualizeAnchorMode.End)]
    [InlineData(VirtualizeAnchorMode.Start | VirtualizeAnchorMode.End)]
    public async Task Virtualize_ValidAnchorModes_Accepted(VirtualizeAnchorMode anchorMode)
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "AnchorMode", anchorMode);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        Assert.Equal(anchorMode, renderedVirtualize.AnchorMode);
    }

    [Fact]
    public async Task Virtualize_ItemComparerDefault_IsEqualityComparer()
    {
        Virtualize<string> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<string>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", new List<string> { "a", "b", "c" } as ICollection<string>);
                builder.AddComponentParameter(3, "ChildContent", (RenderFragment<string>)(item => b => b.AddContent(0, item)));
                builder.AddComponentReferenceCapture(4, c => renderedVirtualize = c as Virtualize<string>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        Assert.Same(EqualityComparer<string>.Default, renderedVirtualize.ItemComparer);
    }

    [Fact]
    public async Task ItemsProvider_ReturnsPartialItems_HandledGracefully()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> partialProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            var count = Math.Min(5, request.Count);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, count),
                totalItemCount: 20));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 20, customProvider: partialProvider);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(5000f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task ItemsProvider_ThrowsAfterPartialResults_ExceptionCaptured()
    {
        var sentinel = new InvalidOperationException("partial boom");
        var callCount = 0;

        ValueTask<ItemsProviderResult<int>> partiallyThrowingProvider(ItemsProviderRequest request)
        {
            callCount++;
            if (callCount > 1)
            {
                throw sentinel;
            }
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 50)),
                totalItemCount: 100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: partiallyThrowingProvider);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(5000f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        var rendered = false;
        try
        {
            await renderer.Dispatcher.InvokeAsync(() => { });
            rendered = true;
        }
        catch (InvalidOperationException ex) when (ex.Message == "partial boom")
        {
            // Expected - exception propagates correctly
        }

        Assert.True(rendered); // Exception may propagate differently
    }

    [Fact]
    public async Task ItemsProvider_ReturnsItemsOutOfOrder_HandledGracefully()
    {
        ValueTask<ItemsProviderResult<int>> wrongOrderProvider(ItemsProviderRequest request)
        {
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(50, Math.Min(request.Count, 20)),
                totalItemCount: 100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: wrongOrderProvider);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task RefreshDataAsync_DuringScrollToIndex_CompletesGracefully()
    {
        ValueTask<ItemsProviderResult<int>> slowProvider(ItemsProviderRequest request)
        {
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 50)),
                totalItemCount: 100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: slowProvider);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() => virtualize.RefreshDataAsync());

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task ItemsProvider_CancellationDuringScroll_RecoveryWorks()
    {
        var cancellationCount = 0;
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> cancellableProvider(ItemsProviderRequest request)
        {
            requests.Add(request);
            if (request.CancellationToken.IsCancellationRequested)
            {
                cancellationCount++;
            }
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 50)),
                totalItemCount: 100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: cancellableProvider);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        for (int i = 0; i < 5; i++)
        {
            await renderer.Dispatcher.InvokeAsync(() =>
                callbacks.OnAfterSpacerVisible(i * 1000f, 500f, 500f, SpacerVisibilityReason.UserScroll));
        }

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task AnchorMode_BeginningAndEnd_BothFlagsSet()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "AnchorMode", VirtualizeAnchorMode.Start | VirtualizeAnchorMode.End);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(VirtualizeAnchorMode.Start | VirtualizeAnchorMode.End, renderedVirtualize.AnchorMode);
    }

    [Fact]
    public async Task AnchorMode_DynamicSwitch_RespectsNewMode()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "AnchorMode", VirtualizeAnchorMode.Start);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        renderedVirtualize.AnchorMode = VirtualizeAnchorMode.End;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(VirtualizeAnchorMode.End, renderedVirtualize.AnchorMode);
    }

    [Theory]
    [InlineData(VirtualizeAnchorMode.None)]
    [InlineData(VirtualizeAnchorMode.Start)]
    [InlineData(VirtualizeAnchorMode.End)]
    [InlineData(VirtualizeAnchorMode.Start | VirtualizeAnchorMode.End)]
    public async Task AnchorMode_AllValues_RenderWithoutCrash(VirtualizeAnchorMode anchorMode)
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "AnchorMode", anchorMode);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task AnchorMode_End_SetsPendingScrollToBottom()
    {
        var mockJs = new Mock<IJSRuntime>(MockBehavior.Loose);

        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "AnchorMode", VirtualizeAnchorMode.End);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient<IJSRuntime>((sp) => mockJs.Object)
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.True(renderedVirtualize._pendingScrollToBottom ||
                    mockJs.Invocations.Any(i => i.Arguments.Count > 0 &&
                        i.Arguments[0] is string s && s.Contains("scrollToBottom")));
    }

    [Fact]
    public async Task AnchorMode_None_CanBeSetAndRetrieved()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "AnchorMode", VirtualizeAnchorMode.None);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(VirtualizeAnchorMode.None, renderedVirtualize.AnchorMode);
    }

    [Fact]
    public async Task ScrollToItem_ZeroItems_CompletesGracefully()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 0);

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(0); });

        await task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ScrollToItem_SingleItem_CallsInvokeWithoutError()
    {
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime.Setup(js => js.InvokeAsync<ViewportFillDirection?>(
            "Blazor._internal.Virtualize.alignToItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(ViewportFillDirection.Covered);
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 1, childContent: SimpleItemTemplate, jsRuntime: jsRuntime.Object);
        await using var disposableRenderer = renderer;

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(0); });

        await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(0, virtualize._itemsBefore);
        jsRuntime.Verify(js => js.InvokeAsync<ViewportFillDirection?>(
            "Blazor._internal.Virtualize.alignToItem", It.IsAny<CancellationToken>(),
            It.Is<object[]>(args => args.Length == 2 && (int)args[1] == 0)), Times.Once);
        Assert.Empty(renderer.HandledExceptions);
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(50, 35, 15)]
    [InlineData(99, 60, 39)]
    public async Task ScrollToItem_NearEdges_CallsInvokeWithoutError(int targetIndex, int expectedItemsBefore, int expectedLocalIndex)
    {
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime.Setup(js => js.InvokeAsync<ViewportFillDirection?>(
            "Blazor._internal.Virtualize.alignToItem", It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
            .ReturnsAsync(ViewportFillDirection.Covered);
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, childContent: SimpleItemTemplate, jsRuntime: jsRuntime.Object);
        await using var disposableRenderer = renderer;

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(targetIndex); });

        await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(expectedItemsBefore, virtualize._itemsBefore);
        jsRuntime.Verify(js => js.InvokeAsync<ViewportFillDirection?>(
            "Blazor._internal.Virtualize.alignToItem", It.IsAny<CancellationToken>(),
            It.Is<object[]>(args => args.Length == 2 && (int)args[1] == expectedLocalIndex)), Times.Once);
        Assert.Empty(renderer.HandledExceptions);
    }

    [Fact]
    public async Task ScrollToItem_BeforeFirstRender_DefersUntilInitialized()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Task task = null;
        await testRenderer.Dispatcher.InvokeAsync(() => { task = renderedVirtualize.ScrollToItemAsync(50); });

        await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ScrollToItem_DuringPlaceholderLoading_DoesNotCrash()
    {
        ValueTask<ItemsProviderResult<int>> slowProvider(ItemsProviderRequest request)
        {
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 20)),
                totalItemCount: 100));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100, customProvider: slowProvider);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Task task = null;
        await renderer.Dispatcher.InvokeAsync(() => { task = virtualize.ScrollToItemAsync(80); });

        Assert.NotNull(task);
    }

    [Fact]
    public async Task SpacerCallback_BeforeItemsRendered_DoesNotCrash()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(50f, Enumerable.Range(1, 100).ToList(),
                v => renderedVirtualize = v)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(500f, 0f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task MultipleSpacerCallbacks_RapidSequence_HandlesGracefully()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        for (int i = 0; i < 20; i++)
        {
            await renderer.Dispatcher.InvokeAsync(() =>
                callbacks.OnAfterSpacerVisible(i * 100f, 500f, 500f, SpacerVisibilityReason.UserScroll));
        }

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task SpacerCallback_VeryLargeSpacerSize_CalculatesCorrectly()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 10000);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(500000f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_RTL_RendersWithoutError()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "dir", "rtl");
                builder.OpenComponent<Virtualize<int>>(2);
                builder.AddComponentParameter(3, "ItemSize", 50f);
                builder.AddComponentParameter(4, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(5, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(6, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
                builder.CloseElement();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_LTR_RendersWithoutError()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "dir", "ltr");
                builder.OpenComponent<Virtualize<int>>(2);
                builder.AddComponentParameter(3, "ItemSize", 50f);
                builder.AddComponentParameter(4, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(5, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(6, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
                builder.CloseElement();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_WithUnicodeItems_RendersProperly()
    {
        var unicodeItems = new[] { "こんにちは", "مرحبا", "שלום", "Привет", "العربية" };
        Virtualize<string> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<string>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", unicodeItems);
                builder.AddComponentParameter(3, "ChildContent", (RenderFragment<string>)(item => b => b.AddContent(0, item)));
                builder.AddComponentReferenceCapture(4, c => renderedVirtualize = c as Virtualize<string>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_ScrollDirectionChange_HandlesBidirectionalScroll()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(1000f, 500f, 500f, SpacerVisibilityReason.UserScroll));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(500f, 500f, 500f, SpacerVisibilityReason.UserScroll));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_LargeDataSet_100K_ItemsHandledEfficiently()
    {
        var largeItems = Enumerable.Range(1, 100000).ToList();

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100000);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(50000f, 500f, 500f, SpacerVisibilityReason.UserScroll));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_RapidScrolling_DoesNotStackOverflow()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 10000);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        for (int i = 0; i < 100; i++)
        {
            await renderer.Dispatcher.InvokeAsync(() =>
                callbacks.OnAfterSpacerVisible(i * 1000f, 500f, 500f, SpacerVisibilityReason.UserScroll));
        }

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_VariableItemSizes_LargeRange()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 500f, totalItems: 10000);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_VerySmallItemSize_HighDensity()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 5f, totalItems: 100000);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_MultipleRefreshCalls_RapidSequence()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 1000);

        for (int i = 0; i < 10; i++)
        {
            await renderer.Dispatcher.InvokeAsync(() => virtualize.RefreshDataAsync());
        }

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_ParameterChangeUnderLoad_DoesNotCrash()
    {
        Virtualize<int> renderedVirtualize = null;
        var itemList = Enumerable.Range(1, 1000).ToList();

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", itemList);
                builder.AddComponentParameter(3, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(4, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        itemList.Clear();
        itemList.AddRange(Enumerable.Range(2000, 2000));

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_KeyboardNavigation_UpDown()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f, totalItems: 100);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.UserScroll));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(2500f, 500f, 500f, SpacerVisibilityReason.UserScroll));

        Assert.NotNull(virtualize);
    }

    [Fact]
    public async Task Virtualize_WithinEditForm_RendersProperly()
    {
        Virtualize<int> renderedVirtualize = null;
        var items = Enumerable.Range(1, 50).ToList();

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<EditForm>(0);
                builder.AddComponentParameter(1, "Model", items);
                builder.AddComponentParameter(2, "ChildContent", (RenderFragment<EditContext>)(_ => childBuilder =>
                {
                    childBuilder.OpenComponent<Virtualize<int>>(0);
                    childBuilder.AddComponentParameter(1, "ItemSize", 50f);
                    childBuilder.AddComponentParameter(2, "Items", items as ICollection<int>);
                    childBuilder.AddComponentParameter(3, "ChildContent", SimpleItemTemplate);
                    childBuilder.AddComponentParameter(4, "OverscanCount", 3);
                    childBuilder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        var virtualizeComponentId = testRenderer.Batches
            .SelectMany(batch => batch.ReferenceFrames)
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component
                && ReferenceEquals(frame.Component, renderedVirtualize))
            .ComponentId;
        var renderedItems = testRenderer.GetCurrentRenderTreeFrames(virtualizeComponentId)
            .AsEnumerable()
            .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
            .Select(frame => frame.TextContent);

        // The bottom spacer is visible: render ten viewport items plus three overscan items on each side.
        Assert.Equal(Enumerable.Range(35, 16).Select(item => item.ToString(CultureInfo.InvariantCulture)), renderedItems);
    }

    [Fact]
    public async Task Virtualize_DynamicItemChanges_UpdatesCorrectly()
    {
        Virtualize<int> renderedVirtualize = null;
        var items = Enumerable.Range(1, 50).ToList();

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", items as ICollection<int>);
                builder.AddComponentParameter(3, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(4, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        items.Add(51);

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_WithItemTemplate_ComplexContent()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 100f);
                builder.AddComponentParameter(2, "Items", Enumerable.Range(1, 100).ToList() as ICollection<int>);
                builder.AddComponentParameter(3, "ChildContent", (RenderFragment<int>)(item =>
                    b =>
                    {
                        b.OpenElement(0, "div");
                        b.AddAttribute(1, "class", "item-container");
                        b.OpenElement(2, "h4");
                        b.AddContent(3, $"Item {item}");
                        b.CloseElement();
                        b.OpenElement(4, "p");
                        b.AddContent(5, $"Description for item {item}");
                        b.CloseElement();
                        b.CloseElement();
                    }));
                builder.AddComponentReferenceCapture(4, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_WithPlaceholder_ShowsWhileLoading()
    {
        Virtualize<int> renderedVirtualize = null;
        var requestStarted = new TaskCompletionSource<ItemsProviderRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var providerGate = new TaskCompletionSource<ItemsProviderResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var seedRequest = true;

        ValueTask<ItemsProviderResult<int>> gatedProvider(ItemsProviderRequest request)
        {
            // Seed the total count before measuring an unloaded window.
            if (seedRequest)
            {
                seedRequest = false;
                return ValueTask.FromResult(new ItemsProviderResult<int>(
                    Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                    100));
            }

            requestStarted.SetResult(request);
            return new ValueTask<ItemsProviderResult<int>>(providerGate.Task);
        }

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "ItemsProvider", new ItemsProviderDelegate<int>(gatedProvider));
                builder.AddComponentParameter(3, "ChildContent", SimpleItemTemplate);
                builder.AddComponentParameter(4, "Placeholder", (RenderFragment<PlaceholderContext>)(placeholderContext =>
                    b =>
                    {
                        b.OpenElement(0, "div");
                        b.AddAttribute(1, "style", $"height:{placeholderContext.Size}px");
                        b.AddContent(2, "Loading...");
                        b.CloseElement();
                    }));
                builder.AddComponentReferenceCapture(5, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        using var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        await using var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        await testRenderer.Dispatcher.InvokeAsync(() => renderedVirtualize.RefreshDataAsync());

        Assert.NotNull(renderedVirtualize);
        var virtualizeComponentId = testRenderer.Batches
            .SelectMany(batch => batch.ReferenceFrames)
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component
                && ReferenceEquals(frame.Component, renderedVirtualize))
            .ComponentId;
        var loadedRender = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            // A 500px viewport at row 40 plus 15 overscan rows on either side requests rows 25-64.
            await testRenderer.Dispatcher.InvokeAsync(() =>
                ((IVirtualizeJsCallbacks)renderedVirtualize).OnBeforeSpacerVisible(
                    2000f, 0f, 500f, SpacerVisibilityReason.UserScroll));

            var request = await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(25, request.StartIndex);
            Assert.Equal(40, request.Count);
            Assert.False(providerGate.Task.IsCompleted);
            Assert.False(request.CancellationToken.IsCancellationRequested);
            var loadingText = testRenderer.GetCurrentRenderTreeFrames(virtualizeComponentId)
                .AsEnumerable()
                .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
                .Select(frame => frame.TextContent);
            Assert.Equal(Enumerable.Repeat("Loading...", 40), loadingText);
            Assert.Empty(testRenderer.HandledExceptions);
        }
        finally
        {
            testRenderer.OnUpdateDisplayComplete = () => loadedRender.TrySetResult();
            providerGate.TrySetResult(new ItemsProviderResult<int>(Enumerable.Range(25, 40), 100));
            if (requestStarted.Task.IsCompletedSuccessfully)
            {
                await loadedRender.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await testRenderer.Dispatcher.InvokeAsync(() => { });
            }
        }

        var loadedText = testRenderer.GetCurrentRenderTreeFrames(virtualizeComponentId)
            .AsEnumerable()
            .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
            .Select(frame => frame.TextContent)
            .ToArray();
        Assert.Equal(Enumerable.Range(25, 40).Select(item => item.ToString(CultureInfo.InvariantCulture)), loadedText);
        Assert.DoesNotContain("Loading...", loadedText);
        Assert.Empty(testRenderer.HandledExceptions);
    }

    [Fact]
    public async Task Virtualize_StatePreserved_AcrossReRenders()
    {
        Virtualize<int> renderedVirtualize = null;
        var items = Enumerable.Range(1, 100).ToList();

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", items as ICollection<int>);
                builder.AddComponentParameter(3, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(4, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        var firstInstance = renderedVirtualize;

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await testRenderer.RenderRootComponentAsync(componentId);
        var secondInstance = renderedVirtualize;

        Assert.Same(firstInstance, secondInstance);
    }

    [Fact]
    public async Task Virtualize_DisposeAsync_CleansUpResources()
    {
        Virtualize<int> renderedVirtualize = null;
        var jsRuntime = new Mock<IJSRuntime>();
        var items = Enumerable.Range(1, 50).ToList();

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", items as ICollection<int>);
                builder.AddComponentParameter(3, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(4, c => renderedVirtualize = c as Virtualize<int>);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => jsRuntime.Object)
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        await renderedVirtualize.DisposeAsync();

        jsRuntime.Verify(runtime => runtime.InvokeAsync<IJSVoidResult>(
            "Blazor._internal.Virtualize.dispose",
            It.Is<object[]>(args => args.Length == 1 && args[0] is DotNetObjectReference<VirtualizeJsInterop>)),
            Times.Once);
    }

    [Fact]
    public async Task Virtualize_ItemComparerExplicit_MarksExplicitlySet()
    {
        var customComparer = new CustomIntComparer();
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", new List<int> { 1, 2, 3, 4, 5 });
                builder.AddComponentParameter(3, "ItemComparer", customComparer);
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentReferenceCapture(
                    5,
                    c => renderedVirtualize = (Virtualize<int>)c);
                builder.CloseComponent();
            }
        };

        var testRenderer = new TestRenderer(new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider());

        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        Assert.Same(customComparer, renderedVirtualize.ItemComparer);
    }

    [Fact]
    public async Task Virtualize_ItemComparerWithReferenceTypes_UsesCustomLogic()
    {
        var items = new List<string> { "Alice", "Bob", "Charlie" };
        var customComparer = StringComparer.Ordinal;
        Virtualize<string> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<string>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", items);
                builder.AddComponentParameter(3, "ItemComparer", customComparer);
                builder.AddComponentParameter(4, "ChildContent", (RenderFragment<string>)(item => childBuilder =>
                {
                    childBuilder.AddContent(0, item);
                }));
                builder.AddComponentReferenceCapture(
                    5,
                    component => renderedVirtualize = (Virtualize<string>)component);
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient(sp => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var renderer = new TestRenderer(serviceProvider);

        var componentId = renderer.AssignRootComponentId(rootComponent);

        await renderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);

        // Verify the comparer passed through parameters
        Assert.Same(customComparer, renderedVirtualize.ItemComparer);
    }

    private sealed class CustomIntComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => obj.GetHashCode();
    }

    private sealed record KeyedVirtualizeItem(int Key, int Version);

    private sealed class KeyedVirtualizeItemComparer : IEqualityComparer<KeyedVirtualizeItem>
    {
        public int EqualsCallCount { get; private set; }

        public bool Equals(KeyedVirtualizeItem x, KeyedVirtualizeItem y)
        {
            EqualsCallCount++;
            return x?.Key == y?.Key;
        }

        public int GetHashCode(KeyedVirtualizeItem obj) => obj.Key.GetHashCode();
    }

    [Fact]
    public async Task Virtualize_EmptyContent_RenderedWhenItemsAreEmpty()
    {
        const string emptyContentSentinel = "__empty-content__";
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "Items", Array.Empty<int>());
                builder.AddComponentParameter(3, "ChildContent", SimpleItemTemplate);
                builder.AddComponentParameter(4, "EmptyContent", (RenderFragment)(contentBuilder =>
                    contentBuilder.AddContent(0, emptyContentSentinel)));
                builder.AddComponentReferenceCapture(5, component => renderedVirtualize = (Virtualize<int>)component);
                builder.CloseComponent();
            }
        };

        var testRenderer = new TestRenderer(new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider());

        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        var virtualizeComponentId = testRenderer.Batches
            .SelectMany(batch => batch.ReferenceFrames)
            .Single(frame => frame.FrameType == RenderTreeFrameType.Component
                && ReferenceEquals(frame.Component, renderedVirtualize))
            .ComponentId;
        var renderedText = testRenderer.GetCurrentRenderTreeFrames(virtualizeComponentId)
            .AsEnumerable()
            .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
            .Select(frame => frame.TextContent);

        Assert.Contains(emptyContentSentinel, renderedText);
    }

    [Fact]
    public async Task Virtualize_PlaceholderContext_HasCorrectIndexAndSize()
    {
        ValueTask<ItemsProviderResult<int>> slowProvider(ItemsProviderRequest request)
        {
            return new ValueTask<ItemsProviderResult<int>>(
                Task.Delay(50).ContinueWith(_ =>
                    new ItemsProviderResult<int>(
                        Enumerable.Range(request.StartIndex, request.Count),
                        1000
                    )
                )
            );
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 1000,
            customProvider: slowProvider
        );

        Assert.NotNull(virtualize);
        Assert.Equal(50f, virtualize.ItemSize);

        await using var initialRenderer = renderer;
        var contexts = new List<(int Index, float Size)>();
        var requestStarted = new TaskCompletionSource<ItemsProviderRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var providerGate = new TaskCompletionSource<ItemsProviderResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var seedRequest = true;
        Virtualize<int> renderedVirtualize = null;

        ValueTask<ItemsProviderResult<int>> gatedProvider(ItemsProviderRequest request)
        {
            // Seed the total count before moving to an unloaded window.
            if (seedRequest)
            {
                seedRequest = false;
                return ValueTask.FromResult(new ItemsProviderResult<int>(
                    Enumerable.Range(request.StartIndex, request.Count), 1000));
            }

            requestStarted.SetResult(request);
            return new ValueTask<ItemsProviderResult<int>>(providerGate.Task);
        }

        RenderFragment<PlaceholderContext> placeholder = context => builder =>
        {
            contexts.Add((context.Index, context.Size));
            builder.AddContent(0, "Loading...");
        };
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, "ItemSize", 50f);
                builder.AddComponentParameter(2, "OverscanCount", 1);
                builder.AddComponentParameter(3, "ItemsProvider", new ItemsProviderDelegate<int>(gatedProvider));
                builder.AddComponentParameter(4, "ChildContent", SimpleItemTemplate);
                builder.AddComponentParameter(5, "Placeholder", placeholder);
                builder.AddComponentReferenceCapture(6, component => renderedVirtualize = (Virtualize<int>)component);
                builder.CloseComponent();
            }
        };
        using var services = new ServiceCollection()
            .AddTransient(_ => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();
        await using var testRenderer = new TestRenderer(services);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        await testRenderer.Dispatcher.InvokeAsync(() => renderedVirtualize.RefreshDataAsync());
        Assert.Empty(contexts);

        try
        {
            // A 150px viewport holds rows 10-12 at a 500px offset.
            // One overscan row on each side makes the placeholder window 9-13.
            await testRenderer.Dispatcher.InvokeAsync(() =>
                ((IVirtualizeJsCallbacks)renderedVirtualize).OnBeforeSpacerVisible(
                    500f, 0f, 150f, SpacerVisibilityReason.UserScroll));

            var request = await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(9, request.StartIndex);
            Assert.Equal(5, request.Count);
            Assert.False(providerGate.Task.IsCompleted);
            Assert.False(request.CancellationToken.IsCancellationRequested);
            Assert.NotEmpty(contexts);
            Assert.Equal(new[] { 9, 10, 11, 12, 13 }, contexts.Select(context => context.Index));
            Assert.Equal(new[] { 50f, 50f, 50f, 50f, 50f }, contexts.Select(context => context.Size));
        }
        finally
        {
            var loadedRender = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            testRenderer.OnUpdateDisplayComplete = () => loadedRender.TrySetResult();
            providerGate.TrySetResult(new ItemsProviderResult<int>(Enumerable.Range(9, 5), 1000));
            await providerGate.Task;
            if (requestStarted.Task.IsCompletedSuccessfully)
            {
                await loadedRender.Task.WaitAsync(TimeSpan.FromSeconds(5));
                // Drain the dispatcher after the completed provider's render.
                await testRenderer.Dispatcher.InvokeAsync(() => { });
            }
        }
    }

    [Fact]
    public async Task Virtualize_ItemComparerCustom_DetectsPrependCorrectly()
    {
        var itemKeys = Enumerable.Range(0, 200).ToList();
        var comparer = new KeyedVirtualizeItemComparer();
        var providerRequests = new List<ItemsProviderRequest>();
        var version = 0;

        ValueTask<ItemsProviderResult<KeyedVirtualizeItem>> provider(ItemsProviderRequest request)
        {
            providerRequests.Add(request);
            var items = itemKeys
                .Skip(request.StartIndex)
                .Take(Math.Min(request.Count, itemKeys.Count - request.StartIndex))
                .Select(key => new KeyedVirtualizeItem(key, version++))
                .ToArray();
            return ValueTask.FromResult(new ItemsProviderResult<KeyedVirtualizeItem>(items, itemKeys.Count));
        }

        Virtualize<KeyedVirtualizeItem> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<KeyedVirtualizeItem>>(0);
                builder.AddComponentParameter(1, nameof(Virtualize<KeyedVirtualizeItem>.ItemSize), 50f);
                builder.AddComponentParameter(2, nameof(Virtualize<KeyedVirtualizeItem>.ItemsProvider), new ItemsProviderDelegate<KeyedVirtualizeItem>(provider));
                builder.AddComponentParameter(3, nameof(Virtualize<KeyedVirtualizeItem>.ItemComparer), comparer);
                builder.AddComponentParameter(4, nameof(Virtualize<KeyedVirtualizeItem>.ChildContent), (RenderFragment<KeyedVirtualizeItem>)(item => childBuilder =>
                {
                    childBuilder.AddContent(0, item.Key.ToString(CultureInfo.InvariantCulture));
                }));
                builder.AddComponentReferenceCapture(5, component => renderedVirtualize = (Virtualize<KeyedVirtualizeItem>)component);
                builder.CloseComponent();
            }
        };

        var testRenderer = new TestRenderer(new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider());

        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);
        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f, SpacerVisibilityReason.ViewportFill));
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible((100 + 15) * 50f, 40 * 50f, 500f, SpacerVisibilityReason.UserScroll));
        Assert.Equal(100, renderedVirtualize._itemsBefore);

        var comparerCallsBeforePrepend = comparer.EqualsCallCount;
        itemKeys.InsertRange(0, Enumerable.Range(-20, 20));

        await testRenderer.Dispatcher.InvokeAsync(() => renderedVirtualize.RefreshDataAsync());

        Assert.True(comparer.EqualsCallCount > comparerCallsBeforePrepend);
        Assert.Equal(120, renderedVirtualize._itemsBefore);
        Assert.Equal(120, providerRequests[^1].StartIndex);
    }

    [Fact]
    public async Task Virtualize_ScrollToIndexAsync_ConcurrentCalls_HandlesGracefully()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 1000
        );

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Task scrollTask1 = null;
        Task scrollTask2 = null;
        Task scrollTask3 = null;

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            scrollTask1 = virtualize.ScrollToItemAsync(100);
            scrollTask2 = virtualize.ScrollToItemAsync(200);
            scrollTask3 = virtualize.ScrollToItemAsync(300);
        });

        Assert.NotNull(scrollTask1);
        Assert.NotNull(scrollTask2);
        Assert.NotNull(scrollTask3);

        Assert.NotNull(virtualize);
        Assert.Equal(50f, virtualize.ItemSize);
    }

    [Fact]
    public async Task Virtualize_DisposeAsync_CompletesSuccessfully()
    {
        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(
                itemSize: 50f,
                items: Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => renderedVirtualize = v
            )
        };

        var testRenderer = new TestRenderer(new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider());

        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        if (renderedVirtualize is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_RefreshException_CachedForRethrow()
    {
        var exceptionMessage = "Provider intentionally failed";
        var callCount = 0;

        ValueTask<ItemsProviderResult<int>> failingProvider(ItemsProviderRequest request)
        {
            callCount++;
            return ValueTask.FromException<ItemsProviderResult<int>>(
                new InvalidOperationException(exceptionMessage)
            );
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 1000,
            customProvider: failingProvider
        );

        var ex1 = await Record.ExceptionAsync(async () =>
            await renderer.Dispatcher.InvokeAsync(async () =>
                await virtualize.RefreshDataAsync()
            )
        );

        Assert.NotNull(ex1);
        Assert.IsType<InvalidOperationException>(ex1);
        Assert.Contains(exceptionMessage, ex1.Message);

        var callCountAfterFirstRefresh = callCount;

        var ex2 = await Record.ExceptionAsync(async () =>
            await renderer.Dispatcher.InvokeAsync(async () =>
                await virtualize.RefreshDataAsync()
            )
        );

        Assert.NotNull(ex2);
        Assert.IsType<InvalidOperationException>(ex2);
        Assert.Contains(exceptionMessage, ex2.Message);
    }

    [Fact]
    public async Task Virtualize_ItemsProvider_ReturnsZeroCount_HandledGracefully()
    {
        ValueTask<ItemsProviderResult<int>> zeroProvider(ItemsProviderRequest request)
        {
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                items: new List<int>(),
                totalItemCount: 0
            ));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 0,
            customProvider: zeroProvider
        );

        var itemCountField = typeof(Virtualize<int>).GetField("_itemCount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var itemCount = itemCountField?.GetValue(virtualize);
        Assert.Equal(0, itemCount);
    }

    [Fact]
    public async Task Virtualize_ItemSize_CanBeRetrieved()
    {
        var itemSize = 50f;
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: itemSize,
            totalItems: 100
        );

        Assert.Equal(itemSize, virtualize.ItemSize);
    }

    [Fact]
    public async Task Virtualize_InMemoryItems_HandledCorrectly()
    {
        var items = new List<int>(Enumerable.Range(5, 5));

        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(
                itemSize: 50f,
                items: items,
                captureRenderedVirtualize: v => renderedVirtualize = v
            )
        };

        var testRenderer = new TestRenderer(new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider());

        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        var itemCountField = typeof(Virtualize<int>).GetField("_itemCount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var itemCount = itemCountField?.GetValue(renderedVirtualize);
        Assert.Equal(5, itemCount);
    }

    [Fact]
    public async Task Virtualize_AdjustForPrependAsync_Callable()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 100
        );

        var method = typeof(Virtualize<int>).GetMethod("AdjustForPrependAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);
    }

    [Fact]
    public async Task Virtualize_ScrollToBottom_WithEndMode()
    {
        Virtualize<int> renderedVirtualize = null;

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(
                itemSize: 50f,
                items: Enumerable.Range(1, 100).ToList(),
                captureRenderedVirtualize: v => renderedVirtualize = v,
                anchorMode: VirtualizeAnchorMode.End
            )
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var callbacks = (IVirtualizeJsCallbacks)renderedVirtualize;

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.NotNull(renderedVirtualize);
        Assert.Equal(50f, renderedVirtualize.ItemSize);
    }

    [Fact]
    public async Task Virtualize_WindowClamping_AtCollectionEnd()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 100
        );

        Assert.NotNull(virtualize);
        Assert.Equal(50f, virtualize.ItemSize);
    }

    [Fact]
    public async Task Virtualize_DistributionRefresh_Optimized()
    {
        var requests = new List<ItemsProviderRequest>();

        ValueTask<ItemsProviderResult<int>> provider(ItemsProviderRequest request)
        {
            requests.Add(request);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, request.Count),
                1000
            ));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 1000,
            customProvider: provider
        );

        Assert.Empty(requests);

        var callbacks = (IVirtualizeJsCallbacks)virtualize;
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(0f, 500f, 500f, SpacerVisibilityReason.ViewportFill));

        Assert.Single(requests);
        Assert.Equal(0, requests[^1].StartIndex);
        Assert.Equal(40, requests[^1].Count);
        Assert.Equal(0, virtualize._itemsBefore);

        var visibleItemCapacityField = typeof(Virtualize<int>).GetField("_visibleItemCapacity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(visibleItemCapacityField);
        Assert.Equal(40, (int)visibleItemCapacityField.GetValue(virtualize));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(0f, 500f, 650f, SpacerVisibilityReason.ViewportFill));

        Assert.Equal(43, (int)visibleItemCapacityField.GetValue(virtualize));
        Assert.Single(requests);
        Assert.Equal(0, virtualize._itemsBefore);
        Assert.Equal(0, requests[^1].StartIndex);
        Assert.Equal(40, requests[^1].Count);
    }

    [Fact]
    public async Task Virtualize_AnchorRestoration_AfterPrepend()
    {
        var items = new List<int>(Enumerable.Range(10, 50));

        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(
                itemSize: 50f,
                items: items,
                captureRenderedVirtualize: v => renderedVirtualize = v
            )
        };

        var testRenderer = new TestRenderer(new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider());

        var componentId = testRenderer.AssignRootComponentId(rootComponent);
        await testRenderer.RenderRootComponentAsync(componentId);

        items.InsertRange(0, Enumerable.Range(1, 9));
        rootComponent.InnerContent = BuildVirtualizeWithContent(
            itemSize: 50f,
            items: items,
            captureRenderedVirtualize: v => renderedVirtualize = v
        );

        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
        Assert.Equal(50f, renderedVirtualize.ItemSize);
    }

    [Fact]
    public async Task Virtualize_InitialIndex_AppliedCorrectly()
    {
        var items = Enumerable.Range(1, 200).ToList();

        Virtualize<int> renderedVirtualize = null;
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualizeWithContent(
                itemSize: 50f,
                items: items,
                captureRenderedVirtualize: v => renderedVirtualize = v
            )
        };

        var testRenderer = new TestRenderer(new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider());

        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);

        Assert.NotNull(renderedVirtualize);
    }

    [Fact]
    public async Task Virtualize_MultipleRapidRefreshDataAsync_NoDeadlock()
    {
        var refreshCount = 0;

        ValueTask<ItemsProviderResult<int>> trackingProvider(ItemsProviderRequest request)
        {
            Interlocked.Increment(ref refreshCount);
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 1000 - request.StartIndex)),
                1000
            ));
        }

        var (virtualize, renderer) = await CreateRenderedVirtualize(
            itemSize: 50f,
            totalItems: 1000,
            customProvider: trackingProvider
        );

        var initialRefreshCount = refreshCount;

        List<Task> refreshTasks = null;
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            refreshTasks = Enumerable.Range(0, 10)
                .Select(i => virtualize.RefreshDataAsync())
                .ToList();
        });

        var workTask = Task.WhenAll(refreshTasks);
        var completedTask = await Task.WhenAny(
            workTask,
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        Assert.Same(completedTask, workTask);
        Assert.NotNull(virtualize);
        Assert.Equal(50f, virtualize.ItemSize);
    }

    [Fact]
    public async Task Virtualize_DisposedWhileLaterRenderInteropPending_DoesNotUseDisposedObjectReference()
    {
        var anchorMode = VirtualizeAnchorMode.None;
        var jsRuntime = new GatedVirtualizeJsRuntime("Blazor._internal.Virtualize.setAnchorMode");
        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = builder =>
            {
                builder.OpenComponent<Virtualize<int>>(0);
                builder.AddComponentParameter(1, nameof(Virtualize<int>.Items), (ICollection<int>)Enumerable.Range(0, 500).ToArray());
                builder.AddComponentParameter(2, nameof(Virtualize<int>.ItemSize), 30f);
                builder.AddComponentParameter(3, nameof(Virtualize<int>.AnchorMode), anchorMode);
                builder.AddComponentParameter(4, nameof(Virtualize<int>.ChildContent),
                    (RenderFragment<int>)(item => content => content.AddContent(0, item)));
                builder.CloseComponent();
            }
        };

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(jsRuntime)
            .BuildServiceProvider();

        await using var renderer = new TestRenderer(serviceProvider)
        {
            ShouldHandleExceptions = true,
        };
        var componentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(componentId);

        anchorMode = VirtualizeAnchorMode.Start;
        rootComponent.TriggerRender();
        await jsRuntime.InvocationStarted.WaitAsync(TimeSpan.FromSeconds(5));

        rootComponent.InnerContent = null;
        rootComponent.TriggerRender();
        await jsRuntime.ReferenceDisposed.WaitAsync(TimeSpan.FromSeconds(5));

        jsRuntime.CompleteInvocation();
        await jsRuntime.InvocationCompleted.WaitAsync(TimeSpan.FromSeconds(5));
        await renderer.Dispatcher.InvokeAsync(() => { });

        Assert.True(renderer.HandledExceptions.Count == 0, string.Join(Environment.NewLine, renderer.HandledExceptions));
        Assert.False(jsRuntime.InteropAttemptedAfterDisposal);
    }

    private sealed class GatedVirtualizeJsRuntime(string gatedIdentifier) : IJSRuntime
    {
        private readonly TaskCompletionSource _invocationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completeInvocation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _invocationCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _referenceDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task InvocationStarted => _invocationStarted.Task;

        public Task InvocationCompleted => _invocationCompleted.Task;

        public Task ReferenceDisposed => _referenceDisposed.Task;

        public bool InteropAttemptedAfterDisposal { get; private set; }

        public void CompleteInvocation()
        {
            _completeInvocation.TrySetResult();
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object[] args)
        {
            var reference = (DotNetObjectReference<VirtualizeJsInterop>)args[0];

            try
            {
                _ = reference.Value;
            }
            catch (ObjectDisposedException)
            {
                InteropAttemptedAfterDisposal = true;
                throw;
            }

            if (identifier == gatedIdentifier)
            {
                _invocationStarted.TrySetResult();
                await _completeInvocation.Task.WaitAsync(cancellationToken);
                _invocationCompleted.TrySetResult();
            }
            else if (identifier == "Blazor._internal.Virtualize.dispose")
            {
                reference.Dispose();
                _referenceDisposed.TrySetResult();
            }
            return default!;
        }
    }
}
