# Design: Remove SSR Response Buffering in Blazor Components Endpoints

## Overview

This document describes a design to remove the synchronous buffering requirement when writing SSR (Server-Side Rendering) render batches to the HTTP response in `Microsoft.AspNetCore.Components.Endpoints`.

## Problem Statement

### Current Behavior

Currently, render batches **must** be written synchronously to the response output. This is because if we yield the thread (via `await`) while writing to the output, other continuations can be scheduled on the `RendererSynchronizationContext` and modify the render state between async boundaries.

The code comment in `EndpointHtmlRenderer.cs` explicitly states this constraint:

```csharp
// Important: SendBatchAsStreamingUpdate *must* be invoked synchronously
// before any 'await' in this method. That's enforced by the compiler
// (the method has an 'in' parameter) but even if it wasn't, it would still
// be important, because the RenderBatch buffers may be overwritten as soon
// as we yield the sync context.
```

### Why This is a Problem

1. **Memory Pressure**: Synchronous writing requires buffering the entire batch content before writing, increasing memory usage.
2. **Latency**: Buffered writes delay when content reaches the client.
3. **Scalability**: Synchronous writes can block threads, reducing the server's ability to handle concurrent requests efficiently.

## Background: RendererSynchronizationContext

The `RendererSynchronizationContext` is a custom `SynchronizationContext` that controls how async continuations are scheduled when running inside Blazor's rendering pipeline.

### Key Characteristics

1. **Task Queue**: Maintains a `_taskQueue` field that serializes all work items.
2. **Exclusive Access**: Only one work item runs at a time; subsequent work is queued.
3. **Post Method**: Queues work to run after the current task completes.
4. **InvokeAsync Methods**: Either runs synchronously (if quiescent) or queues for later execution.

```csharp
internal sealed class RendererSynchronizationContext : SynchronizationContext
{
    private readonly object _lock;
    private Task _taskQueue;  // The key serialization mechanism

    // Work is queued by chaining onto _taskQueue
    public override void Post(SendOrPostCallback d, object? state)
    {
        lock (_lock)
        {
            _taskQueue = PostAsync(_taskQueue, static s => s.d(s.state), (d, state));
        }
    }
}
```

### How Rendering Uses This

1. `RazorComponentEndpointInvoker.Render` calls `_renderer.Dispatcher.InvokeAsync(() => RenderComponentCore(context))`.
2. All rendering work happens within this sync context.
3. When components have async work, their continuations post back to this context.
4. The render batches are generated and written to output.

## Proposed Solution

### Core Idea

Before writing to the response asynchronously, we can "block" the sync context's task queue by inserting a `Task` that we control. This prevents any other continuations from executing until the task completes after the async write finishes.

### Mechanism

1. **Create a gate**: Before starting an async write, create a `TaskCompletionSource` (the caller controls this).
2. **Insert into queue**: Call a method on the sync context to set the TCS's task as the current `_taskQueue`.
3. **Write asynchronously**: Use `WriteAsync` with `ConfigureAwait(false)` to avoid posting continuations back to the sync context.
4. **Signal completion**: After the write completes, call `tcs.SetResult()` to release the queue.

### Low-Level API Design

The API should be simple and composable - the sync context just provides a way to insert a blocking task, and the caller manages the lifecycle:

```csharp
// In RendererSynchronizationContext - new method
/// <summary>
/// Inserts a task into the queue that blocks subsequent work items until it completes.
/// The caller is responsible for ensuring the task eventually completes.
/// </summary>
/// <remarks>
/// This is useful when the caller needs to perform async I/O operations (like writing
/// to the HTTP response) while preventing other continuations from running. The caller
/// must use ConfigureAwait(false) on all async operations to avoid deadlock.
/// </remarks>
public void EnqueueBlockingTask(Task blockingTask)
{
    lock (_lock)
    {
        _taskQueue = ChainBlockingTask(_taskQueue, blockingTask);
    }

    static async Task ChainBlockingTask(Task antecedent, Task blockingTask)
    {
        await antecedent.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await blockingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
```

### Usage Pattern

```csharp
// Caller creates and controls the TCS
var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

// Insert the blocking task into the queue
syncContext.EnqueueBlockingTask(tcs.Task);

try
{
    // Perform async I/O without capturing the sync context
    await writer.WriteAsync(content).ConfigureAwait(false);
    await writer.FlushAsync().ConfigureAwait(false);
}
finally
{
    // Release the queue - other continuations can now run
    tcs.SetResult();
}
```

