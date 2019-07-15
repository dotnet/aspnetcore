// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Rendering;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Text.Json;
using Microsoft.JSInterop.Internal;
using System.Reflection;

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
            string circuitId,
            IServiceScope scope,
            CircuitClientProxy client,
            RendererRegistry rendererRegistry,
            RemoteRenderer renderer,
            IList<ComponentDescriptor> descriptors,
            RemoteJSRuntime jsRuntime,
            CircuitHandler[] circuitHandlers,
            ILogger logger)
        {
            CircuitId = circuitId;
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
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

        public string CircuitId { get; }

        public Circuit Circuit { get; }

        public CircuitClientProxy Client { get; set; }

        public RemoteJSRuntime JSRuntime { get; }

        public RemoteRenderer Renderer { get; }

        public RendererRegistry RendererRegistry { get; }

        public IList<ComponentDescriptor> Descriptors { get; }

        public IServiceProvider Services { get; }

        public Task<ComponentRenderedText> PrerenderComponentAsync(Type componentType, ParameterCollection parameters)
        {
            return Renderer.Dispatcher.InvokeAsync(async () =>
            {
                var result = await Renderer.RenderComponentAsync(componentType, parameters);

                // When we prerender we start the circuit in a disconnected state. As such, we only call
                // OnCircuitOpenenedAsync here and when the client reconnects we run OnConnectionUpAsync
                await OnCircuitOpenedAsync(CancellationToken.None);

                return result;
            });
        }

        internal void InitializeCircuitAfterPrerender(UnhandledExceptionEventHandler unhandledException)
        {
            if (!_initialized)
            {
                _initialized = true;
                UnhandledException += unhandledException;
                var uriHelper = (RemoteUriHelper)Services.GetRequiredService<IUriHelper>();
                if (!uriHelper.HasAttachedJSRuntime)
                {
                    uriHelper.AttachJsRuntime(JSRuntime);
                }

                var navigationInterception = (RemoteNavigationInterception)Services.GetRequiredService<INavigationInterception>();
                if (!navigationInterception.HasAttachedJSRuntime)
                {
                    navigationInterception.AttachJSRuntime(JSRuntime);
                }
            }
        }

        internal void SendPendingBatches()
        {
            // Dispatch any buffered renders we accumulated during a disconnect.
            // Note that while the rendering is async, we cannot await it here. The Task returned by ProcessBufferedRenderBatches relies on
            // OnRenderCompleted to be invoked to complete, and SignalR does not allow concurrent hub method invocations.
            var _ = Renderer.Dispatcher.InvokeAsync(() => Renderer.ProcessBufferedRenderBatches());
        }

        internal async Task EndInvokeDotNetFromJS(string arguments)
        {
            try
            {
                AssertInitialized();

                await Renderer.Dispatcher.InvokeAsync(() =>
                {
                    SetCurrentCircuitHost(this);
                    var document = JsonDocument.Parse(arguments);
                    if (document.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        // Log error and return
                        return;
                    }
                    var length = document.RootElement.GetArrayLength();
                    if (length != 3)
                    {
                        // Log error and return
                        return;
                    }
                    long asyncHandle = 0;
                    bool succeeded = false;
                    JSAsyncCallResult result = null;
                    var i = 0;
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (i == 0)
                        {
                            asyncHandle = element.GetInt64();
                        }
                        if (i == 1)
                        {
                            succeeded = element.GetBoolean();
                        }

                        if (i == 2 && element.ValueKind != JsonValueKind.Null)
                        {
                            result = (JSAsyncCallResult)typeof(JSAsyncCallResult).GetConstructor(
                                BindingFlags.NonPublic | BindingFlags.Instance,
                                binder: null,
                                new Type[] { typeof(JsonDocument), typeof(JsonElement) },
                                null).Invoke(new object[] { document, element });
                        }

                        i++;
                    }

                    DotNetDispatcher.EndInvoke(asyncHandle, succeeded, result);
                });
            }
            catch (Exception ex)
            {
                var args = JsonSerializer.Serialize(
                    new object[]
                    {
                        0,
                        false,
                        JSRuntime.SanitizeJSInteropException(ex, typeof(JSRuntimeBase).Assembly.GetName().Name, nameof(DotNetDispatcher.EndInvoke))
                    },
                    JsonSerializerOptionsProvider.Options);
                var _ = JSRuntime.InvokeAsync<object>("DotNet.jsCallDispatcher.endInvokeDotNetFromJS", args);
            }
        }

        internal async Task DispatchEvent(RendererRegistryEventDispatcher.BrowserEventDescriptor eventDescriptor, string eventArgsJson)
        {
            try
            {
                AssertInitialized();
                await Renderer.Dispatcher.InvokeAsync(() =>
                {
                    SetCurrentCircuitHost(this);
                    return RendererRegistryEventDispatcher.DispatchEvent(eventDescriptor, eventArgsJson);
                });
            }
            catch (Exception ex)
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await Renderer.Dispatcher.InvokeAsync(async () =>
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
                        var (componentType, domElementSelector, prerendered) = Descriptors[i];
                        if (!prerendered)
                        {
                            await Renderer.AddComponentAsync(componentType, domElementSelector);
                        }
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

        public async Task BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            try
            {
                AssertInitialized();

                await Renderer.Dispatcher.InvokeAsync(() =>
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
            Log.CircuitOpened(_logger, Circuit.Id);

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
            Log.ConnectionUp(_logger, Circuit.Id, Client.ConnectionId);

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
            Log.ConnectionDown(_logger, Circuit.Id, Client.ConnectionId);

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
            Log.CircuitClosed(_logger, Circuit.Id);

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

            await Renderer.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await OnConnectionDownAsync(CancellationToken.None);
                    await OnCircuitDownAsync();
                }
                finally
                {
                    Renderer.Dispose();
                    _scope.Dispose();
                }
            });
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
            private static readonly Action<ILogger, string, Exception> _onCircuitOpened;
            private static readonly Action<ILogger, string, string, Exception> _onConnectionUp;
            private static readonly Action<ILogger, string, string, Exception> _onConnectionDown;
            private static readonly Action<ILogger, string, Exception> _onCircuitClosed;

            private static class EventIds
            {
                public static readonly EventId ExceptionInvokingCircuitHandlerMethod = new EventId(100, "ExceptionInvokingCircuitHandlerMethod");
                public static readonly EventId DisposingCircuit = new EventId(101, "DisposingCircuitHost");
                public static readonly EventId OnCircuitOpened = new EventId(102, "OnCircuitOpened");
                public static readonly EventId OnConnectionUp = new EventId(103, "OnConnectionUp");
                public static readonly EventId OnConnectionDown = new EventId(104, "OnConnectionDown");
                public static readonly EventId OnCircuitClosed = new EventId(105, "OnCircuitClosed");
            }

            static Log()
            {
                _unhandledExceptionInvokingCircuitHandler = LoggerMessage.Define<Type, string, string>(
                    LogLevel.Error,
                    EventIds.ExceptionInvokingCircuitHandlerMethod,
                    "Unhandled error invoking circuit handler type {handlerType}.{handlerMethod}: {Message}");

                _disposingCircuit = LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    EventIds.DisposingCircuit,
                    "Disposing circuit with identifier {CircuitId}");

                _onCircuitOpened = LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    EventIds.OnCircuitOpened,
                    "Opening circuit with id {CircuitId}.");

                _onConnectionUp = LoggerMessage.Define<string, string>(
                    LogLevel.Debug,
                    EventIds.OnConnectionUp,
                    "Circuit id {CircuitId} connected using connection {ConnectionId}.");

                _onConnectionDown = LoggerMessage.Define<string, string>(
                    LogLevel.Debug,
                    EventIds.OnConnectionDown,
                    "Circuit id {CircuitId} disconnected from connection {ConnectionId}.");

                _onCircuitClosed = LoggerMessage.Define<string>(
                   LogLevel.Debug,
                   EventIds.OnCircuitClosed,
                   "Closing circuit with id {CircuitId}.");
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

            public static void CircuitOpened(ILogger logger, string circuitId) => _onCircuitOpened(logger, circuitId, null);

            public static void ConnectionUp(ILogger logger, string circuitId, string connectionId) =>
                _onConnectionUp(logger, circuitId, connectionId, null);

            public static void ConnectionDown(ILogger logger, string circuitId, string connectionId) =>
                _onConnectionDown(logger, circuitId, connectionId, null);

            public static void CircuitClosed(ILogger logger, string circuitId) =>
                _onCircuitClosed(logger, circuitId, null);
        }
    }
}
