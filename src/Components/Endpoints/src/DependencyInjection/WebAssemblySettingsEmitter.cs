// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal record WebAssemblySettings(string EnvironmentName, Dictionary<string, string> EnvironmentVariables);

internal class WebAssemblySettingsEmitter(IHostEnvironment hostEnvironment)
{
    private bool wasEmittedAlready;

    private const string dotnetModifiableAssembliesName = "DOTNET_MODIFIABLE_ASSEMBLIES";
    private const string aspnetcoreBrowserToolsName = "__ASPNETCORE_BROWSER_TOOLS";

    private static readonly string? s_dotnetModifiableAssemblies = GetNonEmptyEnvironmentVariableValue(dotnetModifiableAssembliesName);
    private static readonly string? s_aspnetcoreBrowserTools = GetNonEmptyEnvironmentVariableValue(aspnetcoreBrowserToolsName);

    private static string? GetNonEmptyEnvironmentVariableValue(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    public bool TryGetSettingsOnce([NotNullWhen(true)] out WebAssemblySettings? settings)
    {
        if (wasEmittedAlready)
        {
            settings = default;
            return false;
        }

        var environmentVariables = new Dictionary<string, string>();

        // DOTNET_MODIFIABLE_ASSEMBLIES is used by the runtime to initialize hot-reload specific environment variables and is configured
        // by the launching process (dotnet-watch / Visual Studio).
        // Always add the header if the environment variable is set, regardless of the kind of environment.
        if (s_dotnetModifiableAssemblies != null)
        {
            environmentVariables[dotnetModifiableAssembliesName] = s_dotnetModifiableAssemblies;
        }

        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        // Translate the _ASPNETCORE_BROWSER_TOOLS environment configured by the browser tools agent in to a HTTP response header.
        if (s_aspnetcoreBrowserTools != null)
        {
            environmentVariables[aspnetcoreBrowserToolsName] = s_aspnetcoreBrowserTools;
        }

        wasEmittedAlready = true;
        settings = new (hostEnvironment.EnvironmentName, environmentVariables);
        return true;
    }
}
