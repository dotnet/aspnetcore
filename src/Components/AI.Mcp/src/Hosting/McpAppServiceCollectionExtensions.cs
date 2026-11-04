// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Mcp;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers conversation-scoped MCP app hosting services.</summary>
public static class McpAppServiceCollectionExtensions
{
    /// <summary>Registers an app host for each dependency-injection scope.</summary>
    /// <remarks>
    /// Configure and bind the returned host before use. Applications with multiple independent conversations
    /// in one scope should create a separate <see cref="McpAppHost"/> for each conversation.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The conversation's retention and originating tool execution limits.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMcpAppHost(this IServiceCollection services, McpAppHostOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped(_ => new McpAppHost(options ?? new()));
        return services;
    }
}
