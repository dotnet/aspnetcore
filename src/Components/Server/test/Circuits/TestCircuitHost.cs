// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Web.Rendering;
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
        private TestCircuitHost(string circuitId, IServiceScope scope, CircuitOptions options, CircuitClientProxy client, RemoteRenderer renderer, IReadOnlyList<ComponentDescriptor> descriptors, RemoteJSRuntime jsRuntime, CircuitHandler[] circuitHandlers, ILogger logger)
            : base(circuitId, scope, options, client, renderer, descriptors, jsRuntime, circuitHandlers, logger)
        {
        }

        public static CircuitHost Create(
            string circuitId = null,
            IServiceScope serviceScope = null,
            RemoteRenderer remoteRenderer = null,
            CircuitHandler[] handlers = null,
            CircuitClientProxy clientProxy = null)
        {
            serviceScope = serviceScope ?? Mock.Of<IServiceScope>();
            clientProxy = clientProxy ?? new CircuitClientProxy(Mock.Of<IClientProxy>(), Guid.NewGuid().ToString());
            var jsRuntime = new RemoteJSRuntime(Options.Create(new CircuitOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());

            if (remoteRenderer == null)
            {
                remoteRenderer = new RemoteRenderer(
                    serviceScope.ServiceProvider ?? Mock.Of<IServiceProvider>(),
                    NullLoggerFactory.Instance,
                    new CircuitOptions(),
                    jsRuntime,
                    clientProxy,
                    HtmlEncoder.Default,
                    NullLogger.Instance);
            }

            handlers = handlers ?? Array.Empty<CircuitHandler>();
            return new TestCircuitHost(
                circuitId ?? Guid.NewGuid().ToString(),
                serviceScope,
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
