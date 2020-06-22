// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class KestrelServer : IServer
    {
        private readonly ServerAddressesFeature _serverAddresses;
        private readonly TransportManager _transportManager;
        private readonly IConnectionListenerFactory _transportFactory;
        private readonly IMultiplexedConnectionListenerFactory _multiplexedTransportFactory;

        private readonly SemaphoreSlim _bindSemaphore = new SemaphoreSlim(initialCount: 1);
        private bool _hasStarted;
        private int _stopping;
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
        private readonly TaskCompletionSource _stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        private IDisposable _configChangedRegistration;

        public KestrelServer(
            IOptions<KestrelServerOptions> options,
            IEnumerable<IConnectionListenerFactory> transportFactories,
            ILoggerFactory loggerFactory)
            : this(transportFactories, null, CreateServiceContext(options, loggerFactory))
        {
        }

        public KestrelServer(
            IOptions<KestrelServerOptions> options,
            IEnumerable<IConnectionListenerFactory> transportFactories,
            IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedFactories,
            ILoggerFactory loggerFactory)
            : this(transportFactories, multiplexedFactories, CreateServiceContext(options, loggerFactory))
        {
        }

        // For testing
        internal KestrelServer(IEnumerable<IConnectionListenerFactory> transportFactories, ServiceContext serviceContext)
            : this(transportFactories, null, serviceContext)
        {
        }

        // For testing
        internal KestrelServer(
            IEnumerable<IConnectionListenerFactory> transportFactories,
            IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedFactories,
            ServiceContext serviceContext)
        {
            if (transportFactories == null)
            {
                throw new ArgumentNullException(nameof(transportFactories));
            }

            _transportFactory = transportFactories?.LastOrDefault();
            _multiplexedTransportFactory = multiplexedFactories?.LastOrDefault();

            if (_transportFactory == null && _multiplexedTransportFactory == null)
            {
                throw new InvalidOperationException(CoreStrings.TransportNotFound);
            }

            ServiceContext = serviceContext;

            Features = new FeatureCollection();
            _serverAddresses = new ServerAddressesFeature();
            Features.Set<IServerAddressesFeature>(_serverAddresses);

            _transportManager = new TransportManager(_transportFactory, _multiplexedTransportFactory,  ServiceContext);

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

        private AddressBindContext AddressBindContext { get; set; }

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
                        if (_multiplexedTransportFactory is null)
                        {
                            throw new InvalidOperationException($"Cannot start HTTP/3 server if no {nameof(IMultiplexedConnectionListenerFactory)} is registered.");
                        }

                        options.UseHttp3Server(ServiceContext, application, options.Protocols);
                        var multiplexedConnectionDelegate = ((IMultiplexedConnectionBuilder)options).Build();

                        // Add the connection limit middleware
                        multiplexedConnectionDelegate = EnforceConnectionLimit(multiplexedConnectionDelegate, Options.Limits.MaxConcurrentConnections, Trace);

                        options.EndPoint = await _transportManager.BindAsync(options.EndPoint, multiplexedConnectionDelegate, options.EndpointConfig).ConfigureAwait(false);
                    }

                    // Add the HTTP middleware as the terminal connection middleware
                    if ((options.Protocols & HttpProtocols.Http1) == HttpProtocols.Http1
                        || (options.Protocols & HttpProtocols.Http2) == HttpProtocols.Http2
                        || options.Protocols == HttpProtocols.None) // TODO a test fails because it doesn't throw an exception in the right place
                                                                    // when there is no HttpProtocols in KestrelServer, can we remove/change the test?
                    {
                        if (_transportFactory is null)
                        {
                            throw new InvalidOperationException($"Cannot start HTTP/1.x or HTTP/2 server if no {nameof(IConnectionListenerFactory)} is registered.");
                        }

                        options.UseHttpServer(ServiceContext, application, options.Protocols);
                        var connectionDelegate = options.Build();

                        // Add the connection limit middleware
                        connectionDelegate = EnforceConnectionLimit(connectionDelegate, Options.Limits.MaxConcurrentConnections, Trace);

                        options.EndPoint = await _transportManager.BindAsync(options.EndPoint, connectionDelegate, options.EndpointConfig).ConfigureAwait(false);
                    }
                }

                AddressBindContext = new AddressBindContext
                {
                    ServerAddressesFeature = _serverAddresses,
                    ServerOptions = Options,
                    Logger = Trace,
                    CreateBinding = OnBind,
                };

                await BindAsync(cancellationToken).ConfigureAwait(false);
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

            _stopCts.Cancel();

            // Don't use cancellationToken when acquiring the semaphore. Dispose calls this with a pre-canceled token.
            await _bindSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                await _transportManager.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _stoppedTcs.TrySetException(ex);
                throw;
            }
            finally
            {
                ServiceContext.Heartbeat?.Dispose();
                _configChangedRegistration?.Dispose();
                _stopCts.Dispose();
                _bindSemaphore.Release();
            }

            _stoppedTcs.TrySetResult();
        }

        // Ungraceful shutdown
        public void Dispose()
        {
            StopAsync(new CancellationToken(canceled: true)).GetAwaiter().GetResult();
        }

        private async Task BindAsync(CancellationToken cancellationToken)
        {
            await _bindSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_stopping == 1)
                {
                    throw new InvalidOperationException("Kestrel has already been stopped.");
                }

                IChangeToken reloadToken = null;

                _serverAddresses.InternalCollection.PreventPublicMutation();

                if (Options.ConfigurationLoader?.ReloadOnChange == true && (!_serverAddresses.PreferHostingUrls || _serverAddresses.InternalCollection.Count == 0))
                {
                    reloadToken = Options.ConfigurationLoader.Configuration.GetReloadToken();
                }

                Options.ConfigurationLoader?.Load();

                await AddressBinder.BindAsync(Options.ListenOptions, AddressBindContext).ConfigureAwait(false);
                _configChangedRegistration = reloadToken?.RegisterChangeCallback(async state => await ((KestrelServer)state).RebindAsync(), this);
            }
            finally
            {
                _bindSemaphore.Release();
            }
        }

        private async Task RebindAsync()
        {
            await _bindSemaphore.WaitAsync();

            IChangeToken reloadToken = null;

            try
            {
                if (_stopping == 1)
                {
                    return;
                }

                reloadToken = Options.ConfigurationLoader.Configuration.GetReloadToken();
                var (endpointsToStop, endpointsToStart) = Options.ConfigurationLoader.Reload();

                Trace.LogDebug("Config reload token fired. Checking for changes...");

                if (endpointsToStop.Count > 0)
                {
                    var urlsToStop = endpointsToStop.Select(lo => lo.EndpointConfig.Url ?? "<unknown>");
                    Trace.LogInformation("Config changed. Stopping the following endpoints: '{endpoints}'", string.Join("', '", urlsToStop));

                    // 5 is the default value for WebHost's "shutdownTimeoutSeconds", so use that.
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_stopCts.Token, timeoutCts.Token);

                    // TODO: It would be nice to start binding to new endpoints immediately and reconfigured endpoints as soon
                    // as the unbinding finished for the given endpoint rather than wait for all transports to unbind first.
                    var configsToStop = endpointsToStop.Select(lo => lo.EndpointConfig).ToList();
                    await _transportManager.StopEndpointsAsync(configsToStop, combinedCts.Token).ConfigureAwait(false);

                    foreach (var listenOption in endpointsToStop)
                    {
                        Options.OptionsInUse.Remove(listenOption);
                        _serverAddresses.InternalCollection.Remove(listenOption.GetDisplayName());
                    }
                }

                if (endpointsToStart.Count > 0)
                {
                    var urlsToStart = endpointsToStart.Select(lo => lo.EndpointConfig.Url ?? "<unknown>");
                    Trace.LogInformation("Config changed. Starting the following endpoints: '{endpoints}'", string.Join("', '", urlsToStart));

                    foreach (var listenOption in endpointsToStart)
                    {
                        try
                        {
                            // TODO: This should probably be canceled by the _stopCts too, but we don't currently support bind cancellation even in StartAsync().
                            await listenOption.BindAsync(AddressBindContext).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Trace.LogCritical(0, ex, "Unable to bind to '{url}' on config reload.", listenOption.EndpointConfig.Url ?? "<unknown>");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.LogCritical(0, ex, "Unable to reload configuration.");
            }
            finally
            {
                _configChangedRegistration = reloadToken?.RegisterChangeCallback(async state => await ((KestrelServer)state).RebindAsync(), this);
                _bindSemaphore.Release();
            }
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

            if (Options.RequestHeaderEncodingSelector is null)
            {
                throw new InvalidOperationException($"{nameof(KestrelServerOptions)}.{nameof(KestrelServerOptions.RequestHeaderEncodingSelector)} must not be set to null.");
            }
        }

        private static ConnectionDelegate EnforceConnectionLimit(ConnectionDelegate innerDelegate, long? connectionLimit, IKestrelTrace trace)
        {
            if (!connectionLimit.HasValue)
            {
                return innerDelegate;
            }

            return new ConnectionLimitMiddleware<ConnectionContext>(c => innerDelegate(c), connectionLimit.Value, trace).OnConnectionAsync;
        }

        private static MultiplexedConnectionDelegate EnforceConnectionLimit(MultiplexedConnectionDelegate innerDelegate, long? connectionLimit, IKestrelTrace trace)
        {
            if (!connectionLimit.HasValue)
            {
                return innerDelegate;
            }

            return new ConnectionLimitMiddleware<MultiplexedConnectionContext>(c => innerDelegate(c), connectionLimit.Value, trace).OnConnectionAsync;
        }
    }
}
