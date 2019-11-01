// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitHost : IAsyncDisposable
    {
        private readonly IServiceScope _scope;
        private readonly CircuitOptions _options;
        private readonly CircuitHandler[] _circuitHandlers;
        private readonly ILogger _logger;
        private bool _initialized;
        private bool _disposed;

        // This event is fired when there's an unrecoverable exception coming from the circuit, and
        // it need so be torn down. The registry listens to this even so that the circuit can
        // be torn down even when a client is not connected.
        //
        // We don't expect the registry to do anything with the exception. We only provide it here
        // for testability.
        public event UnhandledExceptionEventHandler UnhandledException;

        public CircuitHost(
            CircuitId circuitId,
            IServiceScope scope,
            CircuitOptions options,
            CircuitClientProxy client,
            RemoteRenderer renderer,
            IReadOnlyList<ComponentDescriptor> descriptors,
            RemoteJSRuntime jsRuntime,
            CircuitHandler[] circuitHandlers,
            ILogger logger)
        {
            CircuitId = circuitId;
            if (CircuitId.Secret is null)
            {
                // Prevent the use of a 'default' secret.
                throw new ArgumentException(nameof(circuitId));
            }

            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
            JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _circuitHandlers = circuitHandlers ?? throw new ArgumentNullException(nameof(circuitHandlers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Services = scope.ServiceProvider;

            Circuit = new Circuit(this);
            Handle = new CircuitHandle() { CircuitHost = this, };

            Renderer.UnhandledException += Renderer_UnhandledException;
            Renderer.UnhandledSynchronizationException += SynchronizationContext_UnhandledException;
        }

        public CircuitHandle Handle { get; }

        public CircuitId CircuitId { get; }

        public Circuit Circuit { get; }

        public CircuitClientProxy Client { get; set; }

        public RemoteJSRuntime JSRuntime { get; }

        public RemoteRenderer Renderer { get; }

        public IReadOnlyList<ComponentDescriptor> Descriptors { get; }

        public IServiceProvider Services { get; }

        // InitializeAsync is used in a fire-and-forget context, so it's responsible for its own
        // error handling.
        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            Log.InitializationStarted(_logger);

            return Renderer.Dispatcher.InvokeAsync(async () =>
            {
                if (_initialized)
                {
                    throw new InvalidOperationException("The circuit host is already initialized.");
                }

                try
                {
                    _initialized = true; // We're ready to accept incoming JSInterop calls from here on

                    await OnCircuitOpenedAsync(cancellationToken);
                    await OnConnectionUpAsync(cancellationToken);

                    // We add the root components *after* the circuit is flagged as open.
                    // That's because AddComponentAsync waits for quiescence, which can take
                    // arbitrarily long. In the meantime we might need to be receiving and
                    // processing incoming JSInterop calls or similar.
                    var count = Descriptors.Count;
                    for (var i = 0; i < count; i++)
                    {
                        var (componentType, parameters, sequence) = Descriptors[i];
                        await Renderer.AddComponentAsync(componentType, parameters, sequence.ToString());
                    }

                    Log.InitializationSucceeded(_logger);
                }
                catch (Exception ex)
                {
                    // Report errors asynchronously. InitializeAsync is designed not to throw.
                    Log.InitializationFailed(_logger, ex);
                    UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
                    await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex), ex);
                }
            });
        }

        // We handle errors in DisposeAsync because there's no real value in letting it propagate.
        // We run user code here (CircuitHandlers) and it's reasonable to expect some might throw, however,
        // there isn't anything better to do than log when one of these exceptions happens - because the
        // client is already gone.
        public async ValueTask DisposeAsync()
        {
            Log.DisposeStarted(_logger, CircuitId);

            await Renderer.Dispatcher.InvokeAsync(async () =>
            {
                if (_disposed)
                {
                    return;
                }

                // Make sure that no hub or connection can refer to this circuit anymore now that it's shutting down.
                Handle.CircuitHost = null;
                _disposed = true;

                try
                {
                    await OnConnectionDownAsync(CancellationToken.None);
                }
                catch
                {
                    // Individual exceptions logged as part of OnConnectionDownAsync - nothing to do here
                    // since we're already shutting down.
                }

                try
                {
                    await OnCircuitDownAsync(CancellationToken.None);
                }
                catch
                {
                    // Individual exceptions logged as part of OnCircuitDownAsync - nothing to do here
                    // since we're already shutting down.
                }

                try
                {
                    Renderer.Dispose();

                    // This cast is needed because it's possible the scope may not support async dispose.
                    // Our DI container does, but other DI systems may not.
                    if (_scope is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else
                    {
                        _scope.Dispose();
                    }

                    Log.DisposeSucceeded(_logger, CircuitId);
                }
                catch (Exception ex)
                {
                    Log.DisposeFailed(_logger, CircuitId, ex);
                }
            });
        }

        // Note: we log exceptions and re-throw while running handlers, because there may be multiple
        // exceptions.
        private async Task OnCircuitOpenedAsync(CancellationToken cancellationToken)
        {
            Log.CircuitOpened(_logger, CircuitId);

            Renderer.Dispatcher.AssertAccess();

            List<Exception> exceptions = null;

            for (var i = 0; i < _circuitHandlers.Length; i++)
            {
                var circuitHandler = _circuitHandlers[i];
                try
                {
                    await circuitHandler.OnCircuitOpenedAsync(Circuit, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.CircuitHandlerFailed(_logger, circuitHandler, nameof(CircuitHandler.OnCircuitOpenedAsync), ex);
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("Encountered exceptions while executing circuit handlers.", exceptions);
            }
        }

        public async Task OnConnectionUpAsync(CancellationToken cancellationToken)
        {
            Log.ConnectionUp(_logger, CircuitId, Client.ConnectionId);

            Renderer.Dispatcher.AssertAccess();

            List<Exception> exceptions = null;

            for (var i = 0; i < _circuitHandlers.Length; i++)
            {
                var circuitHandler = _circuitHandlers[i];
                try
                {
                    await circuitHandler.OnConnectionUpAsync(Circuit, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.CircuitHandlerFailed(_logger, circuitHandler, nameof(CircuitHandler.OnConnectionUpAsync), ex);
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("Encountered exceptions while executing circuit handlers.", exceptions);
            }
        }

        public async Task OnConnectionDownAsync(CancellationToken cancellationToken)
        {
            Log.ConnectionDown(_logger, CircuitId, Client.ConnectionId);

            Renderer.Dispatcher.AssertAccess();

            List<Exception> exceptions = null;

            for (var i = 0; i < _circuitHandlers.Length; i++)
            {
                var circuitHandler = _circuitHandlers[i];
                try
                {
                    await circuitHandler.OnConnectionDownAsync(Circuit, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.CircuitHandlerFailed(_logger, circuitHandler, nameof(CircuitHandler.OnConnectionDownAsync), ex);
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("Encountered exceptions while executing circuit handlers.", exceptions);
            }
        }

        private async Task OnCircuitDownAsync(CancellationToken cancellationToken)
        {
            Log.CircuitClosed(_logger, CircuitId);

            List<Exception> exceptions = null;

            for (var i = 0; i < _circuitHandlers.Length; i++)
            {
                var circuitHandler = _circuitHandlers[i];
                try
                {
                    await circuitHandler.OnCircuitClosedAsync(Circuit, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.CircuitHandlerFailed(_logger, circuitHandler, nameof(CircuitHandler.OnCircuitClosedAsync), ex);
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("Encountered exceptions while executing circuit handlers.", exceptions);
            }
        }

        // Called by the client when it completes rendering a batch.
        // OnRenderCompletedAsync is used in a fire-and-forget context, so it's responsible for its own
        // error handling.
        public async Task OnRenderCompletedAsync(long renderId, string errorMessageOrNull)
        {
            AssertInitialized();
            AssertNotDisposed();

            try
            {
                _ = Renderer.OnRenderCompletedAsync(renderId, errorMessageOrNull);
            }
            catch (Exception e)
            {
                // Captures sync exceptions when invoking OnRenderCompletedAsync.
                // An exception might be throw synchronously when we receive an ack for a batch we never produced.
                Log.OnRenderCompletedFailed(_logger, renderId, CircuitId, e);
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(e, $"Failed to complete render batch '{renderId}'."));
                UnhandledException(this, new UnhandledExceptionEventArgs(e, isTerminating: false));
            }
        }

        // BeginInvokeDotNetFromJS is used in a fire-and-forget context, so it's responsible for its own
        // error handling.
        public async Task BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            AssertInitialized();
            AssertNotDisposed();

            try
            {
                await Renderer.Dispatcher.InvokeAsync(() =>
                {
                    Log.BeginInvokeDotNet(_logger, callId, assemblyName, methodIdentifier, dotNetObjectId);
                    var invocationInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId);
                    DotNetDispatcher.BeginInvokeDotNet(JSRuntime, invocationInfo, argsJson);
                });
            }
            catch (Exception ex)
            {
                // We don't expect any of this code to actually throw, because DotNetDispatcher.BeginInvoke doesn't throw
                // however, we still want this to get logged if we do.
                Log.BeginInvokeDotNetFailed(_logger, callId, assemblyName, methodIdentifier, dotNetObjectId, ex);
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Interop call failed."));
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            }
        }

        // EndInvokeJSFromDotNet is used in a fire-and-forget context, so it's responsible for its own
        // error handling.
        public async Task EndInvokeJSFromDotNet(long asyncCall, bool succeeded, string arguments)
        {
            AssertInitialized();
            AssertNotDisposed();

            try
            {
                await Renderer.Dispatcher.InvokeAsync(() =>
                {
                    if (!succeeded)
                    {
                        // We can log the arguments here because it is simply the JS error with the call stack.
                        Log.EndInvokeJSFailed(_logger, asyncCall, arguments);
                    }
                    else
                    {
                        Log.EndInvokeJSSucceeded(_logger, asyncCall);
                    }

                    DotNetDispatcher.EndInvokeJS(JSRuntime, arguments);
                });
            }
            catch (Exception ex)
            {
                // An error completing JS interop means that the user sent invalid data, a well-behaved
                // client won't do this.
                Log.EndInvokeDispatchException(_logger, ex);
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Invalid interop arguments."));
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            }
        }

        // DispatchEvent is used in a fire-and-forget context, so it's responsible for its own
        // error handling.
        public async Task DispatchEvent(string eventDescriptorJson, string eventArgsJson)
        {
            AssertInitialized();
            AssertNotDisposed();

            WebEventData webEventData;
            try
            {
                webEventData = WebEventData.Parse(eventDescriptorJson, eventArgsJson);
            }
            catch (Exception ex)
            {
                // Invalid event data is fatal. We expect a well-behaved client to send valid JSON.
                Log.DispatchEventFailedToParseEventData(_logger, ex);
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Bad input data."));
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
                return;
            }

            try
            {
                await Renderer.Dispatcher.InvokeAsync(() =>
                {
                    return Renderer.DispatchEventAsync(
                        webEventData.EventHandlerId,
                        webEventData.EventFieldInfo,
                        webEventData.EventArgs);
                });
            }
            catch (Exception ex)
            {
                // A failure in dispatching an event means that it was an attempt to use an invalid event id.
                // A well-behaved client won't do this.
                Log.DispatchEventFailedToDispatchEvent(_logger, webEventData.EventHandlerId.ToString(), ex);
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Failed to dispatch event."));
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            }
        }

        // OnLocationChangedAsync is used in a fire-and-forget context, so it's responsible for its own
        // error handling.
        public async Task OnLocationChangedAsync(string uri, bool intercepted)
        {
            AssertInitialized();
            AssertNotDisposed();

            try
            {
                await Renderer.Dispatcher.InvokeAsync(() =>
                {
                    Log.LocationChange(_logger, uri, CircuitId);
                    var navigationManager = (RemoteNavigationManager)Services.GetRequiredService<NavigationManager>();
                    navigationManager.NotifyLocationChanged(uri, intercepted);
                    Log.LocationChangeSucceeded(_logger, uri, CircuitId);
                });
            }

            // It's up to the NavigationManager implementation to validate the URI.
            //
            // Note that it's also possible that setting the URI could cause a failure in code that listens
            // to NavigationManager.LocationChanged.
            //
            // In either case, a well-behaved client will not send invalid URIs, and we don't really
            // want to continue processing with the circuit if setting the URI failed inside application
            // code. The safest thing to do is consider it a critical failure since URI is global state,
            // and a failure means that an update to global state was partially applied.
            catch (LocationChangeException nex)
            {
                // LocationChangeException means that it failed in user-code. Treat this like an unhandled
                // exception in user-code.
                Log.LocationChangeFailedInCircuit(_logger, uri, CircuitId, nex);
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(nex, "Location change failed."));
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(nex, isTerminating: false));
            }
            catch (Exception ex)
            {
                // Any other exception means that it failed validation, or inside the NavigationManager. Treat
                // this like bad data.
                Log.LocationChangeFailed(_logger, uri, CircuitId, ex);
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, $"Location change to '{uri}' failed."));
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            }
        }

        public void SetCircuitUser(ClaimsPrincipal user)
        {
            // This can be called before the circuit is initialized.
            AssertNotDisposed();

            var authenticationStateProvider = Services.GetService<AuthenticationStateProvider>() as IHostEnvironmentAuthenticationStateProvider;
            if (authenticationStateProvider != null)
            {
                var authenticationState = new AuthenticationState(user);
                authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
            }
        }

        public void SendPendingBatches()
        {
            AssertInitialized();
            AssertNotDisposed();

            // Dispatch any buffered renders we accumulated during a disconnect.
            // Note that while the rendering is async, we cannot await it here. The Task returned by ProcessBufferedRenderBatches relies on
            // OnRenderCompletedAsync to be invoked to complete, and SignalR does not allow concurrent hub method invocations.
            _ = Renderer.Dispatcher.InvokeAsync(() => Renderer.ProcessBufferedRenderBatches());
        }

        private void AssertInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Circuit is being invoked prior to initialization.");
            }
        }

        private void AssertNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(objectName: null);
            }
        }

        // An unhandled exception from the renderer is always fatal because it came from user code.
        // We want to notify the client if it's still connected, and then tear-down the circuit.
        private async void Renderer_UnhandledException(object sender, Exception e)
        {
            await ReportUnhandledException(e);
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, isTerminating: false));
        }

        // An unhandled exception from the renderer is always fatal because it came from user code.
        // We want to notify the client if it's still connected, and then tear-down the circuit.
        private async void SynchronizationContext_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await ReportUnhandledException((Exception)e.ExceptionObject);
            UnhandledException?.Invoke(this, e);
        }

        private async Task ReportUnhandledException(Exception exception)
        {
            Log.CircuitUnhandledException(_logger, CircuitId, exception);

            await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(exception), exception);
        }

        private string GetClientErrorMessage(Exception exception, string additionalInformation = null)
        {
            if (_options.DetailedErrors)
            {
                return exception.ToString();
            }
            else
            {
                return $"There was an unhandled exception on the current circuit, so this circuit will be terminated. For more details turn on " +
                    $"detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set '{typeof(CircuitOptions).Name}.{nameof(CircuitOptions.DetailedErrors)}'. {additionalInformation}";
            }
        }

        // exception is only populated when either the renderer or the synchronization context signal exceptions.
        // In other cases it is null and should never be sent to the client.
        // error contains the information to send to the client.
        private async Task TryNotifyClientErrorAsync(IClientProxy client, string error, Exception exception = null)
        {
            if (!Client.Connected)
            {
                Log.UnhandledExceptionClientDisconnected(
                    _logger,
                    CircuitId,
                    exception);
                return;
            }

            try
            {
                Log.CircuitTransmittingClientError(_logger, CircuitId);
                await client.SendAsync("JS.Error", error);
                Log.CircuitTransmittedClientErrorSuccess(_logger, CircuitId);
            }
            catch (Exception ex)
            {
                Log.CircuitTransmitErrorFailed(_logger, CircuitId, ex);
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _initializationStarted;
            private static readonly Action<ILogger, Exception> _initializationSucceded;
            private static readonly Action<ILogger, Exception> _initializationFailed;
            private static readonly Action<ILogger, CircuitId, Exception> _disposeStarted;
            private static readonly Action<ILogger, CircuitId, Exception> _disposeSucceeded;
            private static readonly Action<ILogger, CircuitId, Exception> _disposeFailed;
            private static readonly Action<ILogger, CircuitId, Exception> _onCircuitOpened;
            private static readonly Action<ILogger, CircuitId, string, Exception> _onConnectionUp;
            private static readonly Action<ILogger, CircuitId, string, Exception> _onConnectionDown;
            private static readonly Action<ILogger, CircuitId, Exception> _onCircuitClosed;
            private static readonly Action<ILogger, Type, string, string, Exception> _circuitHandlerFailed;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitUnhandledException;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitTransmittingClientError;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitTransmittedClientErrorSuccess;
            private static readonly Action<ILogger, CircuitId, Exception> _circuitTransmitErrorFailed;
            private static readonly Action<ILogger, CircuitId, Exception> _unhandledExceptionClientDisconnected;

            private static readonly Action<ILogger, string, string, string, Exception> _beginInvokeDotNetStatic;
            private static readonly Action<ILogger, string, long, string, Exception> _beginInvokeDotNetInstance;
            private static readonly Action<ILogger, string, string, string, Exception> _beginInvokeDotNetStaticFailed;
            private static readonly Action<ILogger, string, long, string, Exception> _beginInvokeDotNetInstanceFailed;
            private static readonly Action<ILogger, Exception> _endInvokeDispatchException;
            private static readonly Action<ILogger, long, string, Exception> _endInvokeJSFailed;
            private static readonly Action<ILogger, long, Exception> _endInvokeJSSucceeded;
            private static readonly Action<ILogger, Exception> _dispatchEventFailedToParseEventData;
            private static readonly Action<ILogger, string, Exception> _dispatchEventFailedToDispatchEvent;
            private static readonly Action<ILogger, string, CircuitId, Exception> _locationChange;
            private static readonly Action<ILogger, string, CircuitId, Exception> _locationChangeSucceeded;
            private static readonly Action<ILogger, string, CircuitId, Exception> _locationChangeFailed;
            private static readonly Action<ILogger, string, CircuitId, Exception> _locationChangeFailedInCircuit;
            private static readonly Action<ILogger, long, CircuitId, Exception> _onRenderCompletedFailed;

            private static class EventIds
            {
                // 100s used for lifecycle stuff
                public static readonly EventId InitializationStarted = new EventId(100, "InitializationStarted");
                public static readonly EventId InitializationSucceeded = new EventId(101, "InitializationSucceeded");
                public static readonly EventId InitializationFailed = new EventId(102, "InitializationFailed");
                public static readonly EventId DisposeStarted = new EventId(103, "DisposeStarted");
                public static readonly EventId DisposeSucceeded = new EventId(104, "DisposeSucceeded");
                public static readonly EventId DisposeFailed = new EventId(105, "DisposeFailed");
                public static readonly EventId OnCircuitOpened = new EventId(106, "OnCircuitOpened");
                public static readonly EventId OnConnectionUp = new EventId(107, "OnConnectionUp");
                public static readonly EventId OnConnectionDown = new EventId(108, "OnConnectionDown");
                public static readonly EventId OnCircuitClosed = new EventId(109, "OnCircuitClosed");
                public static readonly EventId CircuitHandlerFailed = new EventId(110, "CircuitHandlerFailed");
                public static readonly EventId CircuitUnhandledException = new EventId(111, "CircuitUnhandledException");
                public static readonly EventId CircuitTransmittingClientError = new EventId(112, "CircuitTransmittingClientError");
                public static readonly EventId CircuitTransmittedClientErrorSuccess = new EventId(113, "CircuitTransmittedClientErrorSuccess");
                public static readonly EventId CircuitTransmitErrorFailed = new EventId(114, "CircuitTransmitErrorFailed");
                public static readonly EventId UnhandledExceptionClientDisconnected = new EventId(115, "UnhandledExceptionClientDisconnected");

                // 200s used for interactive stuff
                public static readonly EventId DispatchEventFailedToParseEventData = new EventId(200, "DispatchEventFailedToParseEventData");
                public static readonly EventId DispatchEventFailedToDispatchEvent = new EventId(201, "DispatchEventFailedToDispatchEvent");
                public static readonly EventId BeginInvokeDotNet = new EventId(202, "BeginInvokeDotNet");
                public static readonly EventId BeginInvokeDotNetFailed = new EventId(203, "BeginInvokeDotNetFailed");
                public static readonly EventId EndInvokeDispatchException = new EventId(204, "EndInvokeDispatchException");
                public static readonly EventId EndInvokeJSFailed = new EventId(205, "EndInvokeJSFailed");
                public static readonly EventId EndInvokeJSSucceeded = new EventId(206, "EndInvokeJSSucceeded");
                public static readonly EventId DispatchEventThroughJSInterop = new EventId(207, "DispatchEventThroughJSInterop");
                public static readonly EventId LocationChange = new EventId(208, "LocationChange");
                public static readonly EventId LocationChangeSucceeded = new EventId(209, "LocationChangeSucceeded");
                public static readonly EventId LocationChangeFailed = new EventId(210, "LocationChangeFailed");
                public static readonly EventId LocationChangeFailedInCircuit = new EventId(211, "LocationChangeFailedInCircuit");
                public static readonly EventId OnRenderCompletedFailed = new EventId(212, "OnRenderCompletedFailed");
            }

            static Log()
            {
                _initializationStarted = LoggerMessage.Define(
                    LogLevel.Debug,
                    EventIds.InitializationStarted,
                    "Circuit initialization started.");

                _initializationSucceded = LoggerMessage.Define(
                    LogLevel.Debug,
                    EventIds.InitializationSucceeded,
                    "Circuit initialization succeeded.");

                _initializationFailed = LoggerMessage.Define(
                    LogLevel.Debug,
                    EventIds.InitializationFailed,
                    "Circuit initialization failed.");

                _disposeStarted = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.DisposeStarted,
                    "Disposing circuit '{CircuitId}' started.");

                _disposeSucceeded = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.DisposeSucceeded,
                    "Disposing circuit '{CircuitId}' succeeded.");

                _disposeFailed = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.DisposeFailed,
                    "Disposing circuit '{CircuitId}' failed.");

                _onCircuitOpened = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.OnCircuitOpened,
                    "Opening circuit with id '{CircuitId}'.");

                _onConnectionUp = LoggerMessage.Define<CircuitId, string>(
                    LogLevel.Debug,
                    EventIds.OnConnectionUp,
                    "Circuit id '{CircuitId}' connected using connection '{ConnectionId}'.");

                _onConnectionDown = LoggerMessage.Define<CircuitId, string>(
                    LogLevel.Debug,
                    EventIds.OnConnectionDown,
                    "Circuit id '{CircuitId}' disconnected from connection '{ConnectionId}'.");

                _onCircuitClosed = LoggerMessage.Define<CircuitId>(
                   LogLevel.Debug,
                   EventIds.OnCircuitClosed,
                   "Closing circuit with id '{CircuitId}'.");

                _circuitHandlerFailed = LoggerMessage.Define<Type, string, string>(
                    LogLevel.Error,
                    EventIds.CircuitHandlerFailed,
                    "Unhandled error invoking circuit handler type {handlerType}.{handlerMethod}: {Message}");

                _circuitUnhandledException = LoggerMessage.Define<CircuitId>(
                   LogLevel.Error,
                   EventIds.CircuitUnhandledException,
                   "Unhandled exception in circuit '{CircuitId}'.");

                _circuitTransmittingClientError = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.CircuitTransmittingClientError,
                    "About to notify client of an error in circuit '{CircuitId}'.");

                _circuitTransmittedClientErrorSuccess = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.CircuitTransmittedClientErrorSuccess,
                    "Successfully transmitted error to client in circuit '{CircuitId}'.");

                _circuitTransmitErrorFailed = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.CircuitTransmitErrorFailed,
                    "Failed to transmit exception to client in circuit '{CircuitId}'.");

                _unhandledExceptionClientDisconnected = LoggerMessage.Define<CircuitId>(
                    LogLevel.Debug,
                    EventIds.UnhandledExceptionClientDisconnected,
                    "An exception occurred on the circuit host '{CircuitId}' while the client is disconnected.");

                _beginInvokeDotNetStatic = LoggerMessage.Define<string, string, string>(
                    LogLevel.Debug,
                    EventIds.BeginInvokeDotNet,
                    "Invoking static method with identifier '{MethodIdentifier}' on assembly '{Assembly}' with callback id '{CallId}'.");

                _beginInvokeDotNetInstance = LoggerMessage.Define<string, long, string>(
                    LogLevel.Debug,
                    EventIds.BeginInvokeDotNet,
                    "Invoking instance method '{MethodIdentifier}' on instance '{DotNetObjectId}' with callback id '{CallId}'.");

                _beginInvokeDotNetStaticFailed = LoggerMessage.Define<string, string, string>(
                    LogLevel.Debug,
                    EventIds.BeginInvokeDotNetFailed,
                    "Failed to invoke static method with identifier '{MethodIdentifier}' on assembly '{Assembly}' with callback id '{CallId}'.");

                _beginInvokeDotNetInstanceFailed = LoggerMessage.Define<string, long, string>(
                    LogLevel.Debug,
                    EventIds.BeginInvokeDotNetFailed,
                    "Failed to invoke instance method '{MethodIdentifier}' on instance '{DotNetObjectId}' with callback id '{CallId}'.");

                _endInvokeDispatchException = LoggerMessage.Define(
                    LogLevel.Debug,
                    EventIds.EndInvokeDispatchException,
                    "There was an error invoking 'Microsoft.JSInterop.DotNetDispatcher.EndInvoke'.");

                _endInvokeJSFailed = LoggerMessage.Define<long, string>(
                    LogLevel.Debug,
                    EventIds.EndInvokeJSFailed,
                    "The JS interop call with callback id '{AsyncCall}' failed with error '{Error}'.");

                _endInvokeJSSucceeded = LoggerMessage.Define<long>(
                    LogLevel.Debug,
                    EventIds.EndInvokeJSSucceeded,
                    "The JS interop call with callback id '{AsyncCall}' succeeded.");

                _dispatchEventFailedToParseEventData = LoggerMessage.Define(
                    LogLevel.Debug,
                    EventIds.DispatchEventFailedToParseEventData,
                    "Failed to parse the event data when trying to dispatch an event.");

                _dispatchEventFailedToDispatchEvent = LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    EventIds.DispatchEventFailedToDispatchEvent,
                    "There was an error dispatching the event '{EventHandlerId}' to the application.");

                _locationChange = LoggerMessage.Define<string, CircuitId>(
                    LogLevel.Debug,
                    EventIds.LocationChange,
                    "Location changing to {URI} in circuit '{CircuitId}'.");

                _locationChangeSucceeded = LoggerMessage.Define<string, CircuitId>(
                    LogLevel.Debug,
                    EventIds.LocationChangeSucceeded,
                    "Location change to '{URI}' in circuit '{CircuitId}' succeeded.");

                _locationChangeFailed = LoggerMessage.Define<string, CircuitId>(
                    LogLevel.Debug,
                    EventIds.LocationChangeFailed,
                    "Location change to '{URI}' in circuit '{CircuitId}' failed.");

                _locationChangeFailedInCircuit = LoggerMessage.Define<string, CircuitId>(
                    LogLevel.Error,
                    EventIds.LocationChangeFailed,
                    "Location change to '{URI}' in circuit '{CircuitId}' failed.");

                _onRenderCompletedFailed = LoggerMessage.Define<long, CircuitId>(
                    LogLevel.Debug,
                    EventIds.OnRenderCompletedFailed,
                    "Failed to complete render batch '{RenderId}' in circuit host '{CircuitId}'.");
            }

            public static void InitializationStarted(ILogger logger) => _initializationStarted(logger, null);
            public static void InitializationSucceeded(ILogger logger) => _initializationSucceded(logger, null);
            public static void InitializationFailed(ILogger logger, Exception exception) => _initializationFailed(logger, exception);
            public static void DisposeStarted(ILogger logger, CircuitId circuitId) => _disposeStarted(logger, circuitId, null);
            public static void DisposeSucceeded(ILogger logger, CircuitId circuitId) => _disposeSucceeded(logger, circuitId, null);
            public static void DisposeFailed(ILogger logger, CircuitId circuitId, Exception exception) => _disposeFailed(logger, circuitId, exception);
            public static void CircuitOpened(ILogger logger, CircuitId circuitId) => _onCircuitOpened(logger, circuitId, null);
            public static void ConnectionUp(ILogger logger, CircuitId circuitId, string connectionId) => _onConnectionUp(logger, circuitId, connectionId, null);
            public static void ConnectionDown(ILogger logger, CircuitId circuitId, string connectionId) => _onConnectionDown(logger, circuitId, connectionId, null);
            public static void CircuitClosed(ILogger logger, CircuitId circuitId) => _onCircuitClosed(logger, circuitId, null);

            public static void CircuitHandlerFailed(ILogger logger, CircuitHandler handler, string handlerMethod, Exception exception)
            {
                _circuitHandlerFailed(
                    logger,
                    handler.GetType(),
                    handlerMethod,
                    exception.Message,
                    exception);
            }

            public static void CircuitUnhandledException(ILogger logger, CircuitId circuitId, Exception exception) => _circuitUnhandledException(logger, circuitId, exception);
            public static void CircuitTransmitErrorFailed(ILogger logger, CircuitId circuitId, Exception exception) => _circuitTransmitErrorFailed(logger, circuitId, exception);
            public static void EndInvokeDispatchException(ILogger logger, Exception ex) => _endInvokeDispatchException(logger, ex);
            public static void EndInvokeJSFailed(ILogger logger, long asyncHandle, string arguments) => _endInvokeJSFailed(logger, asyncHandle, arguments, null);
            public static void EndInvokeJSSucceeded(ILogger logger, long asyncCall) => _endInvokeJSSucceeded(logger, asyncCall, null);
            public static void DispatchEventFailedToParseEventData(ILogger logger, Exception ex) => _dispatchEventFailedToParseEventData(logger, ex);
            public static void DispatchEventFailedToDispatchEvent(ILogger logger, string eventHandlerId, Exception ex) => _dispatchEventFailedToDispatchEvent(logger, eventHandlerId ?? "", ex);

            public static void BeginInvokeDotNet(ILogger logger, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId)
            {
                if (assemblyName != null)
                {
                    _beginInvokeDotNetStatic(logger, methodIdentifier, assemblyName, callId, null);
                }
                else
                {
                    _beginInvokeDotNetInstance(logger, methodIdentifier, dotNetObjectId, callId, null);
                }
            }

            public static void BeginInvokeDotNetFailed(ILogger logger, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, Exception exception)
            {
                if (assemblyName != null)
                {
                    _beginInvokeDotNetStaticFailed(logger, methodIdentifier, assemblyName, callId, null);
                }
                else
                {
                    _beginInvokeDotNetInstanceFailed(logger, methodIdentifier, dotNetObjectId, callId, null);
                }
            }

            public static void LocationChange(ILogger logger, string uri, CircuitId circuitId) => _locationChange(logger, uri, circuitId, null);
            public static void LocationChangeSucceeded(ILogger logger, string uri, CircuitId circuitId) => _locationChangeSucceeded(logger, uri, circuitId, null);
            public static void LocationChangeFailed(ILogger logger, string uri, CircuitId circuitId, Exception exception) => _locationChangeFailed(logger, uri, circuitId, exception);
            public static void LocationChangeFailedInCircuit(ILogger logger, string uri, CircuitId circuitId, Exception exception) => _locationChangeFailedInCircuit(logger, uri, circuitId, exception);
            public static void UnhandledExceptionClientDisconnected(ILogger logger, CircuitId circuitId, Exception exception) => _unhandledExceptionClientDisconnected(logger, circuitId, exception);
            public static void CircuitTransmittingClientError(ILogger logger, CircuitId circuitId) => _circuitTransmittingClientError(logger, circuitId, null);
            public static void CircuitTransmittedClientErrorSuccess(ILogger logger, CircuitId circuitId) => _circuitTransmittedClientErrorSuccess(logger, circuitId, null);
            public static void OnRenderCompletedFailed(ILogger logger, long renderId, CircuitId circuitId, Exception e) => _onRenderCompletedFailed(logger, renderId, circuitId, e);
        }
    }
}
