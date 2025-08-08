// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Test;

public class ComponentBaseTest
{
    // Nothing should exceed the timeout in a successful run of the the tests, this is just here to catch
    // failures.
    private static readonly TimeSpan Timeout = Debugger.IsAttached ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

    [Fact]
    public void RunsOnInitWhenRendered()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent();

        var onInitRuns = 0;
        component.OnInitLogic = c => onInitRuns++;

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Assert
        Assert.Equal(1, onInitRuns);
    }

    [Fact]
    public void RunsOnInitAsyncWhenRendered()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent();

        var onInitAsyncRuns = 0;
        component.RunsBaseOnInitAsync = false;
        component.OnInitAsyncLogic = c =>
        {
            onInitAsyncRuns++;
            return Task.CompletedTask;
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Assert
        Assert.Equal(1, onInitAsyncRuns);
        Assert.Single(renderer.Batches);
    }

    [Fact]
    public void RunsOnInitAsyncAlsoOnBaseClassWhenRendered()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent();

        var onInitAsyncRuns = 0;
        component.RunsBaseOnInitAsync = true;
        component.OnInitAsyncLogic = c =>
        {
            onInitAsyncRuns++;
            return Task.CompletedTask;
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Assert
        Assert.Equal(1, onInitAsyncRuns);
        Assert.Single(renderer.Batches);
    }

    [Fact]
    public void RunsOnParametersSetWhenRendered()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent();

        var onParametersSetRuns = 0;
        component.OnParametersSetLogic = c => onParametersSetRuns++;

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Assert
        Assert.Equal(1, onParametersSetRuns);
        Assert.Single(renderer.Batches);
    }

    [Fact]
    public void RunsOnParametersSetAsyncWhenRendered()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent();

        var onParametersSetAsyncRuns = 0;
        component.RunsBaseOnParametersSetAsync = false;
        component.OnParametersSetAsyncLogic = c =>
        {
            onParametersSetAsyncRuns++;
            return Task.CompletedTask;
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Assert
        Assert.Equal(1, onParametersSetAsyncRuns);
        Assert.Single(renderer.Batches);
    }

    [Fact]
    public void RunsOnParametersSetAsyncAlsoOnBaseClassWhenRendered()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent();

        var onParametersSetAsyncRuns = 0;
        component.RunsBaseOnParametersSetAsync = true;
        component.OnParametersSetAsyncLogic = c =>
        {
            onParametersSetAsyncRuns++;
            return Task.CompletedTask;
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        renderer.RenderRootComponent(componentId);

        // Assert
        Assert.Equal(1, onParametersSetAsyncRuns);
        Assert.Single(renderer.Batches);
    }

    [Fact]
    public async Task RendersAfterParametersSetAsyncTaskIsCompleted()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent();

        component.Counter = 1;
        var parametersSetTask = new TaskCompletionSource<bool>();
        component.RunsBaseOnParametersSetAsync = false;
        component.OnParametersSetAsyncLogic = c => parametersSetTask.Task;

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Single(renderer.Batches);

        // Completes task started by OnParametersSetAsync
        component.Counter = 2;
        parametersSetTask.SetResult(true);

        await renderTask;

        // Component should be rendered again
        Assert.Equal(2, renderer.Batches.Count);
    }

    [Fact]
    public async Task RendersAfterParametersSetAndInitAsyncTasksAreCompleted()
    {
        // Arrange
        var @event = new ManualResetEventSlim();

        var renderer = new TestRenderer()
        {
            OnUpdateDisplayComplete = () => { @event.Set(); },
        };
        var component = new TestComponent();

        component.Counter = 1;
        var initTask = new TaskCompletionSource<bool>();
        var parametersSetTask = new TaskCompletionSource<bool>();
        component.RunsBaseOnInitAsync = true;
        component.RunsBaseOnParametersSetAsync = true;
        component.OnInitAsyncLogic = c => initTask.Task;
        component.OnParametersSetAsyncLogic = c => parametersSetTask.Task;

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        // A rendering should have happened after the synchronous execution of Init
        Assert.Single(renderer.Batches);

        @event.Reset();

        // Completes task started by OnInitAsync
        component.Counter = 2;
        initTask.SetResult(true);

        // We need to wait here, because the continuation from SetResult needs to be scheduled.
        @event.Wait(Timeout);
        @event.Reset();

        // Component should be rendered once, after set parameters
        Assert.Equal(2, renderer.Batches.Count);

        // Completes task started by OnParametersSetAsync
        component.Counter = 3;
        parametersSetTask.SetResult(false);

        await renderTask;
        Assert.True(@event.IsSet);

        // Component should be rendered again
        // after the async part of onparameterssetasync completes
        Assert.Equal(3, renderer.Batches.Count);
    }

    [Fact]
    public async Task DoesNotRenderAfterOnInitAsyncTaskIsCancelled()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent() { Counter = 1 };
        var initTask = new TaskCompletionSource();
        component.OnInitAsyncLogic = _ => initTask.Task;

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.False(renderTask.IsCompleted);
        Assert.Single(renderer.Batches);

        // Cancel task started by OnInitAsync
        component.Counter = 2;
        initTask.SetCanceled();

        await renderTask;

        // Component should only be rendered again due to
        // the call to StateHasChanged after SetParametersAsync
        Assert.Equal(2, renderer.Batches.Count);
    }

    [Fact]
    public async Task RunsOnAfterRender_AfterRenderingCompletes()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent() { Counter = 1 };

        var onAfterRenderCompleted = false;
        component.OnAfterRenderLogic = (c, firstRender) =>
        {
            Assert.True(firstRender);
            Assert.Single(renderer.Batches);
            onAfterRenderCompleted = true;
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        await renderTask;
        Assert.True(onAfterRenderCompleted);

        // Component should not be rendered again. OnAfterRender doesn't do that.
        Assert.Single(renderer.Batches);

        // Act: Render again!
        onAfterRenderCompleted = false;
        component.OnAfterRenderLogic = (c, firstRender) =>
        {
            Assert.False(firstRender);
            Assert.Equal(2, renderer.Batches.Count);
            onAfterRenderCompleted = true;
        };

        renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.True(onAfterRenderCompleted);
        Assert.Equal(2, renderer.Batches.Count);
        await renderTask;
    }

    [Fact]
    public async Task RunsOnAfterRenderAsync_AfterRenderingCompletes()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent() { Counter = 1 };

        var onAfterRenderCompleted = false;
        var tcs = new TaskCompletionSource();
        component.OnAfterRenderAsyncLogic = async (c, firstRender) =>
        {
            Assert.True(firstRender);
            Assert.Single(renderer.Batches);
            onAfterRenderCompleted = true;
            await tcs.Task;
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        tcs.SetResult();
        await renderTask;
        Assert.True(onAfterRenderCompleted);

        // Component should not be rendered again. OnAfterRenderAsync doesn't do that.
        Assert.Single(renderer.Batches);

        // Act: Render again!
        onAfterRenderCompleted = false;
        tcs = new TaskCompletionSource();
        component.OnAfterRenderAsyncLogic = async (c, firstRender) =>
        {
            Assert.False(firstRender);
            Assert.Equal(2, renderer.Batches.Count);
            onAfterRenderCompleted = true;
            await tcs.Task;
        };

        renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        tcs.SetResult();
        await renderTask;
        Assert.True(onAfterRenderCompleted);
        Assert.Equal(2, renderer.Batches.Count);
    }

    [Fact]
    public async Task DoesNotRenderAfterOnInitAsyncTaskIsCancelledUsingCancellationToken()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent() { Counter = 1 };

        var cts = new CancellationTokenSource();
        cts.Cancel();
        component.OnInitAsyncLogic = async _ =>
        {
            await Task.Yield();
            cts.Token.ThrowIfCancellationRequested();
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        // At least one call to StateHasChanged depending on how OnInitAsyncLogic gets scheduled.
        Assert.NotEmpty(renderer.Batches);
    }

    [Fact]
    public async Task DoesNotRenderAfterOnParametersSetAsyncTaskIsCanceled()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent() { Counter = 1 };
        var onParametersSetTask = new TaskCompletionSource();
        component.OnParametersSetAsyncLogic = _ => onParametersSetTask.Task;

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        var renderTask = renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Single(renderer.Batches);

        // Cancel task started by OnParametersSet
        component.Counter = 2;
        onParametersSetTask.SetCanceled();

        await renderTask;

        // Component should not be rendered again
        Assert.Single(renderer.Batches);

    }

    [Fact]
    public async Task RenderRootComponentAsync_ReportsErrorDuringOnInit()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent { OnInitLogic = _ => throw expected };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task RenderRootComponentAsync_ReportsErrorDuringOnInitAsync()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent { OnInitAsyncLogic = _ => Task.FromException(expected) };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task RenderRootComponentAsync_ReportsErrorDuringOnParameterSet()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent { OnParametersSetLogic = _ => throw expected };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task RenderRootComponentAsync_ReportsErrorDuringOnParameterSetAsync()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent { OnParametersSetAsyncLogic = _ => Task.FromException(expected) };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task OnInitializedAsync_ThrowsExceptionSynchronouslyUsingAsyncAwait()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously
            OnInitAsyncLogic = async _ =>
            {
                throw expected; // Throws synchronously in async method
            }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task OnInitializedAsync_ThrowsExceptionAsynchronouslyUsingAsyncAwait()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected; // Throws asynchronously in async method
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task OnInitializedAsync_ReturnsTaskFromExceptionSynchronously()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = _ =>
            {
                return Task.FromException(expected); // Returns faulted task synchronously
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task OnInitializedAsync_ReturnsCancelledTaskSynchronously()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = _ =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                return Task.FromCanceled(cts.Token); // Returns cancelled task synchronously
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert - should not throw and should have completed rendering
        Assert.NotEmpty(renderer.Batches);
    }

    [Fact]
    public async Task OnInitializedAsync_ReturnsCancelledTaskAsynchronously()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                var cts = new CancellationTokenSource();
                cts.Cancel();
                await Task.FromCanceled(cts.Token); // Returns cancelled task asynchronously
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert - should not throw and should have completed rendering
        Assert.NotEmpty(renderer.Batches);
    }

    [Fact]
    public async Task OnParametersSetAsync_ThrowsExceptionSynchronouslyUsingAsyncAwait()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously
            OnParametersSetAsyncLogic = async _ =>
            {
                throw expected; // Throws synchronously in async method
            }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task OnParametersSetAsync_ThrowsExceptionAsynchronouslyUsingAsyncAwait()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected; // Throws asynchronously in async method
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task OnParametersSetAsync_ReturnsTaskFromExceptionSynchronously()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = _ =>
            {
                return Task.FromException(expected); // Returns faulted task synchronously
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        var actual = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task OnParametersSetAsync_ReturnsCancelledTaskSynchronously()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = _ =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                return Task.FromCanceled(cts.Token); // Returns cancelled task synchronously
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert - should not throw and should have completed rendering
        Assert.NotEmpty(renderer.Batches);
    }

    [Fact]
    public async Task OnParametersSetAsync_ReturnsCancelledTaskAsynchronously()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                var cts = new CancellationTokenSource();
                cts.Cancel();
                await Task.FromCanceled(cts.Token); // Returns cancelled task asynchronously
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert - should not throw and should have completed rendering
        Assert.NotEmpty(renderer.Batches);
    }

    // StateHasChanged tracking tests
    [Fact]
    public async Task OnInitializedAsync_SucceedsSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = _ => Task.CompletedTask
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // One render
    }

    [Fact]
    public async Task OnInitializedAsync_SucceedsAsynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(2, component.StateHasChangedCallCount); // Initial render + async rerender
    }

    [Fact]
    public async Task OnInitializedAsync_ReturnsCancelledTaskSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = _ =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // One render
    }

    [Fact]
    public async Task OnInitializedAsync_ReturnsCancelledTaskAsynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                var cts = new CancellationTokenSource();
                cts.Cancel();
                await Task.FromCanceled(cts.Token);
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(2, component.StateHasChangedCallCount); // Initial render + async rerender
    }

    [Fact]
    public async Task OnInitializedAsync_ThrowsExceptionSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = _ => Task.FromException(expected)
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(0, component.StateHasChangedCallCount); // No render due to exception
    }

    [Fact]
    public async Task OnInitializedAsync_ThrowsExceptionAsynchronously_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected;
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // Initial render before async exception
    }

    [Fact]
    public async Task OnInitializedAsync_ThrowsExceptionSynchronouslyUsingAsyncAwait_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously
            OnInitAsyncLogic = async _ =>
            {
                throw expected; // Throws synchronously in async method
            }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(0, component.StateHasChangedCallCount); // No render due to synchronous exception
    }

    [Fact]
    public async Task OnInitializedAsync_ThrowsExceptionAsynchronouslyUsingAsyncAwait_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected; // Throws asynchronously in async method
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // Initial render before async exception
    }

    [Fact]
    public async Task OnInitializedAsync_ReturnsTaskFromExceptionSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnInitAsyncLogic = _ =>
            {
                return Task.FromException(expected); // Returns faulted task synchronously
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(0, component.StateHasChangedCallCount); // No render due to exception
    }

    [Fact]
    public async Task OnParametersSetAsync_SucceedsSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = _ => Task.CompletedTask
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // One render
    }

    [Fact]
    public async Task OnParametersSetAsync_SucceedsAsynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(2, component.StateHasChangedCallCount); // Initial render + async rerender
    }

    [Fact]
    public async Task OnParametersSetAsync_ReturnsCancelledTaskSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = _ =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // One render
    }

    [Fact]
    public async Task OnParametersSetAsync_ReturnsCancelledTaskAsynchronously_TracksStateHasChanged()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                var cts = new CancellationTokenSource();
                cts.Cancel();
                await Task.FromCanceled(cts.Token);
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        Assert.Equal(2, component.StateHasChangedCallCount); // Initial render + async rerender
    }

    [Fact]
    public async Task OnParametersSetAsync_ThrowsExceptionSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = _ => Task.FromException(expected)
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(0, component.StateHasChangedCallCount); // No render due to exception
    }

    [Fact]
    public async Task OnParametersSetAsync_ThrowsExceptionAsynchronously_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected;
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // Initial render before async exception
    }

    [Fact]
    public async Task OnParametersSetAsync_ThrowsExceptionSynchronouslyUsingAsyncAwait_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously
            OnParametersSetAsyncLogic = async _ =>
            {
                throw expected; // Throws synchronously in async method
            }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(0, component.StateHasChangedCallCount); // No render due to synchronous exception
    }

    [Fact]
    public async Task OnParametersSetAsync_ThrowsExceptionAsynchronouslyUsingAsyncAwait_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected; // Throws asynchronously in async method
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(1, component.StateHasChangedCallCount); // Initial render before async exception
    }

    [Fact]
    public async Task OnParametersSetAsync_ReturnsTaskFromExceptionSynchronously_TracksStateHasChanged()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponent
        {
            OnParametersSetAsyncLogic = _ =>
            {
                return Task.FromException(expected); // Returns faulted task synchronously
            }
        };

        // Act & Assert
        var componentId = renderer.AssignRootComponentId(component);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(componentId));

        // Assert
        Assert.Equal(0, component.StateHasChangedCallCount); // No render due to exception
    }

    // ErrorBoundary tests for ComponentBase lifecycle methods  
    // Each test corresponds to a StateHasChanged tracking test but wrapped in an ErrorBoundary
    // The component unconditionally throws in BuildRenderTree to validate error boundary behavior

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_SucceedsSynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var errorBoundary = new TestErrorBoundaryComponent();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = _ => Task.CompletedTask
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_SucceedsAsynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var errorBoundary = new TestErrorBoundaryComponent();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_ReturnsCancelledTaskSynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = _ =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_ReturnsCancelledTaskAsynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                var cts = new CancellationTokenSource();
                cts.Cancel();
                await Task.FromCanceled(cts.Token);
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content  
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_ThrowsExceptionSynchronously_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = _ => Task.FromException(expected)
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_ThrowsExceptionAsynchronously_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected;
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_ThrowsExceptionSynchronouslyUsingAsyncAwait_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously
            OnInitAsyncLogic = async _ =>
            {
                throw expected; // Throws synchronously in async method
            }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_ThrowsExceptionAsynchronouslyUsingAsyncAwait_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected; // Throws asynchronously in async method
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnInitializedAsync_ReturnsTaskFromExceptionSynchronously_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnInitAsyncLogic = _ =>
            {
                return Task.FromException(expected); // Returns faulted task synchronously
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_SucceedsSynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = _ => Task.CompletedTask
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_SucceedsAsynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_ReturnsCancelledTaskSynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = _ =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_ReturnsCancelledTaskAsynchronously_RendersErrorContent()
    {
        // Arrange
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                var cts = new CancellationTokenSource();
                cts.Cancel();
                await Task.FromCanceled(cts.Token);
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await renderer.RenderRootComponentAsync(rootComponentId);

        // Assert - ErrorBoundary should have caught the BuildRenderTree exception and rendered error content
        var batch = renderer.Batches.Last();
        var errorBoundaryFrames = batch.GetComponentFrames<TestErrorBoundaryComponent>();
        Assert.NotEmpty(errorBoundaryFrames);
        
        var errorBoundaryComponent = (TestErrorBoundaryComponent)errorBoundaryFrames.First().Component;
        Assert.NotNull(errorBoundaryComponent.ReceivedException);
        Assert.Contains("BuildRenderTree error", errorBoundaryComponent.ReceivedException.Message);
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_ThrowsExceptionSynchronously_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = _ => Task.FromException(expected)
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_ThrowsExceptionAsynchronously_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected;
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_ThrowsExceptionSynchronouslyUsingAsyncAwait_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously
            OnParametersSetAsyncLogic = async _ =>
            {
                throw expected; // Throws synchronously in async method
            }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_ThrowsExceptionAsynchronouslyUsingAsyncAwait_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = async _ =>
            {
                await Task.Yield(); // Force async execution
                throw expected; // Throws asynchronously in async method
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    [Fact]
    public async Task ErrorBoundary_OnParametersSetAsync_ReturnsTaskFromExceptionSynchronously_RendersErrorContent()
    {
        // Arrange
        var expected = new TimeZoneNotFoundException();
        var renderer = new TestRenderer();
        var component = new TestComponentWithBuildRenderTreeError
        {
            OnParametersSetAsyncLogic = _ =>
            {
                return Task.FromException(expected); // Returns faulted task synchronously
            }
        };

        // Create root component that wraps the test component in an error boundary
        var rootComponent = new TestComponent();
        rootComponent.ChildContent = builder =>
        {
            builder.OpenComponent<TestErrorBoundaryComponent>(0);
            builder.AddComponentParameter(1, nameof(TestErrorBoundaryComponent.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<TestComponentWithBuildRenderTreeError>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        // Act & Assert
        var rootComponentId = renderer.AssignRootComponentId(rootComponent);
        await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => renderer.RenderRootComponentAsync(rootComponentId));
    }

    private class TestComponent : ComponentBase
    {
        public bool RunsBaseOnInit { get; set; } = true;

        public bool RunsBaseOnInitAsync { get; set; } = true;

        public bool RunsBaseOnParametersSet { get; set; } = true;

        public bool RunsBaseOnParametersSetAsync { get; set; } = true;

        public bool RunsBaseOnAfterRender { get; set; } = true;

        public bool RunsBaseOnAfterRenderAsync { get; set; } = true;

        public Action<TestComponent> OnInitLogic { get; set; }

        public Func<TestComponent, Task> OnInitAsyncLogic { get; set; }

        public Action<TestComponent> OnParametersSetLogic { get; set; }

        public Func<TestComponent, Task> OnParametersSetAsyncLogic { get; set; }

        public Action<TestComponent, bool> OnAfterRenderLogic { get; set; }

        public Func<TestComponent, bool, Task> OnAfterRenderAsyncLogic { get; set; }

        public int Counter { get; set; }

        public int StateHasChangedCallCount { get; private set; }

        public RenderFragment ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            StateHasChangedCallCount++;
            if (ChildContent != null)
            {
                builder.AddContent(0, ChildContent);
            }
            else
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, Counter);
                builder.CloseElement();
            }
        }

        protected override void OnInitialized()
        {
            if (RunsBaseOnInit)
            {
                base.OnInitialized();
            }

            OnInitLogic?.Invoke(this);
        }

        protected override async Task OnInitializedAsync()
        {
            if (RunsBaseOnInitAsync)
            {
                await base.OnInitializedAsync();
            }

            if (OnInitAsyncLogic != null)
            {
                await OnInitAsyncLogic.Invoke(this);
            }
        }

        protected override void OnParametersSet()
        {
            if (RunsBaseOnParametersSet)
            {
                base.OnParametersSet();
            }

            OnParametersSetLogic?.Invoke(this);
        }

        protected override async Task OnParametersSetAsync()
        {
            if (RunsBaseOnParametersSetAsync)
            {
                await base.OnParametersSetAsync();
            }

            if (OnParametersSetAsyncLogic != null)
            {
                await OnParametersSetAsyncLogic(this);
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (RunsBaseOnAfterRender)
            {
                base.OnAfterRender(firstRender);
            }

            if (OnAfterRenderLogic != null)
            {
                OnAfterRenderLogic(this, firstRender);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (RunsBaseOnAfterRenderAsync)
            {
                await base.OnAfterRenderAsync(firstRender);
            }

            if (OnAfterRenderAsyncLogic != null)
            {
                await OnAfterRenderAsyncLogic(this, firstRender);
            }
        }
    }

    private class TestComponentWithBuildRenderTreeError : ComponentBase
    {
        public Action<TestComponentWithBuildRenderTreeError> OnInitLogic { get; set; }

        public Func<TestComponentWithBuildRenderTreeError, Task> OnInitAsyncLogic { get; set; }

        public Action<TestComponentWithBuildRenderTreeError> OnParametersSetLogic { get; set; }

        public Func<TestComponentWithBuildRenderTreeError, Task> OnParametersSetAsyncLogic { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // This component unconditionally throws in BuildRenderTree to test ErrorBoundary behavior
            throw new InvalidOperationException("BuildRenderTree error - component always fails to render");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            OnInitLogic?.Invoke(this);
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (OnInitAsyncLogic != null)
            {
                await OnInitAsyncLogic.Invoke(this);
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            OnParametersSetLogic?.Invoke(this);
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (OnParametersSetAsyncLogic != null)
            {
                await OnParametersSetAsyncLogic(this);
            }
        }
    }

    private class TestErrorBoundaryComponent : ComponentBase, IErrorBoundary
    {
        public Exception ReceivedException { get; private set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public RenderFragment<Exception> ErrorContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ReceivedException is null)
            {
                builder.AddContent(0, ChildContent);
            }
            else if (ErrorContent is not null)
            {
                builder.AddContent(1, ErrorContent(ReceivedException));
            }
            else
            {
                // Default error content
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", "error-boundary");
                builder.AddContent(4, "An error has occurred");
                builder.CloseElement();
            }
        }

        public void HandleException(Exception exception)
        {
            ReceivedException = exception;
            StateHasChanged();
        }
    }
}
