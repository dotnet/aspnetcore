// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A component that configures the Blazor browser runtime by merging
/// options into the <see cref="BrowserOptions"/> on the current
/// <see cref="HttpContext"/>. The merged options are emitted as
/// a <c>&lt;!--Blazor-Configuration:{...}--&gt;</c> DOM comment by the renderer.
/// </summary>
public sealed class ConfigureBrowser : IComponent
{
    private RenderHandle _renderHandle;

    /// <summary>
    /// Gets or sets the <see cref="BrowserOptions"/> to merge.
    /// </summary>
    [Parameter, EditorRequired]
    public BrowserOptions Options { get; set; } = default!;

    [CascadingParameter]
    internal HttpContext? HttpContext { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (HttpContext is not null)
        {
            var existing = BrowserOptions.GetBrowserOptions(HttpContext);
            MergeInto(existing, Options);
        }

        return Task.CompletedTask;
    }

    internal static void MergeInto(BrowserOptions target, BrowserOptions source)
    {
        target.LogLevel = source.LogLevel ?? target.LogLevel;

        // Interactive WebAssembly
        target.InteractiveWebAssembly.EnvironmentName = source.InteractiveWebAssembly.EnvironmentName ?? target.InteractiveWebAssembly.EnvironmentName;
        target.InteractiveWebAssembly.ApplicationCulture = source.InteractiveWebAssembly.ApplicationCulture ?? target.InteractiveWebAssembly.ApplicationCulture;
        foreach (var kvp in source.InteractiveWebAssembly.EnvironmentVariables)
        {
            target.InteractiveWebAssembly.EnvironmentVariables[kvp.Key] = kvp.Value;
        }

        // Interactive server
        target.InteractiveServer.ReconnectionMaxRetries = source.InteractiveServer.ReconnectionMaxRetries ?? target.InteractiveServer.ReconnectionMaxRetries;
        target.InteractiveServer.ReconnectionRetryInterval = source.InteractiveServer.ReconnectionRetryInterval ?? target.InteractiveServer.ReconnectionRetryInterval;
        target.InteractiveServer.ReconnectionDialogId = source.InteractiveServer.ReconnectionDialogId ?? target.InteractiveServer.ReconnectionDialogId;
        foreach (var kvp in source.InteractiveServer.Extensions)
        {
            target.InteractiveServer.Extensions[kvp.Key] = kvp.Value;
        }

        // Static server (SSR)
        target.StaticServer.PreserveDom = source.StaticServer.PreserveDom ?? target.StaticServer.PreserveDom;
        target.StaticServer.CircuitInactivityTimeout = source.StaticServer.CircuitInactivityTimeout ?? target.StaticServer.CircuitInactivityTimeout;
    }
}
