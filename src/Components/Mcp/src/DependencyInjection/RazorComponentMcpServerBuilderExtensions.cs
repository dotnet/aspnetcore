// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Mcp;
using Microsoft.AspNetCore.Components.Mcp.DependencyInjection;
using Microsoft.AspNetCore.Components.Mcp.Protocol;
using Microsoft.AspNetCore.Components.Mcp.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides methods for exposing Razor component applications through an MCP server.
/// </summary>
public static class RazorComponentMcpServerBuilderExtensions
{
    /// <summary>
    /// Adds a statically rendered Razor component application and its MCP Apps tools and resource.
    /// </summary>
    /// <typeparam name="TRootComponent">The application root component.</typeparam>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="configure">Configures the application name, base URI, and initial location.</param>
    /// <returns>The supplied MCP server builder.</returns>
    /// <remarks>
    /// Register the MCP application after other component hosting services, including
    /// <c>AddInteractiveServerComponents</c>, so that each rendering scope selects its host's services.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddMcpServer()
    ///     .WithRazorComponents&lt;App&gt;(options =&gt;
    ///     {
    ///         options.Name = "sample";
    ///         options.BaseUri = new Uri("https://example.com/");
    ///     })
    ///     .WithHttpTransport();
    /// </code>
    /// </example>
    [RequiresUnreferencedCode("Razor component rendering and form mapping do not support trimming.")]
    [RequiresDynamicCode("Razor component form mapping requires dynamic code.")]
    public static IMcpServerBuilder WithRazorComponents<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRootComponent>(
        this IMcpServerBuilder builder,
        Action<RazorComponentMcpOptions> configure) where TRootComponent : IComponent
        => AddApplication<TRootComponent>(builder, configure, openingToolName: null, initialParametersFactory: null);

    /// <summary>
    /// Associates an existing MCP SDK tool with a statically rendered Razor component application.
    /// </summary>
    /// <typeparam name="TRootComponent">The application root component.</typeparam>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="openingToolName">The name of a tool registered with the MCP SDK.</param>
    /// <param name="initialParametersFactory">Projects a successful opening tool result into serializable root component parameters.</param>
    /// <param name="configure">Configures the application and its browser runtime.</param>
    /// <returns>The supplied MCP server builder.</returns>
    /// <remarks>
    /// The SDK retains the tool's input schema, metadata, and invocation pipeline.
    /// The opening tool runs once; later form submissions reconstruct fresh parameter values
    /// without invoking it again. Error results do not invoke the parameter factory or render the application.
    /// </remarks>
    [RequiresUnreferencedCode("Razor component rendering and form mapping do not support trimming.")]
    [RequiresDynamicCode("Razor component form mapping requires dynamic code.")]
    public static IMcpServerBuilder WithRazorComponents<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRootComponent>(
        this IMcpServerBuilder builder,
        string openingToolName,
        Func<CallToolResult, IReadOnlyDictionary<string, object?>> initialParametersFactory,
        Action<RazorComponentMcpOptions> configure) where TRootComponent : IComponent
    {
        ArgumentException.ThrowIfNullOrEmpty(openingToolName);
        ArgumentNullException.ThrowIfNull(initialParametersFactory);
        return AddApplication<TRootComponent>(builder, configure, openingToolName, initialParametersFactory);
    }

    private static IMcpServerBuilder AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRootComponent>(
        IMcpServerBuilder builder,
        Action<RazorComponentMcpOptions> configure,
        string? openingToolName,
        Func<CallToolResult, IReadOnlyDictionary<string, object?>>? initialParametersFactory) where TRootComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        var options = new RazorComponentMcpOptions { Name = openingToolName ?? "blazor" };
        configure(options);
        ArgumentException.ThrowIfNullOrEmpty(options.Name);
        ArgumentNullException.ThrowIfNull(options.BaseUri);
        ArgumentNullException.ThrowIfNull(options.InitialPath);
        if (!Enum.IsDefined(options.LogLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(configure), "The MCP browser log level is invalid.");
        }

        if (!options.BaseUri.IsAbsoluteUri || !options.BaseUri.AbsoluteUri.EndsWith('/'))
        {
            throw new ArgumentException("The application base URI must be absolute and end with a slash.", nameof(configure));
        }

        var initialLocation = new Uri(options.BaseUri, options.InitialPath);
        if (!options.BaseUri.IsBaseOf(initialLocation))
        {
            throw new ArgumentException("The initial location must be within the application base URI.", nameof(configure));
        }

        var application = new McpComponentApplication(options.Name, typeof(TRootComponent), options.BaseUri, initialLocation,
            preserveDom: options.PreserveDom, logLevel: options.LogLevel, openingToolName, initialParametersFactory);
        if (builder.Services.Any(descriptor => descriptor.ImplementationInstance is McpComponentApplication other
            && (other.Name == application.Name || other.OpeningToolName == application.OpeningToolName)))
        {
            throw new InvalidOperationException($"A component application is already registered with the application or opening tool name '{application.Name}'.");
        }

        builder.Services.AddMcpComponentServices();
        builder.Services.AddSingleton(application);
        builder.Services.AddKeyedScoped<McpComponentInvoker>(application, (services, _) => new McpComponentInvoker(
            application, services.GetRequiredService<McpComponentHost>(), services.GetRequiredService<IServiceScopeFactory>()));
        if (openingToolName is null)
        {
            builder.Services.AddSingleton<McpServerTool>(services => new McpComponentTool(application, services.GetRequiredService<IServiceScopeFactory>(), dispatch: false));
        }

        builder.Services.AddSingleton<McpServerTool>(services => new McpComponentTool(application, services.GetRequiredService<IServiceScopeFactory>(), dispatch: true));
        builder.Services.AddSingleton<McpServerResource>(services => new McpComponentResource(application, services.GetRequiredService<IServiceScopeFactory>()));
        builder.Services.AddSingleton<IPostConfigureOptions<McpServerOptions>>(services => new McpComponentOptionsSetup(
            application, services.GetServices<McpServerTool>(), services.GetServices<McpServerResource>(),
            services.GetRequiredService<IServiceScopeFactory>()));
        builder.Services.Configure<McpServerOptions>(serverOptions =>
        {
            serverOptions.Capabilities ??= new();
            serverOptions.Capabilities.Extensions ??= new Dictionary<string, object>();
            serverOptions.Capabilities.Extensions.TryAdd("io.modelcontextprotocol/ui", new JsonObject());
        });
        return builder;
    }
}