### Application in EndpointHtmlRenderer

#### Current Code in UpdateDisplayAsync

```csharp
protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
{
    if (_streamingUpdatesWriter is { } writer && !_rendererIsStopped)
    {
        // MUST be synchronous - cannot await before this completes
        SendBatchAsStreamingUpdate(renderBatch, writer);
        return FlushThenComplete(writer, base.UpdateDisplayAsync(renderBatch));
    }
    else
    {
        return base.UpdateDisplayAsync(renderBatch);
    }
}
```

#### Proposed Change in EndpointHtmlRenderer

```csharp
protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
{
    if (_streamingUpdatesWriter is { } writer && !_rendererIsStopped)
    {
        // Serialize the batch content first (must be sync due to 'in' parameter)
        var batchContent = SerializeBatchAsStreamingUpdate(renderBatch);

        // Now we can write asynchronously while blocking the queue
        return WriteWithQueueBlockAsync(writer, batchContent);
    }
    else
    {
        return base.UpdateDisplayAsync(renderBatch);
    }
}

private async Task WriteWithQueueBlockAsync(TextWriter writer, string content)
{
    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    // Block the sync context queue while writing
    _syncContext.EnqueueBlockingTask(tcs.Task);

    try
    {
        await writer.WriteAsync(content).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }
    finally
    {
        tcs.SetResult();
    }

    await base.UpdateDisplayAsync(/* ... */);
}
```

### Application in RazorComponentEndpointInvoker

The `RazorComponentEndpointInvoker.RenderComponentCore` method also needs modifications. Currently it has this critical section:

```csharp
// Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
// in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
// streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
// renderer sync context and cause a batch that would get missed.
htmlContent.WriteTo(bufferWriter, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above
```

#### Proposed Change in RazorComponentEndpointInvoker

Add a helper method that handles the blocking and async write internally:

```csharp
/// <summary>
/// Writes HTML content to the response while blocking the renderer's sync context queue,
/// preventing other continuations from running until the write completes.
/// </summary>
private async Task WriteContentAsync(HtmlContentBuilder htmlContent, TextWriter writer)
{
    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    _renderer.EnqueueBlockingTask(tcs.Task);

    try
    {
        await htmlContent.WriteToAsync(writer, HtmlEncoder.Default).ConfigureAwait(false);
    }
    finally
    {
        tcs.SetResult();
    }
}
```

Then the usage is simple:

```csharp
// Now we can write asynchronously without worrying about missed batches
await WriteContentAsync(htmlContent, bufferWriter);

if (!quiesceTask.IsCompletedSuccessfully)
{
    await _renderer.SendStreamingUpdatesAsync(context, quiesceTask, bufferWriter);
    // ... rest of streaming handling
}
// ... rest of the method
```

This allows the initial HTML content to be written asynchronously (using `WriteToAsync` instead of `WriteTo`) while still maintaining the invariant that no batches are missed. The helper method fully encapsulates the blocking pattern.

## Detailed Implementation Plan

### Phase 1: Extend RendererSynchronizationContext

1. **Add blocking capability** to `RendererSynchronizationContext`:
   - New method: `void EnqueueBlockingTask(Task blockingTask)`
   - This method chains the provided task onto the queue, blocking subsequent work until it completes.
   - Simple, low-level API - caller manages the `TaskCompletionSource` lifecycle.

2. **Expose via Dispatcher** (optional):
   - Could add corresponding method to `Dispatcher` abstract class
   - Or expose the sync context directly for internal use
   - Implement in `RendererSynchronizationContextDispatcher` if needed

### Phase 2: Modify EndpointHtmlRenderer

1. **Access the sync context**:
   - Add internal access to `RendererSynchronizationContext` for the blocking API
   - Or route through the `Dispatcher`

2. **Update `UpdateDisplayAsync`**:
   - Extract batch serialization (must remain synchronous due to `in RenderBatch` lifetime)
   - Use `EnqueueBlockingTask` + `TaskCompletionSource` pattern for async writes
   - Ensure `ConfigureAwait(false)` on all write operations

3. **Update `SendStreamingUpdatesAsync`**:
   - Apply similar pattern for streaming batch writes
   - Each batch write blocks the queue until complete

