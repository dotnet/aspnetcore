// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace BasicTestApp.FormsTest;

// Test controlled gate that holds async DataAnnotations validation in the pending state until the
// test releases it, so the pending to settled transition is driven by a button click rather than by
// wall clock timing. Interactive hosts register this so the E2E tests stay deterministic; hosts that
// do not register it (for example the static SSR host) let the validators complete on their own.
//
// Blazor E2E parallelization is disabled, so a single instance per app is safe. The hosting component
// calls Reset on mount so every test starts from a fresh, unsignaled gate.
public sealed class AsyncValidationGate
{
    private TaskCompletionSource _gate = Create();

    private static TaskCompletionSource Create()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Reset() => _gate = Create();

    public Task WaitAsync(CancellationToken cancellationToken) => _gate.Task.WaitAsync(cancellationToken);

    public void Release() => _gate.TrySetResult();
}
