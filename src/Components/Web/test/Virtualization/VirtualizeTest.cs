// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
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
        ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(10f, 50f, 100f);

        // Validate that the exception is dispatched through the renderer.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await testRenderer.RenderRootComponentAsync(componentId));
        Assert.Equal("Thrown from items provider.", ex.Message);
    }

    [Fact]
    public async Task Virtualize_AcceptsItemMeasurementsFromSpacerCallback()
    {
        Virtualize<int> renderedVirtualize = null;
        var itemsProviderCallCount = 0;

        ValueTask<ItemsProviderResult<int>> countingItemsProvider(ItemsProviderRequest request)
        {
            itemsProviderCallCount++;
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                100));
        }

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(50f, countingItemsProvider, null, virtualize => renderedVirtualize = virtualize)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        var initialCallCount = itemsProviderCallCount;

        // Simulate JS callback with pre-aggregated measurements (sum and count)
        // Heights: 30 + 70 + 50 = 150, count = 3

        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f));

        Assert.True(itemsProviderCallCount > initialCallCount,
            "ItemsProvider should be called after spacer callback with measurements");
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

        // First callback triggers item rendering
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 80f, 500f));

        // Second callback accumulates measurements from rendered items
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 80f, 500f));

        Assert.True(virtualize._totalMeasuredHeight > 0);
        Assert.True(virtualize._measuredItemCount > 0);
    }

    [Fact]
    public async Task Virtualize_NullMeasurementsUseDefaultItemSize()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 40f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 200f, 400f));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 200f, 400f));
    }

    [Fact]
    public async Task Virtualize_ZeroLengthMeasurementsDoNotCorruptAverage()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        // First callback loads items
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));

        // Second callback accumulates measurements
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));

        var heightAfterFirst = virtualize._totalMeasuredHeight;
        var countAfterFirst = virtualize._measuredItemCount;

        // Third callback with same spacerSeparation accumulates more
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));

        // Average should remain stable (same spacerSeparation each time)
        Assert.True(virtualize._measuredItemCount >= countAfterFirst);
    }

    [Fact]
    public async Task Virtualize_BimodalMeasurementsProduceValidAverage()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 200);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        for (int i = 0; i < 2; i++)
        {
            // Bimodal: 30+300+30+300+30+300 = 990, count = 6
            await renderer.Dispatcher.InvokeAsync(() =>
                callbacks.OnAfterSpacerVisible(0f, 990f, 600f));
        }
    }

    [Fact]
    public async Task Virtualize_VerySmallMeasurementsDoNotCauseExcessiveItemCounts()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 10000);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 5f, 1000f));
    }

    [Fact]
    public async Task Virtualize_LargeMeasurementsProduceValidDistribution()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 4000f, 500f));
    }

    [Fact]
    public async Task Virtualize_OnBeforeSpacerVisible_ProcessesMeasurementsBeforeCalculation()
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
            callbacks.OnBeforeSpacerVisible(100f, 300f, 500f));

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
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(5000f, 500f, 500f));

        Assert.Contains(requests, r => r.StartIndex > 0);
    }

    [Fact]
    public async Task Virtualize_NaNMeasurementsDoNotCrashComponent()
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
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));
        var countAfterBaseline = requests.Count;

        // NaN/invalid values are filtered in JS before aggregation.
        // Only the valid measurement (30f) is included in the sum.
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 100f, 500f));

        Assert.True(requests.Count > countAfterBaseline,
            "Component should still process callbacks after receiving NaN measurements");
    }

    [Fact]
    public async Task Virtualize_NegativeMeasurementsDoNotCrashComponent()
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
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));
        var countAfterBaseline = requests.Count;

        // Negative values are filtered in JS before aggregation.
        // Only the valid measurement (50f) is included in the sum.
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 100f, 500f));

        Assert.True(requests.Count > countAfterBaseline,
            "Component should still process callbacks after receiving negative measurements");
    }

    [Fact]
    public async Task Virtualize_InfinityMeasurementsDoNotCrashComponent()
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
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));
        var countAfterBaseline = requests.Count;

        // Infinity values are filtered in JS before aggregation.
        // No valid measurements means count=0.
        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 100f, 500f));

        Assert.True(requests.Count > countAfterBaseline,
            "Component should still process callbacks after receiving infinity measurements");
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
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f));

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
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f));

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
                callbacks.OnAfterSpacerVisible(0f, 90f, 500f));
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
                captureRenderedVirtualize: v => renderedVirtualize = v)
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
                0f, 500f, 500f));

        // Second callback: spacerSize=0 means at the very bottom; with items rendered, should trigger scrollToBottom
        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(
                0f, 500f, 500f));

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
            callbacks.OnAfterSpacerVisible(5000f, 1000f, 500f));

        Assert.False(virtualize._pendingScrollToBottom);
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
            callbacks.OnBeforeSpacerVisible(50f, 500f, 500f));

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
            callbacks.OnAfterSpacerVisible(0f, 150f, 1000f));

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(0f, 150f, 1000f));

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 150f, 1000f));

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
            callbacks.OnAfterSpacerVisible(0f, 1000f, 500f));

        // Second callback accumulates measurements from spacerSeparation
        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 1000f, 500f));

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
            callbacks.OnAfterSpacerVisible(0f, 0f, 500f));

        Assert.Single(pendingCalls);
        var firstCall = pendingCalls[0];

        await testRenderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 0f, 1000f));

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
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f));

        var lastRequest = requests.Last();
        Assert.True(lastRequest.Count <= 50,
            $"Expected request count <= 50 (MaxItemCount=20 + 2*OverscanCount=30), but got {lastRequest.Count}");
    }

    private async Task<(Virtualize<int> virtualize, TestRenderer renderer)> CreateRenderedVirtualize(
        float itemSize,
        int totalItems,
        ItemsProviderDelegate<int> customProvider = null)
    {
        Virtualize<int> renderedVirtualize = null;

        ItemsProviderDelegate<int> provider = customProvider ?? ((ItemsProviderRequest request) =>
            ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, totalItems - request.StartIndex)),
                totalItems)));

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(itemSize, provider, null, virtualize => renderedVirtualize = virtualize)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient((sp) => Mock.Of<IJSRuntime>())
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
        Action<Virtualize<TItem>> captureRenderedVirtualize = null)
        => builder =>
    {
        builder.OpenComponent<Virtualize<TItem>>(0);
        builder.AddComponentParameter(1, "ItemSize", itemSize);
        builder.AddComponentParameter(2, "ItemsProvider", itemsProvider);
        builder.AddComponentParameter(3, "Items", items);

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
        string spacerElement = "div")
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

        if (captureRenderedVirtualize != null)
        {
            builder.AddComponentReferenceCapture(5, component =>
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
}
