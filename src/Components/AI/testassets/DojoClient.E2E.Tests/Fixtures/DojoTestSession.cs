// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace DojoClient.E2E.Tests.Fixtures;

/// <summary>A test's isolated model and checkpoints on shared dojo hosts.</summary>
public sealed class DojoTestSession : IAsyncDisposable
{
    private readonly HttpClient _client;
    private readonly string _controlPath;
    private bool _created;
    private bool _disposed;

    internal DojoTestSession(ServerInstance ui, ServerInstance model)
    {
        UI = ui;
        Model = model;
        _client = new HttpClient { BaseAddress = new Uri(model.AppUrl) };
        _controlPath = $"{DojoRunStore.ControlPath}/{Id}";
        Checkpoints = new ApiCheckpointClient(model, Id);
    }

    /// <summary>Gets the shared UI host selected by the backend data row.</summary>
    public ServerInstance UI { get; }

    /// <summary>Gets the shared host that owns this session's model.</summary>
    public ServerInstance Model { get; }

    /// <summary>Gets the session ID carried by test navigation and model requests.</summary>
    public string Id { get; } = Guid.NewGuid().ToString("N");

    internal ApiCheckpointClient Checkpoints { get; }

    /// <summary>Builds a scenario URL carrying this test's recording-session selection.</summary>
    /// <param name="path">The scenario's relative path.</param>
    /// <returns>The routed UI URL with its session query parameter.</returns>
    public string GetScenarioUrl(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var separator = path.Contains('?') ? '&' : '?';

        return $"{UI.TestUrl.TrimEnd('/')}/{path.TrimStart('/')}{separator}{DojoRunStore.RunKey}={Id}";
    }

    internal async Task InitializeAsync(DojoRecording? recording)
    {
        var query = recording is { } selected ? $"?recording={selected}" : "";
        using var response = await _client.PostAsync($"{_controlPath}{query}", content: null);
        response.EnsureSuccessStatusCode();
        _created = true;
    }

    /// <summary>Cancels outstanding model work and removes only this test's state.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (_created)
            {
                using var response = await _client.DeleteAsync(_controlPath);
                response.EnsureSuccessStatusCode();
            }
        }
        finally
        {
            _client.Dispose();
        }
    }
}
