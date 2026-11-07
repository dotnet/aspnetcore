// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.WebAssembly.Rendering;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.WebAssembly.Tests;

[CollectionDefinition(nameof(WebAssemblyDispatcherTest), DisableParallelization = true)]
public class WebAssemblyDispatcherTestCollection
{
}

[Collection(nameof(WebAssemblyDispatcherTest))]
public class WebAssemblyDispatcherTest : IDisposable
{
    private readonly WebAssemblyDispatcher _dispatcher = new();
    private readonly BlockingCollection<(SendOrPostCallback Callback, object State)> _workItems = new();
    private readonly SynchronizationContext _previousContext = WebAssemblyDispatcher._mainSynchronizationContext;
    private readonly int _previousThreadId = WebAssemblyDispatcher._mainManagedThreadId;
    private readonly Thread _dispatcherThread;

    public WebAssemblyDispatcherTest()
    {
        using var started = new ManualResetEventSlim();
        _dispatcherThread = new Thread(() =>
        {
            var context = new TestSynchronizationContext(_workItems);
            SynchronizationContext.SetSynchronizationContext(context);
            WebAssemblyDispatcher._mainSynchronizationContext = context;
            WebAssemblyDispatcher._mainManagedThreadId = Environment.CurrentManagedThreadId;
            started.Set();

            foreach (var (callback, state) in _workItems.GetConsumingEnumerable())
            {
                callback(state);
            }
        });
        _dispatcherThread.IsBackground = true;
        _dispatcherThread.Start();
        Assert.True(started.Wait(TimeSpan.FromSeconds(10)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task InvokeAsync_PreservesAsynchronousExceptions(bool useGenericOverload, bool multipleExceptions)
    {
        var expected = new InvalidOperationException("Provider failed.");
        Exception[] exceptions = multipleExceptions ? [expected, new ArgumentException("Another failure.")] : [expected];
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.False(_dispatcher.CheckAccess());

        var task = InvokeAsync(useGenericOverload, () =>
        {
            Assert.True(_dispatcher.CheckAccess());
            started.SetResult();
            return completion.Task;
        });
        await started.Task.DefaultTimeout();
        completion.SetException(exceptions);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => task.DefaultTimeout());
        Assert.Same(expected, actual);
        Assert.Equal(exceptions, task.Exception.InnerExceptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvokeAsync_PreservesExplicitAggregateException(bool useGenericOverload)
    {
        var expected = new AggregateException(new InvalidOperationException("Provider failed."));
        var task = InvokeAsync(useGenericOverload, () => Task.FromException<int>(expected));

        var actual = await Assert.ThrowsAsync<AggregateException>(() => task.DefaultTimeout());
        Assert.Same(expected, actual);
        Assert.Same(expected, Assert.Single(task.Exception.InnerExceptions));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvokeAsync_PreservesSynchronousException(bool useGenericOverload)
    {
        var expected = new InvalidOperationException("Provider failed.");
        var task = InvokeAsync(useGenericOverload, () => throw expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => task.DefaultTimeout());
        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvokeAsync_CompletesSuccessfully(bool useGenericOverload)
    {
        var task = InvokeAsync(useGenericOverload, () =>
        {
            Assert.True(_dispatcher.CheckAccess());
            return Task.FromResult(42);
        });

        await task.DefaultTimeout();
        Assert.True(task.IsCompletedSuccessfully);
        if (useGenericOverload)
        {
            Assert.Equal(42, await Assert.IsType<Task<int>>(task));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvokeAsync_PreservesCancellation(bool useGenericOverload)
    {
        var task = InvokeAsync(useGenericOverload, () => Task.FromCanceled<int>(new CancellationToken(canceled: true)));

        await Assert.ThrowsAsync<TaskCanceledException>(() => task.DefaultTimeout());
        Assert.True(task.IsCanceled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvokeAsync_OnDispatcherReturnsOriginalTask(bool useGenericOverload)
    {
        var expected = new InvalidOperationException("Provider failed.");
        var originalTask = Task.FromException<int>(expected);

        await _dispatcher.InvokeAsync(() =>
        {
            Assert.True(_dispatcher.CheckAccess());
            Assert.Same(originalTask, InvokeAsync(useGenericOverload, () => originalTask));
        }).DefaultTimeout();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => originalTask);
        Assert.Same(expected, actual);
    }

    private Task InvokeAsync(bool useGenericOverload, Func<Task<int>> workItem)
    {
        if (useGenericOverload)
        {
            return _dispatcher.InvokeAsync(workItem);
        }

        Func<Task> nonGenericWorkItem = () => workItem();
        return _dispatcher.InvokeAsync(nonGenericWorkItem);
    }

    public void Dispose()
    {
        _workItems.CompleteAdding();
        Assert.True(_dispatcherThread.Join(TimeSpan.FromSeconds(10)));
        WebAssemblyDispatcher._mainSynchronizationContext = _previousContext;
        WebAssemblyDispatcher._mainManagedThreadId = _previousThreadId;
        _workItems.Dispose();
    }

    private sealed class TestSynchronizationContext(BlockingCollection<(SendOrPostCallback Callback, object State)> workItems) : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object state)
        {
            workItems.Add((callback, state));
        }
    }
}
