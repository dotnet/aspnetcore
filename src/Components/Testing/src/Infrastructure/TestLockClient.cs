// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// High-level client for deterministic async state control via <see cref="TestLockProvider"/>.
/// Encapsulates session ID generation, cookie setup, and lock release HTTP calls.
/// </summary>
/// <remarks>
/// <para>Create a client, then use <see cref="Lock"/> to gate server-side async operations:</para>
/// <code>
/// var locks = await TestLockClient.CreateAsync(server, context);
/// await using (locks.Lock("weather-data"))
/// {
///     // Verify loading state while data is blocked
/// }
/// // Lock released automatically
/// </code>
/// </remarks>
public class TestLockClient
{
    private readonly ServerInstance _server;
    private readonly string _sessionId;

    TestLockClient(ServerInstance server, string sessionId)
    {
        _server = server;
        _sessionId = sessionId;
    }

    /// <summary>
    /// Creates a lock client, generates a unique session ID, and sets
    /// the <c>test-session-id</c> cookie on the browser context.
    /// </summary>
    /// <param name="server">The server instance to control locks on.</param>
    /// <param name="context">The browser context to set the session cookie on.</param>
    /// <returns>A <see cref="TestLockClient"/> bound to the server and session.</returns>
    public static async Task<TestLockClient> CreateAsync(
        ServerInstance server, IBrowserContext context)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        await context.SetTestSession(server, sessionId).ConfigureAwait(false);
        return new TestLockClient(server, sessionId);
    }

    /// <summary>
    /// Creates a lock handle for the given name. The server-side lock key
    /// is <c>{sessionId}:{name}</c>. The lock is released when the handle is
    /// disposed or when <see cref="RemoteLock.ReleaseAsync"/> is called explicitly.
    /// </summary>
    /// <param name="name">The lock name (combined with the session ID to form the full key).</param>
    /// <returns>A <see cref="RemoteLock"/> that releases the named gate on disposal.</returns>
    public RemoteLock Lock(string name)
        => new(_server, $"{_sessionId}:{name}");
}
