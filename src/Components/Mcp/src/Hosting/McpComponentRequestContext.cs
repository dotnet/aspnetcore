// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Components.Mcp;

/// <summary>
/// Describes the current logical MCP component rendering operation.
/// </summary>
public sealed class McpComponentRequestContext
{
    private readonly Hosting.McpOperationContext _context;

    internal McpComponentRequestContext(Hosting.McpOperationContext context) => _context = context;

    /// <summary>
    /// Gets whether the current scope belongs to an MCP component operation.
    /// </summary>
    public bool IsActive => _context.IsActive;

    /// <summary>
    /// Gets whether the application is rendering its resource document shell before tool data is available.
    /// </summary>
    /// <remarks>
    /// The phase is fixed for the operation scope. Defer data-dependent content in this phase,
    /// and declare the document's runtime script outside the conditional content.
    /// </remarks>
    public bool IsShellRendering => _context.IsShellRendering;

    /// <summary>
    /// Gets the logical application location being rendered.
    /// </summary>
    public Uri Location => GetCurrent().Request.Location;

    /// <summary>
    /// Gets the registered application base URI.
    /// </summary>
    public Uri BaseUri => GetCurrent().BaseUri;

    /// <summary>
    /// Gets the principal supplied by the current MCP request.
    /// </summary>
    public ClaimsPrincipal User => GetCurrent().Request.User;

    /// <summary>
    /// Gets the token that requests cooperative cancellation of the operation.
    /// </summary>
    public CancellationToken CancellationToken => GetCurrent().CancellationToken;

    private Hosting.McpOperationContext GetCurrent()
        => _context.IsActive ? _context : throw new InvalidOperationException("The current scope does not contain an MCP component operation.");
}
