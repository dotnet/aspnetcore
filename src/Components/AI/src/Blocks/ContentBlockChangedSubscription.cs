// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a subscription to <see cref="ContentBlock.OnChanged(Action)"/>.
/// Disposing the subscription removes the callback from the block.
/// </summary>
public readonly struct ContentBlockChangedSubscription : IDisposable
{
    private readonly ContentBlock _owner;
    private readonly Action _callback;

    internal ContentBlockChangedSubscription(ContentBlock owner, Action callback)
    {
        _owner = owner;
        _callback = callback;
    }

    /// <summary>
    /// Removes the callback from the block it was registered on.
    /// </summary>
    public void Dispose()
    {
        _owner?.RemoveCallback(_callback);
    }
}
