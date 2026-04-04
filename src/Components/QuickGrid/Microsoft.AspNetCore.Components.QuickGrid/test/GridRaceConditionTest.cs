// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class GridRaceConditionTest
{

    [Fact]
    public async Task CanCorrectlyDisposeAsync()
    {
        var moduleLoadCompletion = new TaskCompletionSource();
        var moduleImportStarted = new TaskCompletionSource();
        var testJsRuntime = new TestJsRuntime(moduleLoadCompletion, moduleImportStarted);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(testJsRuntime)
            .BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var testComponent = new SimpleTestComponent();

        var componentId = renderer.AssignRootComponentId(testComponent);
        renderer.RenderRootComponent(componentId);

        // Wait until JS import has started but not completed
        await moduleImportStarted.Task;

        // Dispose component while JS module loading is pending
        testJsRuntime.MarkDisposed();
        await renderer.DisposeAsync();

        // Complete the JS module loading
        moduleLoadCompletion.SetResult();

        // Wait until after OnAfterRenderAsync has completed to test the disposal of the jsModule
        var notFailingGrid = testComponent.NotFailingGrid;
        await notFailingGrid.OnAfterRenderCompleted;

        // Assert that init was not called after disposal and JsModule was disposed of
        Assert.False(testJsRuntime.InitWasCalledAfterDisposal,
            "Init should not be called on a disposed component.");
        Assert.True(testJsRuntime.JsModuleDisposed);
    }

    [Fact]
    public async Task FailingQuickGridCallsInitAfterDisposal()
    {
        var moduleLoadCompletion = new TaskCompletionSource();
        var moduleImportStarted = new TaskCompletionSource();
        var testJsRuntime = new TestJsRuntime(moduleLoadCompletion, moduleImportStarted);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(testJsRuntime)
            .BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);

        var testComponent = new FailingGridTestComponent();

        var componentId = renderer.AssignRootComponentId(testComponent);
        renderer.RenderRootComponent(componentId);

        // Wait until JS import has started but not completed
        await moduleImportStarted.Task;

        // Dispose component while JS module loading is pending
        testJsRuntime.MarkDisposed();
        await renderer.DisposeAsync();

        // Complete the JS module loading - this allows the FailingQuickGrid's OnAfterRenderAsync to continue
        // and demonstrate the race condition by calling init after disposal
        moduleLoadCompletion.SetResult();

        // Wait until after OnAfterRenderAsync has completed, to make sure jsmodule import started and the reported issue is reproduced
        var failingGrid = testComponent.FailingQuickGrid;
        await failingGrid.OnAfterRenderCompleted;

        // Assert that init WAS called after disposal
        // The FailingQuickGrid's OnAfterRenderAsync should have called init despite being disposed
        // The FailingQuickGrid should not have disposed of JsModule 
        Assert.True(testJsRuntime.InitWasCalledAfterDisposal,
            $"FailingQuickGrid should call init after disposal, demonstrating the race condition bug. " +
            $"InitWasCalledAfterDisposal: {testJsRuntime.InitWasCalledAfterDisposal}, " +
            $"DisposeAsyncWasCalled: {failingGrid.DisposeAsyncWasCalled}, " +
            $"_disposeBool is false: {failingGrid.IsWasDisposedFalse()}");
        Assert.False(testJsRuntime.JsModuleDisposed);
    }
}

internal class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal abstract class BaseTestComponent<TGrid> : ComponentBase
    where TGrid : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    protected TGrid _grid;
    public TGrid Grid => _grid;

    private readonly List<Person> _people = [
        new() { Id = 1, Name = "John" },
        new() { Id = 2, Name = "Jane" }
    ];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TGrid>(0);
        builder.AddAttribute(1, "Items", _people.AsQueryable());
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<PropertyColumn<Person, int>>(0);
            b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<Person, int>>)(p => p.Id));
            b.CloseComponent();
        }));
        builder.AddComponentReferenceCapture(3, component => _grid = (TGrid)component);
        builder.CloseComponent();
    }
}

internal class SimpleTestComponent : BaseTestComponent<NotFailingGrid<Person>>
{
    public NotFailingGrid<Person> NotFailingGrid => Grid;
}

internal class FailingGridTestComponent : BaseTestComponent<FailingQuickGrid<Person>>
{
    public FailingQuickGrid<Person> FailingQuickGrid => Grid;
}

internal class TestJsRuntime(TaskCompletionSource moduleCompletion, TaskCompletionSource importStarted) : IJSRuntime
{
    private readonly TaskCompletionSource _moduleCompletion = moduleCompletion;
    private readonly TaskCompletionSource _importStarted = importStarted;
    private bool _disposed;

    public bool JsModuleDisposed { get; private set; }

    public bool InitWasCalledAfterDisposal { get; private set; }

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args = null)
    {
        if (identifier == "import" && args?.Length > 0 && args[0] is string modulePath &&
            modulePath == "./_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js")
        {
            // Signal that import has started
            _importStarted.TrySetResult();

            // Wait for test to control when import completes
            await _moduleCompletion.Task;
            return (TValue)(object)new TestJSObjectReference(this);
        }
        throw new InvalidOperationException($"Unexpected JS call: {identifier}");
    }

    public void MarkDisposed() => _disposed = true;

    public void MarkJsModuleDisposed() => JsModuleDisposed = true;

    public void RecordInitCall()
    {
        if (_disposed)
        {
            InitWasCalledAfterDisposal = true;
        }
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
        InvokeAsync<TValue>(identifier, args);
}

internal class TestJSObjectReference(TestJsRuntime jsRuntime) : IJSObjectReference
{
    private readonly TestJsRuntime _jsRuntime = jsRuntime;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
    {
        if (identifier == "init")
        {
            _jsRuntime.RecordInitCall();
        }
        return default!;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
        InvokeAsync<TValue>(identifier, args);

    public ValueTask DisposeAsync() {
        _jsRuntime.MarkJsModuleDisposed();
        return default!;
    }
}
