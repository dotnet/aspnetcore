// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUIDojoApi;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;

namespace DojoClient.E2E.Tests.Fixtures;

/// <summary>
/// Starts the same dojo UI with either the AG-UI API or an in-process model.
/// </summary>
public abstract class DojoTestBase : BrowserTest
{
    /// <summary>
    /// Starts the selected backend and returns the UI and the host that owns model checkpoints.
    /// </summary>
    /// <param name="backend">The backend data row: AGUI or Direct.</param>
    /// <param name="configureModel">The recorded-model configuration, or null for the offline scripted model.</param>
    /// <returns>The UI server and the server running the model.</returns>
    protected async Task<(ServerInstance UI, ServerInstance Model)> StartDojoAsync(
        string backend,
        Action<ServerStartOptions>? configureModel = null)
    {
        if (backend is not ("AGUI" or "Direct"))
        {
            throw new ArgumentException($"Unknown dojo backend '{backend}'.", nameof(backend));
        }

        ServerInstance? api = null;
        if (backend == "AGUI")
        {
            api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, ConfigureModel);
        }

        var ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["DOJO_BACKEND"] = backend;
            if (api is not null)
            {
                options.EnvironmentVariables["AGUI_DOJO_API_URL"] = api.AppUrl;
            }
            else
            {
                // An accidental AG-UI dependency must fail rather than reach an ambient server.
                options.EnvironmentVariables["AGUI_DOJO_API_URL"] = "http://127.0.0.1:1";
                ConfigureModel(options);
            }
        });

        void ConfigureModel(ServerStartOptions options)
        {
            options.EnvironmentVariables["DOJO_BACKEND"] = backend;
            options.EnvironmentVariables["OPENAI_BASE_URL"] = "";
            options.EnvironmentVariables["OPENAI_API_KEY"] = "";
            configureModel?.Invoke(options);
        }

        return (ui, api ?? ui);
    }
}
