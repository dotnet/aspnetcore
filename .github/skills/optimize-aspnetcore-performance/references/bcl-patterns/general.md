# Broad performance wins performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Avoid accidental closure allocations

Structure lambdas, callbacks, and generated code so success paths do not allocate display classes or delegates.

- Do: Use static lambdas, pass state explicitly, narrow capture scopes, or open-code simple loops.
- Instead of: Captured lambdas in hot paths or failure-message factories that allocate on success.
- Why: Captured locals allocate closure objects, often before the branch that actually needs the delegate.
- Since .NET 6. Supersedes: Captured delegates cleaned up in .NET 6; .NET 9 source generators avoided success-path closures
- Hot path: yes | Complexity: low

## Slice spans instead of allocating substrings or arrays

Use ReadOnlySpan<char>, slicing, span Trim, SequenceEqual, and span parsing when inspecting parts of strings.

- Do: Use string.AsSpan, MemoryExtensions.Trim, SequenceEqual, and span-based Parse/TryParse overloads.
- Instead of: Substring, Replace, Trim, or ToCharArray solely to inspect or parse a portion of a string.
- Why: It removes substring, Replace, ToCharArray, and temporary array allocations from parsing and validation loops.
- Since .NET 6. Supersedes: Allocation-heavy string parsing patterns from earlier releases
- Hot path: yes | Complexity: low
- APIs: `System.String.AsSpan`, `System.MemoryExtensions.Trim`, `System.MemoryExtensions.SequenceEqual`

## Use Nullable GetValueOrDefault when default is acceptable

Use Nullable<T>.GetValueOrDefault when the null case should produce default(T).

- Do: Call nullable.GetValueOrDefault() after logic has made default acceptable.
- Instead of: Accessing nullable.Value when no throw is needed for the missing case.
- Why: It avoids the HasValue check and exception path built into Nullable<T>.Value.
- Since .NET Core 3.0. Supersedes: Nullable<T>.Value in no-throw paths
- Hot path: yes | Complexity: low
- APIs: `System.Nullable<T>.GetValueOrDefault`, `System.Nullable<T>.Value`

## Use TickCount for elapsed-time decisions

Use Environment.TickCount64 or TickCount for coarse elapsed-time and timeout calculations.

- Do: Use Environment.TickCount64 for relative age, timeout, and reuse checks.
- Instead of: DateTime.UtcNow for internal elapsed-time decisions.
- Why: Monotonic ticks are cheaper and avoid wall-clock work when calendar time is irrelevant.
- Since .NET Core 3.0. Supersedes: DateTime.UtcNow in hot elapsed-time checks
- Hot path: yes | Complexity: low
- APIs: `System.Environment.TickCount64`, `System.Environment.TickCount`, `System.DateTime.UtcNow`

## Use interpolated handler Text for span consumers

Use DefaultInterpolatedStringHandler.Text when custom handler code can consume formatted text as a span.

- Do: Append to DefaultInterpolatedStringHandler, then consume its Text property as ReadOnlySpan<char>.
- Instead of: Calling ToString() when the next step only needs a span.
- Why: It avoids materializing a final string solely to feed another span-capable API.
- Since .NET 10. Supersedes: Handler code limited to string output in .NET 6-.NET 9
- Hot path: yes | Complexity: low
- APIs: `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.Text`

## Use the cheapest sufficient membership API

Use Contains when you only need a yes/no answer and IndexOf only when you need the position.

- Do: Use string.Contains, MemoryExtensions.Contains, or collection Contains for membership checks.
- Instead of: IndexOf(...) >= 0 when the index is discarded.
- Why: Contains can avoid tracking the exact index and is slightly cheaper in tight parsing code.
- Since .NET Core 3.0. Supersedes: IndexOf-based boolean checks in older code
- Hot path: yes | Complexity: low
- APIs: `System.String.Contains`, `System.MemoryExtensions.Contains`, `System.String.IndexOf`, `System.MemoryExtensions.IndexOf`

## Build strings directly when simple concat is not enough

Use string.Create and interpolated string handlers to format into the final string without temporary strings.

