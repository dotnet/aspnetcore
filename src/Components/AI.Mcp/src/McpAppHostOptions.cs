// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Configures retention and originating tool execution for a conversation-scoped MCP Apps host.</summary>
public sealed class McpAppHostOptions
{
    /// <summary>Gets or sets the maximum retained originating calls. Entries are retained until host disposal.</summary>
    public int MaximumRetainedCalls { get; set; } = 128;

    /// <summary>Gets or sets the maximum retained UTF-8 bytes in an originating tool result.</summary>
    public int MaximumResultBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Gets or sets the deadline for the originating model tool call.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
