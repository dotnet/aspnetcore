// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a subscription to component state restoration events. Dispose to unsubscribe.
/// </summary>
public readonly struct RestoringComponentStateSubscription : IDisposable
{
    private readonly List<RestoreComponentStateRegistration> _registeredRestoringCallbacks;
    private readonly RestoreComponentStateRegistration? _registration;

    internal RestoringComponentStateSubscription(
        List<RestoreComponentStateRegistration> registeredRestoringCallbacks,
        RestoreComponentStateRegistration registration)
    {
        _registeredRestoringCallbacks = registeredRestoringCallbacks;
        _registration = registration;
    }

    /// <summary>
    /// Unsubscribes from state restoration events.
    /// </summary>
    public void Dispose()
    {
        if (_registration.HasValue)
        {
            _registeredRestoringCallbacks?.Remove(_registration.Value);
        }
    }
}
