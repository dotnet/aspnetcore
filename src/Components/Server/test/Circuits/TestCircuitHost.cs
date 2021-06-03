// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class TestCircuitHost : CircuitHost
    {
        private TestCircuitHost(CircuitId circuitId, AsyncServiceScope scope, CircuitOptions options, CircuitClientProxy client, RemoteRenderer renderer, IReadOnlyList<ComponentDescriptor> descriptors, RemoteJSRuntime jsRuntime, CircuitHandler[] circuitHandlers, ILogger logger)
            : base(circuitId, scope, options, client, renderer, descriptors, jsRuntime, circuitHandlers, logger)
        {
        }

        public static CircuitHost Create(
            CircuitId? circuitId = null,
            AsyncServiceScope? serviceScope = null,
            RemoteRenderer remoteRenderer = null,
            CircuitHandler[] handlers = null,
            CircuitClientProxy clientProxy = null)
        {
            serviceScope = serviceScope ?? new AsyncServiceScope(Mock.Of<IServiceScope>());
            clientProxy = clientProxy ?? new CircuitClientProxy(Mock.Of<IClientProxy>(), Guid.NewGuid().ToString());
            var jsRuntime = new RemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());

            if (remoteRenderer == null)
            {
                remoteRenderer = new RemoteRenderer(
                    serviceScope.Value.ServiceProvider ?? Mock.Of<IServiceProvider>(),
                    NullLoggerFactory.Instance,
                    new CircuitOptions(),
                    clientProxy,
                    NullLogger.Instance,
                    null);
            }

            handlers ??= Array.Empty<CircuitHandler>();
            return new TestCircuitHost(
                circuitId is null ? new CircuitId(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()) : circuitId.Value,
                serviceScope.Value,
                new CircuitOptions(),
                clientProxy,
                remoteRenderer,
                new List<ComponentDescriptor>(),
                jsRuntime,
                handlers,
                NullLogger<CircuitHost>.Instance);
        }
    }
}
