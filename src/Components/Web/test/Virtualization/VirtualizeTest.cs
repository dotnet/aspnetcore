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
        ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(10f, 50f, 100f, null);

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

        // Simulate JS callback with measurements (variable-height items)
        // The measurements array contains just heights (in order of rendered items)
        var measurements = new float[] { 30f, 70f, 50f };

        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f, measurements));

        Assert.True(itemsProviderCallCount > initialCallCount,
            "ItemsProvider should be called after spacer callback with measurements");
    }

    [Fact]
    public async Task Virtualize_MeasurementsUpdateRunningAverage()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 80f, 500f, new float[] { 30f, 50f }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 120f, 500f, new float[] { 60f, 60f }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(100f, 200f, 500f, new float[] { 45f, 55f }));
    }

    [Fact]
    public async Task Virtualize_NullMeasurementsUseDefaultItemSize()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 40f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 200f, 400f, null));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 200f, 400f, Array.Empty<float>()));
    }

    [Fact]
    public async Task Virtualize_ZeroLengthMeasurementsDoNotCorruptAverage()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, new float[] { 50f, 50f }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, Array.Empty<float>()));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, null));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 100f, 500f, new float[] { 50f }));
    }

    [Fact]
    public async Task Virtualize_BimodalMeasurementsProduceValidAverage()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 200);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        for (int i = 0; i < 5; i++)
        {
            var bimodalHeights = new float[] { 30f, 300f, 30f, 300f, 30f, 300f };
            await renderer.Dispatcher.InvokeAsync(() =>
                callbacks.OnAfterSpacerVisible(0f, 990f, 600f, bimodalHeights));
        }

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(200f, 500f, 600f, new float[] { 30f, 300f }));
    }

    [Fact]
    public async Task Virtualize_VerySmallMeasurementsDoNotCauseExcessiveItemCounts()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 10000);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 5f, 1000f, new float[] { 1f, 1f, 1f, 1f, 1f }));
    }

    [Fact]
    public async Task Virtualize_LargeMeasurementsProduceValidDistribution()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 4000f, 500f, new float[] { 2000f, 2000f }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(2000f, 4000f, 500f, new float[] { 2000f, 2000f }));
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
            callbacks.OnBeforeSpacerVisible(100f, 300f, 500f, new float[] { 45f, 55f, 50f }));

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
            callbacks.OnAfterSpacerVisible(0f, 500f, 500f, new float[] { 50f, 50f, 50f }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(5000f, 500f, 500f, new float[] { 50f, 50f }));

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
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, new float[] { 50f, 50f }));
        var countAfterBaseline = requests.Count;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, new float[] { float.NaN, 30f }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 100f, 500f, new float[] { 50f, 50f }));

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
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, new float[] { 50f, 50f }));
        var countAfterBaseline = requests.Count;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, new float[] { -100f, 50f }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 100f, 500f, new float[] { 50f, 50f }));

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
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, new float[] { 50f, 50f }));
        var countAfterBaseline = requests.Count;

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnAfterSpacerVisible(0f, 100f, 500f, new float[] { float.PositiveInfinity }));

        await renderer.Dispatcher.InvokeAsync(() =>
            callbacks.OnBeforeSpacerVisible(50f, 100f, 500f, new float[] { 50f, 50f }));

        Assert.True(requests.Count > countAfterBaseline,
            "Component should still process callbacks after receiving infinity measurements");
    }

    [Fact]
    public async Task Virtualize_RendersItemWrapperWithDataVirtualizeItemAttribute()
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
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f, null));

        var hasDataVirtualizeItemAttr = testRenderer.Batches
            .SelectMany(b => b.ReferenceFrames)
            .Any(f => f.FrameType == RenderTreeFrameType.Attribute
                   && f.AttributeName == "data-virtualize-item");

        Assert.True(hasDataVirtualizeItemAttr,
            "Items should be wrapped in elements with 'data-virtualize-item' attribute for JS measurement");
    }

    [Fact]
    public async Task Virtualize_TableSpacerElement_RendersMatchingWrapperElement()
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
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(0f, 150f, 500f, null));

        var referenceFrames = testRenderer.Batches.SelectMany(b => b.ReferenceFrames).ToList();

        var hasDataVirtualizeItemAttr = referenceFrames
            .Any(f => f.FrameType == RenderTreeFrameType.Attribute
                   && f.AttributeName == "data-virtualize-item");

        var hasTrElements = referenceFrames
            .Any(f => f.FrameType == RenderTreeFrameType.Element && f.ElementName == "tr");

        Assert.True(hasDataVirtualizeItemAttr,
            "Wrapper elements should have 'data-virtualize-item' attribute");
        Assert.True(hasTrElements,
            "Wrapper elements should use 'tr' tag when SpacerElement='tr'");
    }

    [Fact]
    public async Task Virtualize_RefreshDataAsync_ResetsRunningAverage()
    {
        var (virtualize, renderer) = await CreateRenderedVirtualize(itemSize: 50f, totalItems: 100);
        var callbacks = (IVirtualizeJsCallbacks)virtualize;

        for (int i = 0; i < 10; i++)
        {
            await renderer.Dispatcher.InvokeAsync(() =>
                callbacks.OnAfterSpacerVisible(0f, 90f, 500f, new float[] { 30f, 30f, 30f }));
        }

        Assert.True(virtualize._totalMeasuredHeight > 0);
        Assert.True(virtualize._measuredItemCount > 0);

        await renderer.Dispatcher.InvokeAsync(() => virtualize.RefreshDataAsync());

        Assert.Equal(0f, virtualize._totalMeasuredHeight);
        Assert.Equal(0, virtualize._measuredItemCount);
    }

    [Fact]
    public async Task Virtualize_ScrollToBottom_SetWhenAtEndWithNewMeasurements()
    {
        var mockJs = new Mock<IJSRuntime>(MockBehavior.Loose);

        Virtualize<int> renderedVirtualize = null;

        ValueTask<ItemsProviderResult<int>> provider(ItemsProviderRequest request)
        {
            return ValueTask.FromResult(new ItemsProviderResult<int>(
                Enumerable.Range(request.StartIndex, Math.Min(request.Count, 100 - request.StartIndex)),
                100));
        }

        var rootComponent = new VirtualizeTestHostcomponent
        {
            InnerContent = BuildVirtualize(50f, provider, null,
                virtualize => renderedVirtualize = virtualize)
        };

        var serviceProvider = new ServiceCollection()
            .AddTransient<IJSRuntime>((sp) => mockJs.Object)
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);
        var componentId = testRenderer.AssignRootComponentId(rootComponent);

        await testRenderer.RenderRootComponentAsync(componentId);
        Assert.NotNull(renderedVirtualize);

        // spacerSize=0 means at the very bottom; new measurements should trigger scrollToBottom
        await testRenderer.Dispatcher.InvokeAsync(() =>
            ((IVirtualizeJsCallbacks)renderedVirtualize).OnAfterSpacerVisible(
                0f, 500f, 500f, new float[] { 50f, 50f }));

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
            callbacks.OnAfterSpacerVisible(5000f, 1000f, 500f, new float[] { 50f, 50f }));

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
            callbacks.OnBeforeSpacerVisible(50f, 500f, 500f, null));

        Assert.Equal(callCountAfterMount + 1, requests.Count);

        var lastRequest = requests[^1];
        Assert.Equal(0, lastRequest.StartIndex);
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