- Do: Use string.Create(provider, stackalloc char[...], $"...{value}...") or string.Create(length, state, action).
- Instead of: string.Format, repeated concatenation, ToString plus Concat, or new char[] plus new string(charArray).
- Why: It avoids intermediate ToString, Format, Concat, or temporary char-array allocations.
- Since .NET 6. Supersedes: Manual formatting and temporary string construction patterns from earlier releases
- Hot path: yes | Complexity: medium
- APIs: `System.String.Create`, `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler`

## Reset ArrayBufferWriter without clearing

Reuse ArrayBufferWriter<T> with ResetWrittenCount when stale underlying bytes do not need zeroing.

- Do: Call ArrayBufferWriter<T>.ResetWrittenCount before reuse when consumers only read WrittenSpan or WrittenMemory.
- Instead of: Calling Clear on every reuse when zeroing is unnecessary.
- Why: It avoids Clear's buffer-clearing work while making the writer logically empty.
- Since .NET 9. Supersedes: ArrayBufferWriter<T>.Clear for no-zero reuse scenarios in .NET 8 and earlier
- Hot path: yes | Complexity: medium
- APIs: `System.Buffers.ArrayBufferWriter<T>.ResetWrittenCount`, `System.Buffers.ArrayBufferWriter<T>.Clear`

## Skip ExecutionContext flow only when safe

Use no-flow registration or socket APIs only when callbacks do not need AsyncLocal or other ambient state.

- Do: Use CancellationToken.UnsafeRegister or SocketAsyncEventArgs(bool unsafeSuppressExecutionContextFlow) after auditing callbacks.
- Instead of: Register or default SocketAsyncEventArgs when callback context is known irrelevant.
- Why: Avoiding ExecutionContext capture and restore removes per-operation overhead.
- Since .NET 5. Supersedes: Manual no-flow cleanup in .NET Core 3.0; .NET 5 added the SocketAsyncEventArgs constructor
- Hot path: yes | Complexity: medium
- APIs: `System.Threading.CancellationToken.UnsafeRegister`, `System.Net.Sockets.SocketAsyncEventArgs.SocketAsyncEventArgs(bool)`

## Use specialized array allocation when justified

Use uninitialized or pinned GC array allocation for buffers that will be fully overwritten or must be stable for native interop.

- Do: Use GC.AllocateUninitializedArray<T> for fully overwritten non-reference buffers and GC.AllocateArray<T>(length, pinned: true) for pinned buffers.
- Instead of: new T[length] followed by complete overwrite, or repeatedly pinning ordinary arrays.
- Why: Skipping zeroing saves initialization work, and POH pinned arrays avoid repeated pinning overhead.
- Since .NET 5. Supersedes: Always using new T[] plus fixed or GCHandle for buffers
- Hot path: yes | Complexity: medium
- APIs: `System.GC.AllocateUninitializedArray<T>`, `System.GC.AllocateArray<T>`

## Avoid trivial explicit static constructors

Initialize static fields inline when no custom static-constructor logic is needed.

- Do: Use static field initializers and delete empty or assignment-only static constructors.
- Instead of: An explicit static constructor that only assigns static fields.
- Why: beforefieldinit types give the runtime and JIT more flexibility and avoid small initialization checks.
- Since .NET Core 3.0. Supersedes: Simple explicit cctors used for static initialization
- Hot path: either | Complexity: low

## Make constants and immutable fields explicit

Use const, readonly, static readonly, and get-only properties for values that do not change.

- Do: Use const for compile-time constants, readonly for assigned-once fields, and remove unnecessary setters.
- Instead of: Mutable fields or settable properties for fixed values.
- Why: The JIT can propagate constants and optimize immutable static data while code avoids unnecessary reads and mutation surface.
- Since .NET 9. Supersedes: Mutable storage for fixed values in older code
- Hot path: either | Complexity: low

## Prefer ReadOnlySpan for non-mutating inputs

Accept ReadOnlySpan<T> instead of Span<T> when a method only reads the data.

