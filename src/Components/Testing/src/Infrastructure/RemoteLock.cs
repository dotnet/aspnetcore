// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// A lock handle that releases a server-side async gate via HTTP POST on disposal.
/// Supports both <c>await using</c> (automatic) and explicit <see cref="ReleaseAsync"/> patterns.
/// </summary>
/// <remarks>
/// Uses <see cref="HttpClient"/> directly (not page JavaScript execution) because
/// JS execution is blocked during Blazor streaming navigation.
/// </remarks>
public sealed class RemoteLock : IAsyncDisposable
{
    private readonly ServerInstance _server;
    private readonly string _key;
    private bool _released;

    internal RemoteLock(ServerInstance server, string key)
    {
        _server = server;
        _key = key;
    }

    /// <summary>
    /// Explicitly releases the lock by posting to the app's lock release endpoint.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    public async Task ReleaseAsync()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        var encodedKey = Uri.EscapeDataString(_key);
        using var client = new HttpClient();
        await client.PostAsync(
            $"{_server.AppUrl}/_test/lock/release?key={encodedKey}", content: null).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await ReleaseAsync().ConfigureAwait(false);
}
