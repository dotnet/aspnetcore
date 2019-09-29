// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class KestrelServer : IServer
    {
        private readonly List<(IConnectionListener, Task)> _transports = new List<(IConnectionListener, Task)>();
        private readonly IServerAddressesFeature _serverAddresses;
        private readonly IConnectionListenerFactory _transportFactory;

        private bool _hasStarted;
        private int _stopping;
        private readonly TaskCompletionSource<object> _stoppedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Connections.Server _server;

        public KestrelServer(IOptions<KestrelServerOptions> options, IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory)
            : this(transportFactory, CreateServiceContext(options, loggerFactory))
        {
        }

        // For testing
        internal KestrelServer(IConnectionListenerFactory transportFactory, ServiceContext serviceContext)
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

                var serverOptions = new ServerOptions(Options.ApplicationServices);

                Task OnBind(ListenOptions options)
                {
                    // Add the connection limit middleware
                    options.UseConnectionLimit(Options, Trace);

                    // Add the HTTP middleware as the terminal connection middleware
                    options.UseHttpServer(ServiceContext, application, options.Protocols);

                    var connectionDelegate = options.Build();

                    serverOptions.Bindings.Add(new ServerBinding(options.EndPoint, connectionDelegate, _transportFactory));

                    return Task.CompletedTask;
                }

                await AddressBinder.BindAsync(_serverAddresses, Options, Trace, OnBind).ConfigureAwait(false);

                _server = new Connections.Server(NullLoggerFactory.Instance, serverOptions);
                await _server.StartAsync(cancellationToken);
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
                await _server.StopAsync(cancellationToken);

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
