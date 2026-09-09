// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace DojoClient.E2E.Tests.Fixtures;

internal sealed class FunctionScenarioClient(ServerInstance server, string threadId) : IAsyncDisposable
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri(server.AppUrl) };
    private readonly string _path = $"/_test/functions/{Uri.EscapeDataString(threadId)}";

    internal Task<int> GetInvocationCountAsync()
        => _client.GetFromJsonAsync<int>($"{_path}/invocations");

    public async ValueTask DisposeAsync()
    {
        try
        {
            using var response = await _client.DeleteAsync(_path);
            response.EnsureSuccessStatusCode();
        }
        finally
        {
            _client.Dispose();
        }
    }
}
