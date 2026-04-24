// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// This class represents an active subscription to a cascading parameter. The implementation is responsible for providing the current value and notifying about changes by causing the subscriber component to re-render when necessary.
/// </summary>
public abstract class CascadingParameterSubscription : IDisposable
{
    /// <summary>
    /// Function that returns the current value for the subscription.
    /// </summary>
    public abstract object? GetCurrentValue();

    /// <inheritdoc/>
    public abstract void Dispose();
}
