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

    /// <summary>
    /// Gets or sets the <see cref="HttpContext"/> for the current request.
    /// </summary>
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (HttpContext is not null)
        {
            var existing = HttpContext.GetBrowserOptions();
            MergeInto(existing, Options);
        }

        return Task.CompletedTask;
    }

    internal static void MergeInto(BrowserOptions target, BrowserOptions source)
    {
        target.LogLevel = source.LogLevel ?? target.LogLevel;

        // WebAssembly
        target.WebAssembly.EnvironmentName = source.WebAssembly.EnvironmentName ?? target.WebAssembly.EnvironmentName;
        target.WebAssembly.ApplicationCulture = source.WebAssembly.ApplicationCulture ?? target.WebAssembly.ApplicationCulture;
        foreach (var kvp in source.WebAssembly.EnvironmentVariables)
        {
            target.WebAssembly.EnvironmentVariables[kvp.Key] = kvp.Value;
        }

        // Server
        target.Server.ReconnectionMaxRetries = source.Server.ReconnectionMaxRetries ?? target.Server.ReconnectionMaxRetries;
        target.Server.ReconnectionRetryInterval = source.Server.ReconnectionRetryInterval ?? target.Server.ReconnectionRetryInterval;
        target.Server.ReconnectionDialogId = source.Server.ReconnectionDialogId ?? target.Server.ReconnectionDialogId;
        target.Server.AutoPauseEnabled = source.Server.AutoPauseEnabled ?? target.Server.AutoPauseEnabled;
        target.Server.AutoPauseHiddenDelayMilliseconds = source.Server.AutoPauseHiddenDelayMilliseconds ?? target.Server.AutoPauseHiddenDelayMilliseconds;

        // SSR
        target.Ssr.PreserveDom = source.Ssr.PreserveDom ?? target.Ssr.PreserveDom;
        target.Ssr.CircuitInactivityTimeout = source.Ssr.CircuitInactivityTimeout ?? target.Ssr.CircuitInactivityTimeout;
    }
}
