// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Client;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Configures an application's MCP content host.</summary>
public sealed class McpContentOptions
{
    /// <summary>Gets or sets the absolute address of the distinct-origin sandbox proxy.</summary>
    public required Uri ProxyUrl { get; set; }

    /// <summary>Gets the browser origins and permissions the application permits resource declarations to request.</summary>
    public McpContentPolicy BrowserPolicy { get; } = new();

    /// <summary>Gets or sets the maximum inline app height in CSS pixels.</summary>
    public int MaximumHeight { get; set; } = 1200;

    /// <summary>Gets or sets an additional app tool policy, taking the owning client and original server tool name.</summary>
    public Func<McpClient, string, bool>? AllowToolCall { get; set; }

    /// <summary>Gets or sets a policy for other resource reads, taking the owning client and resource URI.</summary>
    public Func<McpClient, string, bool>? AllowResourceRead { get; set; }

    /// <summary>Gets or sets the maximum number of simultaneous incoming operations.</summary>
    public int MaximumPendingRequests { get; set; } = 32;

    /// <summary>Gets or sets the maximum UTF-8 bytes in a request or response.</summary>
    public int MaximumMessageBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Gets or sets the maximum UTF-8 document size.</summary>
    public int MaximumDocumentBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Gets or sets the initialization deadline.</summary>
    public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the deadline for each operation.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the teardown deadline.</summary>
    public TimeSpan TeardownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    internal void Validate()
    {
        if (!ProxyUrl.IsAbsoluteUri || ProxyUrl.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("The app proxy requires an absolute HTTP address.", nameof(ProxyUrl));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumPendingRequests);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumMessageBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaximumDocumentBytes);
        ValidateTimeout(InitializationTimeout);
        ValidateTimeout(OperationTimeout);
        ValidateTimeout(TeardownTimeout);
    }

    private static void ValidateTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
    }
}
