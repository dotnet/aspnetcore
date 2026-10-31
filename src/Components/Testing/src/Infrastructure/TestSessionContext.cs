// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Scoped service populated by the test infrastructure middleware.
/// Reads the <c>test-session-id</c> cookie from the request and exposes it to
/// any service in the request's DI scope (e.g., a test service override
/// that uses <see cref="TestLockProvider"/> with session-scoped lock keys).
/// </summary>
/// <remarks>
/// Always registered by the hosting startup; no-op when the cookie is absent.
/// </remarks>
public class TestSessionContext
{
    /// <summary>
    /// Gets or sets the test session identifier from the <c>test-session-id</c> cookie.
    /// <c>null</c> when no test session cookie is present on the request.
    /// </summary>
    public string? Id { get; set; }
}