### Phase 3: Modify RazorComponentEndpointInvoker

1. **Update `RenderComponentCore`**:
   - Use blocking task pattern around the initial HTML write
   - Convert `WriteTo` to `WriteToAsync` for better async behavior
   - Maintain invariant that no streaming batches are missed

2. **Consider scope of blocking**:
   - Option A: Block for each individual write operation
   - Option B: Block for the entire write-then-stream sequence
   - Trade-off between granularity and complexity

### Phase 4: Testing

1. **Unit tests** for `RendererSynchronizationContext.EnqueueBlockingTask`:
   - Verify queue blocking behavior
   - Verify proper ordering after unblocking
   - Verify behavior with exceptions

2. **Integration tests** verifying:
   - No race conditions during batch writing
   - Correct ordering of streaming updates
   - No deadlocks under various scenarios
   - Async writes complete successfully

3. **Performance tests**:
   - Memory usage comparison (before/after)
   - Latency measurements
   - Throughput under load

## Alternative Approaches Considered

### 1. Clone/Serialize the RenderBatch Synchronously

**Approach**: Deep clone or serialize the `RenderBatch` synchronously, then write asynchronously.

**Pros**: Simpler, no sync context modifications needed.

**Cons**:
- Still requires buffering the entire batch in memory
- Adds serialization overhead
- The current `SendBatchAsStreamingUpdate` already writes to a TextWriter, so this is effectively what we have

### 2. Use System.IO.Pipelines

**Approach**: Write to a `Pipe` synchronously, read and write to response asynchronously.

**Pros**: Efficient buffering with backpressure.

**Cons**:
- More complex implementation
- Still requires synchronous write to the pipe
- Doesn't fundamentally solve the sync context issue

### 3. Separate Writer Thread

**Approach**: Use a dedicated background thread for writing.

**Pros**: Completely decouples writing from rendering.

**Cons**:
- Adds threading complexity
- Requires thread-safe handoff of content
- May introduce additional latency

## Key Implementation Considerations

### 1. TaskCreationOptions.RunContinuationsAsynchronously

When creating the `TaskCompletionSource`, use `RunContinuationsAsynchronously` to prevent continuations from running synchronously on the thread calling `SetResult()`. This avoids potential stack overflows and ensures predictable behavior.

### 2. ConfigureAwait(false) Usage

All async operations while the queue is blocked must use `ConfigureAwait(false)` to prevent attempting to post back to the blocked sync context, which would cause deadlock.

### 3. Exception Handling

Ensure proper exception propagation:
- If the async write fails, the exception should propagate correctly
- The TCS must always be signaled (in `finally` block) to avoid permanently blocking the queue
- Use `try/finally` pattern consistently:

```csharp
var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
syncContext.EnqueueBlockingTask(tcs.Task);

try
{
    await SomeAsyncOperation().ConfigureAwait(false);
}
finally
{
    tcs.SetResult(); // Always release, even on exception
}
```

### 4. Cancellation Support

Consider adding `CancellationToken` support for long-running write operations to allow graceful cancellation. The TCS should still be signaled even when cancelled.

### 5. Caller Responsibility

With the low-level API, the caller is responsible for:
- Creating the `TaskCompletionSource` with appropriate options
- Calling `EnqueueBlockingTask` before starting async work
- Using `ConfigureAwait(false)` on all async calls
- Always signaling the TCS in a `finally` block

This provides maximum flexibility but requires careful usage.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Deadlock if ConfigureAwait is forgotten | High - Blocks all rendering | Code review, analyzer rules |
| Performance regression from additional async machinery | Medium | Benchmark before/after |
| Breaking change in rare edge cases | Low | Extensive testing |
| Queue permanently blocked on exception | High | Always signal TCS in finally |

## Success Criteria

1. **Functional**: All existing E2E tests pass
2. **Performance**: No regression in streaming rendering latency/throughput
3. **Memory**: Reduced peak memory usage during large batch writes
4. **Reliability**: No deadlocks or race conditions under stress testing

## References

- [SynchronizationContext Class](https://learn.microsoft.com/en-us/dotnet/api/system.threading.synchronizationcontext)
- [TaskCompletionSource](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource)
- [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
- [Consuming the Task-based Asynchronous Pattern](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/consuming-the-task-based-asynchronous-pattern)
