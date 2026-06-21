# Async and tasks performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Avoid closure captures in async and queued delegates

Prefer static lambdas, explicit state parameters, or local static methods for Task, ThreadPool, Channel, and cancellation callbacks.

- Do: ThreadPool.QueueUserWorkItem(static s => Process((State)s!), state) or async static ValueTask Body(T item, CancellationToken ct)
- Instead of: Task.Run(() => Process(local)); token.Register(() => Use(local)); async item => await Use(item, outer)
- Why: Captured lambdas allocate display classes and often delegates; .NET 10 can elide some delegate allocations but captured state can still allocate.
- Since .NET 10. Supersedes: Depending on JIT delegate escape analysis to fix avoidable closure allocations
- Hot path: yes | Complexity: low
- APIs: `System.Threading.ThreadPool.QueueUserWorkItem`, `System.Threading.CancellationToken.Register`, `System.Threading.Tasks.Parallel.ForEachAsync`

## Register cancellation callbacks without closure captures

Use CancellationToken.Register overloads that pass state and the token to a static callback.

- Do: token.Register(static (state, token) => ((MyState)state!).Cancel(token), state)
- Instead of: token.Register(() => Cancel(token, localState)) with captured locals
- Why: Static callbacks and explicit state let the compiler cache delegates and avoid display-class allocations.
- Since .NET 6. Supersedes: Closure-based cancellation callbacks used to access the token or state
- Hot path: yes | Complexity: low
- APIs: `System.Threading.CancellationToken.Register`, `System.Threading.CancellationTokenRegistration.Dispose`

## Use ConfigureAwaitOptions.ForceYielding instead of Task.Run for async handoff

When you only need to ensure the rest of an async method runs asynchronously, force the next await to yield rather than wrapping the method in Task.Run.

- Do: await task.ConfigureAwait(ConfigureAwaitOptions.ForceYielding) or await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding)
- Instead of: Task.Run(async () => await WorkAsync()) solely to prevent synchronous continuation
- Why: ForceYielding can avoid an extra Task allocation and an extra queued work item while preserving asynchronous handoff semantics.
- Since .NET 8. Supersedes: Custom awaiters or Task.Run handoff used only to force asynchronous continuation
- Hot path: yes | Complexity: low
- APIs: `System.Threading.Tasks.ConfigureAwaitOptions.ForceYielding`, `System.Threading.Tasks.Task.ConfigureAwait`

## Use ConfigureAwaitOptions.SuppressThrowing for observe-only awaits

Await a faulted or canceled Task without throwing when another component will propagate or inspect the error.

- Do: await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing); cast Task<TResult> to Task first, then inspect the original task
- Instead of: try { await task.ConfigureAwait(false); } catch { } or custom no-throw awaiters
- Why: SuppressThrowing replaces try-catch swallowing and custom no-throw awaiters with a cheaper built-in path.
- Since .NET 8. Supersedes: Custom no-throw awaiters and catch-swallow patterns
- Hot path: yes | Complexity: low
- APIs: `System.Threading.Tasks.ConfigureAwaitOptions.SuppressThrowing`, `System.Threading.Tasks.Task.ConfigureAwait`

## Use Parallel.ForAsync for numeric async ranges

Use Parallel.ForAsync for asynchronous numeric range loops instead of wrapping Enumerable.Range in Parallel.ForEachAsync.

- Do: await Parallel.ForAsync(0, count, parallelOptions, static async (i, ct) => { ... });
- Instead of: await Parallel.ForEachAsync(Enumerable.Range(0, count), async (i, ct) => { ... });
- Why: ForAsync avoids enumerable and enumerator allocation and uses cheaper Interlocked-based work distribution for supported types.
- Since .NET 8. Supersedes: .NET 6 Parallel.ForEachAsync over Enumerable.Range for for-loop-shaped work
- Hot path: yes | Complexity: low
- APIs: `System.Threading.Tasks.Parallel.ForAsync`, `System.Threading.Tasks.Parallel.ForEachAsync`, `System.Numerics.IBinaryInteger<T>`

