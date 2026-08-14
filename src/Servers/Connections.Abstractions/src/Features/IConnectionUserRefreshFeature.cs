// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// A feature that allows components to validate and observe changes to the user associated with a connection,
/// for example, during an authentication refresh.
/// </summary>
public interface IConnectionUserRefreshFeature
{
    /// <summary>
    /// Gets or sets the callback invoked before the <see cref="IConnectionUserFeature.User"/> is refreshed.
    /// </summary>
    /// <remarks>
    /// The callback is invoked synchronously while the user update is locked and must return <see langword="true"/>
    /// for the update to proceed. It should complete quickly and must not block or reenter the user update.
    /// An exception thrown by the callback rejects the update and is not propagated to the caller.
    /// Setting this property replaces any previously configured callback. When set to <see langword="null"/>,
    /// the feature implementation's default validation policy applies.
    /// </remarks>
    Func<ClaimsPrincipal, bool>? OnUserRefreshing { get; set; }

    /// <summary>
    /// Registers a callback to be invoked after the <see cref="IConnectionUserFeature.User"/> has been refreshed.
    /// </summary>
    /// <param name="callback">The callback to invoke with the refreshed principal and associated <paramref name="state"/>.</param>
    /// <param name="state">The state to pass to <paramref name="callback"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that can be disposed to unregister the callback.</returns>
    /// <remarks>
    /// Callbacks should be quick and avoid blocking; the callback is invoked on the thread that performed the update.
    /// Exceptions thrown from callbacks are not propagated to the caller of the update.
    /// The previous principal is intentionally not exposed because its underlying resources
    /// (for example a <c>WindowsIdentity</c>'s <c>SafeHandle</c>) may be disposed when the
    /// authentication-refresh request completes, making later access unsafe.
    /// </remarks>
    IDisposable OnUserRefreshed(Action<ClaimsPrincipal, object?> callback, object? state);
}
