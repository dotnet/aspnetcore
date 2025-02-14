// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

#pragma warning disable CA1852 // Seal internal types
internal partial class CircuitHost : IAsyncDisposable
#pragma warning restore CA1852 // Seal internal types
{
    private readonly AsyncServiceScope _scope;
    private readonly CircuitOptions _options;
    private readonly RemoteNavigationManager _navigationManager;
    private readonly ILogger _logger;
    private Func<Func<Task>, Task> _dispatchInboundActivity;
    private CircuitHandler[] _circuitHandlers;
    private bool _initialized;
    private bool _isFirstUpdate = true;
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
        AsyncServiceScope scope,
        CircuitOptions options,
        CircuitClientProxy client,
        RemoteRenderer renderer,
        IReadOnlyList<ComponentDescriptor> descriptors,
        RemoteJSRuntime jsRuntime,
        RemoteNavigationManager navigationManager,
        CircuitHandler[] circuitHandlers,
        ILogger logger)
    {
        CircuitId = circuitId;
        if (CircuitId.Secret is null)
        {
            // Prevent the use of a 'default' secret.
            throw new ArgumentException($"Property '{nameof(CircuitId.Secret)}' cannot be null.", nameof(circuitId));
        }

        _scope = scope;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _circuitHandlers = circuitHandlers ?? throw new ArgumentNullException(nameof(circuitHandlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Services = scope.ServiceProvider;

        Circuit = new Circuit(this);
        Handle = new CircuitHandle() { CircuitHost = this, };

        _dispatchInboundActivity = BuildInboundActivityDispatcher(_circuitHandlers, Circuit);

        // An unhandled exception from the renderer is always fatal because it came from user code.
        Renderer.UnhandledException += ReportAndInvoke_UnhandledException;
        Renderer.UnhandledSynchronizationException += SynchronizationContext_UnhandledException;

        JSRuntime.UnhandledException += ReportAndInvoke_UnhandledException;

        _navigationManager.UnhandledException += ReportAndInvoke_UnhandledException;
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
    public Task InitializeAsync(ProtectedPrerenderComponentApplicationStore store, CancellationToken cancellationToken)
    {
        Log.InitializationStarted(_logger);

        return HandleInboundActivityAsync(() => Renderer.Dispatcher.InvokeAsync(async () =>
        {
            if (_initialized)
            {
                throw new InvalidOperationException("The circuit host is already initialized.");
            }

            try
            {
                _initialized = true; // We're ready to accept incoming JSInterop calls from here on

                // We only run the handlers in case we are in a Blazor Server scenario, which renders
                // the components inmediately during start.
                // On Blazor Web scenarios we delay running these handlers until the first UpdateRootComponents call
                // We do this so that the handlers can have access to the restored application state.
                if (Descriptors.Count > 0)
                {
                    await OnCircuitOpenedAsync(cancellationToken);
                    await OnConnectionUpAsync(cancellationToken);
                }

                // Here, we add each root component but don't await the returned tasks so that the
                // components can be processed in parallel.
                var count = Descriptors.Count;
                var pendingRenders = new Task[count];
                for (var i = 0; i < count; i++)
                {
                    var (componentType, parameters, sequence) = Descriptors[i];
                    pendingRenders[i] = Renderer.AddComponentAsync(componentType, parameters, sequence.ToString(CultureInfo.InvariantCulture));
                }

                // Now we wait for all components to finish rendering.
                await Task.WhenAll(pendingRenders);

                // At this point all components have successfully produced an initial render and we can clear the contents of the component
                // application state store. This ensures the memory that was not used during the initial render of these components gets
                // reclaimed since no-one else is holding on to it any longer.
                // This is also important because otherwise components will keep reusing the existing state after
                // the initial render instead of initializing their state from the original sources like the Db or a
                // web service, preventing UI updates.
                if (Descriptors.Count > 0)
                {
                    store.ExistingState.Clear();
                }

                // This variable is used to track that this is the first time we are updating components.
                // In Blazor Web scenarios the app will send an initial empty list of descriptors,
                // so we want to make sure that we allow setting up the state in that case.
                // In Blazor Server the initial set of descriptors is provided via the call to Start, so
                // we want to make sure we don't take any state afterwards.
                _isFirstUpdate = Descriptors.Count == 0;

                Log.InitializationSucceeded(_logger);
            }
            catch (Exception ex)
            {
                // Report errors asynchronously. InitializeAsync is designed not to throw.
                Log.InitializationFailed(_logger, ex);
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex), ex);
            }
        }));
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
                // Prevent any further JS interop calls
                // Helps with scenarios like https://github.com/dotnet/aspnetcore/issues/32808
                JSRuntime.MarkPermanentlyDisconnected();

                await Renderer.DisposeAsync();
                await _scope.DisposeAsync();

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
            _ = HandleInboundActivityAsync(() => Renderer.OnRenderCompletedAsync(renderId, errorMessageOrNull));
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
            await HandleInboundActivityAsync(() => Renderer.Dispatcher.InvokeAsync(() =>
            {
                Log.BeginInvokeDotNet(_logger, callId, assemblyName, methodIdentifier, dotNetObjectId);
                var invocationInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId);
                DotNetDispatcher.BeginInvokeDotNet(JSRuntime, invocationInfo, argsJson);
            }));
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
            await HandleInboundActivityAsync(() => Renderer.Dispatcher.InvokeAsync(() =>
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
            }));
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

    // ReceiveByteArray is used in a fire-and-forget context, so it's responsible for its own
    // error handling.
    internal async Task ReceiveByteArray(int id, byte[] data)
    {
        AssertInitialized();
        AssertNotDisposed();

        try
        {
            await HandleInboundActivityAsync(() => Renderer.Dispatcher.InvokeAsync(() =>
            {
                Log.ReceiveByteArraySuccess(_logger, id);
                DotNetDispatcher.ReceiveByteArray(JSRuntime, id, data);
            }));
        }
        catch (Exception ex)
        {
            // An error completing JS interop means that the user sent invalid data, a well-behaved
            // client won't do this.
            Log.ReceiveByteArrayException(_logger, id, ex);
            await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Invalid byte array."));
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
        }
    }

    // ReceiveJSDataChunk is used in a fire-and-forget context, so it's responsible for its own
    // error handling.
    internal async Task<bool> ReceiveJSDataChunk(long streamId, long chunkId, byte[] chunk, string error)
    {
        AssertInitialized();
        AssertNotDisposed();

        try
        {
            return await HandleInboundActivityAsync(() => Renderer.Dispatcher.InvokeAsync(() =>
            {
                return RemoteJSDataStream.ReceiveData(JSRuntime, streamId, chunkId, chunk, error);
            }));
        }
        catch (Exception ex)
        {
            // An error completing JS interop means that the user sent invalid data, a well-behaved
            // client won't do this.
            Log.ReceiveJSDataChunkException(_logger, streamId, ex);
            await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Invalid chunk supplied to stream."));
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            return false;
        }
    }

    public async Task<int> SendDotNetStreamAsync(DotNetStreamReference dotNetStreamReference, long streamId, byte[] buffer)
    {
        AssertInitialized();
        AssertNotDisposed();

        try
        {
            return await Renderer.Dispatcher.InvokeAsync(async () => await dotNetStreamReference.Stream.ReadAsync(buffer));
        }
        catch (Exception ex)
        {
            // An error completing stream interop means that the user sent invalid data, a well-behaved
            // client won't do this.
            Log.SendDotNetStreamException(_logger, streamId, ex);
            await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Unable to send .NET stream."));
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            return 0;
        }
    }

    public async Task<DotNetStreamReference> TryClaimPendingStream(long streamId)
    {
        AssertInitialized();
        AssertNotDisposed();

        DotNetStreamReference dotNetStreamReference = null;

        try
        {
            return await Renderer.Dispatcher.InvokeAsync<DotNetStreamReference>(() =>
            {
                if (!JSRuntime.TryClaimPendingStreamForSending(streamId, out dotNetStreamReference))
                {
                    throw new InvalidOperationException($"The stream with ID {streamId} is not available. It may have timed out.");
                }

                return dotNetStreamReference;
            });
        }
        catch (Exception ex)
        {
            // An error completing stream interop means that the user sent invalid data, a well-behaved
            // client won't do this.
            Log.SendDotNetStreamException(_logger, streamId, ex);
            await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex, "Unable to locate .NET stream."));
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            return default;
        }
    }

    // OnLocationChangedAsync is used in a fire-and-forget context, so it's responsible for its own
    // error handling.
    public async Task OnLocationChangedAsync(string uri, string state, bool intercepted)
    {
        AssertInitialized();
        AssertNotDisposed();

        try
        {
            await HandleInboundActivityAsync(() => Renderer.Dispatcher.InvokeAsync(() =>
            {
                Log.LocationChange(_logger, uri, CircuitId);
                _navigationManager.NotifyLocationChanged(uri, state, intercepted);
                Log.LocationChangeSucceeded(_logger, uri, CircuitId);
            }));
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

    public async Task OnLocationChangingAsync(int callId, string uri, string? state, bool intercepted)
    {
        AssertInitialized();
        AssertNotDisposed();

        try
        {
            var shouldContinueNavigation = await HandleInboundActivityAsync(() => Renderer.Dispatcher.InvokeAsync(async () =>
            {
                Log.LocationChanging(_logger, uri, CircuitId);
                return await _navigationManager.HandleLocationChangingAsync(uri, state, intercepted);
            }));

            await Client.SendAsync("JS.EndLocationChanging", callId, shouldContinueNavigation);
        }
        catch (Exception ex)
        {
            // An exception caught at this point was probably thrown inside the NavigationManager. Treat
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
        _ = Renderer.Dispatcher.InvokeAsync(Renderer.ProcessBufferedRenderBatches);
    }

    // Internal for testing.
    internal Task HandleInboundActivityAsync(Func<Task> handler)
        => _dispatchInboundActivity(handler);

    // Internal for testing.
    internal async Task<TResult> HandleInboundActivityAsync<TResult>(Func<Task<TResult>> handler)
    {
        TResult result = default;
        await _dispatchInboundActivity(async () => result = await handler());
        return result;
    }

    private static Func<Func<Task>, Task> BuildInboundActivityDispatcher(IReadOnlyList<CircuitHandler> circuitHandlers, Circuit circuit)
    {
        if (circuitHandlers.Count == 0)
        {
            // If there are no registered handlers, there is no need to allocate a context on each call.
            return static handler => handler();
        }

        var result = static (CircuitInboundActivityContext context) => context.Handler();

        for (var i = circuitHandlers.Count - 1; i >= 0; i--)
        {
            var next = result;
            result = circuitHandlers[i].CreateInboundActivityHandler(next);
        }

        return handler => result(new(handler, circuit));
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
#pragma warning disable CA1513 // Use ObjectDisposedException throw helper
        if (_disposed)
        {
            throw new ObjectDisposedException(objectName: null);
        }
#pragma warning restore CA1513 // Use ObjectDisposedException throw helper
    }

    // We want to notify the client if it's still connected, and then tear-down the circuit.
    private async void ReportAndInvoke_UnhandledException(object sender, Exception e)
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

    internal Task UpdateRootComponents(
        RootComponentOperationBatch operationBatch,
        ProtectedPrerenderComponentApplicationStore store,
        CancellationToken cancellation)
    {
        Log.UpdateRootComponentsStarted(_logger);

        return Renderer.Dispatcher.InvokeAsync(async () =>
        {
            var shouldClearStore = false;
            var shouldWaitForQuiescence = false;
            var operations = operationBatch.Operations;
            var batchId = operationBatch.BatchId;
            try
            {
                if (Descriptors.Count > 0)
                {
                    // Block updating components if they were provided during StartCircuit. This keeps
                    // the footprint for Blazor Server closer to what it was before.
                    throw new InvalidOperationException("UpdateRootComponents is not supported when components have" +
                        " been provided during circuit start up.");
                }
                if (_isFirstUpdate)
                {
                    _isFirstUpdate = false;
                    shouldWaitForQuiescence = true;
                    if (store != null)
                    {
                        shouldClearStore = true;
                        // We only do this if we have no root components. Otherwise, the state would have been
                        // provided during the start up process
                        var appLifetime = _scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
                        await appLifetime.RestoreStateAsync(store);
                        RestoreAntiforgeryToken(_scope);
                    }

                    // Retrieve the circuit handlers at this point.
                    _circuitHandlers = [.. _scope.ServiceProvider.GetServices<CircuitHandler>().OrderBy(h => h.Order)];
                    _dispatchInboundActivity = BuildInboundActivityDispatcher(_circuitHandlers, Circuit);
                    await OnCircuitOpenedAsync(cancellation);
                    await OnConnectionUpAsync(cancellation);

                    for (var i = 0; i < operations.Length; i++)
                    {
                        var operation = operations[i];
                        if (operation.Type != RootComponentOperationType.Add)
                        {
                            throw new InvalidOperationException($"The first set of update operations must always be of type {nameof(RootComponentOperationType.Add)}");
                        }
                    }
                }

                await PerformRootComponentOperations(operations, shouldWaitForQuiescence);

                await Client.SendAsync("JS.EndUpdateRootComponents", batchId);

                Log.UpdateRootComponentsSucceeded(_logger);
            }
            catch (Exception ex)
            {
                // Report errors asynchronously. UpdateRootComponents is designed not to throw.
                Log.UpdateRootComponentsFailed(_logger, ex);
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
                await TryNotifyClientErrorAsync(Client, GetClientErrorMessage(ex), ex);
            }
            finally
            {
                if (shouldClearStore)
                {
                    // At this point all components have successfully produced an initial render and we can clear the contents of the component
                    // application state store. This ensures the memory that was not used during the initial render of these components gets
                    // reclaimed since no-one else is holding on to it any longer.
                    store.ExistingState.Clear();
                }
            }
        });
    }

    private static void RestoreAntiforgeryToken(AsyncServiceScope scope)
    {
        // GetAntiforgeryToken makes sure the antiforgery token is restored from persitent component
        // state and is available on the circuit whether or not is used by a component on the first
        // render.
        var antiforgery = scope.ServiceProvider.GetService<AntiforgeryStateProvider>();
        _ = antiforgery?.GetAntiforgeryToken();
    }

    private async ValueTask PerformRootComponentOperations(
        RootComponentOperation[] operations,
        bool shouldWaitForQuiescence)
    {
        var webRootComponentManager = Renderer.GetOrCreateWebRootComponentManager();
        var pendingTasks = shouldWaitForQuiescence
            ? new Task[operations.Length]
            : null;

        // The inbound activity pipeline needs to be awaited because it populates
        // the pending tasks used to wait for quiescence.
        await HandleInboundActivityAsync(() =>
        {
            for (var i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];
                switch (operation.Type)
                {
                    case RootComponentOperationType.Add:
                        var task = webRootComponentManager.AddRootComponentAsync(
                            operation.SsrComponentId,
                            operation.Descriptor.ComponentType,
                            operation.Marker.Value.Key,
                            operation.Descriptor.Parameters);
                        if (pendingTasks != null)
                        {
                            pendingTasks[i] = task;
                        }
                        break;
                    case RootComponentOperationType.Update:
                        // We don't need to await component updates as any unhandled exception will be reported and terminate the circuit.
                        _ = webRootComponentManager.UpdateRootComponentAsync(
                            operation.SsrComponentId,
                            operation.Descriptor.ComponentType,
                            operation.Marker.Value.Key,
                            operation.Descriptor.Parameters);
                        break;
                    case RootComponentOperationType.Remove:
                        webRootComponentManager.RemoveRootComponent(operation.SsrComponentId);
                        break;
                }
            }

            return Task.CompletedTask;
        });

        if (pendingTasks != null)
        {
            await Task.WhenAll(pendingTasks);
        }
    }

    private static partial class Log
    {
        // 100s used for lifecycle stuff
        // 200s used for interactive stuff

        [LoggerMessage(100, LogLevel.Debug, "Circuit initialization started.", EventName = "InitializationStarted")]
        public static partial void InitializationStarted(ILogger logger);

        [LoggerMessage(101, LogLevel.Debug, "Circuit initialization succeeded.", EventName = "InitializationSucceeded")]
        public static partial void InitializationSucceeded(ILogger logger);

        [LoggerMessage(102, LogLevel.Debug, "Circuit initialization failed.", EventName = "InitializationFailed")]
        public static partial void InitializationFailed(ILogger logger, Exception exception);

        [LoggerMessage(103, LogLevel.Debug, "Disposing circuit '{CircuitId}' started.", EventName = "DisposeStarted")]
        public static partial void DisposeStarted(ILogger logger, CircuitId circuitId);

        [LoggerMessage(104, LogLevel.Debug, "Disposing circuit '{CircuitId}' succeeded.", EventName = "DisposeSucceeded")]
        public static partial void DisposeSucceeded(ILogger logger, CircuitId circuitId);

        [LoggerMessage(105, LogLevel.Debug, "Disposing circuit '{CircuitId}' failed.", EventName = "DisposeFailed")]
        public static partial void DisposeFailed(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(106, LogLevel.Debug, "Opening circuit with id '{CircuitId}'.", EventName = "OnCircuitOpened")]
        public static partial void CircuitOpened(ILogger logger, CircuitId circuitId);

        [LoggerMessage(107, LogLevel.Debug, "Circuit id '{CircuitId}' connected using connection '{ConnectionId}'.", EventName = "OnConnectionUp")]
        public static partial void ConnectionUp(ILogger logger, CircuitId circuitId, string connectionId);

        [LoggerMessage(108, LogLevel.Debug, "Circuit id '{CircuitId}' disconnected from connection '{ConnectionId}'.", EventName = "OnConnectionDown")]
        public static partial void ConnectionDown(ILogger logger, CircuitId circuitId, string connectionId);

        [LoggerMessage(109, LogLevel.Debug, "Closing circuit with id '{CircuitId}'.", EventName = "OnCircuitClosed")]
        public static partial void CircuitClosed(ILogger logger, CircuitId circuitId);

        [LoggerMessage(110, LogLevel.Error, "Unhandled error invoking circuit handler type {handlerType}.{handlerMethod}: {Message}", EventName = "CircuitHandlerFailed")]
        private static partial void CircuitHandlerFailed(ILogger logger, Type handlerType, string handlerMethod, string message, Exception exception);

        [LoggerMessage(111, LogLevel.Debug, "Update root components started.", EventName = nameof(UpdateRootComponentsStarted))]
        public static partial void UpdateRootComponentsStarted(ILogger logger);

        [LoggerMessage(112, LogLevel.Debug, "Update root components succeeded.", EventName = nameof(UpdateRootComponentsSucceeded))]
        public static partial void UpdateRootComponentsSucceeded(ILogger logger);

        [LoggerMessage(113, LogLevel.Debug, "Update root components failed.", EventName = nameof(UpdateRootComponentsFailed))]
        public static partial void UpdateRootComponentsFailed(ILogger logger, Exception exception);

        public static void CircuitHandlerFailed(ILogger logger, CircuitHandler handler, string handlerMethod, Exception exception)
        {
            CircuitHandlerFailed(
                logger,
                handler.GetType(),
                handlerMethod,
                exception.Message,
                exception);
        }

        [LoggerMessage(111, LogLevel.Error, "Unhandled exception in circuit '{CircuitId}'.", EventName = "CircuitUnhandledException")]
        public static partial void CircuitUnhandledException(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(112, LogLevel.Debug, "About to notify client of an error in circuit '{CircuitId}'.", EventName = "CircuitTransmittingClientError")]
        public static partial void CircuitTransmittingClientError(ILogger logger, CircuitId circuitId);

        [LoggerMessage(113, LogLevel.Debug, "Successfully transmitted error to client in circuit '{CircuitId}'.", EventName = "CircuitTransmittedClientErrorSuccess")]
        public static partial void CircuitTransmittedClientErrorSuccess(ILogger logger, CircuitId circuitId);

        [LoggerMessage(114, LogLevel.Debug, "Failed to transmit exception to client in circuit '{CircuitId}'.", EventName = "CircuitTransmitErrorFailed")]
        public static partial void CircuitTransmitErrorFailed(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(115, LogLevel.Debug, "An exception occurred on the circuit host '{CircuitId}' while the client is disconnected.", EventName = "UnhandledExceptionClientDisconnected")]
        public static partial void UnhandledExceptionClientDisconnected(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(116, LogLevel.Debug, "The root component operation of type 'Update' was invalid: {Message}", EventName = nameof(InvalidComponentTypeForUpdate))]
        public static partial void InvalidComponentTypeForUpdate(ILogger logger, string message);

        [LoggerMessage(200, LogLevel.Debug, "Failed to parse the event data when trying to dispatch an event.", EventName = "DispatchEventFailedToParseEventData")]
        public static partial void DispatchEventFailedToParseEventData(ILogger logger, Exception ex);

        [LoggerMessage(201, LogLevel.Debug, "There was an error dispatching the event '{EventHandlerId}' to the application.", EventName = "DispatchEventFailedToDispatchEvent")]
        public static partial void DispatchEventFailedToDispatchEvent(ILogger logger, string eventHandlerId, Exception ex);

        [LoggerMessage(202, LogLevel.Debug, "Invoking instance method '{MethodIdentifier}' on instance '{DotNetObjectId}' with callback id '{CallId}'.", EventName = "BeginInvokeDotNet")]
        private static partial void BeginInvokeDotNet(ILogger logger, string methodIdentifier, long dotNetObjectId, string callId);

        [LoggerMessage(203, LogLevel.Debug, "Failed to invoke instance method '{MethodIdentifier}' on instance '{DotNetObjectId}' with callback id '{CallId}'.", EventName = "BeginInvokeDotNetFailed")]
        private static partial void BeginInvokeDotNetFailed(ILogger logger, string methodIdentifier, long dotNetObjectId, string callId, Exception exception);

        [LoggerMessage(204, LogLevel.Debug, "There was an error invoking 'Microsoft.JSInterop.DotNetDispatcher.EndInvoke'.", EventName = "EndInvokeDispatchException")]
        public static partial void EndInvokeDispatchException(ILogger logger, Exception ex);

        [LoggerMessage(205, LogLevel.Debug, "The JS interop call with callback id '{AsyncCall}' with arguments {Arguments}.", EventName = "EndInvokeJSFailed")]
        public static partial void EndInvokeJSFailed(ILogger logger, long asyncCall, string arguments);

        [LoggerMessage(206, LogLevel.Debug, "The JS interop call with callback id '{AsyncCall}' succeeded.", EventName = "EndInvokeJSSucceeded")]
        public static partial void EndInvokeJSSucceeded(ILogger logger, long asyncCall);

        [LoggerMessage(208, LogLevel.Debug, "Location changing to {URI} in circuit '{CircuitId}'.", EventName = "LocationChange")]
        public static partial void LocationChange(ILogger logger, string uri, CircuitId circuitId);

        [LoggerMessage(209, LogLevel.Debug, "Location change to '{URI}' in circuit '{CircuitId}' succeeded.", EventName = "LocationChangeSucceeded")]
        public static partial void LocationChangeSucceeded(ILogger logger, string uri, CircuitId circuitId);

        [LoggerMessage(210, LogLevel.Debug, "Location change to '{URI}' in circuit '{CircuitId}' failed.", EventName = "LocationChangeFailed")]
        public static partial void LocationChangeFailed(ILogger logger, string uri, CircuitId circuitId, Exception exception);

        [LoggerMessage(211, LogLevel.Debug, "Location is about to change to {URI} in ciruit '{CircuitId}'.", EventName = "LocationChanging")]
        public static partial void LocationChanging(ILogger logger, string uri, CircuitId circuitId);

        [LoggerMessage(212, LogLevel.Debug, "Failed to complete render batch '{RenderId}' in circuit host '{CircuitId}'.", EventName = "OnRenderCompletedFailed")]
        public static partial void OnRenderCompletedFailed(ILogger logger, long renderId, CircuitId circuitId, Exception e);

        [LoggerMessage(213, LogLevel.Debug, "The ReceiveByteArray call with id '{id}' succeeded.", EventName = "ReceiveByteArraySucceeded")]
        public static partial void ReceiveByteArraySuccess(ILogger logger, long id);

        [LoggerMessage(214, LogLevel.Debug, "The ReceiveByteArray call with id '{id}' failed.", EventName = "ReceiveByteArrayException")]
        public static partial void ReceiveByteArrayException(ILogger logger, long id, Exception ex);

        [LoggerMessage(215, LogLevel.Debug, "The ReceiveJSDataChunk call with stream id '{streamId}' failed.", EventName = "ReceiveJSDataChunkException")]
        public static partial void ReceiveJSDataChunkException(ILogger logger, long streamId, Exception ex);

        [LoggerMessage(216, LogLevel.Debug, "The SendDotNetStreamAsync call with id '{id}' failed.", EventName = "SendDotNetStreamException")]
        public static partial void SendDotNetStreamException(ILogger logger, long id, Exception ex);

        [LoggerMessage(217, LogLevel.Debug, "Invoking static method with identifier '{MethodIdentifier}' on assembly '{Assembly}' with callback id '{CallId}'.", EventName = "BeginInvokeDotNetStatic")]
        private static partial void BeginInvokeDotNetStatic(ILogger logger, string methodIdentifier, string assembly, string callId);

        public static void BeginInvokeDotNet(ILogger logger, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId)
        {
            if (assemblyName != null)
            {
                BeginInvokeDotNetStatic(logger, methodIdentifier, assemblyName, callId);
            }
            else
            {
                BeginInvokeDotNet(logger, methodIdentifier, dotNetObjectId, callId);
            }
        }

        [LoggerMessage(218, LogLevel.Debug, "Failed to invoke static method with identifier '{MethodIdentifier}' on assembly '{Assembly}' with callback id '{CallId}'.", EventName = "BeginInvokeDotNetStaticFailed")]
        private static partial void BeginInvokeDotNetStaticFailed(ILogger logger, string methodIdentifier, string assembly, string callId, Exception exception);

        public static void BeginInvokeDotNetFailed(ILogger logger, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, Exception exception)
        {
            if (assemblyName != null)
            {
                BeginInvokeDotNetStaticFailed(logger, methodIdentifier, assemblyName, callId, exception);
            }
            else
            {
                BeginInvokeDotNetFailed(logger, methodIdentifier, dotNetObjectId, callId, exception);
            }
        }

        [LoggerMessage(219, LogLevel.Error, "Location change to '{URI}' in circuit '{CircuitId}' failed.", EventName = "LocationChangeFailedInCircuit")]
        public static partial void LocationChangeFailedInCircuit(ILogger logger, string uri, CircuitId circuitId, Exception exception);
    }
}
