// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AIApp.Shared;

/// <summary>
/// Exposes the replay checkpoint currently blocking a test circuit.
/// </summary>
public sealed class ReplayCheckpointState
{
    private int _nextCallIndex;
    private bool _callActive;

    /// <summary>
    /// Gets the name of the current checkpoint, or <see langword="null"/> when replay is not blocked.
    /// </summary>
    public string? CurrentCheckpoint { get; private set; }

    /// <summary>
    /// Gets the replay generation. The initial generation is zero and each reset increments it.
    /// </summary>
    public int Generation { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a replay call is active.
    /// </summary>
    public bool IsReplayActive => _callActive;

    /// <summary>
    /// Occurs when the replay activity, generation, or checkpoint changes.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Starts the next replay call.
    /// </summary>
    /// <returns>The zero-based call index for the current replay generation.</returns>
    public int BeginReplayCall()
    {
        if (_callActive)
        {
            throw new InvalidOperationException("A replay call is already active.");
        }

        _callActive = true;
        var callIndex = _nextCallIndex++;
        Changed?.Invoke();
        return callIndex;
    }

    /// <summary>
    /// Marks the current replay call as complete.
    /// </summary>
    public void EndReplayCall()
    {
        if (!_callActive)
        {
            throw new InvalidOperationException("No replay call is active.");
        }

        _callActive = false;
        Changed?.Invoke();
    }

    /// <summary>
    /// Starts a new replay generation at call zero.
    /// </summary>
    public void ResetReplay()
    {
        if (_callActive)
        {
            throw new InvalidOperationException("Replay cannot be reset while a call is active.");
        }

        _nextCallIndex = 0;
        Generation++;
        CurrentCheckpoint = null;
        Changed?.Invoke();
    }

    /// <summary>
    /// Sets the checkpoint currently blocking replay.
    /// </summary>
    /// <param name="checkpoint">The checkpoint name.</param>
    public void SetCheckpoint(string checkpoint)
    {
        ArgumentException.ThrowIfNullOrEmpty(checkpoint);
        CurrentCheckpoint = checkpoint;
        Changed?.Invoke();
    }

    /// <summary>
    /// Clears the checkpoint when its gate is released.
    /// </summary>
    public void ClearCheckpoint()
    {
        if (CurrentCheckpoint is null)
        {
            return;
        }

        CurrentCheckpoint = null;
        Changed?.Invoke();
    }
}