## Use Task.WhenEach to process completions as they arrive

Consume many tasks in completion order with Task.WhenEach and await foreach.

- Do: await foreach (Task completed in Task.WhenEach(tasks)) { await completed; }
- Instead of: while (tasks.Count > 0) { var t = await Task.WhenAny(tasks); tasks.Remove(t); }
- Why: It replaces the O(N^2) loop of Task.WhenAny plus List.Remove with a simpler and much lower-allocation IAsyncEnumerable-based pattern.
- Since .NET 9. Supersedes: Task.WhenAny loops for completion-order processing
- Hot path: yes | Complexity: low
- APIs: `System.Threading.Tasks.Task.WhenEach`, `System.Collections.Generic.IAsyncEnumerable<T>`, `System.Threading.Tasks.Task.WhenAny`

## Avoid sync-over-async on ThreadPool threads

Keep async flows async instead of blocking on Task.Result, Task.Wait, or Task.WaitAll from ThreadPool work items.

- Do: await tasks, expose async APIs, or bridge to synchronous contracts only at a narrow boundary
- Instead of: GetValueAsync().Result, task.Wait(), Task.WaitAll(tasks) in request or work-item code
- Why: Blocking consumes worker threads and can starve continuations; newer runtimes mitigate common cases but do not make the pattern scalable.
- Since .NET 6. Supersedes: Relying on slow starvation injection before .NET 6
- Hot path: yes | Complexity: medium
- APIs: `System.Threading.Tasks.Task`, `System.Threading.ThreadPool`

## Avoid unnecessary volatile on lazy reference caches

Avoid defensive volatile fields for ordinary lazy publication of reference objects when the documented memory model already provides the needed guarantees.

- Do: private MyType? _instance; public MyType Instance => _instance ??= new MyType(); when benign races are acceptable
- Instead of: private volatile MyType? _instance for every lazy cache by default
- Why: Volatile reads and writes inhibit optimizations and can add memory barrier cost, especially on Arm.
- Since .NET 9. Supersedes: Older defensive volatile usage before the .NET memory model was documented
- Hot path: yes | Complexity: medium
- APIs: `System.Threading.Volatile.Read`, `System.Threading.Volatile.Write`

## Pool expensive reusable objects with ObjectPool only when reset is safe

Use an ObjectPool<T> for expensive, reusable helper objects in high-throughput async infrastructure when every checkout can be fully reset.

- Do: Use Microsoft.Extensions.ObjectPool.ObjectPool<T>.Get and Return around a try-finally and clear all per-operation state before returning
- Instead of: Pooling small short-lived objects, pooled objects with uncleared captured request state, or objects that are not safe to reuse
- Why: Pooling can reduce allocation pressure, but retained objects can increase working set and GC costs if they hold references or are cheap to allocate.
- Since .NET Core 1.0. Supersedes: Ad hoc static queues of reusable objects without reset policy
- Hot path: yes | Complexity: medium
- APIs: `Microsoft.Extensions.ObjectPool.ObjectPool<T>`, `Microsoft.Extensions.ObjectPool.ObjectPoolProvider`, `Microsoft.Extensions.ObjectPool.PooledObjectPolicy<T>`

## Return Task by default and ValueTask only for measured hot paths

Use Task by default and choose ValueTask only when synchronous completion is common or allocations are proven costly.

