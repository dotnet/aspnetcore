// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class KestrelServer : IServer
    {
        private readonly List<ITransport> _transports = new List<ITransport>();
        private readonly IServerAddressesFeature _serverAddresses;
        private readonly ITransportFactory _transportFactory;

        private bool _hasStarted;
        private int _stopping;
        private readonly TaskCompletionSource<object> _stoppedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

#pragma warning disable PUB0001 // Pubternal type in public API
        public KestrelServer(IOptions<KestrelServerOptions> options, ITransportFactory transportFactory, ILoggerFactory loggerFactory)
#pragma warning restore PUB0001
            : this(transportFactory, CreateServiceContext(options, loggerFactory))
        {
        }

        // For testing
        internal KestrelServer(ITransportFactory transportFactory, ServiceContext serviceContext)
        {
            if (transportFactory == null)
            {
                throw new ArgumentNullException(nameof(transportFactory));
            }

            _transportFactory = transportFactory;
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

            // TODO: This logic will eventually move into the IConnectionHandler<T> and off
            // the service context once we get to https://github.com/aspnet/KestrelHttpServer/issues/1662
            PipeScheduler scheduler = null;
            switch (serverOptions.ApplicationSchedulingMode)
            {
                case SchedulingMode.Default:
                case SchedulingMode.ThreadPool:
                    scheduler = PipeScheduler.ThreadPool;
                    break;
                case SchedulingMode.Inline:
                    scheduler = PipeScheduler.Inline;
                    break;
                default:
                    throw new NotSupportedException(CoreStrings.FormatUnknownTransportMode(serverOptions.ApplicationSchedulingMode));
            }

            return new ServiceContext
            {
                Log = trace,
                HttpParser = new HttpParser<Http1ParsingHandler>(trace.IsEnabled(LogLevel.Information)),
                Scheduler = scheduler,
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

                async Task OnBind(ListenOptions endpoint)
                {
                    // Add the HTTP middleware as the terminal connection middleware
                    endpoint.UseHttpServer(endpoint.ConnectionAdapters, ServiceContext, application, endpoint.Protocols);

                    var connectionDelegate = endpoint.Build();

                    // Add the connection limit middleware
                    if (Options.Limits.MaxConcurrentConnections.HasValue)
                    {
                        connectionDelegate = new ConnectionLimitMiddleware(connectionDelegate, Options.Limits.MaxConcurrentConnections.Value, Trace).OnConnectionAsync;
                    }

                    var connectionDispatcher = new ConnectionDispatcher(ServiceContext, connectionDelegate);
                    var transport = _transportFactory.Create(endpoint, connectionDispatcher);
                    _transports.Add(transport);

                    await transport.BindAsync().ConfigureAwait(false);
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
                var tasks = new Task[_transports.Count];
                for (int i = 0; i < _transports.Count; i++)
                {
                    tasks[i] = _transports[i].UnbindAsync();
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

                for (int i = 0; i < _transports.Count; i++)
                {
                    tasks[i] = _transports[i].StopAsync();
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
