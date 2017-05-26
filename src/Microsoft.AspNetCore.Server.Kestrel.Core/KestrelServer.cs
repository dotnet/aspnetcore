// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class KestrelServer : IServer
    {
        private readonly List<ITransport> _transports = new List<ITransport>();
        private readonly Heartbeat _heartbeat;
        private readonly IServerAddressesFeature _serverAddresses;
        private readonly ITransportFactory _transportFactory;

        private bool _hasStarted;
        private int _stopped;

        public KestrelServer(IOptions<KestrelServerOptions> options, ITransportFactory transportFactory, ILoggerFactory loggerFactory)
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

            var frameHeartbeatManager = new FrameHeartbeatManager(serviceContext.ConnectionManager);
            _heartbeat = new Heartbeat(
                new IHeartbeatHandler[] { serviceContext.DateHeaderValueManager, frameHeartbeatManager },
                serviceContext.SystemClock, Trace);

            Features = new FeatureCollection();
            _serverAddresses = new ServerAddressesFeature();
            Features.Set(_serverAddresses);
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
            var connectionManager = new FrameConnectionManager(
                trace,
                serverOptions.Limits.MaxConcurrentConnections,
                serverOptions.Limits.MaxConcurrentUpgradedConnections);

            var systemClock = new SystemClock();
            var dateHeaderValueManager = new DateHeaderValueManager(systemClock);

            // TODO: This logic will eventually move into the IConnectionHandler<T> and off
            // the service context once we get to https://github.com/aspnet/KestrelHttpServer/issues/1662
            IThreadPool threadPool = null;
            switch (serverOptions.ApplicationSchedulingMode)
            {
                case SchedulingMode.Default:
                case SchedulingMode.ThreadPool:
                    threadPool = new LoggingThreadPool(trace);
                    break;
                case SchedulingMode.Inline:
                    threadPool = new InlineLoggingThreadPool(trace);
                    break;
                default:
                    throw new NotSupportedException(CoreStrings.FormatUnknownTransportMode(serverOptions.ApplicationSchedulingMode));
            }

            return new ServiceContext
            {
                Log = trace,
                HttpParserFactory = frameParser => new HttpParser<FrameAdapter>(frameParser.Frame.ServiceContext.Log.IsEnabled(LogLevel.Information)),
                ThreadPool = threadPool,
                SystemClock = systemClock,
                DateHeaderValueManager = dateHeaderValueManager,
                ConnectionManager = connectionManager,
                ServerOptions = serverOptions
            };
        }

        public IFeatureCollection Features { get; }

        public KestrelServerOptions Options => ServiceContext.ServerOptions;

        private ServiceContext ServiceContext { get; }

        private IKestrelTrace Trace => ServiceContext.Log;

        private FrameConnectionManager ConnectionManager => ServiceContext.ConnectionManager;

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
                _heartbeat.Start();

                async Task OnBind(ListenOptions endpoint)
                {
                    var connectionHandler = new ConnectionHandler<TContext>(endpoint, ServiceContext, application);
                    var transport = _transportFactory.Create(endpoint, connectionHandler);
                    _transports.Add(transport);

                    await transport.BindAsync().ConfigureAwait(false);
                }

                await AddressBinder.BindAsync(_serverAddresses, Options.ListenOptions, Trace, OnBind).ConfigureAwait(false);
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
            if (Interlocked.Exchange(ref _stopped, 1) == 1)
            {
                return;
            }

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

            _heartbeat.Dispose();
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
