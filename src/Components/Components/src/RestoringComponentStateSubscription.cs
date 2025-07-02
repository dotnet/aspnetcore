// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a subscription to state restoration notifications.
/// </summary>
public readonly struct RestoringComponentStateSubscription : IDisposable
{
    private readonly List<RestoreComponentStateRegistration>? _callbacks;
    private readonly IPersistentStateFilter? _filter;
    private readonly Action? _callback;

    internal RestoringComponentStateSubscription(
        List<RestoreComponentStateRegistration> callbacks,
        IPersistentStateFilter filter,
        Action callback)
    {
        _callbacks = callbacks;
        _filter = filter;
        _callback = callback;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_callbacks != null && _filter != null && _callback != null)
        {
            for (int i = _callbacks.Count - 1; i >= 0; i--)
            {
                var registration = _callbacks[i];
                if (ReferenceEquals(registration.Filter, _filter) && ReferenceEquals(registration.Callback, _callback))
                {
                    _callbacks.RemoveAt(i);
                    break;
                }
            }
        }
    }
}