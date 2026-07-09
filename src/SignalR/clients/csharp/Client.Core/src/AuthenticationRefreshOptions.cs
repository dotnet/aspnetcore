// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Configures automatic authentication token refresh for a <see cref="HubConnection"/>.
/// </summary>
public sealed class AuthenticationRefreshOptions
{
    /// <summary>
    /// Enables automatic token refresh before the server-reported token expiration.
    /// Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// When enabled, the client schedules a refresh based on the <c>tokenLifetimeSeconds</c>
    /// reported by the server in the negotiate (and subsequent refresh) responses. If the server
    /// does not report a token lifetime, no automatic refresh is scheduled regardless of this setting;
    /// the application may still call <see cref="HubConnection.RefreshAuthenticationAsync"/> manually.
    /// </remarks>
    public bool EnableAutoRefresh { get; set; } = true;

    /// <summary>
    /// How far before the server-reported token expiration the client should refresh.
    /// The client schedules refresh at: <c>now + tokenLifetimeSeconds - RefreshBeforeExpiration</c>.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan RefreshBeforeExpiration
    {
        get;
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be zero or greater.");
            }

            field = value;
        }
    } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Optional callback invoked after a successful authentication refresh.
    /// </summary>
    public Func<AuthenticationRefreshedContext, Task>? OnAuthenticationRefreshed { get; set; }

    /// <summary>
    /// Optional callback invoked when an authentication refresh attempt fails.
    /// </summary>
    public Func<AuthenticationRefreshFailedContext, Task>? OnAuthenticationRefreshFailed { get; set; }
}

/// <summary>
/// Context passed to <see cref="AuthenticationRefreshOptions.OnAuthenticationRefreshed"/> after a successful refresh.
/// </summary>
public sealed class AuthenticationRefreshedContext
{
    internal AuthenticationRefreshedContext(HubConnection hubConnection, TimeSpan? newTokenLifetime)
    {
        HubConnection = hubConnection;
        NewTokenLifetime = newTokenLifetime;
        RefreshedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the <see cref="HubConnection"/> whose authentication was refreshed.
    /// </summary>
    public HubConnection HubConnection { get; }

    /// <summary>
    /// Gets the new token lifetime reported by the server, or <c>null</c> if not provided.
    /// </summary>
    public TimeSpan? NewTokenLifetime { get; }

    /// <summary>
    /// Gets the time at which the refresh completed.
    /// </summary>
    public DateTimeOffset RefreshedAt { get; }
}

/// <summary>
/// Context passed to <see cref="AuthenticationRefreshOptions.OnAuthenticationRefreshFailed"/> when a refresh attempt fails.
/// </summary>
public sealed class AuthenticationRefreshFailedContext
{
    internal AuthenticationRefreshFailedContext(HubConnection hubConnection, Exception exception)
    {
        HubConnection = hubConnection;
        Exception = exception;
    }

    /// <summary>
    /// Gets the <see cref="HubConnection"/> on which the refresh attempt failed.
    /// </summary>
    public HubConnection HubConnection { get; }

    /// <summary>
    /// Gets the exception that caused the refresh attempt to fail.
    /// </summary>
    public Exception Exception { get; }
}
