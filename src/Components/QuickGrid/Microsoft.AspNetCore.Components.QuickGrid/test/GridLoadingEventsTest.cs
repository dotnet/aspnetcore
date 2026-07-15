// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class GridLoadingEventsTest
{
    [Fact]
    public void OnDataLoading_And_OnDataLoaded_Properties_Exist_With_Correct_Type()
    {
        var gridType = typeof(QuickGrid<>);

        var onDataLoadingProperty = gridType.GetProperty("OnDataLoading");
        var onDataLoadedProperty = gridType.GetProperty("OnDataLoaded");

        Assert.NotNull(onDataLoadingProperty);
        Assert.Equal(typeof(EventCallback), onDataLoadingProperty!.PropertyType);

        Assert.NotNull(onDataLoadedProperty);
        Assert.Equal(typeof(EventCallback), onDataLoadedProperty!.PropertyType);
    }

    [Fact]
    public void OnDataLoading_And_OnDataLoaded_Are_Declared_As_Parameters()
    {
        var gridType = typeof(QuickGrid<>);

        var onDataLoadingProperty = gridType.GetProperty("OnDataLoading");
        var onDataLoadedProperty = gridType.GetProperty("OnDataLoaded");

        Assert.NotNull(onDataLoadingProperty);
        var loadingAttrs = onDataLoadingProperty!.GetCustomAttributes(typeof(ParameterAttribute), false);
        Assert.NotEmpty(loadingAttrs);

        Assert.NotNull(onDataLoadedProperty);
        var loadedAttrs = onDataLoadedProperty!.GetCustomAttributes(typeof(ParameterAttribute), false);
        Assert.NotEmpty(loadedAttrs);
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
    public async Task OnDataLoading_Fires_When_RefreshDataAsync_IsCalled()
    {
        var loadingFired = 0;
        var ctx = await RenderGridAsync(
            onDataLoading: EventCallback.Factory.Create(this, () => { loadingFired++; return Task.CompletedTask; }));

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
            onDataLoaded: EventCallback.Factory.Create(this, () => { loadedFired++; return Task.CompletedTask; }));

        loadedFired = 0;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () => await ctx.Grid.RefreshDataAsync());

        Assert.True(loadedFired >= 1,
            $"OnDataLoaded should fire after data load completes (fired {loadedFired} times)");
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
    public async Task Multiple_Sequential_Refreshes_Each_Trigger_Separate_Load_Cycles()
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

        Assert.True(afterLoadCycle > beforeLoadCycle, "LoadCycleId should increment for each refresh");
        Assert.True(loadingCount >= 2, $"OnDataLoading should fire for each refresh (fired {loadingCount} times)");
        Assert.True(loadedCount >= 2, $"OnDataLoaded should fire for each refresh (fired {loadedCount} times)");
    }

    [Fact]
    public async Task Multiple_Concurrent_Refreshes_Each_Fire_OnDataLoading()
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
    public async Task Virtualized_Grid_Fires_OnDataLoaded_Exactly_Once_Per_Load_Cycle()
    {
        var loadedCount = 0;
        var onDataLoaded = EventCallback.Factory.Create(this,
            () => { loadedCount++; return Task.CompletedTask; });
        var ctx = await RenderVirtualizedGridAsync(onDataLoaded: onDataLoaded);

        loadedCount = 0;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });

        await Task.Delay(300);

        Assert.True(loadedCount == 1,
            $"OnDataLoaded should fire exactly once per load cycle in virtualized scenario, but fired {loadedCount} times");
    }

    [Fact]
    public async Task Virtualized_Grid_OverscanCount_Does_Not_Cause_Duplicate_OnDataLoaded()
    {
        var loadedCount = 0;
        var onDataLoaded = EventCallback.Factory.Create(this,
            () => { loadedCount++; return Task.CompletedTask; });
        var ctx = await RenderVirtualizedGridAsync(onDataLoaded: onDataLoaded);

        loadedCount = 0;
        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });
        await Task.Delay(300);
        var firstLoadCount = loadedCount;

        loadedCount = 0;
        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });
        await Task.Delay(300);
        var secondLoadCount = loadedCount;

        Assert.True(firstLoadCount == 1,
            $"First refresh should fire OnDataLoaded once, but fired {firstLoadCount} times");
        Assert.True(secondLoadCount == 1,
            $"Second refresh should fire OnDataLoaded once, but fired {secondLoadCount} times");
    }

    [Fact]
    public async Task Virtualized_Grid_Events_Fire_In_Correct_Order()
    {
        var eventSequence = new List<string>();
        var onDataLoading = EventCallback.Factory.Create(this,
            () =>
            {
                eventSequence.Add("OnDataLoading");
                return Task.CompletedTask;
            });
        var onDataLoaded = EventCallback.Factory.Create(this,
            () =>
            {
                eventSequence.Add("OnDataLoaded");
                return Task.CompletedTask;
            });

        var ctx = await RenderVirtualizedGridAsync(
            onDataLoading: onDataLoading,
            onDataLoaded: onDataLoaded);

        eventSequence.Clear();

        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });

        await Task.Delay(300);

        Assert.True(eventSequence.Contains("OnDataLoading"),
            "OnDataLoading should fire before data fetch");
        Assert.True(eventSequence.Contains("OnDataLoaded"),
            "OnDataLoaded should fire after data fetch");

        var loadingIndex = eventSequence.IndexOf("OnDataLoading");
        var loadedIndex = eventSequence.IndexOf("OnDataLoaded");
        Assert.True(loadingIndex < loadedIndex,
            $"OnDataLoading (index {loadingIndex}) should fire before OnDataLoaded (index {loadedIndex})");
    }

    [Fact]
    public async Task Cancelled_Refresh_Does_Not_Prevent_Subsequent_Refreshes()
    {
        var loadedCount = 0;
        var onDataLoaded = EventCallback.Factory.Create(this,
            () => { loadedCount++; return Task.CompletedTask; });
        var ctx = await RenderGridAsync(onDataLoaded: onDataLoaded);

        loadedCount = 0;

        var task1 = ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });

        await Task.Delay(50);

        var task2 = ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });

        await Task.WhenAll(task1, task2);

        loadedCount = 0;
        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });

        Assert.True(loadedCount > 0,
            $"Third refresh should fire OnDataLoaded after cancellation, but fired {loadedCount} times");
    }

    [Fact]
    public async Task OnDataLoading_Fires_For_Every_Call_Even_With_Cancellation()
    {
        var loadingCount = 0;
        var onDataLoading = EventCallback.Factory.Create(this,
            () => { loadingCount++; return Task.CompletedTask; });
        var ctx = await RenderGridAsync(onDataLoading: onDataLoading);

        loadingCount = 0;

        var task1 = ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });
        await Task.Delay(30);

        var task2 = ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });
        await Task.Delay(30);

        var task3 = ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await ctx.Grid.RefreshDataAsync();
        });

        await Task.WhenAll(task1, task2, task3);

        Assert.True(loadingCount >= 3,
            $"OnDataLoading should fire for each RefreshDataAsync call even with cancellation (fired {loadingCount} times)");
    }

    [Fact]
    public async Task Load_Cycle_Tracking_Works_Correctly_With_Concurrent_Cancellation()
    {
        var ctx = await RenderGridAsync();

        var loadCycleField = typeof(QuickGrid<object>).GetField("_loadCycleId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var lastNotifiedField = typeof(QuickGrid<object>).GetField("_lastNotifiedLoadCycleId",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(loadCycleField);
        Assert.NotNull(lastNotifiedField);

        var beforeLoadCycle = (int)loadCycleField!.GetValue(ctx.Grid)!;
        var beforeNotified = (int)lastNotifiedField!.GetValue(ctx.Grid)!;

        await ctx.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            var task1 = ctx.Grid.RefreshDataAsync();
            var task2 = ctx.Grid.RefreshDataAsync();
            var task3 = ctx.Grid.RefreshDataAsync();
            await Task.WhenAll(task1, task2, task3);
        });

        var afterLoadCycle = (int)loadCycleField.GetValue(ctx.Grid)!;
        var afterNotified = (int)lastNotifiedField.GetValue(ctx.Grid)!;

        Assert.True(afterLoadCycle > beforeLoadCycle,
            $"LoadCycleId should increment with concurrent calls (before={beforeLoadCycle}, after={afterLoadCycle})");

        Assert.True(afterNotified == 0 || afterLoadCycle == afterNotified,
            $"LastNotifiedLoadCycleId tracking should work with concurrent calls " +
            $"(lastNotified={afterNotified}, current={afterLoadCycle})");

        var incrementAmount = afterLoadCycle - beforeLoadCycle;
        Assert.True(incrementAmount >= 3,
            $"LoadCycleId should increment by at least 3 for 3 concurrent calls " +
            $"(incremented by {incrementAmount})");
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

    private static async Task<RenderedGrid> RenderVirtualizedGridAsync(EventCallback onDataLoading = default, EventCallback onDataLoaded = default)
    {
        var jsRuntime = new SimpleTestJsRuntime();
        var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(jsRuntime)
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .BuildServiceProvider();
        var renderer = new TestRenderer(services);
        var host = new VirtualizedTestHostComponent
        {
            OnDataLoading = onDataLoading,
            OnDataLoaded = onDataLoaded,
        };
        var id = renderer.AssignRootComponentId(host);
        renderer.RenderRootComponent(id);
        await Task.Delay(200);
        return new RenderedGrid(renderer, host.Grid);
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
            builder.AddAttribute(1, nameof(QuickGrid<object>.Items), _items);
            builder.AddAttribute(2, nameof(QuickGrid<object>.OnDataLoading), OnDataLoading);
            builder.AddAttribute(3, nameof(QuickGrid<object>.OnDataLoaded), OnDataLoaded);
            builder.AddComponentReferenceCapture(4, component =>
            {
                Grid = (TestableQuickGrid<object>)component;
            });
            builder.CloseComponent();
        }
    }

    private class VirtualizedTestHostComponent : ComponentBase
    {
        public EventCallback OnDataLoading { get; set; }
        public EventCallback OnDataLoaded { get; set; }
        public TestableQuickGrid<object> Grid { get; private set; } = default!;

        private static readonly IQueryable<object> _items = new List<object>
        {
            new TestItem("A"), new TestItem("B"), new TestItem("C"),
            new TestItem("D"), new TestItem("E"), new TestItem("F"),
            new TestItem("G"), new TestItem("H"), new TestItem("I"),
            new TestItem("J"), new TestItem("K"), new TestItem("L"),
            new TestItem("M"), new TestItem("N"), new TestItem("O"),
            new TestItem("P"), new TestItem("Q"), new TestItem("R"),
            new TestItem("S"), new TestItem("T"), new TestItem("U"),
            new TestItem("V"), new TestItem("W"), new TestItem("X"),
            new TestItem("Y"), new TestItem("Z"),
            new TestItem("AA"), new TestItem("AB"), new TestItem("AC"),
            new TestItem("AD"), new TestItem("AE"), new TestItem("AF"),
            new TestItem("AG"), new TestItem("AH"), new TestItem("AI"),
            new TestItem("AJ"), new TestItem("AK"), new TestItem("AL"),
            new TestItem("AM"), new TestItem("AN"), new TestItem("AO"),
            new TestItem("AP"), new TestItem("AQ"), new TestItem("AR"),
            new TestItem("AS"), new TestItem("AT"), new TestItem("AU"),
            new TestItem("AV"), new TestItem("AW"), new TestItem("AX"),
        }.AsQueryable();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TestableQuickGrid<object>>(0);
            builder.AddAttribute(1, nameof(QuickGrid<object>.Items), _items);
            builder.AddAttribute(2, nameof(QuickGrid<object>.OnDataLoading), OnDataLoading);
            builder.AddAttribute(3, nameof(QuickGrid<object>.OnDataLoaded), OnDataLoaded);
            builder.AddAttribute(4, nameof(QuickGrid<object>.Virtualize), true);
            builder.AddAttribute(5, nameof(QuickGrid<object>.ItemSize), 50f);
            builder.AddAttribute(6, nameof(QuickGrid<object>.OverscanCount), 3);
            builder.AddComponentReferenceCapture(7, component =>
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
