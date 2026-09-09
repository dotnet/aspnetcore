// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace DojoClient.E2E.Tests.ServiceOverrides;

// Checkpoints belong to a test session, not to the shared model host.
internal sealed class ApiCheckpointClient
{
    private readonly ServerInstance _server;
    private readonly string _runId;

    public ApiCheckpointClient(ServerInstance server, string runId)
    {
        ArgumentNullException.ThrowIfNull(server);
        _server = server;
        _runId = runId;
    }

    public async Task ReleaseAsync(string lastUserMessage, string frameName)
    {
        var key = RecordedChatClient.GetLockKey(lastUserMessage, frameName);
        using var client = new HttpClient();
        using var response = await client.PostAsync(
            $"{_server.AppUrl}{DojoRunStore.ControlPath}/{_runId}/release?key={Uri.EscapeDataString(key)}",
            content: null);
        response.EnsureSuccessStatusCode();
    }

    internal async Task<bool> IsReleasedAsync(string lastUserMessage, string frameName)
    {
        var key = RecordedChatClient.GetLockKey(lastUserMessage, frameName);
        using var client = new HttpClient();
        var value = await client.GetStringAsync(
            $"{_server.AppUrl}{DojoRunStore.ControlPath}/{_runId}/checkpoint?key={Uri.EscapeDataString(key)}");

        return bool.Parse(value);
    }
}
