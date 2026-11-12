// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// A feature that allows components to validate and observe authentication refreshes on a connection.
/// </summary>
public interface IConnectionAuthenticationRefreshFeature
{
    /// <summary>
    /// Gets or sets the callback invoked before authentication is refreshed.
    /// </summary>
    /// <remarks>
    /// The callback must return <see langword="true"/> for the refresh to proceed. It is invoked without
    /// holding the user update lock and may perform asynchronous work, but should complete quickly.
    /// An exception thrown by the callback rejects the refresh and is not propagated to the caller.
    /// Setting this property replaces any previously configured callback.
    /// </remarks>
    Func<AuthenticationRefreshContext, Task<bool>> OnAuthenticationRefresh { get; set; }

    /// <summary>
    /// Registers a callback to be invoked after authentication is refreshed.
    /// </summary>
    /// <param name="callback">The callback to invoke with the refresh context and associated <paramref name="state"/>.</param>
    /// <param name="state">The state to pass to <paramref name="callback"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that can be disposed to unregister the callback.</returns>
    /// <remarks>
    /// Callbacks should be quick and avoid blocking; the callback is invoked on the thread that performed the update.
    /// Exceptions thrown from callbacks are not propagated to the caller of the update.
    /// The previous principal may own resources that are disposed when the authentication-refresh request completes,
    /// so callbacks must not access <see cref="AuthenticationRefreshContext.PreviousUser"/> after returning.
    /// </remarks>
    IDisposable OnAuthenticationRefreshed(Action<AuthenticationRefreshContext, object?> callback, object? state);
}
