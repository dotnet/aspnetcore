// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.AI.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Maps standalone static MCP Apps sandbox documents.</summary>
public static class McpAppSandboxEndpointRouteBuilderExtensions
{
    /// <summary>Maps a sandbox component on an origin distinct from its parent host.</summary>
    /// <typeparam name="TComponent">The component that renders the sandbox document.</typeparam>
    /// <param name="endpoints">The application's endpoint builder.</param>
    /// <param name="pattern">The endpoint route pattern.</param>
    /// <param name="hostOrigin">The deployment address of the parent host.</param>
    /// <param name="sandboxOrigin">The deployment address serving the sandbox.</param>
    /// <param name="parameters">Additional component parameters, excluding the mapper-owned host origin.</param>
    /// <returns>A builder for applying endpoint conventions.</returns>
    public static RouteHandlerBuilder MapMcpAppSandbox<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        this IEndpointRouteBuilder endpoints, string pattern, Uri hostOrigin, Uri sandboxOrigin,
        IReadOnlyDictionary<string, object?>? parameters = null) where TComponent : McpSandboxBase
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        ArgumentNullException.ThrowIfNull(hostOrigin);
        ArgumentNullException.ThrowIfNull(sandboxOrigin);
        var host = GetOrigin(hostOrigin);
        var sandbox = GetOrigin(sandboxOrigin);
        if (host == sandbox)
        {
            throw new ArgumentException("The sandbox and host must have distinct origins.", nameof(sandboxOrigin));
        }
        var sandboxUri = new Uri(sandbox);
        var sandboxHost = HostString.FromUriComponent(sandboxUri).Host;
        var componentParameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parameters is not null)
        {
            foreach (var parameter in parameters)
            {
                if (string.Equals(parameter.Key, nameof(McpSandboxBase.HostOrigin), StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("HostOrigin is supplied by the sandbox endpoint.", nameof(parameters));
                }
                componentParameters.Add(parameter.Key, parameter.Value);
            }
        }
        componentParameters.Add(nameof(McpSandboxBase.HostOrigin), host);
        return endpoints.MapGet(pattern, (HttpContext context) =>
        {
            var request = context.Request;
            var port = request.Host.Port ?? (request.IsHttps ? 443 : 80);
            if (!string.Equals(request.Scheme, sandboxUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(request.Host.Host, sandboxHost, StringComparison.OrdinalIgnoreCase) ||
                port != sandboxUri.Port)
            {
                return (IResult)Results.NotFound();
            }
            context.Response.Headers.CacheControl = "no-store";
            return new RazorComponentResult<TComponent>(componentParameters)
            {
                ContentType = "text/html; charset=utf-8",
                PreventStreamingRendering = true
            };
        }).RequireHost(sandboxUri.IsDefaultPort ? sandboxHost : new HostString(sandboxHost, sandboxUri.Port).Value!);
    }

    private static string GetOrigin(Uri address) =>
        new UriBuilder(address) { Host = address.IdnHost, UserName = "", Password = "" }
            .Uri.GetLeftPart(UriPartial.Authority);
}
