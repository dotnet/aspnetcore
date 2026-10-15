// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace DojoClient.E2E.Tests.ServiceOverrides;

// Releases the checkpoint gates held by RecordedChatClient inside the API process.
//
// The gates cannot be driven through TestLockClient because the browser never talks to the
// API directly: DojoClient calls it server to server, so no test session cookie reaches it.
// Keys are namespaced by the message the test typed, which makes them unique per run.
internal sealed class ApiCheckpointClient
{
    private readonly ServerInstance _api;

    public ApiCheckpointClient(ServerInstance api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    public async Task ReleaseAsync(string lastUserMessage, string frameName)
    {
        var key = RecordedChatClient.GetLockKey(lastUserMessage, frameName);
        using var client = new HttpClient();
        using var response = await client.PostAsync(
            $"{_api.AppUrl}/_test/lock/release?key={Uri.EscapeDataString(key)}",
            content: null);
        response.EnsureSuccessStatusCode();
    }
}
