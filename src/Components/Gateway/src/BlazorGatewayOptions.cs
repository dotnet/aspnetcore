// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Gateway;

internal sealed class BlazorGatewayOptions
{
    public const string SectionName = "Gateway";

    public string? PathBase { get; set; }

    public HstsOptions Hsts { get; set; } = new();

    public HttpsRedirectionOptions HttpsRedirection { get; set; } = new();

    public HealthCheckOptions HealthChecks { get; set; } = new();

    public TelemetryOptions Telemetry { get; set; } = new();

    internal sealed class HstsOptions
    {
        public bool Enabled { get; set; } = true;
    }

    internal sealed class HttpsRedirectionOptions
    {
        public bool Enabled { get; set; } = true;
    }

    internal sealed class HealthCheckOptions
    {
        public bool Enabled { get; set; } = true;
        public string Path { get; set; } = "/health";
        public string LivenessPath { get; set; } = "/alive";
        public string LivenessTag { get; set; } = "live";
    }

    internal sealed class TelemetryOptions
    {
        public bool Enabled { get; set; } = true;
        public List<string> ExcludePaths { get; set; } = new() { "/health", "/alive", "/_otlp/" };
        public List<string> ExcludeOutboundPaths { get; set; } = new() { "/v1/" };
    }
}
