// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class KestrelServer : IServer
    {
        private readonly List<(IConnectionListener, Task)> _transports = new List<(IConnectionListener, Task)>();
        private readonly List<(IMultiplexedConnectionListener, Task)> _multiplexedTransports = new List<(IMultiplexedConnectionListener, Task)>();
        private readonly IServerAddressesFeature _serverAddresses;
        private readonly List<IConnectionListenerFactory> _transportFactories;
        private readonly List<IMultiplexedConnectionListenerFactory> _multiplexedTransportFactories;

        private bool _hasStarted;
        private int _stopping;
        private readonly TaskCompletionSource<object> _stoppedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public KestrelServer(IOptions<KestrelServerOptions> options, IEnumerable<IConnectionListenerFactory> transportFactories, ILoggerFactory loggerFactory)
            : this(transportFactories, null, CreateServiceContext(options, loggerFactory))
        {
        }
        public KestrelServer(IOptions<KestrelServerOptions> options, IEnumerable<IConnectionListenerFactory> transportFactories, IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedFactories, ILoggerFactory loggerFactory)
            : this(transportFactories, multiplexedFactories, CreateServiceContext(options, loggerFactory))
        {
        }

        // For testing
        internal KestrelServer(IEnumerable<IConnectionListenerFactory> transportFactories, ServiceContext serviceContext)
            : this(transportFactories, null, serviceContext)
        {
        }

        // For testing
        internal KestrelServer(IEnumerable<IConnectionListenerFactory> transportFactories, IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedFactories, ServiceContext serviceContext)
        {
            if (transportFactories == null)
            {
                throw new ArgumentNullException(nameof(transportFactories));
            }

            _transportFactories = transportFactories.ToList();
            _multiplexedTransportFactories = multiplexedFactories?.ToList();

            if (_transportFactories.Count == 0 && (_multiplexedTransportFactories == null || _multiplexedTransportFactories.Count == 0))
            {
                throw new InvalidOperationException(CoreStrings.TransportNotFound);
            }

            ServiceContext = serviceContext;

            Features = new FeatureCollection();
            _serverAddresses = new ServerAddressesFeature();
            Features.Set(_serverAddresses);

            HttpCharacters.Initialize();
        }

        private static ServiceContext CreateServiceContext(IOptions<KestrelServerOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            var serverOptions = options.Value ?? new KestrelServerOptions();
            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel");
            var trace = new KestrelTrace(logger);
            var connectionManager = new ConnectionManager(
                trace,
                serverOptions.Limits.MaxConcurrentUpgradedConnections);

            var heartbeatManager = new HeartbeatManager(connectionManager);
            var dateHeaderValueManager = new DateHeaderValueManager();

            var heartbeat = new Heartbeat(
                new IHeartbeatHandler[] { dateHeaderValueManager, heartbeatManager },
                new SystemClock(),
                DebuggerWrapper.Singleton,
                trace);

            return new ServiceContext
            {
                Log = trace,
                HttpParser = new HttpParser<Http1ParsingHandler>(trace.IsEnabled(LogLevel.Information)),
                Scheduler = PipeScheduler.ThreadPool,
                SystemClock = heartbeatManager,
                DateHeaderValueManager = dateHeaderValueManager,
                ConnectionManager = connectionManager,
                Heartbeat = heartbeat,
                ServerOptions = serverOptions,
            };
        }

        public IFeatureCollection Features { get; }

        public KestrelServerOptions Options => ServiceContext.ServerOptions;

        private ServiceContext ServiceContext { get; }

        private IKestrelTrace Trace => ServiceContext.Log;

        private ConnectionManager ConnectionManager => ServiceContext.ConnectionManager;

        public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            try
            {
                if (!BitConverter.IsLittleEndian)
                {
                    throw new PlatformNotSupportedException(CoreStrings.BigEndianNotSupported);
                }

                ValidateOptions();

                if (_hasStarted)
                {
                    // The server has already started and/or has not been cleaned up yet
                    throw new InvalidOperationException(CoreStrings.ServerAlreadyStarted);
                }
                _hasStarted = true;

                ServiceContext.Heartbeat?.Start();

                async Task OnBind(ListenOptions options)
                {
                    // INVESTIGATE: For some reason, MsQuic needs to bind before
                    // sockets for it to successfully listen. It also seems racy.
                    if ((options.Protocols & HttpProtocols.Http3) == HttpProtocols.Http3)
                    {
                        if (_multiplexedTransportFactories == null || _multiplexedTransportFactories.Count == 0)
                        {
                            throw new InvalidOperationException("Cannot start HTTP/3 server if no MultiplexedTransportFactories are registered.");
                        }

                        options.UseHttp3Server(ServiceContext, application, options.Protocols);
                        var multiplxedConnectionDelegate = ((IMultiplexedConnectionBuilder)options).Build();

                        var multiplexedConnectionDispatcher = new MultiplexedConnectionDispatcher(ServiceContext, multiplxedConnectionDelegate);
                        var multiplexedFactory = _multiplexedTransportFactories.Last();
                        var multiplexedTransport = await multiplexedFactory.BindAsync(options.EndPoint).ConfigureAwait(false);

                        var acceptLoopTask = multiplexedConnectionDispatcher.StartAcceptingConnections(multiplexedTransport);
                        _multiplexedTransports.Add((multiplexedTransport, acceptLoopTask));

                        options.EndPoint = multiplexedTransport.EndPoint;
                    }

                    // Add the HTTP middleware as the terminal connection middleware
                    if ((options.Protocols & HttpProtocols.Http1) == HttpProtocols.Http1
                        || (options.Protocols & HttpProtocols.Http2) == HttpProtocols.Http2
                        || options.Protocols == HttpProtocols.None) // TODO a test fails because it doesn't throw an exception in the right place
                                                                    // when there is no HttpProtocols in KestrelServer, can we remove/change the test?
                    {
                        options.UseHttpServer(ServiceContext, application, options.Protocols);
                        var connectionDelegate = options.Build();

                        // Add the connection limit middleware
                        if (Options.Limits.MaxConcurrentConnections.HasValue)
                        {
                            connectionDelegate = new ConnectionLimitMiddleware(connectionDelegate, Options.Limits.MaxConcurrentConnections.Value, Trace).OnConnectionAsync;
                        }

                        var connectionDispatcher = new ConnectionDispatcher(ServiceContext, connectionDelegate);
                        var factory = _transportFactories.Last();
                        var transport = await factory.BindAsync(options.EndPoint).ConfigureAwait(false);

                        var acceptLoopTask = connectionDispatcher.StartAcceptingConnections(transport);

                        _transports.Add((transport, acceptLoopTask));
                        options.EndPoint = transport.EndPoint;
                    }
                }

                await AddressBinder.BindAsync(_serverAddresses, Options, Trace, OnBind).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.LogCritical(0, ex, "Unable to start Kestrel.");
                Dispose();
                throw;
            }
        }

        // Graceful shutdown if possible
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                await _stoppedTcs.Task.ConfigureAwait(false);
                return;
            }

            try
            {
                var connectionTransportCount = _transports.Count;
                var totalTransportCount = _transports.Count + _multiplexedTransports.Count;
                var tasks = new Task[totalTransportCount];

                for (int i = 0; i < connectionTransportCount; i++)
                {
                    (IConnectionListener listener, Task acceptLoop) = _transports[i];
                    tasks[i] = Task.WhenAll(listener.UnbindAsync(cancellationToken).AsTask(), acceptLoop);
                }

                for (int i = connectionTransportCount; i < totalTransportCount; i++)
                {
                    (IMultiplexedConnectionListener listener, Task acceptLoop) = _multiplexedTransports[i - connectionTransportCount];
                    tasks[i] = Task.WhenAll(listener.UnbindAsync(cancellationToken).AsTask(), acceptLoop);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (!await ConnectionManager.CloseAllConnectionsAsync(cancellationToken).ConfigureAwait(false))
                {
                    Trace.NotAllConnectionsClosedGracefully();

                    if (!await ConnectionManager.AbortAllConnectionsAsync().ConfigureAwait(false))
                    {
                        Trace.NotAllConnectionsAborted();
                    }
                }

                for (int i = 0; i < connectionTransportCount; i++)
                {
                    (IConnectionListener listener, Task acceptLoop) = _transports[i];
                    tasks[i] = listener.DisposeAsync().AsTask();
                }

                for (int i = connectionTransportCount; i < totalTransportCount; i++)
                {
                    (IMultiplexedConnectionListener listener, Task acceptLoop) = _multiplexedTransports[i - connectionTransportCount];
                    tasks[i] = listener.DisposeAsync().AsTask();
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                ServiceContext.Heartbeat?.Dispose();
            }
            catch (Exception ex)
            {
                _stoppedTcs.TrySetException(ex);
                throw;
            }

            _stoppedTcs.TrySetResult(null);
        }

        // Ungraceful shutdown
        public void Dispose()
        {
            var cancelledTokenSource = new CancellationTokenSource();
            cancelledTokenSource.Cancel();
            StopAsync(cancelledTokenSource.Token).GetAwaiter().GetResult();
        }

        private void ValidateOptions()
        {
            Options.ConfigurationLoader?.Load();

            if (Options.Limits.MaxRequestBufferSize.HasValue &&
                Options.Limits.MaxRequestBufferSize < Options.Limits.MaxRequestLineSize)
            {
                throw new InvalidOperationException(
                    CoreStrings.FormatMaxRequestBufferSmallerThanRequestLineBuffer(Options.Limits.MaxRequestBufferSize.Value, Options.Limits.MaxRequestLineSize));
            }

            if (Options.Limits.MaxRequestBufferSize.HasValue &&
                Options.Limits.MaxRequestBufferSize < Options.Limits.MaxRequestHeadersTotalSize)
            {
                throw new InvalidOperationException(
                    CoreStrings.FormatMaxRequestBufferSmallerThanRequestHeaderBuffer(Options.Limits.MaxRequestBufferSize.Value, Options.Limits.MaxRequestHeadersTotalSize));
            }
        }
    }
}
