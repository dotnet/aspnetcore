// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Mcp.Hosting;

internal sealed record McpComponentRequest(
    Uri Location,
    ClaimsPrincipal User,
    string? HandlerName = null,
    IReadOnlyDictionary<string, StringValues>? Form = null,
    IFormFileCollection? Files = null,
    bool IsShellRendering = false);

internal sealed class McpOperationContext
{
    private McpComponentRequest? _request;

    public bool IsActive => _request is not null;
    public bool IsShellRendering { get; private set; }
    public McpComponentRequest Request => _request ?? throw new InvalidOperationException("No MCP component rendering operation is active in this scope.");
    public Uri BaseUri { get; private set; } = default!;
    public CancellationToken CancellationToken { get; private set; }
    public Uri? Redirect { get; set; }
    public bool ReplaceHistory { get; set; }

    public void Initialize(McpComponentRequest request, Uri baseUri, CancellationToken cancellationToken)
    {
        if (IsActive)
        {
            throw new InvalidOperationException("The rendering operation has already been initialized.");
        }

        _request = request;
        IsShellRendering = request.IsShellRendering;
        BaseUri = baseUri;
        CancellationToken = cancellationToken;
    }
}
