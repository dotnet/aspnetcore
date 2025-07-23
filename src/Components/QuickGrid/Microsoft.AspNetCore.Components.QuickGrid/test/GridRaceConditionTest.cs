// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.QuickGrid.Test;

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
        await testComponent.DisposeAsync();

        // Complete the JS module loading
        moduleLoadCompletion.SetResult();

        // Assert that init was not called after disposal (the fix working correctly)
        Assert.False(testJsRuntime.InitWasCalledAfterDisposal,
            "Init should not be called on a disposed component - this indicates the race condition fix is not working");
    }

    [Fact]
    public async Task FailingQuickGrid_CallsInitAfterDisposal_DemonstratesRaceCondition()
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
        await testComponent.DisposeAsync();

        // Verify our FailingQuickGrid's DisposeAsync was called and _disposeBool should still be false
        var failingGrid = testComponent.FailingQuickGrid;
        Assert.NotNull(failingGrid);
        Assert.True(failingGrid.DisposeAsyncWasCalled, "FailingQuickGrid.DisposeAsync should have been called");
        Assert.True(failingGrid.IsDisposeBoolFalse(), "_disposeBool should still be false since we didn't call base.DisposeAsync()");

        // Complete the JS module loading - this allows the FailingQuickGrid's OnAfterRenderAsync to continue
        // and demonstrate the race condition by calling init after disposal
        moduleLoadCompletion.SetResult();

        // Wait for OnAfterRenderAsync to complete - deterministic timing instead of arbitrary delay
        await failingGrid.OnAfterRenderCompleted;

        // Assert that init WAS called after disposal (demonstrating the race condition bug)
        // The FailingQuickGrid's OnAfterRenderAsync should have called init despite being disposed
        Assert.True(testJsRuntime.InitWasCalledAfterDisposal,
            $"FailingQuickGrid should call init after disposal, demonstrating the race condition bug. " +
            $"InitWasCalledAfterDisposal: {testJsRuntime.InitWasCalledAfterDisposal}, " +
            $"DisposeAsyncWasCalled: {failingGrid.DisposeAsyncWasCalled}, " +
            $"_disposeBool is false: {failingGrid.IsDisposeBoolFalse()}");
    }
}

internal class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal class SimpleTestComponent : ComponentBase, IAsyncDisposable
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    private readonly List<Person> _people = [
        new() { Id = 1, Name = "John" },
        new() { Id = 2, Name = "Jane" }
    ];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<QuickGrid<Person>>(0);
        builder.AddAttribute(1, "Items", _people.AsQueryable());
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<PropertyColumn<Person, int>>(0);
            b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<Person, int>>)(p => p.Id));
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }

    public ValueTask DisposeAsync()
    {
        if (JSRuntime is TestJsRuntime testRuntime)
        {
            testRuntime.MarkDisposed();
        }
        return ValueTask.CompletedTask;
    }
}

internal class FailingGridTestComponent : ComponentBase, IAsyncDisposable
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    private FailingQuickGrid<Person> _failingGrid;

    public FailingQuickGrid<Person> FailingQuickGrid => _failingGrid;

    private readonly List<Person> _people = [
        new() { Id = 1, Name = "John" },
        new() { Id = 2, Name = "Jane" }
    ];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<FailingQuickGrid<Person>>(0);
        builder.AddAttribute(1, "Items", _people.AsQueryable());
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<PropertyColumn<Person, int>>(0);
            b.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<Person, int>>)(p => p.Id));
            b.CloseComponent();
        }));
        builder.AddComponentReferenceCapture(3, component => _failingGrid = (FailingQuickGrid<Person>)component);
        builder.CloseComponent();
    }

    public async ValueTask DisposeAsync()
    {
        if (JSRuntime is TestJsRuntime testRuntime)
        {
            testRuntime.MarkDisposed();
        }

        if (_failingGrid is not null)
        {
            await _failingGrid.DisposeAsync();
        }
    }
}

internal class TestJsRuntime(TaskCompletionSource moduleCompletion, TaskCompletionSource importStarted) : IJSRuntime
{
    private readonly TaskCompletionSource _moduleCompletion = moduleCompletion;
    private readonly TaskCompletionSource _importStarted = importStarted;
    private bool _disposed;

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

            // Return a mock IJSObjectReference
            return (TValue)(object)new TestJSObjectReference(this);
        }
        throw new InvalidOperationException($"Unexpected JS call: {identifier}");
    }

    public void MarkDisposed() => _disposed = true;

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

/// <summary>
/// Mock JS object reference for the QuickGrid module
/// </summary>
internal class TestJSObjectReference(TestJsRuntime jsRuntime) : IJSObjectReference
{
    private readonly TestJsRuntime _jsRuntime = jsRuntime;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
    {
        if (identifier == "init")
        {
            _jsRuntime.RecordInitCall();
        }
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask InvokeVoidAsync(string identifier, object[] args)
    {
        if (identifier == "init")
        {
            _jsRuntime.RecordInitCall();
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
        InvokeAsync<TValue>(identifier, args);

    public ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, object[] args) =>
        InvokeVoidAsync(identifier, args);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
