// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Browser;
using Microsoft.AspNetCore.Components.Browser.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitHost : IAsyncDisposable
    {
        private static readonly AsyncLocal<CircuitHost> _current = new AsyncLocal<CircuitHost>();
        private readonly SemaphoreSlim HandlerLock = new SemaphoreSlim(1);
        private readonly IServiceScope _scope;
        private readonly CircuitHandler[] _circuitHandlers;
        private readonly ILogger _logger;
        private bool _initialized;

        /// <summary>
        /// Gets the current <see cref="Circuit"/>, if any.
        /// </summary>
        public static CircuitHost Current => _current.Value;

        /// <summary>
        /// Sets the current <see cref="Circuits.Circuit"/>.
        /// </summary>
        /// <param name="circuitHost">The <see cref="Circuits.Circuit"/>.</param>
        /// <remarks>
        /// Calling <see cref="SetCurrentCircuitHost(CircuitHost)"/> will store the circuit
        /// and other related values such as the <see cref="IJSRuntime"/> and <see cref="Renderer"/>
        /// in the local execution context. Application code should not need to call this method,
        /// it is primarily used by the Server-Side Components infrastructure.
        /// </remarks>
        public static void SetCurrentCircuitHost(CircuitHost circuitHost)
        {
            _current.Value = circuitHost ?? throw new ArgumentNullException(nameof(circuitHost));

            JSInterop.JSRuntime.SetCurrentJSRuntime(circuitHost.JSRuntime);
            RendererRegistry.SetCurrentRendererRegistry(circuitHost.RendererRegistry);
        }

        public event UnhandledExceptionEventHandler UnhandledException;

        public CircuitHost(
            IServiceScope scope,
            CircuitClientProxy client,
            RendererRegistry rendererRegistry,
            RemoteRenderer renderer,
            IList<ComponentDescriptor> descriptors,
            IDispatcher dispatcher,
            RemoteJSRuntime jsRuntime,
            CircuitHandler[] circuitHandlers,
            ILogger logger)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Dispatcher = dispatcher;
            Client = client;
            RendererRegistry = rendererRegistry ?? throw new ArgumentNullException(nameof(rendererRegistry));
            Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _logger = logger;

            Services = scope.ServiceProvider;

            Circuit = new Circuit(this);
            _circuitHandlers = circuitHandlers;

            Renderer.UnhandledException += Renderer_UnhandledException;
            Renderer.UnhandledSynchronizationException += SynchronizationContext_UnhandledException;
        }

        public string CircuitId { get; } = Guid.NewGuid().ToString();

        public Circuit Circuit { get; }

        public CircuitClientProxy Client { get; set; }

        public RemoteJSRuntime JSRuntime { get; }

        public RemoteRenderer Renderer { get; }

        public RendererRegistry RendererRegistry { get; }

        public IList<ComponentDescriptor> Descriptors { get; }

        public IServiceProvider Services { get; }

        public IDispatcher Dispatcher { get; }

        public Task<IEnumerable<string>> PrerenderComponentAsync(Type componentType, ParameterCollection parameters)
        {
            return Dispatcher.InvokeAsync(async () =>
            {
                var result = await Renderer.RenderComponentAsync(componentType, parameters);
                return result;
            });
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await Renderer.InvokeAsync(async () =>
            {
                try
                {
                    SetCurrentCircuitHost(this);
                    _initialized = true; // We're ready to accept incoming JSInterop calls from here on

                    await OnCircuitOpenedAsync(cancellationToken);
                    await OnConnectionUpAsync(cancellationToken);

                    // We add the root components *after* the circuit is flagged as open.
                    // That's because AddComponentAsync waits for quiescence, which can take
                    // arbitrarily long. In the meantime we might need to be receiving and
                    // processing incoming JSInterop calls or similar.
                    for (var i = 0; i < Descriptors.Count; i++)
                    {
                        var (componentType, domElementSelector) = Descriptors[i];
                        await Renderer.AddComponentAsync(componentType, domElementSelector);
                    }
                }
                catch (Exception ex)
                {
                    // We have to handle all our own errors here, because the upstream caller
                    // has to fire-and-forget this
                    Renderer_UnhandledException(this, ex);
                }
            });
        }

        public async void BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            AssertInitialized();

            try
            {
                await Renderer.Invoke(() =>
                {
                    SetCurrentCircuitHost(this);
                    DotNetDispatcher.BeginInvoke(callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson);
                });
            }
            catch (Exception ex)
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            }
        }

        private async Task OnCircuitOpenedAsync(CancellationToken cancellationToken)
        {
            for (var i = 0; i < _circuitHandlers.Length; i++)
            {
                var circuitHandler = _circuitHandlers[i];
                try
                {
                    await circuitHandler.OnCircuitOpenedAsync(Circuit, cancellationToken);
                }
                catch (Exception ex)
                {
                    OnHandlerError(circuitHandler, nameof(CircuitHandler.OnCircuitOpenedAsync), ex);
                }
            }
        }

        public async Task OnConnectionUpAsync(CancellationToken cancellationToken)
        {
            try
            {
                await HandlerLock.WaitAsync(cancellationToken);

                for (var i = 0; i < _circuitHandlers.Length; i++)
                {
                    var circuitHandler = _circuitHandlers[i];
                    try
                    {
                        await circuitHandler.OnConnectionUpAsync(Circuit, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        OnHandlerError(circuitHandler, nameof(CircuitHandler.OnConnectionUpAsync), ex);
                    }
                }
            }
            finally
            {
                HandlerLock.Release();
            }
        }

        public async Task OnConnectionDownAsync(CancellationToken cancellationToken)
        {
            try
            {
                await HandlerLock.WaitAsync(cancellationToken);

                for (var i = 0; i < _circuitHandlers.Length; i++)
                {
                    var circuitHandler = _circuitHandlers[i];
                    try
                    {
                        await circuitHandler.OnConnectionDownAsync(Circuit, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        OnHandlerError(circuitHandler, nameof(CircuitHandler.OnConnectionDownAsync), ex);
                    }
                }
            }
            finally
            {
                HandlerLock.Release();
            }
        }

        protected virtual void OnHandlerError(CircuitHandler circuitHandler, string handlerMethod, Exception ex)
        {
            Log.UnhandledExceptionInvokingCircuitHandler(_logger, circuitHandler, handlerMethod, ex);
        }

        private async Task OnCircuitDownAsync()
        {
            for (var i = 0; i < _circuitHandlers.Length; i++)
            {
                var circuitHandler = _circuitHandlers[i];
                try
                {
                    await circuitHandler.OnCircuitClosedAsync(Circuit, default);
                }
                catch (Exception ex)
                {
                    OnHandlerError(circuitHandler, nameof(CircuitHandler.OnCircuitClosedAsync), ex);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Log.DisposingCircuit(_logger, CircuitId);

            await Renderer.InvokeAsync((Func<Task>)(async () =>
            {
                await OnConnectionDownAsync(CancellationToken.None);
                await OnCircuitDownAsync();
            }));

            _scope.Dispose();
            Renderer.Dispose();
        }

        private void AssertInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Circuit is being invoked prior to initialization.");
            }
        }

        private void Renderer_UnhandledException(object sender, Exception e)
        {
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, isTerminating: false));
        }

        private void SynchronizationContext_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException?.Invoke(this, e);
        }

        private static class Log
        {
            private static readonly Action<ILogger, Type, string, string, Exception> _unhandledExceptionInvokingCircuitHandler;
            private static readonly Action<ILogger, string, Exception> _disposingCircuit;

            private static class EventIds
            {
                public static readonly EventId ExceptionInvokingCircuitHandlerMethod = new EventId(100, "ExceptionInvokingCircuitHandlerMethod");
                public static readonly EventId DisposingCircuit = new EventId(101, "DisposingCircuitHost");
            }

            static Log()
            {
                _unhandledExceptionInvokingCircuitHandler = LoggerMessage.Define<Type, string, string>(
                    LogLevel.Error,
                    EventIds.ExceptionInvokingCircuitHandlerMethod,
                    "Unhandled error invoking circuit handler type {handlerType}.{handlerMethod}: {Message}");

                _disposingCircuit = LoggerMessage.Define<string>(
                    LogLevel.Trace,
                    EventIds.DisposingCircuit,
                    "Disposing circuit with identifier {CircuitId}");
            }

            public static void UnhandledExceptionInvokingCircuitHandler(ILogger logger, CircuitHandler handler, string handlerMethod, Exception exception)
            {
                _unhandledExceptionInvokingCircuitHandler(
                    logger,
                    handler.GetType(),
                    handlerMethod,
                    exception.Message,
                    exception);
            }

            public static void DisposingCircuit(ILogger logger, string circuitId) => _disposingCircuit(logger, circuitId, null);
        }
    }
}
