// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUIDojoApi;
using DojoAgent;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;

namespace DojoClient.E2E.Tests.Fixtures;

/// <summary>
/// Selects shared dojo hosts and owns isolated recording sessions for each test.
/// </summary>
public abstract class DojoTestBase : BrowserTest
{
    private readonly List<DojoTestSession> _sessions = [];

    /// <summary>
    /// Acquires the selected backend's suite-wide hosts and creates isolated model state.
    /// </summary>
    /// <param name="backend">The backend data row: AGUI or Direct.</param>
    /// <param name="recording">The recording to use, or null for the offline scripted model.</param>
    /// <returns>The test session, including host routing and checkpoint controls.</returns>
    protected async Task<DojoTestSession> GetDojoAsync(
        DojoBackendKind backend,
        DojoRecording? recording = null)
    {
        if (backend is not (DojoBackendKind.AGUI or DojoBackendKind.Direct))
        {
            throw new ArgumentException($"Unknown dojo backend '{backend}'.", nameof(backend));
        }

        ServerInstance? api = null;
        if (backend == DojoBackendKind.AGUI)
        {
            api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
            {
                ConfigureEnvironment(options, backend);
                options.ConfigureServices<DojoModelOverrides>(nameof(DojoModelOverrides.ConfigureApi));
            });
        }

        var ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            ConfigureEnvironment(options, backend);
            options.ConfigureServices<DojoModelOverrides>(nameof(DojoModelOverrides.ConfigureUI));
            if (api is not null)
            {
                options.EnvironmentVariables["AGUI_DOJO_API_URL"] = api.AppUrl;
            }
            else
            {
                options.EnvironmentVariables["AGUI_DOJO_API_URL"] = "http://127.0.0.1:1";
            }
        });

        var session = new DojoTestSession(ui, api ?? ui);
        _sessions.Add(session);
        await session.InitializeAsync(recording);

        return session;
    }

    /// <inheritdoc />
    protected override async Task CleanupCoreAsync()
    {
        try
        {
            await Task.WhenAll(_sessions.Select(session => session.DisposeAsync().AsTask()));
        }
        finally
        {
            await base.CleanupCoreAsync();
        }
    }

    private static void ConfigureEnvironment(ServerStartOptions options, DojoBackendKind backend)
    {
        options.EnvironmentVariables["DOJO_BACKEND"] = backend.ToString();
        options.EnvironmentVariables["OPENAI_BASE_URL"] = "";
        options.EnvironmentVariables["OPENAI_API_KEY"] = "";
    }
}
