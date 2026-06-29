// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// A feature that allows callbacks to be notified when the user associated with the connection is refreshed,
/// for example, via an authentication refresh.
/// </summary>
public interface IConnectionUserRefreshFeature
{
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
