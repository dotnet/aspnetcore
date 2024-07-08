// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// A connection used to invoke hub methods on a SignalR Server.
/// </summary>
/// <remarks>
/// A <see cref="HubConnection"/> should be created using <see cref="HubConnectionBuilder"/>.
/// Before hub methods can be invoked the connection must be started using <see cref="StartAsync"/>.
/// Clean up a connection using <see cref="StopAsync"/> or <see cref="DisposeAsync"/>.
/// </remarks>
public partial class HubConnection : IAsyncDisposable
{
    /// <summary>
    /// The default timeout which specifies how long to wait for a message before closing the connection. Default is 30 seconds.
    /// </summary>
    public static readonly TimeSpan DefaultServerTimeout = TimeSpan.FromSeconds(30); // Server ping rate is 15 sec, this is 2 times that.

    /// <summary>
    /// The default timeout which specifies how long to wait for the handshake to respond before closing the connection. Default is 15 seconds.
    /// </summary>
    public static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The default interval that the client will send keep alive messages to let the server know to not close the connection. Default is 15 second interval.
    /// </summary>
    public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(15);

    // Default amount of bytes we'll buffer when using Stateful Reconnect until applying backpressure to sends from the client.
    internal const long DefaultStatefulReconnectBufferSize = 100_000;

