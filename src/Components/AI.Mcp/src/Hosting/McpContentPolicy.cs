// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Contains an isolated HTML document and its effective host-approved browser policy.</summary>
internal sealed class McpAppDocument
{
    /// <summary>Initializes an isolated document.</summary>
    public McpAppDocument(string html, McpContentPolicy policy)
    {
        Html = html ?? throw new ArgumentNullException(nameof(html));
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <summary>Gets the HTML to render.</summary>
    public string Html { get; }

    /// <summary>Gets the effective browser policy.</summary>
    public McpContentPolicy Policy { get; }
}

/// <summary>Specifies explicit browser grants for an isolated document.</summary>
public sealed class McpContentPolicy
{
    /// <summary>Gets the resource origins allowed by the host.</summary>
    public IList<string> ResourceDomains { get; } = new List<string>();

    /// <summary>Gets the HTTP, HTTPS, WS, or WSS network origins allowed by the host.</summary>
    public IList<string> ConnectDomains { get; } = new List<string>();

    /// <summary>Gets the origins allowed for nested frames.</summary>
    public IList<string> FrameDomains { get; } = new List<string>();

    /// <summary>Gets the origins allowed for base addresses.</summary>
    public IList<string> BaseUriDomains { get; } = new List<string>();

    /// <summary>Gets browser feature names explicitly granted to the document.</summary>
    public IList<string> Permissions { get; } = new List<string>();
}