- Do: Return ValueTask<TResult> from hot APIs that often complete synchronously and document single-consumption rules
- Instead of: Using ValueTask everywhere or awaiting the same ValueTask multiple times
- Why: ValueTask can remove allocation on synchronous completion but has stricter consumption rules and can add overhead when misapplied.
- Since .NET Core 2.1. Supersedes: Using Task for every hot synchronous-completion async method when ValueTask is proven beneficial
- Hot path: yes | Complexity: medium
- APIs: `System.Threading.Tasks.ValueTask`, `System.Threading.Tasks.ValueTask<TResult>`, `System.Threading.Tasks.Task`
- Snippet: [code](../snippets/bcl/async.md#return-task-by-default-and-valuetask-only-for-measured-hot-paths)

## Reuse CancellationTokenSource only with TryReset

Pool or reuse CancellationTokenSource instances only when TryReset succeeds.

- Do: if (cts.TryReset()) pool.Return(cts); else cts.Dispose();
- Instead of: Manually clearing or reusing a CancellationTokenSource after cancellation or timeout
- Why: TryReset guarantees cancellation was not requested and any timer state was reset, avoiding races from CancelAfter or timeout constructors.
- Since .NET 6. Supersedes: Discarding every uncanceled timeout CTS or unsafely reusing canceled sources
- Hot path: yes | Complexity: medium
- APIs: `System.Threading.CancellationTokenSource.TryReset`, `System.Threading.CancellationTokenSource.CancelAfter`

## Use Interlocked for tiny atomic state transitions

Use Interlocked.Exchange and CompareExchange for simple counters, flags, and enum state machines instead of protecting one field with a lock.

- Do: Interlocked.CompareExchange(ref state, newState, expectedState) or Interlocked.Exchange(ref flag, true)
- Instead of: lock (_gate) { if (state == expected) state = next; } for a single atomic field
- Why: Atomic instructions avoid lock allocation and contention; .NET 9 supports bool, small integers, primitives, and enums.
- Since .NET 9. Supersedes: Using int fields or unsafe casts only because Interlocked could not handle bool, small primitives, or enums
- Hot path: yes | Complexity: medium
- APIs: `System.Threading.Interlocked.Exchange`, `System.Threading.Interlocked.CompareExchange`, `System.Threading.Interlocked.And`, `System.Threading.Interlocked.Or`

## Use ThreadStatic for per-thread hot state when appropriate

Use ThreadStatic or ThreadLocal<T> for per-thread caches and counters that avoid shared synchronization.

- Do: [ThreadStatic] private static MyCache? t_cache; initialize per thread before use
- Instead of: One global mutable cache protected by a lock when state is naturally per-thread
- Why: Thread-static access has become much cheaper in recent runtimes, including .NET 8 inlined fast paths, and can avoid cross-thread contention.
- Since .NET 8. Supersedes: Older assumptions that ThreadStatic access is too expensive for hot paths
- Hot path: yes | Complexity: medium
- APIs: `System.ThreadStaticAttribute`, `System.Threading.ThreadLocal<T>`

## Use ManualResetValueTaskSourceCore for reusable custom async sources

Build reusable awaitable operations on IValueTaskSource with ManualResetValueTaskSourceCore when you control producer and consumer lifetimes.

- Do: Implement IValueTaskSource<TResult> backed by ManualResetValueTaskSourceCore<TResult> and reset only after GetResult completes
- Instead of: Allocating a new TaskCompletionSource<TResult> per operation in a hot primitive
- Why: It enables allocation-free async completions in primitives such as channels and semaphores, with newer runtimes reducing per-source size.
- Since .NET Core 2.1. Supersedes: Ad hoc TaskCompletionSource-based awaitable operations in very hot infrastructure
- Hot path: yes | Complexity: high
- APIs: `System.Threading.Tasks.Sources.IValueTaskSource`, `System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<TResult>`

## Use Volatile barriers only in specialized lock-free code

Reach for Volatile.ReadBarrier and WriteBarrier only when implementing carefully reviewed acquire or release memory ordering.

- Do: Use Volatile.ReadBarrier or Volatile.WriteBarrier to express explicit acquire or release fences
- Instead of: Replacing normal lock or Interlocked usage with manual barriers in ordinary code
- Why: Half fences can be cheaper than full synchronization but incorrect use breaks thread safety and is unnecessary for most library code.
- Since .NET 10. Supersedes: Using heavier synchronization solely to express a one-way fence in very low-level code
- Hot path: yes | Complexity: high
- APIs: `System.Threading.Volatile.ReadBarrier`, `System.Threading.Volatile.WriteBarrier`

## Use pooled async ValueTask builders only after benchmarking

Apply PoolingAsyncValueTaskMethodBuilder only to selected async ValueTask methods whose asynchronous completion allocation dominates cost.

- Do: [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))] on a proven hot async ValueTask method
- Instead of: Enabling pooling broadly or relying on pooled ValueTask instances that callers await multiple times
- Why: Pooling can remove allocations but changes reuse semantics and may increase CPU, working set, and GC scanning costs.
- Since .NET 6. Supersedes: The .NET 5 DOTNET_SYSTEM_THREADING_POOLASYNCVALUETASKS experiment
- Hot path: yes | Complexity: high
- APIs: `System.Runtime.CompilerServices.AsyncMethodBuilderAttribute`, `System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`, `System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<TResult>`

