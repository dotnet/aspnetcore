// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AIApp.Shared;

/// <summary>
/// Exposes the replay checkpoint currently blocking a test circuit.
/// </summary>
public sealed class ReplayCheckpointState
{
    /// <summary>
    /// Gets the name of the current checkpoint, or <see langword="null"/> when replay is not blocked.
    /// </summary>
    public string? CurrentCheckpoint { get; private set; }

    /// <summary>
    /// Occurs when <see cref="CurrentCheckpoint"/> changes.
    /// </summary>
    public event Action? Changed;

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
        CurrentCheckpoint = null;
        Changed?.Invoke();
    }
}
