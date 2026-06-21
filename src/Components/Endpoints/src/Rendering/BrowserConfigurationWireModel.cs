// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

// Internal wire representation of <see cref="BrowserOptions"/>. The serialized format
// is consumed by the Blazor JS runtime and is treated as a versioned/internal contract,
// so it is decoupled from the public options shape. Durations are emitted as milliseconds
// and positive options (e.g. PreserveDom) are mapped to the JS negative form.
internal sealed class BrowserConfigurationWireModel
{
    public int? LogLevel { get; set; }

    public WebAssemblyWireModel WebAssembly { get; set; } = new();

    public ServerWireModel Server { get; set; } = new();

    public SsrWireModel Ssr { get; set; } = new();

    public static BrowserConfigurationWireModel FromOptions(BrowserOptions options)
    {
        var wire = new BrowserConfigurationWireModel
        {
            LogLevel = options.LogLevel is { } logLevel ? (int)logLevel : null,
            WebAssembly = new WebAssemblyWireModel
            {
                EnvironmentName = options.WebAssembly.EnvironmentName,
                ApplicationCulture = options.WebAssembly.ApplicationCulture,
                EnvironmentVariables = options.WebAssembly.EnvironmentVariables.Count > 0
                    ? new Dictionary<string, string>(options.WebAssembly.EnvironmentVariables)
                    : null,
            },
            Server = new ServerWireModel
            {
                ReconnectionMaxRetries = options.Server.ReconnectionMaxRetries,
                ReconnectionRetryIntervalMilliseconds = options.Server.ReconnectionRetryInterval is { } interval
                    ? (int)interval.TotalMilliseconds
                    : null,
                ReconnectionDialogId = options.Server.ReconnectionDialogId,
            },
            Ssr = new SsrWireModel
            {
                DisableDomPreservation = options.Ssr.PreserveDom is { } preserveDom ? !preserveDom : null,
                CircuitInactivityTimeoutMs = options.Ssr.CircuitInactivityTimeout is { } timeout
                    ? (int)timeout.TotalMilliseconds
                    : null,
            },
        };

        return wire;
    }

    internal sealed class WebAssemblyWireModel
    {
        public string? EnvironmentName { get; set; }

        public string? ApplicationCulture { get; set; }

        public IDictionary<string, string>? EnvironmentVariables { get; set; }
    }

    internal sealed class ServerWireModel
    {
        public int? ReconnectionMaxRetries { get; set; }

        public int? ReconnectionRetryIntervalMilliseconds { get; set; }

        public string? ReconnectionDialogId { get; set; }
    }

    internal sealed class SsrWireModel
    {
        public bool? DisableDomPreservation { get; set; }

        public int? CircuitInactivityTimeoutMs { get; set; }
    }
}