## Prefer Task.WaitAsync for timeouts and cancellation

Use Task.WaitAsync overloads to add a timeout, cancellation token, or TimeProvider to an existing task.

- Do: await operation.WaitAsync(timeout, cancellationToken) or await operation.WaitAsync(timeout, timeProvider)
- Instead of: await Task.WhenAny(Task.Delay(timeout, cts.Token), operation) plus manual cancellation
- Why: It is simpler and lower-overhead than composing Task.WhenAny with Task.Delay and CancellationTokenSource plumbing.
- Since .NET 6. Supersedes: Hand-written WhenAny plus Delay timeout wrappers
- Hot path: either | Complexity: low
- APIs: `System.Threading.Tasks.Task.WaitAsync`, `System.Threading.Tasks.Task<TResult>.WaitAsync`, `System.TimeProvider`

## Use ConfigureAwait(false) in library internals unless context is required

In context-agnostic library code, avoid capturing SynchronizationContext or TaskScheduler on awaits.

- Do: await operation.ConfigureAwait(false) or ConfigureAwait(ConfigureAwaitOptions.None)
- Instead of: Bare await in reusable library internals that do not need a captured context
- Why: Skipping context capture reduces continuation overhead and prevents internals from depending on an arbitrary caller context.
- Since .NET Framework 4.5. Supersedes: Custom awaiters used only to avoid captured context
- Hot path: either | Complexity: low
- APIs: `System.Threading.Tasks.Task.ConfigureAwait`, `System.Threading.Tasks.ValueTask.ConfigureAwait`, `System.Threading.Tasks.ConfigureAwaitOptions`
- Snippet: [code](../snippets/bcl/async.md#use-configureawaitfalse-in-library-internals-unless-context-is-required)

## Use Parallel.ForEachAsync for bounded asynchronous fan-out

Use Parallel.ForEachAsync to process IEnumerable<T> or IAsyncEnumerable<T> inputs with a bounded degree of asynchronous parallelism.

- Do: await Parallel.ForEachAsync(source, new ParallelOptions { MaxDegreeOfParallelism = n, CancellationToken = ct }, static async (item, ct) => { ... });
- Instead of: Creating an unbounded Task per item or manually coordinating SemaphoreSlim for routine fan-out
- Why: It centralizes scheduler, cancellation, and maximum degree settings instead of hand-rolling semaphore and task-list orchestration.
- Since .NET 6. Supersedes: Manual async fan-out loops for common bounded parallel iteration
- Hot path: either | Complexity: low
- APIs: `System.Threading.Tasks.Parallel.ForEachAsync`, `System.Threading.Tasks.ParallelOptions`, `System.Collections.Generic.IAsyncEnumerable<T>`

## Use System.Threading.Lock for dedicated locks

Use the .NET 9 Lock type when allocating an object solely for C# lock statements.

- Do: private readonly Lock _lock = new(); lock (_lock) { ... }
- Instead of: private readonly object _gate = new(); lock (_gate) { ... } when it is only a monitor
- Why: Lock is slightly cheaper than Monitor on object and C# 13 lowers it to EnterScope plus Dispose.
- Since .NET 9. Supersedes: Dedicated lock(object) monitors in .NET 8 and earlier
- Hot path: either | Complexity: low
- APIs: `System.Threading.Lock`, `System.Threading.Lock.EnterScope`, `System.Threading.Monitor.Enter`

## Use Task.WhenAll with concrete collections or span-friendly inputs

Batch independent tasks with Task.WhenAll and pass arrays, lists, or collection expressions rather than repeatedly awaiting or hand-joining.

- Do: await Task.WhenAll(tasksArray); await Task.WhenAll([task1, task2]);
- Instead of: Manual continuation counting, repeated waits, or allocating a temporary list solely for WhenAll
- Why: Recent runtimes avoid defensive copies, reduce returned-task size, add span-based overloads, avoid temporary buffers for many enumerables, and return the single task for one-element inputs.
- Since .NET 10. Supersedes: .NET 8 and .NET 9 WhenAll allocation patterns with extra defensive copies or buffers
- Hot path: either | Complexity: low
- APIs: `System.Threading.Tasks.Task.WhenAll`, `System.ReadOnlySpan<T>`

## Use Channels for producer-consumer queues

Use System.Threading.Channels.Channel<T> for asynchronous handoff between producers and consumers.

- Do: Channel.CreateBounded<T>(options), Channel.CreateUnbounded<T>(), Channel.CreateUnboundedPrioritized<T>(), reader.ReadAllAsync(ct)
- Instead of: BlockingCollection<T>, ad hoc Queue<T> plus SemaphoreSlim, or polling loops for async producer-consumer handoff
- Why: Channels are purpose-built fast queues with async reads and writes, ReadAllAsync streaming, prioritization options, unbuffered channels in .NET 10, and improved memory behavior for canceled waiters.
- Since .NET 10. Supersedes: .NET Core 3.0 bounded and unbounded channels when priority or unbuffered semantics are needed
- Hot path: either | Complexity: medium
- APIs: `System.Threading.Channels.Channel<T>`, `System.Threading.Channels.Channel.CreateBounded`, `System.Threading.Channels.Channel.CreateUnbounded`, `System.Threading.Channels.Channel.CreateUnboundedPrioritized`, `System.Threading.Channels.ChannelReader<T>.ReadAllAsync`

## Use IAsyncEnumerable for streaming async results

Expose IAsyncEnumerable<T> when results arrive over time and callers should process them without buffering the whole sequence.

- Do: async IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct) and await foreach (... in source.WithCancellation(ct))
- Instead of: Task<List<T>> or Task<T[]> for long streams where callers can consume incrementally
- Why: Async streams compose with await foreach, ChannelReader.ReadAllAsync, and Parallel.ForEachAsync while reducing intermediate collections and peak memory.
- Since .NET Core 3.0. Supersedes: Buffered task-returning collection APIs for naturally streaming async data
- Hot path: either | Complexity: medium
- APIs: `System.Collections.Generic.IAsyncEnumerable<T>`, `System.Collections.Generic.IAsyncEnumerator<T>`, `System.Threading.Channels.ChannelReader<T>.ReadAllAsync`, `System.Threading.Tasks.Parallel.ForEachAsync`

## Use TimeProvider for time-based async code and tests

Accept TimeProvider in code that delays, creates timers, or measures elapsed time, especially when the code must be tested quickly.

- Do: Use provider.GetTimestamp(), provider.GetElapsedTime(), Task.Delay(delay, provider), and CancellationTokenSource(timeout, provider)
- Instead of: Hard-coded Stopwatch, DateTime.UtcNow, Task.Delay(delay), or real 30-second waits in tests
- Why: It removes wall-clock waits from tests and centralizes time behavior without custom abstractions.
- Since .NET 8. Supersedes: Custom time abstractions for most timer and delay tests
- Hot path: cold | Complexity: low
- APIs: `System.TimeProvider`, `System.Threading.Tasks.Task.Delay`, `System.Threading.CancellationTokenSource`, `System.Threading.ITimer`