- Do: Use ReadOnlySpan<T> for read-only parameters, locals, and helper APIs.
- Instead of: Using Span<T> by default for read-only array or buffer access.
- Why: It avoids Span<T> array-covariance validation costs and communicates that inputs are not modified.
- Since .NET 9. Supersedes: Overbroad Span<T> usage from earlier migrations
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlySpan<T>`, `System.Span<T>`

## Prefer fixed-arity overloads over params arrays

Use fixed-arity overloads when an API provides them for common small counts.

- Do: Use Task.WhenAny(Task, Task) for the two-task case.
- Instead of: Task.WhenAny(params Task[]) with two tasks.
- Why: They avoid params-array allocation and often use specialized fast paths.
- Since .NET 5. Supersedes: Task.WhenAny params array overload for two tasks before .NET 5
- Hot path: either | Complexity: low
- APIs: `System.Threading.Tasks.Task.WhenAny(System.Threading.Tasks.Task,System.Threading.Tasks.Task)`, `System.Threading.Tasks.Task.WhenAny(System.Threading.Tasks.Task[])`

## Seal internal implementation types

Seal non-public implementation types and remove virtual from members that do not need overriding.

- Do: Mark private/internal classes sealed and make internal members non-virtual unless extensibility is required.
- Instead of: Leaving implementation classes inheritable or members virtual by default.
- Why: This enables devirtualization, inlining, cheaper type tests, and fewer array or Span covariance checks.
- Since .NET 6. Supersedes: Unsealed internal type hierarchies; .NET 9 also removed unused internal virtuals
- Hot path: either | Complexity: low

## Share immutable default arrays and constant data

Use shared immutable arrays or ReadOnlySpan-backed constant data when callers cannot mutate the backing storage.

- Do: Store one private static readonly array for immutable internals, return defensive copies when required, or expose constant bytes through ReadOnlySpan<byte>.
- Instead of: Allocating new identical arrays for every object or access.
- Why: It avoids repeated identical array allocations for defaults and small static data.
- Since .NET 9. Supersedes: Per-instance default arrays and static readonly byte[] constants; .NET 6 showed ReadOnlySpan<byte> constant data
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlySpan<T>`

## Use Array.Empty for empty arrays

Return and store Array.Empty<T>() for immutable empty arrays.

- Do: Use Array.Empty<T>() for empty returns, fields, and default values.
- Instead of: new T[0] or new T[] {} in reusable code.
- Why: The shared singleton avoids repeated zero-length array allocations.
- Since .NET Core 3.0. Supersedes: Manual zero-length array allocations
- Hot path: either | Complexity: low
- APIs: `System.Array.Empty<T>`

## Use ThrowIfNull throw helpers

Use static throw helpers, or downlevel polyfills, for common argument validation.

- Do: Use ArgumentNullException.ThrowIfNull and similar ThrowIf APIs; polyfill with C# 14 static extension members for downlevel libraries.
- Instead of: Repeated inline null checks that construct and throw exceptions in the method body.
- Why: They keep the success path small and more inlineable while preserving readable checks.
- Since .NET 6. Supersedes: Manual null-check throw blocks; .NET 10 enables source-wide downlevel polyfills
- Hot path: either | Complexity: low
- APIs: `System.ArgumentNullException.ThrowIfNull`, `System.ObjectDisposedException.ThrowIf`

## Use strongly typed GC handles

Use strongly typed GC handle types when you need explicit pinning, weak, or normal handles.

- Do: Use GCHandle<T>, PinnedGCHandle<T>, or WeakGCHandle<T> and dispose promptly.
- Instead of: GCHandle.Alloc(object, GCHandleType) for every pinned or weak handle.
- Why: They reduce misuse risk and shave overhead versus raw object-based GCHandle patterns.
- Since .NET 10. Supersedes: Raw GCHandle.Alloc handle management in earlier versions
- Hot path: either | Complexity: low
- APIs: `System.Runtime.InteropServices.GCHandle<T>`, `System.Runtime.InteropServices.PinnedGCHandle<T>`, `System.Runtime.InteropServices.WeakGCHandle<T>`

## Use MemoryStream buffer access instead of ToArray

Use TryGetBuffer or GetBuffer when a consumer can accept the existing MemoryStream buffer plus length.

- Do: Call MemoryStream.TryGetBuffer(out ArraySegment<byte>) or GetBuffer and respect the valid range.
- Instead of: MemoryStream.ToArray when an ArraySegment<byte> or ReadOnlySpan<byte> is sufficient.
- Why: It avoids potentially large allocation and copy work from ToArray.
- Since .NET 8. Supersedes: Convenience ToArray calls where direct buffer access was safe
- Hot path: either | Complexity: medium
- APIs: `System.IO.MemoryStream.TryGetBuffer`, `System.IO.MemoryStream.GetBuffer`, `System.IO.MemoryStream.ToArray`
