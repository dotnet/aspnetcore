// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed partial class CircuitFactory : ICircuitFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CircuitIdFactory _circuitIdFactory;
    private readonly CircuitOptions _options;
    private readonly ILogger _logger;

    public CircuitFactory(
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        CircuitIdFactory circuitIdFactory,
        IOptions<CircuitOptions> options)
    {
        _scopeFactory = scopeFactory;
        _loggerFactory = loggerFactory;
        _circuitIdFactory = circuitIdFactory;
        _options = options.Value;
        _logger = _loggerFactory.CreateLogger<CircuitFactory>();
    }

    public async ValueTask<CircuitHost> CreateCircuitHostAsync(
        IReadOnlyList<ComponentDescriptor> components,
        CircuitClientProxy client,
        string baseUri,
        string uri,
        ClaimsPrincipal user,
        IPersistentComponentStateStore store)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var jsRuntime = (RemoteJSRuntime)scope.ServiceProvider.GetRequiredService<IJSRuntime>();
        jsRuntime.Initialize(client);

        var navigationManager = (RemoteNavigationManager)scope.ServiceProvider.GetRequiredService<NavigationManager>();
        var navigationInterception = (RemoteNavigationInterception)scope.ServiceProvider.GetRequiredService<INavigationInterception>();
        if (client.Connected)
        {
            navigationManager.AttachJsRuntime(jsRuntime);
            navigationManager.Initialize(baseUri, uri);

            navigationInterception.AttachJSRuntime(jsRuntime);
        }
        else
        {
            navigationManager.Initialize(baseUri, uri);
        }

        var appLifetime = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
        await appLifetime.RestoreStateAsync(store);

        var jsComponentInterop = new CircuitJSComponentInterop(_options);
        var renderer = new RemoteRenderer(
            scope.ServiceProvider,
            _loggerFactory,
            _options,
            client,
            _loggerFactory.CreateLogger<RemoteRenderer>(),
            jsRuntime,
            jsComponentInterop);

        var circuitHandlers = scope.ServiceProvider.GetServices<CircuitHandler>()
            .OrderBy(h => h.Order)
            .ToArray();

        var circuitHost = new CircuitHost(
            _circuitIdFactory.CreateCircuitId(),
            scope,
            _options,
            client,
            renderer,
            components,
            jsRuntime,
            navigationManager,
            circuitHandlers,
            _loggerFactory.CreateLogger<CircuitHost>());
        Log.CreatedCircuit(_logger, circuitHost);

        // Initialize per - circuit data that services need
        (circuitHost.Services.GetRequiredService<ICircuitAccessor>() as DefaultCircuitAccessor).Circuit = circuitHost.Circuit;
        circuitHost.SetCircuitUser(user);

        return circuitHost;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Created circuit {CircuitId} for connection {ConnectionId}", EventName = "CreatedCircuit")]
        private static partial void CreatedCircuit(ILogger logger, string circuitId, string connectionId);

        internal static void CreatedCircuit(ILogger logger, CircuitHost circuitHost) =>
            CreatedCircuit(logger, circuitHost.CircuitId.Id, circuitHost.Client.ConnectionId);
    }
}
