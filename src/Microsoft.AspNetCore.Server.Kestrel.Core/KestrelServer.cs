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

        private readonly ILogger _logger;
        private readonly IServerAddressesFeature _serverAddresses;
        private readonly ITransportFactory _transportFactory;

        private bool _isRunning;
        private int _stopped;
        private Heartbeat _heartbeat;

        public KestrelServer(
            IOptions<KestrelServerOptions> options,
            ITransportFactory transportFactory,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (transportFactory == null)
            {
                throw new ArgumentNullException(nameof(transportFactory));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            Options = options.Value ?? new KestrelServerOptions();
            _transportFactory = transportFactory;
            _logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel");
            Features = new FeatureCollection();
            _serverAddresses = new ServerAddressesFeature();
            Features.Set(_serverAddresses);
        }

        public IFeatureCollection Features { get; }

        public KestrelServerOptions Options { get; }

        public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            try
            {
                if (!BitConverter.IsLittleEndian)
                {
                    throw new PlatformNotSupportedException("Kestrel does not support big-endian architectures.");
                }

                ValidateOptions();

                if (_isRunning)
                {
                    // The server has already started and/or has not been cleaned up yet
                    throw new InvalidOperationException("Server has already started.");
                }
                _isRunning = true;

                var trace = new KestrelTrace(_logger);

                var systemClock = new SystemClock();
                var dateHeaderValueManager = new DateHeaderValueManager(systemClock);
                var connectionManager = new FrameConnectionManager();
                _heartbeat = new Heartbeat(new IHeartbeatHandler[] { dateHeaderValueManager, connectionManager }, systemClock, trace);

                IThreadPool threadPool;
                if (Options.UseTransportThread)
                {
                    threadPool = new InlineLoggingThreadPool(trace);
                }
                else
                {
                    threadPool = new LoggingThreadPool(trace);
                }

                var serviceContext = new ServiceContext
                {
                    Log = trace,
                    HttpParserFactory = frameParser => new HttpParser<FrameAdapter>(frameParser.Frame.ServiceContext.Log),
                    ThreadPool = threadPool,
                    SystemClock = systemClock,
                    DateHeaderValueManager = dateHeaderValueManager,
                    ConnectionManager = connectionManager,
                    ServerOptions = Options
                };

                async Task OnBind(ListenOptions endpoint)
                {
                    var connectionHandler = new ConnectionHandler<TContext>(endpoint, serviceContext, application);
                    var transport = _transportFactory.Create(endpoint, connectionHandler);
                    _transports.Add(transport);

                    await transport.BindAsync().ConfigureAwait(false);
                }

                await AddressBinder.BindAsync(_serverAddresses, Options.ListenOptions, _logger, OnBind).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(0, ex, "Unable to start Kestrel.");
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

            if (_transports != null)
            {
                var tasks = new Task[_transports.Count];
                for (int i = 0; i < _transports.Count; i++)
                {
                    tasks[i] = _transports[i].UnbindAsync();
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);

                // TODO: Do transport-agnostic connection management/shutdown.
                for (int i = 0; i < _transports.Count; i++)
                {
                    tasks[i] = _transports[i].StopAsync();
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            _heartbeat?.Dispose();
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
                    $"Maximum request buffer size ({Options.Limits.MaxRequestBufferSize.Value}) must be greater than or equal to maximum request line size ({Options.Limits.MaxRequestLineSize}).");
            }

            if (Options.Limits.MaxRequestBufferSize.HasValue &&
                Options.Limits.MaxRequestBufferSize < Options.Limits.MaxRequestHeadersTotalSize)
            {
                throw new InvalidOperationException(
                    $"Maximum request buffer size ({Options.Limits.MaxRequestBufferSize.Value}) must be greater than or equal to maximum request headers size ({Options.Limits.MaxRequestHeadersTotalSize}).");
            }
        }
    }
}