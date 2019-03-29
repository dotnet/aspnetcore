// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Browser;
using Microsoft.AspNetCore.Components.Browser.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class TestCircuitHost : CircuitHost
    {
        private TestCircuitHost(IServiceScope scope, CircuitClientProxy client, RendererRegistry rendererRegistry, RemoteRenderer renderer, IList<ComponentDescriptor> descriptors, IDispatcher dispatcher, RemoteJSRuntime jsRuntime, CircuitHandler[] circuitHandlers, ILogger logger)
            : base(scope, client, rendererRegistry, renderer, descriptors, dispatcher, jsRuntime, circuitHandlers, logger)
        {
        }

        protected override void OnHandlerError(CircuitHandler circuitHandler, string handlerMethod, Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        public static CircuitHost Create(
            IServiceScope serviceScope = null,
            RemoteRenderer remoteRenderer = null,
            CircuitHandler[] handlers = null,
            CircuitClientProxy clientProxy = null)
        {
            serviceScope = serviceScope ?? Mock.Of<IServiceScope>();
            clientProxy = clientProxy ?? new CircuitClientProxy(Mock.Of<IClientProxy>(), Guid.NewGuid().ToString());
            var renderRegistry = new RendererRegistry();
            var jsRuntime = new RemoteJSRuntime();
            var dispatcher = Rendering.Renderer.CreateDefaultDispatcher();

            if (remoteRenderer == null)
            {
                remoteRenderer = new RemoteRenderer(
                    serviceScope.ServiceProvider ?? Mock.Of<IServiceProvider>(),
                    new RendererRegistry(),
                    jsRuntime,
                    clientProxy,
                    dispatcher,
                    HtmlEncoder.Default,
                    NullLogger.Instance);
            }

            handlers = handlers ?? Array.Empty<CircuitHandler>();
            return new TestCircuitHost(
                serviceScope,
                clientProxy,
                renderRegistry,
                remoteRenderer,
                new List<ComponentDescriptor>(),
                dispatcher,
                jsRuntime,
                handlers,
                NullLogger<CircuitHost>.Instance);
        }
    }
}