    // The receive loop has a single reader and single writer at a time so optimize the channel for that
    private static readonly UnboundedChannelOptions _receiveLoopOptions = new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    };

    private static readonly MethodInfo _sendStreamItemsMethod = typeof(HubConnection).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(m => m.Name.Equals(nameof(SendStreamItems)));
    private static readonly MethodInfo _sendIAsyncStreamItemsMethod = typeof(HubConnection).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(m => m.Name.Equals(nameof(SendIAsyncEnumerableStreamItems)));

    // Persistent across all connections
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly ConnectionLogScope _logScope;
    private readonly IHubProtocol _protocol;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IRetryPolicy? _reconnectPolicy;
    private readonly EndPoint _endPoint;
    private readonly ConcurrentDictionary<string, InvocationHandlerList> _handlers = new ConcurrentDictionary<string, InvocationHandlerList>(StringComparer.Ordinal);

    // Holds all mutable state other than user-defined handlers and settable properties.
    private readonly ReconnectingConnectionState _state;

    private bool _disposed;

    /// <summary>
    /// Occurs when the connection is closed. The connection could be closed due to an error or due to either the server or client intentionally
    /// closing the connection without error.
    /// </summary>
    /// <remarks>
    /// If this event was triggered from a connection error, the <see cref="Exception"/> that occurred will be passed in as the
    /// sole argument to this handler. If this event was triggered intentionally by either the client or server, then
    /// the argument will be <see langword="null"/>.
    /// </remarks>
    /// <example>
    /// The following example attaches a handler to the <see cref="Closed"/> event, and checks the provided argument to determine
    /// if there was an error:
    ///
    /// <code>
    /// connection.Closed += (exception) =>
    /// {
    ///     if (exception == null)
    ///     {
    ///         Console.WriteLine("Connection closed without error.");
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine($"Connection closed due to an error: {exception}");
    ///     }
    /// };
    /// </code>
    /// </example>
    public event Func<Exception?, Task>? Closed;

    /// <summary>
    /// Occurs when the <see cref="HubConnection"/> starts reconnecting after losing its underlying connection.
    /// </summary>
    /// <remarks>
    /// The <see cref="Exception"/> that occurred will be passed in as the sole argument to this handler.
    /// </remarks>
    /// <example>
    /// The following example attaches a handler to the <see cref="Reconnecting"/> event, and checks the provided argument to log the error.
    ///
    /// <code>
    /// connection.Reconnecting += (exception) =>
    /// {
    ///     Console.WriteLine($"Connection started reconnecting due to an error: {exception}");
    /// };
    /// </code>
    /// </example>
    public event Func<Exception?, Task>? Reconnecting;

    /// <summary>
    /// Occurs when the <see cref="HubConnection"/> successfully reconnects after losing its underlying connection.
    /// </summary>
    /// <remarks>
    /// The <see cref="string"/> parameter will be the <see cref="HubConnection"/>'s new ConnectionId or null if negotiation was skipped.
    /// </remarks>
    /// <example>
    /// The following example attaches a handler to the <see cref="Reconnected"/> event, and checks the provided argument to log the ConnectionId.
    ///
    /// <code>
    /// connection.Reconnected += (connectionId) =>
    /// {
    ///     Console.WriteLine($"Connection successfully reconnected. The ConnectionId is now: {connectionId}");
    /// };
    /// </code>
    /// </example>
    public event Func<string?, Task>? Reconnected;

    // internal for testing purposes
    internal TimeSpan TickRate { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the server timeout interval for the connection.
    /// </summary>
    /// <remarks>
    /// The client times out if it hasn't heard from the server for `this` long.
    /// </remarks>
    public TimeSpan ServerTimeout { get; set; }

    /// <summary>
    /// Gets or sets the interval at which the client sends ping messages.
    /// </summary>
    /// <remarks>
    /// Sending any message resets the timer to the start of the interval.
    /// </remarks>
    public TimeSpan KeepAliveInterval { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the initial handshake.
    /// </summary>
    public TimeSpan HandshakeTimeout { get; set; } = DefaultHandshakeTimeout;

    /// <summary>
    /// Gets the connection's current Id. This value will be cleared when the connection is stopped and will have a new value every time the connection is (re)established.
    /// This value will be null if the negotiation step is skipped via HttpConnectionOptions or if the WebSockets transport is explicitly specified because the
    /// client skips negotiation in that case as well.
    /// </summary>
    public string? ConnectionId => _state.CurrentConnectionStateUnsynchronized?.Connection.ConnectionId;

    /// <summary>
    /// Indicates the state of the <see cref="HubConnection"/> to the server.
    /// </summary>
    public HubConnectionState State => _state.OverallState;

    /// <summary>
    /// Initializes a new instance of the <see cref="HubConnection"/> class.
    /// </summary>
    /// <param name="connectionFactory">The <see cref="IConnectionFactory" /> used to create a connection each time <see cref="StartAsync" /> is called.</param>
    /// <param name="protocol">The <see cref="IHubProtocol" /> used by the connection.</param>
    /// <param name="endPoint">The <see cref="EndPoint"/> to connect to.</param>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> containing the services provided to this <see cref="HubConnection"/> instance.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="reconnectPolicy">
    /// The <see cref="IRetryPolicy"/> that controls the timing and number of reconnect attempts.
    /// The <see cref="HubConnection"/> will not reconnect if the <paramref name="reconnectPolicy"/> is null.
    /// </param>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> used to initialize the connection will be disposed when the connection is disposed.
    /// </remarks>
    public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, EndPoint endPoint, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IRetryPolicy reconnectPolicy)
        : this(connectionFactory, protocol, endPoint, serviceProvider, loggerFactory)
    {
        _reconnectPolicy = reconnectPolicy;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HubConnection"/> class.
    /// </summary>
    /// <param name="connectionFactory">The <see cref="IConnectionFactory" /> used to create a connection each time <see cref="StartAsync" /> is called.</param>
    /// <param name="protocol">The <see cref="IHubProtocol" /> used by the connection.</param>
    /// <param name="endPoint">The <see cref="EndPoint"/> to connect to.</param>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> containing the services provided to this <see cref="HubConnection"/> instance.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> used to initialize the connection will be disposed when the connection is disposed.
    /// </remarks>
    public HubConnection(IConnectionFactory connectionFactory,
                         IHubProtocol protocol,
                         EndPoint endPoint,
                         IServiceProvider serviceProvider,
                         ILoggerFactory loggerFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger(typeof(HubConnection));
        _state = new ReconnectingConnectionState(_logger);

        _logScope = new ConnectionLogScope();

        var options = serviceProvider.GetService<IOptions<HubConnectionOptions>>();

        ServerTimeout = options?.Value.ServerTimeout ?? DefaultServerTimeout;

        KeepAliveInterval = options?.Value.KeepAliveInterval ?? DefaultKeepAliveInterval;
    }

    /// <summary>
    /// Starts a connection to the server.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous start.</returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        using (_logger.BeginScope(_logScope))
        {
            await StartAsyncInner(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task StartAsyncInner(CancellationToken cancellationToken = default)
    {
        await _state.WaitConnectionLockAsync(token: cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_state.TryChangeState(HubConnectionState.Disconnected, HubConnectionState.Connecting))
            {
                throw new InvalidOperationException($"The {nameof(HubConnection)} cannot be started if it is not in the {nameof(HubConnectionState.Disconnected)} state.");
            }

            // The StopCts is canceled at the start of StopAsync should be reset every time the connection finishes stopping.
            // If this token is currently canceled, it means that StartAsync was called while StopAsync was still running.
            if (_state.StopCts.Token.IsCancellationRequested)
            {
                throw new InvalidOperationException($"The {nameof(HubConnection)} cannot be started while {nameof(StopAsync)} is running.");
            }

            using (CancellationTokenUtils.CreateLinkedToken(cancellationToken, _state.StopCts.Token, out var linkedToken))
            {
                await StartAsyncCore(linkedToken).ConfigureAwait(false);
            }

            _state.ChangeState(HubConnectionState.Connecting, HubConnectionState.Connected);
        }
        catch
        {
            if (_state.TryChangeState(HubConnectionState.Connecting, HubConnectionState.Disconnected))
            {
                _state.StopCts = new CancellationTokenSource();
            }

            throw;
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }
    }

    /// <summary>
    /// Stops a connection to the server.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous stop.</returns>
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        using (_logger.BeginScope(_logScope))
        {
            await StopAsyncCore(disposing: false).ConfigureAwait(false);
        }
    }

    // Current plan for IAsyncDisposable is that DisposeAsync will NOT take a CancellationToken
    // https://github.com/dotnet/csharplang/blob/195efa07806284d7b57550e7447dc8bd39c156bf/proposals/async-streams.md#iasyncdisposable
    /// <summary>
    /// Disposes the <see cref="HubConnection"/>.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            using (_logger.BeginScope(_logScope))
            {
                await StopAsyncCore(disposing: true).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="parameterTypes">The parameters types expected by the hub method.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <param name="state">A state object that will be passed to the handler.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    /// <remarks>
    /// This is a low level method for registering a handler. Using an <see cref="HubConnectionExtensions"/> <c>On</c> extension method is recommended.
    /// </remarks>
    public virtual IDisposable On(string methodName, Type[] parameterTypes, Func<object?[], object, Task<object?>> handler, object state)
    {
        Log.RegisteringHandler(_logger, methodName);

        CheckDisposed();

        // It's OK to be disposed while registering a callback, we'll just never call the callback anyway (as with all the callbacks registered before disposal).
        var invocationHandler = new InvocationHandler(parameterTypes, handler, state);
        var invocationList = _handlers.AddOrUpdate(methodName, _ => new InvocationHandlerList(invocationHandler),
            (_, invocations) =>
            {
                lock (invocations)
                {
                    invocations.Add(methodName, invocationHandler);
                }
                return invocations;
            });

        return new Subscription(invocationHandler, invocationList);
    }

    // If the registered callback blocks it can cause the client to stop receiving messages. If you need to block, get off the current thread first.
    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="parameterTypes">The parameters types expected by the hub method.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <param name="state">A state object that will be passed to the handler.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    /// <remarks>
    /// This is a low level method for registering a handler. Using an <see cref="HubConnectionExtensions"/> <c>On</c> extension method is recommended.
    /// </remarks>
    public virtual IDisposable On(string methodName, Type[] parameterTypes, Func<object?[], object, Task> handler, object state)
    {
        Log.RegisteringHandler(_logger, methodName);

        CheckDisposed();

        // It's OK to be disposed while registering a callback, we'll just never call the callback anyway (as with all the callbacks registered before disposal).
        var invocationHandler = new InvocationHandler(parameterTypes, handler, state);
        var invocationList = _handlers.AddOrUpdate(methodName, _ => new InvocationHandlerList(invocationHandler),
            (_, invocations) =>
            {
                lock (invocations)
                {
                    invocations.Add(methodName, invocationHandler);
                }
                return invocations;
            });

        return new Subscription(invocationHandler, invocationList);
    }

    /// <summary>
    /// Removes all handlers associated with the method with the specified method name.
    /// </summary>
    /// <param name="methodName">The name of the hub method from which handlers are being removed</param>
    public virtual void Remove(string methodName)
    {
        CheckDisposed();
        Log.RemovingHandlers(_logger, methodName);
        _handlers.TryRemove(methodName, out _);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="returnType">The return type of the server method.</param>
    /// <param name="args">The arguments used to invoke the server method.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    /// <remarks>
    /// This is a low level method for invoking a streaming hub method on the server. Using an <see cref="HubConnectionExtensions"/> <c>StreamAsChannelAsync</c> extension method is recommended.
    /// </remarks>
    public virtual async Task<ChannelReader<object?>> StreamAsChannelCoreAsync(string methodName, Type returnType, object?[] args, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(_logScope))
        {
            return await StreamAsChannelCoreAsyncCore(methodName, returnType, args, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="returnType">The return type of the server method.</param>
    /// <param name="args">The arguments used to invoke the server method.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns an <see cref="object"/> for the hub method return value.
    /// </returns>
    /// <remarks>
    /// This is a low level method for invoking a hub method on the server. Using an <see cref="HubConnectionExtensions"/> <c>InvokeAsync</c> extension method is recommended.
    /// </remarks>
    public virtual async Task<object?> InvokeCoreAsync(string methodName, Type returnType, object?[] args, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(_logScope))
        {
            return await InvokeCoreAsyncCore(methodName, returnType, args, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments used to invoke the server method.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
    /// <remarks>
    /// This is a low level method for invoking a hub method on the server. Using an <see cref="HubConnectionExtensions"/> <c>SendAsync</c> extension method is recommended.
    /// </remarks>
    public virtual async Task SendCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(_logScope))
        {
            await SendCoreAsyncCore(methodName, args, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task StartAsyncCore(CancellationToken cancellationToken)
    {
        _state.AssertInConnectionLock();
        SafeAssert(_state.CurrentConnectionStateUnsynchronized == null, "We already have a connection!");

        cancellationToken.ThrowIfCancellationRequested();

        CheckDisposed();

        Log.Starting(_logger);

        // Start the connection
        var connection = await _connectionFactory.ConnectAsync(_endPoint, cancellationToken).ConfigureAwait(false);
        var startingConnectionState = new ConnectionState(connection, this);

#pragma warning disable CA2252 // This API requires opting into preview features
        var statefulReconnectFeature = connection.Features.Get<IStatefulReconnectFeature>();
#pragma warning restore CA2252 // This API requires opting into preview features

        // From here on, if an error occurs we need to shut down the connection because
        // we still own it.
        try
        {
            var usedProtocolVersion = _protocol.Version;
            if (statefulReconnectFeature is null && _protocol.IsVersionSupported(1))
            {
                // Stateful Reconnect starts with HubProtocol version 2, newer clients connecting to older servers will fail to connect due to
                // the handshake only supporting version 1, so we will try to send version 1 during the handshake to keep old servers working
                // if the client is not attempting to enable stateful reconnect and therefore does not require a newer HubProtocol.
                usedProtocolVersion = 1;
            }
            else if (_protocol.Version < 2)
            {
                if (statefulReconnectFeature is not null)
                {
                    Log.DisablingReconnect(_logger, _protocol.Name, _protocol.Version);
#pragma warning disable CA2252 // This API requires opting into preview features
                    statefulReconnectFeature.DisableReconnect();
#pragma warning restore CA2252 // This API requires opting into preview features
                }
            }

            Log.HubProtocol(_logger, _protocol.Name, usedProtocolVersion);
            await HandshakeAsync(startingConnectionState, usedProtocolVersion, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.ErrorStartingConnection(_logger, ex);

            startingConnectionState.Cleanup();

            // Can't have any invocations to cancel, we're in the lock.
            await CloseAsync(startingConnectionState.Connection).ConfigureAwait(false);
            throw;
        }

        // Set this at the end to avoid setting internal state until the connection is real
        _state.CurrentConnectionStateUnsynchronized = startingConnectionState;
        // Tell the server we intend to ping.
        // Old clients never ping, and shouldn't be timed out, so ping to tell the server that we should be timed out if we stop.
        // StartAsyncCore is invoked and awaited by StartAsyncInner and ReconnectAsync with the connection lock still acquired.
        if (!(connection.Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive ?? false))
        {
            await SendHubMessage(startingConnectionState, PingMessage.Instance, cancellationToken).ConfigureAwait(false);
        }
        startingConnectionState.ReceiveTask = ReceiveLoop(startingConnectionState);

        Log.Started(_logger);
    }

    private static ValueTask CloseAsync(ConnectionContext connection)
    {
        return connection.DisposeAsync();
    }

    // This method does both Dispose and Start, the 'disposing' flag indicates which.
    // The behaviors are nearly identical, except that the _disposed flag is set in the lock
    // if we're disposing.
    private async Task StopAsyncCore(bool disposing)
    {
        // StartAsync acquires the connection lock for the duration of the handshake.
        // ReconnectAsync also acquires the connection lock for reconnect attempts and handshakes.
        // Cancel the StopCts without acquiring the lock so we can short-circuit it.
        _state.StopCts.Cancel();

        // Potentially wait for StartAsync to finish, and block a new StartAsync from
        // starting until we've finished stopping.
        await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);

        // Ensure that ReconnectingState.ReconnectTask is not accessed outside of the lock.
        var reconnectTask = _state.ReconnectTask;

        if (reconnectTask.Status != TaskStatus.RanToCompletion)
        {
            // Let the current reconnect attempts finish if necessary without the lock.
            // Otherwise, ReconnectAsync will stall forever acquiring the lock.
            // It should never throw, even if the reconnect attempts fail.
            // The StopCts should prevent the HubConnection from restarting until it is reset.
            _state.ReleaseConnectionLock();
            await reconnectTask.ConfigureAwait(false);
            await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);
        }

        ConnectionState? connectionState;

        var connectionStateStopTask = Task.CompletedTask;

        try
        {
            if (disposing && _disposed)
            {
                // DisposeAsync should be idempotent.
                return;
            }

            CheckDisposed();
            connectionState = _state.CurrentConnectionStateUnsynchronized;

            // Set the stopping flag so that any invocations after this get a useful error message instead of
            // silently failing or throwing an error about the pipe being completed.
            if (connectionState != null)
            {
                connectionState.Stopping = true;
                // Try to send CloseMessage
                var writeTask = SendHubMessage(connectionState, CloseMessage.Empty);
                if (writeTask.IsFaulted || writeTask.IsCanceled || !writeTask.IsCompleted)
                {
                    // Ignore exception from write, this is a best effort attempt to let the server know the client closed gracefully.
                    // We are already closing the connection via an explicit StopAsync call from the user so don't care about any potential
                    // errors that might happen.
                    _ = writeTask.ContinueWith(
                        static t => _ = t.Exception,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
                }

#pragma warning disable CA2252 // This API requires opting into preview features
                if (connectionState.Connection.Features.Get<IStatefulReconnectFeature>() is IStatefulReconnectFeature feature)
                {
                    feature.DisableReconnect();
                }
#pragma warning restore CA2252 // This API requires opting into preview features
            }
            else
            {
                // Reset StopCts if there isn't an active connection so that the next StartAsync wont immediately fail due to the token being canceled
                _state.StopCts = new CancellationTokenSource();
            }

            if (disposing)
            {
                // Must set this before calling DisposeAsync because the service provider has a reference to the HubConnection and will try to dispose it again
                _disposed = true;
                if (_serviceProvider is IAsyncDisposable asyncDispose)
                {
                    await asyncDispose.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    (_serviceProvider as IDisposable)?.Dispose();
                }
            }

            if (connectionState != null)
            {
                // Start Stop inside the lock so a closure from the transport side at the same time as this doesn't cause an ODE
                // But don't await the call in the lock as that could deadlock with HandleConnectionClose in the ReceiveLoop
                connectionStateStopTask = connectionState.StopAsync();
            }
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }

        await connectionStateStopTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments used to invoke the server method.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="IAsyncEnumerable{TResult}"/> that represents the stream.
    /// </returns>
    public virtual IAsyncEnumerable<TResult> StreamAsyncCore<TResult>(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        var cts = cancellationToken.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : new CancellationTokenSource();
        var stream = CastIAsyncEnumerable<TResult>(methodName, args, cts);
        var cancelableStream = AsyncEnumerableAdapters.MakeCancelableTypedAsyncEnumerable(stream, cts);
        return cancelableStream;
    }

    private async IAsyncEnumerable<T> CastIAsyncEnumerable<T>(string methodName, object?[] args, CancellationTokenSource cts)
    {
        var reader = await StreamAsChannelCoreAsync(methodName, typeof(T), args, cts.Token).ConfigureAwait(false);

        try
        {
            while (await reader.WaitToReadAsync(cts.Token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    yield return (T)item!;
                }
            }
        }
        finally
        {
            // Needed to avoid UnobservedTaskExceptions
            _ = reader.Completion.Exception;
        }
    }

    private async Task<ChannelReader<object?>> StreamAsChannelCoreAsyncCore(string methodName, Type returnType, object?[] args, CancellationToken cancellationToken)
    {
        async Task OnStreamCanceled(InvocationRequest irq)
        {
            // We need to take the connection lock in order to ensure we a) have a connection and b) are the only one accessing the write end of the pipe.
            await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);
            try
            {
                if (_state.CurrentConnectionStateUnsynchronized != null)
                {
                    Log.SendingCancellation(_logger, irq.InvocationId);

                    // Don't pass irq.CancellationToken, that would result in canceling the Flush and a delayed CancelInvocationMessage being sent.
                    await SendHubMessage(_state.CurrentConnectionStateUnsynchronized, new CancelInvocationMessage(irq.InvocationId), cancellationToken: default).ConfigureAwait(false);
                }
                else
                {
                    Log.UnableToSendCancellation(_logger, irq.InvocationId);
                }
            }
            catch
            {
                // Connection closed while trying to cancel a stream. This is fine to ignore.
            }
            finally
            {
                _state.ReleaseConnectionLock();
            }

            // Cancel the invocation
            irq.Dispose();
        }

        var readers = default(Dictionary<string, object>);

        CheckDisposed();
        var connectionState = await _state.WaitForActiveConnectionAsync(nameof(StreamAsChannelCoreAsync), token: cancellationToken).ConfigureAwait(false);

        ChannelReader<object?> channel;
        try
        {
            CheckDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            readers = PackageStreamingParams(connectionState, ref args, out var streamIds);

            // I just want an excuse to use 'irq' as a variable name...
            var irq = InvocationRequest.Stream(cancellationToken, returnType, connectionState.GetNextId(), _loggerFactory, this, out channel);
            await InvokeStreamCore(connectionState, methodName, irq, args, streamIds?.ToArray(), cancellationToken).ConfigureAwait(false);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(state => _ = OnStreamCanceled((InvocationRequest)state!), irq);
            }

            LaunchStreams(connectionState, readers, cancellationToken);
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }

        return channel;
    }

    private Dictionary<string, object>? PackageStreamingParams(ConnectionState connectionState, ref object?[] args, out List<string>? streamIds)
    {
        Dictionary<string, object>? readers = null;
        streamIds = null;
        var newArgsCount = args.Length;
        const int MaxStackSize = 256;
        Span<bool> isStreaming = args.Length <= MaxStackSize
            ? stackalloc bool[MaxStackSize].Slice(0, args.Length)
            : new bool[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is not null && ReflectionHelper.IsStreamingType(arg.GetType()))
            {
                isStreaming[i] = true;
                newArgsCount--;

                if (readers is null)
                {
                    readers = new Dictionary<string, object>();
                }
                if (streamIds is null)
                {
                    streamIds = new List<string>();
                }

                var id = connectionState.GetNextId();
                readers[id] = arg;
                streamIds.Add(id);

                Log.StartingStream(_logger, id);
            }
        }

        if (newArgsCount == args.Length)
        {
            return null;
        }

        var newArgs = newArgsCount > 0
            ? new object?[newArgsCount]
            : Array.Empty<object?>();
        int newArgsIndex = 0;

        for (var i = 0; i < args.Length; i++)
        {
            if (!isStreaming[i])
            {
                newArgs[newArgsIndex] = args[i];
                newArgsIndex++;
            }
        }

        args = newArgs;
        return readers;
    }

    private void LaunchStreams(ConnectionState connectionState, Dictionary<string, object>? readers, CancellationToken cancellationToken)
    {
        if (readers == null)
        {
            // if there were no streaming parameters then readers is never initialized
            return;
        }

        _state.AssertInConnectionLock();
        // It's safe to access connectionState.UploadStreamToken as we still have the connection lock
        var cts = CancellationTokenSource.CreateLinkedTokenSource(connectionState.UploadStreamToken, cancellationToken);

        foreach (var kvp in readers)
        {
            var reader = kvp.Value;

            // For each stream that needs to be sent, run a "send items" task in the background.
            // This reads from the channel, attaches streamId, and sends to server.
            // A single background thread here quickly gets messy.
            if (ReflectionHelper.GetIAsyncEnumerableInterface(reader.GetType()) is { } asyncEnumerableType)
            {
                InvokeStreamMethod(
                    _sendIAsyncStreamItemsMethod,
                    asyncEnumerableType.GetGenericArguments(),
                    connectionState,
                    kvp.Key.ToString(),
                    reader,
                    cts);
                continue;
            }

            if (ReflectionHelper.TryGetStreamType(reader.GetType(), out var channelGenericType))
            {
                InvokeStreamMethod(
                    _sendStreamItemsMethod,
                    [channelGenericType],
                    connectionState,
                    kvp.Key.ToString(),
                    reader,
                    cts);
                continue;
            }

            // Should never get here, we should have already verified the stream types when the user initially calls send/invoke
            throw new InvalidOperationException($"{reader.GetType()} is not a {typeof(ChannelReader<>).Name}.");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2060:MakeGenericMethod",
        Justification = "The methods passed into here (SendStreamItems and SendIAsyncEnumerableStreamItems) don't have trimming annotations.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "ValueTypes are handled without using MakeGenericMethod.")]
    private void InvokeStreamMethod(MethodInfo methodInfo, Type[] genericTypes, ConnectionState connectionState, string streamId, object reader, CancellationTokenSource tokenSource)
    {
        Debug.Assert(genericTypes.Length == 1);
#if NET6_0_OR_GREATER
        if (!RuntimeFeature.IsDynamicCodeSupported && genericTypes[0].IsValueType)
        {
            _ = ReflectionSendStreamItems(methodInfo, connectionState, streamId, reader, tokenSource);
        }
        else
#endif
        {
            _ = methodInfo
                .MakeGenericMethod(genericTypes)
                .Invoke(this, [connectionState, streamId, reader, tokenSource]);
        }
    }

#if NET6_0_OR_GREATER

    /// <summary>
    /// Uses reflection to read items from an IAsyncEnumerable{T} or ChannelReader{T} and send them to the server.
    ///
    /// Used when the runtime does not support dynamic code generation (ex. native AOT) and the generic type is a value type. In this scenario,
    /// we cannot use MakeGenericMethod to call the appropriate SendStreamItems method because the generic type is a value type.
    /// </summary>
    private Task ReflectionSendStreamItems(MethodInfo methodInfo, ConnectionState connectionState, string streamId, object reader, CancellationTokenSource tokenSource)
    {
        async Task ReadAsyncEnumeratorStream(IAsyncEnumerator<object?> enumerator)
        {
            try
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    await SendStreamItemAsync(connectionState, streamId, enumerator.Current, tokenSource).ConfigureAwait(false);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }

        Func<Task> createAndConsumeStream;
        if (methodInfo == _sendStreamItemsMethod)
        {
            // reader is a ChannelReader<T>
            createAndConsumeStream = () => ReadAsyncEnumeratorStream(AsyncEnumerableAdapters.MakeReflectionAsyncEnumeratorFromChannel(reader, tokenSource.Token));
        }
        else
        {
            // reader is an IAsyncEnumerable<T>
            Debug.Assert(methodInfo == _sendIAsyncStreamItemsMethod);

            createAndConsumeStream = () => ReadAsyncEnumeratorStream(AsyncEnumerableAdapters.MakeReflectionAsyncEnumerator(reader, tokenSource.Token));
        }

        return CommonStreaming(connectionState, streamId, createAndConsumeStream, tokenSource);
    }
#endif

    // this is called via reflection using the `_sendStreamItemsMethod` field
    private Task SendStreamItems<T>(ConnectionState connectionState, string streamId, ChannelReader<T> reader, CancellationTokenSource tokenSource)
    {
        async Task ReadChannelStream()
        {
            while (await reader.WaitToReadAsync(tokenSource.Token).ConfigureAwait(false))
            {
                while (!tokenSource.Token.IsCancellationRequested && reader.TryRead(out var item))
                {
                    await SendStreamItemAsync(connectionState, streamId, item, tokenSource).ConfigureAwait(false);
                }
            }
        }

        return CommonStreaming(connectionState, streamId, ReadChannelStream, tokenSource);
    }

    // this is called via reflection using the `_sendIAsyncStreamItemsMethod` field
    private Task SendIAsyncEnumerableStreamItems<T>(ConnectionState connectionState, string streamId, IAsyncEnumerable<T> stream, CancellationTokenSource tokenSource)
    {
        async Task ReadAsyncEnumerableStream()
        {
            await foreach (var streamValue in stream.WithCancellation(tokenSource.Token).ConfigureAwait(false))
            {
                await SendStreamItemAsync(connectionState, streamId, streamValue, tokenSource).ConfigureAwait(false);
            }
        }

        return CommonStreaming(connectionState, streamId, ReadAsyncEnumerableStream, tokenSource);
    }

    private async Task SendStreamItemAsync(ConnectionState connectionState, string streamId, object? item, CancellationTokenSource tokenSource)
    {
        await SendWithLock(connectionState, new StreamItemMessage(streamId, item), tokenSource.Token).ConfigureAwait(false);
        Log.SendingStreamItem(_logger, streamId);
    }

    private async Task CommonStreaming(ConnectionState connectionState, string streamId, Func<Task> createAndConsumeStream, CancellationTokenSource cts)
    {
        // make sure we dispose the CTS created by StreamAsyncCore once streaming completes
        using var _ = cts;

        Log.StartingStream(_logger, streamId);
        string? responseError = null;
        try
        {
            await createAndConsumeStream().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Log.CancelingStream(_logger, streamId);
            responseError = "Stream canceled by client.";
        }
        catch (Exception ex)
        {
            Log.ErroredStream(_logger, streamId, ex);
            responseError = $"Stream errored by client: '{ex}'";
        }

        // Don't use cancellation token here
        // this is triggered by a cancellation token to tell the server that the client is done streaming
        await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);
        try
        {
            // Avoid sending when the connection isn't active, likely happens if there is an active stream when the connection closes
            if (_state.IsConnectionActive())
            {
                Log.CompletingStream(_logger, streamId);
                await SendHubMessage(connectionState, CompletionMessage.WithError(streamId, responseError), cancellationToken: default).ConfigureAwait(false);
            }
            else
            {
                Log.CompletingStreamNotSent(_logger, streamId);
            }
        }
        catch (Exception ex)
        {
            Log.ErrorSendingStreamCompletion(_logger, streamId, ex);
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }
    }

    private async Task<object?> InvokeCoreAsyncCore(string methodName, Type returnType, object?[] args, CancellationToken cancellationToken)
    {
        var readers = default(Dictionary<string, object>);

        CheckDisposed();
        var connectionState = await _state.WaitForActiveConnectionAsync(nameof(InvokeCoreAsync), token: cancellationToken).ConfigureAwait(false);

        Task<object?> invocationTask;
        try
        {
            CheckDisposed();

            readers = PackageStreamingParams(connectionState, ref args, out var streamIds);

            var irq = InvocationRequest.Invoke(cancellationToken, returnType, connectionState.GetNextId(), _loggerFactory, this, out invocationTask);
            await InvokeCore(connectionState, methodName, irq, args, streamIds?.ToArray(), cancellationToken).ConfigureAwait(false);

            LaunchStreams(connectionState, readers, cancellationToken);
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }

        // Wait for this outside the lock, because it won't complete until the server responds
        return await invocationTask.ConfigureAwait(false);
    }

    private async Task InvokeCore(ConnectionState connectionState, string methodName, InvocationRequest irq, object?[] args, string[]? streams, CancellationToken cancellationToken)
    {
        Log.PreparingBlockingInvocation(_logger, irq.InvocationId, methodName, irq.ResultType.FullName!, args.Length);

        // Client invocations are always blocking
        var invocationMessage = new InvocationMessage(irq.InvocationId, methodName, args, streams);

        Log.RegisteringInvocation(_logger, irq.InvocationId);
        connectionState.AddInvocation(irq);

        // Trace the full invocation
        Log.IssuingInvocation(_logger, irq.InvocationId, irq.ResultType.FullName!, methodName, args);

        try
        {
            await SendHubMessage(connectionState, invocationMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.FailedToSendInvocation(_logger, irq.InvocationId, ex);
            connectionState.TryRemoveInvocation(irq.InvocationId, out _);
            irq.Fail(ex);
        }
    }

    private async Task InvokeStreamCore(ConnectionState connectionState, string methodName, InvocationRequest irq, object?[] args, string[]? streams, CancellationToken cancellationToken)
    {
        _state.AssertConnectionValid();

        Log.PreparingStreamingInvocation(_logger, irq.InvocationId, methodName, irq.ResultType.FullName!, args.Length);

        var invocationMessage = new StreamInvocationMessage(irq.InvocationId, methodName, args, streams);

        Log.RegisteringInvocation(_logger, irq.InvocationId);

        connectionState.AddInvocation(irq);

        // Trace the full invocation
        Log.IssuingInvocation(_logger, irq.InvocationId, irq.ResultType.FullName!, methodName, args);

        try
        {
            await SendHubMessage(connectionState, invocationMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.FailedToSendInvocation(_logger, irq.InvocationId, ex);
            connectionState.TryRemoveInvocation(irq.InvocationId, out _);
            irq.Fail(ex);
        }
    }

    private async Task SendHubMessage(ConnectionState connectionState, HubMessage hubMessage, CancellationToken cancellationToken = default)
    {
        _state.AssertConnectionValid();

        Log.SendingMessage(_logger, hubMessage);

        if (connectionState.UsingAcks())
        {
            await connectionState.WriteAsync(hubMessage, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _protocol.WriteMessage(hubMessage, connectionState.Connection.Transport.Output);

            await connectionState.Connection.Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        Log.MessageSent(_logger, hubMessage);

        // We've sent a message, so don't ping for a while
        connectionState.ResetSendPing();
    }

    private async Task SendCoreAsyncCore(string methodName, object?[] args, CancellationToken cancellationToken)
    {
        var readers = default(Dictionary<string, object>);

        CheckDisposed();
        var connectionState = await _state.WaitForActiveConnectionAsync(nameof(SendCoreAsync), token: cancellationToken).ConfigureAwait(false);
        try
        {
            CheckDisposed();

            readers = PackageStreamingParams(connectionState, ref args, out var streamIds);

            Log.PreparingNonBlockingInvocation(_logger, methodName, args.Length);
            var invocationMessage = new InvocationMessage(null, methodName, args, streamIds?.ToArray());
            await SendHubMessage(connectionState, invocationMessage, cancellationToken).ConfigureAwait(false);

            LaunchStreams(connectionState, readers, cancellationToken);
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }
    }

    private async Task SendWithLock(ConnectionState expectedConnectionState, HubMessage message, CancellationToken cancellationToken, [CallerMemberName] string callerName = "")
    {
        CheckDisposed();
        var connectionState = await _state.WaitForActiveConnectionAsync(callerName, token: cancellationToken).ConfigureAwait(false);
        try
        {
            CheckDisposed();

            SafeAssert(ReferenceEquals(expectedConnectionState, connectionState), "The connection state changed unexpectedly!");

            await SendHubMessage(connectionState, message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }
    }

    private async Task<CloseMessage?> ProcessMessagesAsync(HubMessage message, ConnectionState connectionState, ChannelWriter<InvocationMessage> invocationMessageWriter)
    {
        Log.ResettingKeepAliveTimer(_logger);
        connectionState.ResetTimeout();

        if (!connectionState.ShouldProcessMessage(message))
        {
            return null;
        }

        InvocationRequest? irq;
        switch (message)
        {
            case InvocationBindingFailureMessage bindingFailure:
                // The server can't receive a response, so we just drop the message and log
                // REVIEW: Is this the right approach?
                Log.ArgumentBindingFailure(_logger, bindingFailure.InvocationId, bindingFailure.Target, bindingFailure.BindingFailure.SourceException);

                if (!string.IsNullOrEmpty(bindingFailure.InvocationId))
                {
                    await SendWithLock(connectionState, CompletionMessage.WithError(bindingFailure.InvocationId, "Client failed to parse argument(s)."), cancellationToken: default).ConfigureAwait(false);
                }
                break;
            case InvocationMessage invocation:
                Log.ReceivedInvocation(_logger, invocation.InvocationId, invocation.Target, invocation.Arguments);
                await invocationMessageWriter.WriteAsync(invocation).ConfigureAwait(false);
                break;
            case CompletionMessage completion:
                if (!connectionState.TryRemoveInvocation(completion.InvocationId!, out irq))
                {
                    Log.DroppedCompletionMessage(_logger, completion.InvocationId!);
                    break;
                }

                DispatchInvocationCompletion(completion, irq);
                irq.Dispose();

                break;
            case StreamItemMessage streamItem:
                // if there's no open StreamInvocation with the given id, then complete with an error
                if (!connectionState.TryGetInvocation(streamItem.InvocationId!, out irq))
                {
                    Log.DroppedStreamMessage(_logger, streamItem.InvocationId!);
                    break;
                }
                await DispatchInvocationStreamItemAsync(streamItem, irq).ConfigureAwait(false);
                break;
            case CloseMessage close:
                if (string.IsNullOrEmpty(close.Error))
                {
                    Log.ReceivedClose(_logger);
                }
                else
                {
                    Log.ReceivedCloseWithError(_logger, close.Error);
                }

#pragma warning disable CA2252 // This API requires opting into preview features
                if (connectionState.Connection.Features.Get<IStatefulReconnectFeature>() is IStatefulReconnectFeature feature)
                {
                    feature.DisableReconnect();
                }
#pragma warning restore CA2252 // This API requires opting into preview features

                return close;
            case PingMessage _:
                Log.ReceivedPing(_logger);
                // timeout is reset above, on receiving any message
                break;
            case AckMessage ackMessage:
                Log.ReceivedAckMessage(_logger, ackMessage.SequenceId);
                await connectionState.AckAsync(ackMessage).ConfigureAwait(false);
                break;
            case SequenceMessage sequenceMessage:
                Log.ReceivedSequenceMessage(_logger, sequenceMessage.SequenceId);
                break;
            default:
                throw new InvalidOperationException($"Unexpected message type: {message.GetType().FullName}");
        }

        return null;
    }

    private async Task DispatchInvocationAsync(InvocationMessage invocation, ConnectionState connectionState)
    {
        var expectsResult = !string.IsNullOrEmpty(invocation.InvocationId);
        // Find the handler
        if (!_handlers.TryGetValue(invocation.Target, out var invocationHandlerList))
        {
            if (expectsResult)
            {
                Log.MissingResultHandler(_logger, invocation.Target);
                try
                {
                    await SendWithLock(connectionState, CompletionMessage.WithError(invocation.InvocationId!, "Client didn't provide a result."), cancellationToken: default).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.ErrorSendingInvocationResult(_logger, invocation.InvocationId!, invocation.Target, ex);
                }
            }
            else
            {
                Log.MissingHandler(_logger, invocation.Target);
            }
            return;
        }

        // Grabbing the current handlers
        var copiedHandlers = invocationHandlerList.GetHandlers();
        object? result = null;
        Exception? resultException = null;
        var hasResult = false;
        foreach (var handler in copiedHandlers)
        {
            try
            {
                var task = handler.InvokeAsync(invocation.Arguments);
                if (handler.HasResult && task is Task<object?> resultTask)
                {
                    result = await resultTask.ConfigureAwait(false);
                    hasResult = true;
                }
                else
                {
                    await task.ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorInvokingClientSideMethod(_logger, invocation.Target, ex);
                if (handler.HasResult)
                {
                    resultException = ex;
                }
            }
        }

        if (expectsResult)
        {
            try
            {
                if (resultException is not null)
                {
                    await SendWithLock(connectionState, CompletionMessage.WithError(invocation.InvocationId!, resultException.Message), cancellationToken: default).ConfigureAwait(false);
                }
                else if (hasResult)
                {
                    await SendWithLock(connectionState, CompletionMessage.WithResult(invocation.InvocationId!, result), cancellationToken: default).ConfigureAwait(false);
                }
                else
                {
                    Log.MissingResultHandler(_logger, invocation.Target);
                    await SendWithLock(connectionState, CompletionMessage.WithError(invocation.InvocationId!, "Client didn't provide a result."), cancellationToken: default).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorSendingInvocationResult(_logger, invocation.InvocationId!, invocation.Target, ex);
            }
        }
        else if (hasResult)
        {
            Log.ResultNotExpected(_logger, invocation.Target);
        }
    }

    private async Task DispatchInvocationStreamItemAsync(StreamItemMessage streamItem, InvocationRequest irq)
    {
        Log.ReceivedStreamItem(_logger, irq.InvocationId);

        if (irq.CancellationToken.IsCancellationRequested)
        {
            Log.CancelingStreamItem(_logger, irq.InvocationId);
        }
        else if (!await irq.StreamItem(streamItem.Item).ConfigureAwait(false))
        {
            Log.ReceivedStreamItemAfterClose(_logger, irq.InvocationId);
        }
    }

    private void DispatchInvocationCompletion(CompletionMessage completion, InvocationRequest irq)
    {
        Log.ReceivedInvocationCompletion(_logger, irq.InvocationId);

        if (irq.CancellationToken.IsCancellationRequested)
        {
            Log.CancelingInvocationCompletion(_logger, irq.InvocationId);
        }
        else
        {
            irq.Complete(completion);
        }
    }

    private void CheckDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }

    private async Task HandshakeAsync(ConnectionState startingConnectionState, int protocolVersion, CancellationToken cancellationToken)
    {
        // Send the Handshake request
        Log.SendingHubHandshake(_logger);

        var handshakeRequest = new HandshakeRequestMessage(_protocol.Name, protocolVersion);
        HandshakeProtocol.WriteRequestMessage(handshakeRequest, startingConnectionState.Connection.Transport.Output);

        var sendHandshakeResult = await startingConnectionState.Connection.Transport.Output.FlushAsync(CancellationToken.None).ConfigureAwait(false);

        if (sendHandshakeResult.IsCompleted)
        {
            // The other side disconnected
            var ex = new IOException("The server disconnected before the handshake could be started.");
            Log.ErrorReceivingHandshakeResponse(_logger, ex);
            throw ex;
        }

        var input = startingConnectionState.Connection.Transport.Input;

        using var handshakeCts = new CancellationTokenSource(HandshakeTimeout);

        try
        {
            // cancellationToken already contains _state.StopCts.Token, so we don't have to link it again
            using (CancellationTokenUtils.CreateLinkedToken(cancellationToken, handshakeCts.Token, out var linkedToken))
            {
                while (true)
                {
                    var result = await input.ReadAsync(linkedToken).ConfigureAwait(false);

                    var buffer = result.Buffer;
                    var consumed = buffer.Start;
                    var examined = buffer.End;

                    try
                    {
                        // Read first message out of the incoming data
                        if (!buffer.IsEmpty)
                        {
                            if (HandshakeProtocol.TryParseResponseMessage(ref buffer, out var message))
                            {
                                // Adjust consumed and examined to point to the end of the handshake
                                // response, this handles the case where invocations are sent in the same payload
                                // as the negotiate response.
                                consumed = buffer.Start;
                                examined = consumed;

                                if (message.Error != null)
                                {
                                    Log.HandshakeServerError(_logger, message.Error);
                                    throw new HubException(
                                        $"Unable to complete handshake with the server due to an error: {message.Error}");
                                }

                                Log.HandshakeComplete(_logger);

                                break;
                            }
                        }

                        if (result.IsCompleted)
                        {
                            // Not enough data, and we won't be getting any more data.
                            throw new InvalidOperationException(
                                "The server disconnected before sending a handshake response");
                        }
                    }
                    finally
                    {
                        input.AdvanceTo(consumed, examined);
                    }
                }
            }
        }
        catch (HubException)
        {
            // This was already logged as a HandshakeServerError
            throw;
        }
        catch (InvalidDataException ex)
        {
            Log.ErrorInvalidHandshakeResponse(_logger, ex);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            if (handshakeCts.IsCancellationRequested)
            {
                Log.ErrorHandshakeTimedOut(_logger, HandshakeTimeout, ex);
            }
            else
            {
                Log.ErrorHandshakeCanceled(_logger, ex);
            }

            throw;
        }
        catch (Exception ex)
        {
            Log.ErrorReceivingHandshakeResponse(_logger, ex);
            throw;
        }
    }

    private async Task ReceiveLoop(ConnectionState connectionState)
    {
        // We hold a local capture of the connection state because StopAsync may dump out the current one.
        // We'll be locking any time we want to check back in to the "active" connection state.
        _state.AssertInConnectionLock();

        Log.ReceiveLoopStarting(_logger);

        // Performs periodic tasks -- here sending pings and checking timeout
        // Disposed with `timer.Stop()` in the finally block below
        var timer = new TimerAwaitable(TickRate, TickRate);
        var timerTask = connectionState.TimerLoop(timer);

        var uploadStreamSource = new CancellationTokenSource();
        connectionState.UploadStreamToken = uploadStreamSource.Token;
        var invocationMessageChannel = Channel.CreateUnbounded<InvocationMessage>(_receiveLoopOptions);

        // We can't safely wait for this task when closing without introducing deadlock potential when calling StopAsync in a .On method
        connectionState.InvocationMessageReceiveTask = StartProcessingInvocationMessages(invocationMessageChannel.Reader);

        async Task StartProcessingInvocationMessages(ChannelReader<InvocationMessage> invocationMessageChannelReader)
        {
            while (await invocationMessageChannelReader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (invocationMessageChannelReader.TryRead(out var invocationMessage))
                {
                    var invokeTask = DispatchInvocationAsync(invocationMessage, connectionState);
                    // If a client result is expected we shouldn't block on user code as that could potentially permanently block the application
                    // Even if it doesn't permanently block, it would be better if non-client result handlers could still be called while waiting for a result
                    // e.g. chat while waiting for user input for a turn in a game
                    if (string.IsNullOrEmpty(invocationMessage.InvocationId))
                    {
                        await invokeTask.ConfigureAwait(false);
                    }
                }
            }
        }

        var input = connectionState.Connection.Transport.Input;

        try
        {
            while (true)
            {
                var result = await input.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                try
                {
                    if (result.IsCanceled)
                    {
                        // We were canceled. Possibly because we were stopped gracefully
                        break;
                    }
                    else if (!buffer.IsEmpty)
                    {
                        Log.ProcessingMessage(_logger, buffer.Length);

                        CloseMessage? closeMessage = null;

                        while (_protocol.TryParseMessage(ref buffer, connectionState, out var message))
                        {
                            // We have data, process it
                            closeMessage = await ProcessMessagesAsync(message, connectionState, invocationMessageChannel.Writer).ConfigureAwait(false);

                            if (closeMessage != null)
                            {
                                // Closing because we got a close frame, possibly with an error in it.
                                if (closeMessage.Error != null)
                                {
                                    connectionState.CloseException = new HubException($"The server closed the connection with the following error: {closeMessage.Error}");
                                }

                                // Stopping being true indicates the client shouldn't try to reconnect even if automatic reconnects are enabled.
                                if (!closeMessage.AllowReconnect)
                                {
                                    connectionState.Stopping = true;
                                }

                                break;
                            }
                        }

                        // If we're closing stop everything
                        if (closeMessage != null)
                        {
                            break;
                        }
                    }

                    if (result.IsCompleted)
                    {
                        if (!buffer.IsEmpty)
                        {
                            throw new InvalidDataException("Connection terminated while reading a message.");
                        }
                        break;
                    }
                }
                finally
                {
                    // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                    // We mark examined as `buffer.End` so that if we didn't receive a full frame, we'll wait for more data
                    // before yielding the read again.
                    input.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }
        catch (Exception ex)
        {
            Log.ServerDisconnectedWithError(_logger, ex);
            connectionState.CloseException = ex;
        }
        finally
        {
            invocationMessageChannel.Writer.TryComplete();
            timer.Stop();
            await timerTask.ConfigureAwait(false);
            uploadStreamSource.Cancel();
            await HandleConnectionClose(connectionState).ConfigureAwait(false);
        }
    }

    // Internal for testing
    internal Task RunTimerActions()
    {
        // Don't bother acquiring the connection lock. This is only called from tests.
        return _state.CurrentConnectionStateUnsynchronized!.RunTimerActions();
    }

    // Internal for testing
    internal void OnServerTimeout()
    {
        // Don't bother acquiring the connection lock. This is only called from tests.
        _state.CurrentConnectionStateUnsynchronized!.OnServerTimeout();
    }

    private async Task HandleConnectionClose(ConnectionState connectionState)
    {
        // Clear the connectionState field
        await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);
        try
        {
            SafeAssert(ReferenceEquals(_state.CurrentConnectionStateUnsynchronized, connectionState),
                "Someone other than ReceiveLoop cleared the connection state!");
            _state.CurrentConnectionStateUnsynchronized = null;

            // Dispose the connection
            await CloseAsync(connectionState.Connection).ConfigureAwait(false);

            // Cancel any outstanding invocations within the connection lock
            connectionState.CancelOutstandingInvocations(connectionState.CloseException);
            connectionState.Cleanup();

            if (connectionState.Stopping || _reconnectPolicy == null)
            {
                if (connectionState.CloseException != null)
                {
                    Log.ShutdownWithError(_logger, connectionState.CloseException);
                }
                else
                {
                    Log.ShutdownConnection(_logger);
                }

                _state.ChangeState(HubConnectionState.Connected, HubConnectionState.Disconnected);
                CompleteClose(connectionState.CloseException);
            }
            else
            {
                _state.ReconnectTask = ReconnectAsync(connectionState.CloseException);
            }
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }
    }

    private void CompleteClose(Exception? closeException)
    {
        _state.AssertInConnectionLock();
        _state.StopCts = new CancellationTokenSource();
        RunCloseEvent(closeException);
    }

    private void RunCloseEvent(Exception? closeException)
    {
        var closed = Closed;

        async Task RunClosedEventAsync()
        {
            // Dispatch to the thread pool before we invoke the user callback
            await AwaitableThreadPool.Yield();

            try
            {
                Log.InvokingClosedEventHandler(_logger);
                await closed.Invoke(closeException).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.ErrorDuringClosedEvent(_logger, ex);
            }
        }

        // There is no need to start a new task if there is no Closed event registered
        if (closed != null)
        {
            // Fire-and-forget the closed event
            _ = RunClosedEventAsync();
        }
    }

    private async Task ReconnectAsync(Exception? closeException)
    {
        var previousReconnectAttempts = 0;
        var reconnectStartTime = DateTime.UtcNow;
        var retryReason = closeException;
        var nextRetryDelay = GetNextRetryDelay(previousReconnectAttempts, TimeSpan.Zero, retryReason);

        // We still have the connection lock from the caller, HandleConnectionClose.
        _state.AssertInConnectionLock();

        if (nextRetryDelay == null)
        {
            Log.FirstReconnectRetryDelayNull(_logger);

            _state.ChangeState(HubConnectionState.Connected, HubConnectionState.Disconnected);

            CompleteClose(closeException);
            return;
        }

        _state.ChangeState(HubConnectionState.Connected, HubConnectionState.Reconnecting);

        if (closeException != null)
        {
            Log.ReconnectingWithError(_logger, closeException);
        }
        else
        {
            Log.Reconnecting(_logger);
        }

        RunReconnectingEvent(closeException);

        while (nextRetryDelay != null)
        {
            Log.AwaitingReconnectRetryDelay(_logger, previousReconnectAttempts + 1, nextRetryDelay.Value);

            try
            {
                await Task.Delay(nextRetryDelay.Value, _state.StopCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Log.ReconnectingStoppedDuringRetryDelay(_logger);

                await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);
                try
                {
                    _state.ChangeState(HubConnectionState.Reconnecting, HubConnectionState.Disconnected);

                    CompleteClose(GetOperationCanceledException("Connection stopped during reconnect delay. Done reconnecting.", ex, _state.StopCts.Token));
                }
                finally
                {
                    _state.ReleaseConnectionLock();
                }

                return;
            }

            await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);
            try
            {
                SafeAssert(ReferenceEquals(_state.CurrentConnectionStateUnsynchronized, null),
                    "Someone other than Reconnect set the connection state!");

                await StartAsyncCore(_state.StopCts.Token).ConfigureAwait(false);

                Log.Reconnected(_logger, previousReconnectAttempts, DateTime.UtcNow - reconnectStartTime);

                _state.ChangeState(HubConnectionState.Reconnecting, HubConnectionState.Connected);

                RunReconnectedEvent();
                return;
            }
            catch (Exception ex)
            {
                retryReason = ex;

                Log.ReconnectAttemptFailed(_logger, ex);

                if (_state.StopCts.IsCancellationRequested)
                {
                    Log.ReconnectingStoppedDuringReconnectAttempt(_logger);

                    _state.ChangeState(HubConnectionState.Reconnecting, HubConnectionState.Disconnected);

                    CompleteClose(GetOperationCanceledException("Connection stopped during reconnect attempt. Done reconnecting.", ex, _state.StopCts.Token));
                    return;
                }

                previousReconnectAttempts++;
            }
            finally
            {
                _state.ReleaseConnectionLock();
            }

            nextRetryDelay = GetNextRetryDelay(previousReconnectAttempts, DateTime.UtcNow - reconnectStartTime, retryReason);
        }

        await _state.WaitConnectionLockAsync(token: default).ConfigureAwait(false);
        try
        {
            SafeAssert(ReferenceEquals(_state.CurrentConnectionStateUnsynchronized, null),
                "Someone other than Reconnect set the connection state!");

            var elapsedTime = DateTime.UtcNow - reconnectStartTime;
            Log.ReconnectAttemptsExhausted(_logger, previousReconnectAttempts, elapsedTime);

            _state.ChangeState(HubConnectionState.Reconnecting, HubConnectionState.Disconnected);

            var message = $"Reconnect retries have been exhausted after {previousReconnectAttempts} failed attempts and {elapsedTime} elapsed. Disconnecting.";
            CompleteClose(new OperationCanceledException(message));
        }
        finally
        {
            _state.ReleaseConnectionLock();
        }
    }

    private TimeSpan? GetNextRetryDelay(long previousRetryCount, TimeSpan elapsedTime, Exception? retryReason)
    {
        try
        {
            return _reconnectPolicy!.NextRetryDelay(new RetryContext
            {
                PreviousRetryCount = previousRetryCount,
                ElapsedTime = elapsedTime,
                RetryReason = retryReason,
            });
        }
        catch (Exception ex)
        {
            Log.ErrorDuringNextRetryDelay(_logger, ex);
            return null;
        }
    }

#pragma warning disable CA1822 // Avoid different signatures based on TFM
    private OperationCanceledException GetOperationCanceledException(string message, Exception innerException, CancellationToken cancellationToken)
    {
#pragma warning restore CA1822
#if NETSTANDARD2_1 || NETCOREAPP
        return new OperationCanceledException(message, innerException, _state.StopCts.Token);
#else
        return new OperationCanceledException(message, innerException);
#endif
    }

    private void RunReconnectingEvent(Exception? closeException)
    {
        var reconnecting = Reconnecting;

        async Task RunReconnectingEventAsync()
        {
            // Dispatch to the thread pool before we invoke the user callback
            await AwaitableThreadPool.Yield();

            try
            {
                await reconnecting.Invoke(closeException).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.ErrorDuringReconnectingEvent(_logger, ex);
            }
        }

        // There is no need to start a new task if there is no Reconnecting event registered
        if (reconnecting != null)
        {
            // Fire-and-forget the closed event
            _ = RunReconnectingEventAsync();
        }
    }

    private void RunReconnectedEvent()
    {
        var reconnected = Reconnected;

        async Task RunReconnectedEventAsync()
        {
            // Dispatch to the thread pool before we invoke the user callback
            await AwaitableThreadPool.Yield();

            try
            {
                await reconnected.Invoke(ConnectionId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.ErrorDuringReconnectedEvent(_logger, ex);
            }
        }

        // There is no need to start a new task if there is no Reconnected event registered
        if (reconnected != null)
        {
            // Fire-and-forget the reconnected event
            _ = RunReconnectedEventAsync();
        }
    }

    // Debug.Assert plays havoc with Unit Tests. But I want something that I can "assert" only in Debug builds.
    [Conditional("DEBUG")]
    private static void SafeAssert(bool condition, string message, [CallerMemberName] string? memberName = null, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed in {memberName}, at {fileName}:{lineNumber}: {message}");
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly InvocationHandler _handler;
        private readonly InvocationHandlerList _handlerList;

        public Subscription(InvocationHandler handler, InvocationHandlerList handlerList)
        {
            _handler = handler;
            _handlerList = handlerList;
        }

        public void Dispose()
        {
            _handlerList.Remove(_handler);
        }
    }

    private sealed class InvocationHandlerList
    {
        private readonly List<InvocationHandler> _invocationHandlers;
        // A lazy cached copy of the handlers that doesn't change for thread safety.
        // Adding or removing a handler sets this to null.
        private InvocationHandler[]? _copiedHandlers;

        internal InvocationHandlerList(InvocationHandler handler)
        {
            _invocationHandlers = new List<InvocationHandler>() { handler };
        }

        internal InvocationHandler[] GetHandlers()
        {
            var handlers = _copiedHandlers;
            if (handlers == null)
            {
                lock (_invocationHandlers)
                {
                    // Check if the handlers are set, if not we'll copy them over.
                    if (_copiedHandlers == null)
                    {
                        _copiedHandlers = _invocationHandlers.ToArray();
                    }
                    handlers = _copiedHandlers;
                }
            }
            return handlers;
        }

        internal void Add(string methodName, InvocationHandler handler)
        {
            lock (_invocationHandlers)
            {
                if (handler.HasResult)
                {
                    foreach (var m in _invocationHandlers)
                    {
                        if (m.HasResult)
                        {
                            throw new InvalidOperationException($"'{methodName}' already has a value returning handler. Multiple return values are not supported.");
                        }
                    }
                }
                _invocationHandlers.Add(handler);
                _copiedHandlers = null;
            }
        }

        internal void Remove(InvocationHandler handler)
        {
            lock (_invocationHandlers)
            {
                if (_invocationHandlers.Remove(handler))
                {
                    _copiedHandlers = null;
                }
            }
        }
    }

    private readonly struct InvocationHandler
    {
        public Type[] ParameterTypes { get; }
        public bool HasResult => _callback.Method.ReturnType == typeof(Task<object>);
        private readonly Func<object?[], object, Task> _callback;
        private readonly object _state;

        public InvocationHandler(Type[] parameterTypes, Func<object?[], object, Task> callback, object state)
        {
            _callback = callback;
            ParameterTypes = parameterTypes;
            _state = state;
        }

        public Task InvokeAsync(object?[] parameters)
        {
            return _callback(parameters, _state);
        }
    }

    private sealed class ConnectionState : IInvocationBinder
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger _logger;
        private readonly bool _hasInherentKeepAlive;
        private readonly MessageBuffer? _messageBuffer;

        private readonly object _lock = new object();
        private readonly Dictionary<string, InvocationRequest> _pendingCalls = new Dictionary<string, InvocationRequest>(StringComparer.Ordinal);
        private TaskCompletionSource<object?>? _stopTcs;

        private volatile bool _stopping;

        private int _nextInvocationId;

        private long _nextActivationServerTimeout;
        private long _nextActivationSendPing;

        public ConnectionContext Connection { get; }
        public Task? ReceiveTask { get; set; }
        public Exception? CloseException { get; set; }
        public CancellationToken UploadStreamToken { get; set; }

        // We store this task so we can view it in a dump file, but never await it
        public Task? InvocationMessageReceiveTask { get; set; }

        // Indicates the connection is stopping AND the client should NOT attempt to reconnect even if automatic reconnects are enabled.
        // This means either HubConnection.DisposeAsync/StopAsync was called OR a CloseMessage with AllowReconnects set to false was received.
        public bool Stopping
        {
            get => _stopping;
            set => _stopping = value;
        }

        public ConnectionState(ConnectionContext connection, HubConnection hubConnection)
        {
            Connection = connection;

            _hubConnection = hubConnection;
            _hubConnection._logScope.ConnectionId = connection.ConnectionId;

            _logger = _hubConnection._logger;
            _hasInherentKeepAlive = connection.Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive ?? false;

#pragma warning disable CA2252 // This API requires opting into preview features
            if (Connection.Features.Get<IStatefulReconnectFeature>() is IStatefulReconnectFeature feature)
            {
                _messageBuffer = new MessageBuffer(connection, hubConnection._protocol,
                    _hubConnection._serviceProvider.GetService<IOptions<HubConnectionOptions>>()?.Value.StatefulReconnectBufferSize
                        ?? DefaultStatefulReconnectBufferSize, _logger);

                feature.OnReconnected(_messageBuffer.ResendAsync);
            }
#pragma warning restore CA2252 // This API requires opting into preview features
        }

        public string GetNextId() => (++_nextInvocationId).ToString(CultureInfo.InvariantCulture);

        public void AddInvocation(InvocationRequest irq)
        {
            lock (_lock)
            {
                if (_pendingCalls.ContainsKey(irq.InvocationId))
                {
                    Log.InvocationAlreadyInUse(_logger, irq.InvocationId);
                    throw new InvalidOperationException($"Invocation ID '{irq.InvocationId}' is already in use.");
                }
                else
                {
                    _pendingCalls.Add(irq.InvocationId, irq);
                }
            }
        }

        public bool TryGetInvocation(string invocationId, [NotNullWhen(true)] out InvocationRequest? irq)
        {
            lock (_lock)
            {
                return _pendingCalls.TryGetValue(invocationId, out irq);
            }
        }

        public bool TryRemoveInvocation(string invocationId, [NotNullWhen(true)] out InvocationRequest? irq)
        {
            lock (_lock)
            {
                if (_pendingCalls.TryGetValue(invocationId, out irq))
                {
                    _pendingCalls.Remove(invocationId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void CancelOutstandingInvocations(Exception? exception)
        {
            Log.CancelingOutstandingInvocations(_logger);

            lock (_lock)
            {
                foreach (var outstandingCall in _pendingCalls.Values)
                {
                    Log.RemovingInvocation(_logger, outstandingCall.InvocationId);
                    if (exception != null)
                    {
                        outstandingCall.Fail(exception);
                    }
                    outstandingCall.Dispose();
                }
                _pendingCalls.Clear();
            }
        }

        public Task StopAsync()
        {
            // We want multiple StopAsync calls on the same connection state
            // to wait for the same "stop" to complete.
            lock (_lock)
            {
                if (_stopTcs != null)
                {
                    return _stopTcs.Task;
                }
                else
                {
                    _stopTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    return StopAsyncCore();
                }
            }
        }

        private async Task StopAsyncCore()
        {
            _hubConnection._state.AssertInConnectionLock();

            Log.Stopping(_logger);

            // Complete our write pipe, which should cause everything to shut down
            Log.TerminatingReceiveLoop(_logger);
            Connection.Transport.Input.CancelPendingRead();

            // Wait ServerTimeout for the server or transport to shut down.
            Log.WaitingForReceiveLoopToTerminate(_logger);
            await ((ReceiveTask ?? Task.CompletedTask).ConfigureAwait(false));

            Log.Stopped(_logger);

            _hubConnection._logScope.ConnectionId = null;
            _stopTcs!.TrySetResult(null);
        }

        public void Cleanup()
        {
            _messageBuffer?.Dispose();
        }

        public async Task TimerLoop(TimerAwaitable timer)
        {
            // initialize the timers
            timer.Start();
            ResetTimeout();
            ResetSendPing();

            using (timer)
            {
                // await returns True until `timer.Stop()` is called in the `finally` block of `ReceiveLoop`
                while (await timer)
                {
                    await RunTimerActions().ConfigureAwait(false);
                }
            }
        }

        public ValueTask<FlushResult> WriteAsync(HubMessage message, CancellationToken cancellationToken)
        {
            Debug.Assert(_messageBuffer is not null);
            return _messageBuffer.WriteAsync(message, cancellationToken);
        }

        public bool ShouldProcessMessage(HubMessage message)
        {
            if (UsingAcks())
            {
                if (!_messageBuffer.ShouldProcessMessage(message))
                {
                    Log.DroppingMessage(_logger, ((HubInvocationMessage)message).GetType().Name, ((HubInvocationMessage)message).InvocationId);
                    return false;
                }
            }
            return true;
        }

        public Task AckAsync(AckMessage ackMessage)
        {
            if (UsingAcks())
            {
                return _messageBuffer.AckAsync(ackMessage);
            }

            return Task.CompletedTask;
        }

        [MemberNotNullWhen(true, nameof(_messageBuffer))]
        public bool UsingAcks() => _messageBuffer is not null;

        public void ResetSendPing()
        {
            Volatile.Write(ref _nextActivationSendPing, (DateTime.UtcNow + _hubConnection.KeepAliveInterval).Ticks);
        }

        public void ResetTimeout()
        {
            Volatile.Write(ref _nextActivationServerTimeout, (DateTime.UtcNow + _hubConnection.ServerTimeout).Ticks);
        }

        // Internal for testing
        internal async Task RunTimerActions()
        {
            if (_hasInherentKeepAlive)
            {
                return;
            }

            if (DateTime.UtcNow.Ticks > Volatile.Read(ref _nextActivationServerTimeout))
            {
                OnServerTimeout();
            }

            if (DateTime.UtcNow.Ticks > Volatile.Read(ref _nextActivationSendPing) && !Stopping)
            {
                if (!_hubConnection._state.TryAcquireConnectionLock())
                {
                    Log.UnableToAcquireConnectionLockForPing(_logger);
                    return;
                }

                Log.AcquiredConnectionLockForPing(_logger);

                try
                {
                    if (_hubConnection._state.CurrentConnectionStateUnsynchronized != null)
                    {
                        SafeAssert(ReferenceEquals(_hubConnection._state.CurrentConnectionStateUnsynchronized, this),
                            "Something reset the connection state before the timer loop completed!");

                        await _hubConnection.SendHubMessage(this, PingMessage.Instance).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // The exception from send should be seen elsewhere in the client. We'll ignore it here.
                }
                finally
                {
                    _hubConnection._state.ReleaseConnectionLock();
                }
            }
        }

        // Internal for testing
        internal void OnServerTimeout()
        {
            CloseException = new TimeoutException(
                $"Server timeout ({_hubConnection.ServerTimeout.TotalMilliseconds:0.00}ms) elapsed without receiving a message from the server.");
            Connection.Transport.Input.CancelPendingRead();
        }

        Type IInvocationBinder.GetReturnType(string invocationId)
        {
            if (!TryGetInvocation(invocationId, out var irq))
            {
                Log.ReceivedUnexpectedResponse(_logger, invocationId);
                throw new KeyNotFoundException($"No invocation with id '{invocationId}' could be found.");
            }
            return irq.ResultType;
        }

        Type IInvocationBinder.GetStreamItemType(string invocationId)
        {
            // previously, streaming was only server->client, and used GetReturnType for StreamItems
            // literally the same code as the above method
            if (!TryGetInvocation(invocationId, out var irq))
            {
                Log.ReceivedUnexpectedResponse(_logger, invocationId);
                throw new KeyNotFoundException($"No invocation with id '{invocationId}' could be found.");
            }
            return irq.ResultType;
        }

        IReadOnlyList<Type> IInvocationBinder.GetParameterTypes(string methodName)
        {
            if (!_hubConnection._handlers.TryGetValue(methodName, out var invocationHandlerList))
            {
                Log.MissingHandler(_logger, methodName);
                return Type.EmptyTypes;
            }

            // We use the parameter types of the first handler
            var handlers = invocationHandlerList.GetHandlers();
            if (handlers.Length > 0)
            {
                return handlers[0].ParameterTypes;
            }
            throw new InvalidOperationException($"There are no callbacks registered for the method '{methodName}'");
        }
    }

    private sealed class ReconnectingConnectionState
    {
        // This lock protects the connection state.
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        private readonly ILogger _logger;

        public ReconnectingConnectionState(ILogger logger)
        {
            _logger = logger;
            StopCts = new CancellationTokenSource();
            ReconnectTask = Task.CompletedTask;
        }

        public ConnectionState? CurrentConnectionStateUnsynchronized { get; set; }

        public HubConnectionState OverallState { get; private set; }

        public CancellationTokenSource StopCts { get; set; } = new CancellationTokenSource();

        public Task ReconnectTask { get; set; } = Task.CompletedTask;

        public void ChangeState(HubConnectionState expectedState, HubConnectionState newState)
        {
            if (!TryChangeState(expectedState, newState))
            {
                Log.StateTransitionFailed(_logger, expectedState, newState, OverallState);
                throw new InvalidOperationException($"The HubConnection failed to transition from the '{expectedState}' state to the '{newState}' state because it was actually in the '{OverallState}' state.");
            }
        }

        public bool TryChangeState(HubConnectionState expectedState, HubConnectionState newState)
        {
            AssertInConnectionLock();

            Log.AttemptingStateTransition(_logger, expectedState, newState);

            if (OverallState != expectedState)
            {
                return false;
            }

            OverallState = newState;
            return true;
        }

        [Conditional("DEBUG")]
        public void AssertInConnectionLock([CallerMemberName] string? memberName = null, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0) => SafeAssert(_connectionLock.CurrentCount == 0, "We're not in the Connection Lock!", memberName, fileName, lineNumber);

        [Conditional("DEBUG")]
        public void AssertConnectionValid([CallerMemberName] string? memberName = null, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0)
        {
            AssertInConnectionLock(memberName, fileName, lineNumber);
            SafeAssert(CurrentConnectionStateUnsynchronized != null, "We don't have a connection!", memberName, fileName, lineNumber);
        }

        public Task WaitConnectionLockAsync(CancellationToken token, [CallerMemberName] string? memberName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log.WaitingOnConnectionLock(_logger, memberName, filePath, lineNumber);
            return _connectionLock.WaitAsync(token);
        }

        public bool TryAcquireConnectionLock()
        {
            if (OperatingSystem.IsBrowser())
            {
                return _connectionLock.WaitAsync(0).Result;
            }
            return _connectionLock.Wait(0);
        }

        // Don't call this method in a try/finally that releases the lock since we're also potentially releasing the connection lock here.
        public async Task<ConnectionState> WaitForActiveConnectionAsync(string methodName, CancellationToken token)
        {
            await WaitConnectionLockAsync(token, methodName).ConfigureAwait(false);

            if (!IsConnectionActive())
            {
                ReleaseConnectionLock(methodName);
                throw new InvalidOperationException($"The '{methodName}' method cannot be called if the connection is not active");
            }

            return CurrentConnectionStateUnsynchronized;
        }

        [MemberNotNullWhen(true, nameof(CurrentConnectionStateUnsynchronized))]
        public bool IsConnectionActive()
        {
            AssertInConnectionLock();
            return CurrentConnectionStateUnsynchronized is not null && !CurrentConnectionStateUnsynchronized.Stopping;
        }

        public void ReleaseConnectionLock([CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log.ReleasingConnectionLock(_logger, memberName, filePath, lineNumber);
            _connectionLock.Release();
        }
    }
}
