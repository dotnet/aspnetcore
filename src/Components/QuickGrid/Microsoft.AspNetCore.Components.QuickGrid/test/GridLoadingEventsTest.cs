// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class GridLoadingEventsTest
{
    [Fact]
    public void OnDataLoading_EventCallback_Property_Exists()
    {
        var gridType = typeof(QuickGrid<>);
        var property = gridType.GetProperty("OnDataLoading");

        Assert.NotNull(property);
        Assert.Equal(typeof(EventCallback), property!.PropertyType);
    }

    [Fact]
    public void OnDataLoaded_EventCallback_Property_Exists()
    {
        var gridType = typeof(QuickGrid<>);
        var property = gridType.GetProperty("OnDataLoaded");

        Assert.NotNull(property);
        Assert.Equal(typeof(EventCallback), property!.PropertyType);
    }

    [Fact]
    public void OnDataLoading_Is_Declared_As_Parameter()
    {
        var gridType = typeof(QuickGrid<>);
        var property = gridType.GetProperty("OnDataLoading");

        Assert.NotNull(property);
        var attrs = property!.GetCustomAttributes(typeof(ParameterAttribute), false);
        Assert.NotEmpty(attrs);
    }

    [Fact]
    public void OnDataLoaded_Is_Declared_As_Parameter()
    {
        var gridType = typeof(QuickGrid<>);
        var property = gridType.GetProperty("OnDataLoaded");

        Assert.NotNull(property);
        var attrs = property!.GetCustomAttributes(typeof(ParameterAttribute), false);
        Assert.NotEmpty(attrs);
    }

    [Fact]
    public void RefreshDataAsync_Is_Public_Method_Returning_Task()
    {
        var gridType = typeof(QuickGrid<>);
        var method = gridType.GetMethod("RefreshDataAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.True(method!.IsPublic, "RefreshDataAsync should be a public method");
        Assert.Equal(typeof(Task), method.ReturnType);
    }

    [Fact]
    public void RefreshDataCoreAsync_Is_Private_With_RaiseEvents_Parameter()
    {
        var gridType = typeof(QuickGrid<>);
        var method = gridType.GetMethod("RefreshDataCoreAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.False(method!.IsPublic, "RefreshDataCoreAsync should be private");

        var parameters = method.GetParameters();
        var raiseEventsParam = Assert.Single(parameters);
        Assert.Equal("raiseEvents", raiseEventsParam.Name);
        Assert.Equal(typeof(bool), raiseEventsParam.ParameterType);
        Assert.True(raiseEventsParam.HasDefaultValue, "raiseEvents should default to true for backward compatibility");
        Assert.Equal(true, raiseEventsParam.DefaultValue);
    }

    [Fact]
    public void LoadCycleId_Fields_Exist_For_Duplicate_Prevention()
    {
        var gridType = typeof(QuickGrid<>);

        var loadCycleField = gridType.GetField("_loadCycleId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var lastNotifiedField = gridType.GetField("_lastNotifiedLoadCycleId",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(loadCycleField);
        Assert.NotNull(lastNotifiedField);
        Assert.Equal(typeof(int), loadCycleField!.FieldType);
        Assert.Equal(typeof(int), lastNotifiedField!.FieldType);
    }

    [Fact]
    public void ProvideVirtualizedItems_Is_Private_Method_Returning_ValueTask()
    {
        var gridType = typeof(QuickGrid<>);
        var method = gridType.GetMethod("ProvideVirtualizedItems",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.True(method!.IsPrivate);
        Assert.Contains("ValueTask", method.ReturnType.Name);
    }

    [Fact]
    public void ProvideVirtualizedItems_Accepts_ItemsProviderRequest_Parameter()
    {
        var gridType = typeof(QuickGrid<>);
        var method = gridType.GetMethod("ProvideVirtualizedItems",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        var requestParam = Assert.Single(parameters);
        Assert.Equal("request", requestParam.Name);
        Assert.Equal(typeof(ItemsProviderRequest), requestParam.ParameterType);
    }

    private static async Task<RenderedGrid> RenderGridAsync(EventCallback onDataLoading = default, EventCallback onDataLoaded = default)
    {
        var jsRuntime = new SimpleTestJsRuntime();
        var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(jsRuntime)
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .BuildServiceProvider();
        var renderer = new TestRenderer(services);

        var host = new TestHostComponent
        {
            OnDataLoading = onDataLoading,
            OnDataLoaded = onDataLoaded,
        };

        var id = renderer.AssignRootComponentId(host);
        renderer.RenderRootComponent(id);

        await Task.Delay(200);

        return new RenderedGrid(renderer, host.Grid);
    }

    [Fact]
    public async Task OnDataLoading_Fires_When_RefreshDataAsync_IsCalled()
    {
        var loadingFired = 0;
        var ctx = await RenderGridAsync(
            onDataLoading: EventCallback.Factory.Create(this, () => loadingFired++));

        loadingFired = 0;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());

        Assert.True(loadingFired >= 1,
            $"OnDataLoading should fire when RefreshDataAsync is called explicitly (fired {loadingFired} times)");
    }

    [Fact]
    public async Task OnDataLoaded_Fires_After_Data_Load_Completes()
    {
        var loadedFired = 0;
        var ctx = await RenderGridAsync(
            onDataLoaded: EventCallback.Factory.Create(this, () => loadedFired++));

        loadedFired = 0;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());

        Assert.True(loadedFired >= 1,
            $"OnDataLoaded should fire after data load completes (fired {loadedFired} times)");
    }

    [Fact]
    public async Task RefreshDataAsync_Without_Handlers_Does_Not_Throw()
    {
        var ctx = await RenderGridAsync();

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());
    }

    [Fact]
    public async Task Multiple_RefreshDataAsync_Calls_All_Invoke_OnDataLoaded()
    {
        var loadedFired = 0;
        var ctx = await RenderGridAsync(
            onDataLoaded: EventCallback.Factory.Create(this, () => loadedFired++));

        loadedFired = 0;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());
        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());
        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());

        Assert.True(loadedFired >= 3,
            $"OnDataLoaded should fire for each RefreshDataAsync call, but only fired {loadedFired} times");
    }

    [Fact]
    public async Task RefreshDataAsync_Increments_LoadCycleId()
    {
        var ctx = await RenderGridAsync();

        var loadCycleField = typeof(QuickGrid<object>).GetField("_loadCycleId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(loadCycleField);
        var before = (int)loadCycleField!.GetValue(ctx.Grid)!;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());

        var after = (int)loadCycleField.GetValue(ctx.Grid)!;
        Assert.True(after > before,
            $"LoadCycleId should increment on RefreshDataAsync (before={before}, after={after})");
    }

    [Fact]
    public async Task OnDataLoading_And_OnDataLoaded_Fire_In_Correct_Order()
    {
        var eventSequence = new List<string>();
        var onDataLoading = EventCallback.Factory.Create(this,
            () => { eventSequence.Add("OnDataLoading"); return Task.CompletedTask; });
        var onDataLoaded = EventCallback.Factory.Create(this,
            () => { eventSequence.Add("OnDataLoaded"); return Task.CompletedTask; });

        var ctx = await RenderGridAsync(onDataLoading: onDataLoading, onDataLoaded: onDataLoaded);

        eventSequence.Clear();

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());

        Assert.True(eventSequence.Count >= 2,
            $"Both events should fire (fired {eventSequence.Count} events)");
        Assert.Equal("OnDataLoading", eventSequence[0]);
        Assert.Equal("OnDataLoaded", eventSequence[1]);
    }

    [Fact]
    public async Task Concurrent_RefreshDataAsync_Calls_Fire_OnDataLoading_Multiple_Times()
    {
        var loadingCount = 0;
        var onDataLoading = EventCallback.Factory.Create(this,
            () => { loadingCount++; return Task.CompletedTask; });

        var ctx = await RenderGridAsync(onDataLoading: onDataLoading);

        loadingCount = 0;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            var task1 = ctx.Grid.RefreshDataAsync();
            var task2 = ctx.Grid.RefreshDataAsync();
            var task3 = ctx.Grid.RefreshDataAsync();
            await Task.WhenAll(task1, task2, task3);
        });

        Assert.True(loadingCount >= 3,
            $"OnDataLoading should fire for each RefreshDataAsync call (fired {loadingCount} times)");
    }

    [Fact]
    public async Task RefreshDataAsync_With_Data_Provider_Executes_Successfully()
    {
        var loadingCount = 0;
        var loadedCount = 0;
        var onDataLoading = EventCallback.Factory.Create(this,
            () => { loadingCount++; return Task.CompletedTask; });
        var onDataLoaded = EventCallback.Factory.Create(this,
            () => { loadedCount++; return Task.CompletedTask; });

        var ctx = await RenderGridAsync(onDataLoading: onDataLoading, onDataLoaded: onDataLoaded);

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());

        Assert.True(loadingCount > 0, "OnDataLoading should fire at least once");
        Assert.True(loadedCount > 0, "OnDataLoaded should fire at least once");
        Assert.True(loadingCount <= loadedCount, "OnDataLoading should fire before or same number as OnDataLoaded");
    }

    [Fact]
    public async Task Multiple_Refresh_With_Event_Handlers_Tracks_Call_Count()
    {
        var loadingCount = 0;
        var loadedCount = 0;

        var onDataLoading = EventCallback.Factory.Create(this,
            () => { loadingCount++; return Task.CompletedTask; });
        var onDataLoaded = EventCallback.Factory.Create(this,
            () => { loadedCount++; return Task.CompletedTask; });

        var ctx = await RenderGridAsync(onDataLoading: onDataLoading, onDataLoaded: onDataLoaded);

        var loadCycleField = typeof(QuickGrid<object>).GetField("_loadCycleId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(loadCycleField);

        var beforeLoadCycle = (int)loadCycleField!.GetValue(ctx.Grid)!;
        loadingCount = 0;
        loadedCount = 0;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
            await ctx.Grid.RefreshDataAsync();
        });

        var afterLoadCycle = (int)loadCycleField.GetValue(ctx.Grid)!;

        Assert.True(afterLoadCycle > beforeLoadCycle, "LoadCycleId should increment");
        Assert.True(loadingCount >= 2, $"OnDataLoading should fire for each refresh (fired {loadingCount} times)");
        Assert.True(loadedCount >= 2, $"OnDataLoaded should fire for each refresh (fired {loadedCount} times)");
    }

    private class RenderedGrid
    {
        public TestRenderer Renderer { get; }
        public TestableQuickGrid<object> Grid { get; }
        public RenderedGrid(TestRenderer renderer, TestableQuickGrid<object> grid)
        {
            Renderer = renderer;
            Grid = grid;
        }
    }

    private class TestHostComponent : ComponentBase
    {
        public EventCallback OnDataLoading { get; set; }
        public EventCallback OnDataLoaded { get; set; }

        public TestableQuickGrid<object> Grid { get; private set; } = default!;

        private static readonly IQueryable<object> _items = new List<object>
        {
            new TestItem("A"), new TestItem("B"), new TestItem("C")
        }.AsQueryable();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TestableQuickGrid<object>>(0);
            builder.AddAttribute(1, "Items", _items);
            builder.AddAttribute(2, "OnDataLoading", OnDataLoading);
            builder.AddAttribute(3, "OnDataLoaded", OnDataLoaded);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<PropertyColumn<object, string>>(0);
                b.AddAttribute(1, "Property",
                    (System.Linq.Expressions.Expression<Func<object, string>>)(item => ((TestItem)item).Name));
                b.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(5, component =>
            {
                Grid = (TestableQuickGrid<object>)component;
            });
            builder.CloseComponent();
        }
    }

    private class TestableQuickGrid<TGridItem> : QuickGrid<TGridItem>
    {
    }

    private class TestItem
    {
        public string Name { get; }
        public TestItem(string name) => Name = name;
    }

    private class SimpleTestJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            if (identifier == "import")
            {
                return ValueTask.FromResult((TValue)(object)new TestJsObjectReference());
            }
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }

    private class TestJsObjectReference : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
