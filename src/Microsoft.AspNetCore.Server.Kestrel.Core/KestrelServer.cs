// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
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

                var listenOptions = Options.ListenOptions;
                var hasListenOptions = listenOptions.Any();
                var hasServerAddresses = _serverAddresses.Addresses.Any();

                if (_serverAddresses.PreferHostingUrls && hasServerAddresses)
                {
                    if (hasListenOptions)
                    {
                        var joined = string.Join(", ", _serverAddresses.Addresses);
                        _logger.LogInformation($"Overriding endpoints defined in UseKestrel() since {nameof(IServerAddressesFeature.PreferHostingUrls)} is set to true. Binding to address(es) '{joined}' instead.");

                        listenOptions.Clear();
                    }

                    await BindToServerAddresses(listenOptions, serviceContext, application, cancellationToken).ConfigureAwait(false);
                }
                else if (hasListenOptions)
                {
                    if (hasServerAddresses)
                    {
                        var joined = string.Join(", ", _serverAddresses.Addresses);
                        _logger.LogWarning($"Overriding address(es) '{joined}'. Binding to endpoints defined in UseKestrel() instead.");

                        _serverAddresses.Addresses.Clear();
                    }

                    await BindToEndpoints(listenOptions, serviceContext, application).ConfigureAwait(false);
                }
                else if (hasServerAddresses)
                {
                    // If no endpoints are configured directly using KestrelServerOptions, use those configured via the IServerAddressesFeature.
                    await BindToServerAddresses(listenOptions, serviceContext, application, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogDebug($"No listening endpoints were configured. Binding to {Constants.DefaultServerAddress} by default.");

                    // "localhost" for both IPv4 and IPv6 can't be represented as an IPEndPoint.
                    await StartLocalhostAsync(ServerAddress.FromUrl(Constants.DefaultServerAddress), serviceContext, application, cancellationToken).ConfigureAwait(false);

                    // If StartLocalhost doesn't throw, there is at least one listener.
                    // The port cannot change for "localhost".
                    _serverAddresses.Addresses.Add(Constants.DefaultServerAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(0, ex, "Unable to start Kestrel.");
                Dispose();
                throw;
            }
        }

        private async Task BindToServerAddresses<TContext>(List<ListenOptions> listenOptions, ServiceContext serviceContext, IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            var copiedAddresses = _serverAddresses.Addresses.ToArray();
            _serverAddresses.Addresses.Clear();
            foreach (var address in copiedAddresses)
            {
                var parsedAddress = ServerAddress.FromUrl(address);

                if (parsedAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"HTTPS endpoints can only be configured using {nameof(KestrelServerOptions)}.{nameof(KestrelServerOptions.Listen)}().");
                }
                else if (!parsedAddress.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Unrecognized scheme in server address '{address}'. Only 'http://' is supported.");
                }

                if (!string.IsNullOrEmpty(parsedAddress.PathBase))
                {
                    throw new InvalidOperationException($"A path base can only be configured using {nameof(IApplicationBuilder)}.UsePathBase().");
                }

                if (parsedAddress.IsUnixPipe)
                {
                    listenOptions.Add(new ListenOptions(parsedAddress.UnixPipePath));
                }
                else if (string.Equals(parsedAddress.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    // "localhost" for both IPv4 and IPv6 can't be represented as an IPEndPoint.
                    await StartLocalhostAsync(parsedAddress, serviceContext, application, cancellationToken).ConfigureAwait(false);
                    // If StartLocalhost doesn't throw, there is at least one listener.
                    // The port cannot change for "localhost".
                    _serverAddresses.Addresses.Add(parsedAddress.ToString());
                }
                else
                {
                    // These endPoints will be added later to _serverAddresses.Addresses
                    listenOptions.Add(new ListenOptions(CreateIPEndPoint(parsedAddress)));
                }
            }

            await BindToEndpoints(listenOptions, serviceContext, application).ConfigureAwait(false);
        }

        private async Task BindToEndpoints<TContext>(List<ListenOptions> listenOptions, ServiceContext serviceContext, IHttpApplication<TContext> application)
        {
            foreach (var endPoint in listenOptions)
            {
                var connectionHandler = new ConnectionHandler<TContext>(endPoint, serviceContext, application);
                var transport = _transportFactory.Create(endPoint, connectionHandler);
                _transports.Add(transport);

                try
                {
                    await transport.BindAsync().ConfigureAwait(false);
                }
                catch (AddressInUseException ex)
                {
                    throw new IOException($"Failed to bind to address {endPoint}: address already in use.", ex);
                }

                // If requested port was "0", replace with assigned dynamic port.
                _serverAddresses.Addresses.Add(endPoint.GetDisplayName());
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

        private async Task StartLocalhostAsync<TContext>(ServerAddress parsedAddress, ServiceContext serviceContext, IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            if (parsedAddress.Port == 0)
            {
                throw new InvalidOperationException("Dynamic port binding is not supported when binding to localhost. You must either bind to 127.0.0.1:0 or [::1]:0, or both.");
            }

            var exceptions = new List<Exception>();

            try
            {
                var ipv4ListenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, parsedAddress.Port));

                var connectionHandler = new ConnectionHandler<TContext>(ipv4ListenOptions, serviceContext, application);
                var transport = _transportFactory.Create(ipv4ListenOptions, connectionHandler);
                _transports.Add(transport);
                await transport.BindAsync().ConfigureAwait(false);
            }
            catch (AddressInUseException ex)
            {
                throw new IOException($"Failed to bind to address {parsedAddress} on the IPv4 loopback interface: port already in use.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(0, $"Unable to bind to {parsedAddress} on the IPv4 loopback interface: ({ex.Message})");
                exceptions.Add(ex);
            }

            try
            {
                var ipv6ListenOptions = new ListenOptions(new IPEndPoint(IPAddress.IPv6Loopback, parsedAddress.Port));

                var connectionHandler = new ConnectionHandler<TContext>(ipv6ListenOptions, serviceContext, application);
                var transport = _transportFactory.Create(ipv6ListenOptions, connectionHandler);
                _transports.Add(transport);
                await transport.BindAsync().ConfigureAwait(false);
            }
            catch (AddressInUseException ex)
            {
                throw new IOException($"Failed to bind to address {parsedAddress} on the IPv6 loopback interface: port already in use.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(0, $"Unable to bind to {parsedAddress} on the IPv6 loopback interface: ({ex.Message})");
                exceptions.Add(ex);
            }

            if (exceptions.Count == 2)
            {
                throw new IOException($"Failed to bind to address {parsedAddress}.", new AggregateException(exceptions));
            }
        }

        /// <summary>
        /// Returns an <see cref="IPEndPoint"/> for the given host an port.
        /// If the host parameter isn't "localhost" or an IP address, use IPAddress.Any.
        /// </summary>
        internal static IPEndPoint CreateIPEndPoint(ServerAddress address)
        {
            IPAddress ip;

            if (!IPAddress.TryParse(address.Host, out ip))
            {
                ip = IPAddress.IPv6Any;
            }

            return new IPEndPoint(ip, address.Port);
        }
    }
}