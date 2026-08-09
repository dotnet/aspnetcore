// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;

namespace AIApp.E2E.Tests.ServiceOverrides;

internal sealed class ReplayTestSession
{
    public const string QueryParameterName = "test-session-id";

    private readonly TestLockClient _locks;
    private readonly ServerInstance _server;
    private readonly string _sessionId;

    private ReplayTestSession(TestLockClient locks, ServerInstance server, string sessionId)
    {
        _locks = locks;
        _server = server;
        _sessionId = sessionId;
    }

    public static async Task<ReplayTestSession> CreateAsync(
        ServerInstance server,
        IBrowserContext context)
    {
        var locks = await TestLockClient.CreateAsync(server, context);
        var cookies = await context.CookiesAsync([server.TestUrl]);
        var sessionId = cookies.Single(cookie => cookie.Name == QueryParameterName).Value;
        return new ReplayTestSession(locks, server, sessionId);
    }

    public string GetUrl(string path)
        => $"{_server.TestUrl}{path}?{QueryParameterName}={Uri.EscapeDataString(_sessionId)}";

    public RemoteLock Lock(string name)
        => _locks.Lock(name);
}
