// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp.AuthTest;

// Test controlled gate that holds an authorization policy in the pending state until the test releases it,
// so that a navigation can be made while an authorization evaluation is still outstanding.
public sealed class AsyncAuthorizationGate
{
    private TaskCompletionSource _gate = Create();

    private static TaskCompletionSource Create()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int StartedEvaluations { get; private set; }

    public int CompletedEvaluations { get; private set; }

    public bool IsSettled => StartedEvaluations > 0 && CompletedEvaluations == StartedEvaluations;

    public event Action OnProgressChanged;

    public void Reset()
    {
        var previousGate = _gate;
        _gate = Create();
        previousGate.TrySetResult();
        StartedEvaluations = 0;
        CompletedEvaluations = 0;
    }

    public async Task<bool> WaitAsync()
    {
        StartedEvaluations++;
        OnProgressChanged?.Invoke();

        await _gate.Task;

        CompletedEvaluations++;
        OnProgressChanged?.Invoke();

        return true;
    }

    public void Release() => _gate.TrySetResult();
}
