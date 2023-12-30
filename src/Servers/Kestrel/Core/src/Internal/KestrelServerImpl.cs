// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

internal sealed class KestrelServerImpl : IServer
{
    private readonly ServerAddressesFeature _serverAddresses;
    private readonly TransportManager _transportManager;
    private readonly List<IConnectionListenerFactory> _transportFactories;
    private readonly List<IMultiplexedConnectionListenerFactory> _multiplexedTransportFactories;
    private readonly IHttpsConfigurationService _httpsConfigurationService;

    private readonly SemaphoreSlim _bindSemaphore = new SemaphoreSlim(initialCount: 1);
    private bool _hasStarted;
    private int _stopping;
    private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
    private readonly TaskCompletionSource _stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    private IDisposable? _configChangedRegistration;

    public KestrelServerImpl(
        IOptions<KestrelServerOptions> options,
        IEnumerable<IConnectionListenerFactory> transportFactories,
        IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedFactories,
        IHttpsConfigurationService httpsConfigurationService,
        ILoggerFactory loggerFactory,
        KestrelMetrics metrics)
        : this(transportFactories, multiplexedFactories, httpsConfigurationService, CreateServiceContext(options, loggerFactory, diagnosticSource: null, metrics))
    {
    }

    // For testing

    internal KestrelServerImpl(
        IEnumerable<IConnectionListenerFactory> transportFactories,
        IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedFactories,
        IHttpsConfigurationService httpsConfigurationService,
        ServiceContext serviceContext)
    {
        ArgumentNullException.ThrowIfNull(transportFactories);

        _transportFactories = transportFactories.Reverse().ToList();
        _multiplexedTransportFactories = multiplexedFactories.Reverse().ToList();
        _httpsConfigurationService = httpsConfigurationService;

        if (_transportFactories.Count == 0 && _multiplexedTransportFactories.Count == 0)
        {
            throw new InvalidOperationException(CoreStrings.TransportNotFound);
        }

        ServiceContext = serviceContext;

        Features = new FeatureCollection();
        _serverAddresses = new ServerAddressesFeature();
        Features.Set<IServerAddressesFeature>(_serverAddresses);

        _transportManager = new TransportManager(_transportFactories, _multiplexedTransportFactories, _httpsConfigurationService, ServiceContext);
    }

    private static ServiceContext CreateServiceContext(IOptions<KestrelServerOptions> options, ILoggerFactory loggerFactory, DiagnosticSource? diagnosticSource, KestrelMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var serverOptions = options.Value ?? new KestrelServerOptions();
        var trace = new KestrelTrace(loggerFactory);
        var connectionManager = new ConnectionManager(
            trace,
            serverOptions.Limits.MaxConcurrentUpgradedConnections);

        var dateHeaderValueManager = new DateHeaderValueManager(TimeProvider.System);

        var heartbeat = new Heartbeat(
            new IHeartbeatHandler[] { dateHeaderValueManager, connectionManager },
            TimeProvider.System,
            DebuggerWrapper.Singleton,
            trace,
            Heartbeat.Interval);

        return new ServiceContext
        {
            Log = trace,
            Scheduler = PipeScheduler.ThreadPool,
            HttpParser = new HttpParser<Http1ParsingHandler>(trace.IsEnabled(LogLevel.Information), serverOptions.DisableHttp1LineFeedTerminators),
            TimeProvider = TimeProvider.System,
            DateHeaderValueManager = dateHeaderValueManager,
            ConnectionManager = connectionManager,
            Heartbeat = heartbeat,
            ServerOptions = serverOptions,
            DiagnosticSource = diagnosticSource,
            Metrics = metrics
        };
    }

    public IFeatureCollection Features { get; }

    public KestrelServerOptions Options => ServiceContext.ServerOptions;

    private ServiceContext ServiceContext { get; }

    private KestrelTrace Trace => ServiceContext.Log;

    private AddressBindContext? AddressBindContext { get; set; }

    public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        try
        {
            ValidateOptions();

            if (_hasStarted)
            {
                // The server has already started and/or has not been cleaned up yet
                throw new InvalidOperationException(CoreStrings.ServerAlreadyStarted);
            }
            _hasStarted = true;

            ServiceContext.Heartbeat?.Start();

            async Task OnBind(ListenOptions options, CancellationToken onBindCancellationToken)
            {
                var hasHttp1 = options.Protocols.HasFlag(HttpProtocols.Http1);
                var hasHttp2 = options.Protocols.HasFlag(HttpProtocols.Http2);
                var hasHttp3 = options.Protocols.HasFlag(HttpProtocols.Http3);
                var hasTls = options.IsTls;

                // Filter out invalid combinations.

                if (!hasTls)
                {
                    // Http/1 without TLS, no-op HTTP/2 and 3.
                    if (hasHttp1)
                    {
                        if (options.ProtocolsSetExplicitly)
                        {
                            if (hasHttp2)
                            {
                                Trace.Http2DisabledWithHttp1AndNoTls(options.EndPoint);
                            }
                            if (hasHttp3)
                            {
                                Trace.Http3DisabledWithHttp1AndNoTls(options.EndPoint);
                            }
                        }

                        hasHttp2 = false;
                        hasHttp3 = false;
                    }
                    // Http/3 requires TLS. Note we only let it fall back to HTTP/1, not HTTP/2
                    else if (hasHttp3)
                    {
                        throw new InvalidOperationException("HTTP/3 requires HTTPS.");
                    }
                }

                // Quic isn't registered if it's not supported, throw if we can't fall back to 1 or 2
                if (hasHttp3 && _multiplexedTransportFactories.Count == 0 && !(hasHttp1 || hasHttp2))
                {
                    throw new InvalidOperationException("Unable to bind an HTTP/3 endpoint. This could be because QUIC has not been configured using UseQuic, or the platform doesn't support QUIC or HTTP/3.");
                }

                // Disable adding alt-svc header if endpoint has configured not to or there is no
                // multiplexed transport factory, which happens if QUIC isn't supported.
                var addAltSvcHeader = !options.DisableAltSvcHeader && _multiplexedTransportFactories.Count > 0;

                var configuredEndpoint = options.EndPoint;

                // Add the HTTP middleware as the terminal connection middleware
                if (hasHttp1 || hasHttp2
                    || options.Protocols == HttpProtocols.None) // TODO a test fails because it doesn't throw an exception in the right place
                                                                // when there is no HttpProtocols in KestrelServer, can we remove/change the test?
                {
                    if (_transportFactories.Count == 0)
                    {
                        throw new InvalidOperationException($"Cannot start HTTP/1.x or HTTP/2 server if no {nameof(IConnectionListenerFactory)} is registered.");
                    }

                    options.UseHttpServer(ServiceContext, application, options.Protocols, addAltSvcHeader);
                    var connectionDelegate = options.Build();

                    // Add the connection limit middleware
                    connectionDelegate = EnforceConnectionLimit(connectionDelegate, Options.Limits.MaxConcurrentConnections, Trace, ServiceContext.Metrics);

                    options.EndPoint = await _transportManager.BindAsync(configuredEndpoint, connectionDelegate, options.EndpointConfig, onBindCancellationToken).ConfigureAwait(false);
                }

                if (hasHttp3 && _multiplexedTransportFactories.Count > 0)
                {
                    // Check if a previous transport has changed the endpoint. If it has then the endpoint is dynamic and we can't guarantee it will work for other transports.
                    // For more details, see https://github.com/dotnet/aspnetcore/issues/42982
                    if (!configuredEndpoint.Equals(options.EndPoint))
                    {
                        Trace.LogError(CoreStrings.DynamicPortOnMultipleTransportsNotSupported);
                    }
                    else
                    {
                        options.UseHttp3Server(ServiceContext, application, options.Protocols, addAltSvcHeader);
                        var multiplexedConnectionDelegate = ((IMultiplexedConnectionBuilder)options).Build();

                        // Add the connection limit middleware
                        multiplexedConnectionDelegate = EnforceConnectionLimit(multiplexedConnectionDelegate, Options.Limits.MaxConcurrentConnections, Trace, ServiceContext.Metrics);

                        options.EndPoint = await _transportManager.BindAsync(configuredEndpoint, multiplexedConnectionDelegate, options, onBindCancellationToken).ConfigureAwait(false);
                    }
                }
            }

            AddressBindContext = new AddressBindContext(_serverAddresses, Options, Trace, OnBind);

            await BindAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Don't log the error https://github.com/dotnet/aspnetcore/issues/29801
            Dispose();
            throw;
        }

        // Register the options with the event source so it can be logged (if necessary)
        KestrelEventSource.Log.AddServerOptions(Options);
    }

    // Graceful shutdown if possible
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            await _stoppedTcs.Task.ConfigureAwait(false);
            return;
        }

        // Stop background heartbeat first.
        // The heartbeat running as the server is shutting down could produce unexpected behavior.
        ServiceContext.Heartbeat?.Dispose();

        _stopCts.Cancel();

#pragma warning disable CA2016 // Don't use cancellationToken when acquiring the semaphore. Dispose calls this with a pre-canceled token.
        await _bindSemaphore.WaitAsync().ConfigureAwait(false);
#pragma warning restore CA2016

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
            _configChangedRegistration?.Dispose();
            _stopCts.Dispose();
            _bindSemaphore.Release();
        }

        _stoppedTcs.TrySetResult();

        // Remove the options from the event source so we don't have a leak if
        // the server is stopped and started again in the same process.
        KestrelEventSource.Log.RemoveServerOptions(Options);
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

            IChangeToken? reloadToken = null;

            _serverAddresses.InternalCollection.PreventPublicMutation();

            if (Options.ConfigurationLoader?.ReloadOnChange == true && (!_serverAddresses.PreferHostingUrls || _serverAddresses.InternalCollection.Count == 0))
            {
                reloadToken = Options.ConfigurationLoader.GetReloadToken();
            }

            Options.ConfigurationLoader?.LoadInternal();
            Options.ConfigurationLoader?.ProcessEndpointsToAdd();

            await AddressBinder.BindAsync(Options.GetListenOptions(), AddressBindContext!, _httpsConfigurationService.UseHttpsWithDefaults, cancellationToken).ConfigureAwait(false);
            _configChangedRegistration = reloadToken?.RegisterChangeCallback(TriggerRebind, this);
        }
        finally
        {
            _bindSemaphore.Release();
        }
    }

    private static void TriggerRebind(object? state)
    {
        if (state is KestrelServerImpl server)
        {
            _ = server.RebindAsync();
        }
    }

    private async Task RebindAsync()
    {
        // Prevents from interfering with shutdown or other rebinds.
        // All exceptions are caught and logged at the critical level.
        await _bindSemaphore.WaitAsync();

        IChangeToken? reloadToken = null;

        try
        {
            if (_stopping == 1)
            {
                return;
            }

            Debug.Assert(Options.ConfigurationLoader != null, "Rebind can only happen when there is a ConfigurationLoader.");

            reloadToken = Options.ConfigurationLoader.GetReloadToken();
            var (endpointsToStop, endpointsToStart) = Options.ConfigurationLoader.Reload();

            Trace.LogDebug("Config reload token fired. Checking for changes...");

            if (endpointsToStop.Count > 0)
            {
                var urlsToStop = endpointsToStop.Select(lo => lo.EndpointConfig!.Url);
                if (Trace.IsEnabled(LogLevel.Information))
                {
                    Trace.LogInformation("Config changed. Stopping the following endpoints: '{endpoints}'", string.Join("', '", urlsToStop));
                }

                // 5 is the default value for WebHost's "shutdownTimeoutSeconds", so use that.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_stopCts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                // TODO: It would be nice to start binding to new endpoints immediately and reconfigured endpoints as soon
                // as the unbinding finished for the given endpoint rather than wait for all transports to unbind first.
                var configsToStop = new List<EndpointConfig>(endpointsToStop.Count);
                foreach (var lo in endpointsToStop)
                {
                    configsToStop.Add(lo.EndpointConfig!);
                }
                await _transportManager.StopEndpointsAsync(configsToStop, cts.Token).ConfigureAwait(false);

                foreach (var listenOption in endpointsToStop)
                {
                    Options.OptionsInUse.Remove(listenOption);
                    _serverAddresses.InternalCollection.Remove(listenOption.GetDisplayName());
                }
            }

            if (endpointsToStart.Count > 0)
            {
                var urlsToStart = endpointsToStart.Select(lo => lo.EndpointConfig!.Url);
                if (Trace.IsEnabled(LogLevel.Information))
                {
                    Trace.LogInformation("Config changed. Starting the following endpoints: '{endpoints}'", string.Join("', '", urlsToStart));
                }

                foreach (var listenOption in endpointsToStart)
                {
                    try
                    {
                        await listenOption.BindAsync(AddressBindContext!, _stopCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (Trace.IsEnabled(LogLevel.Critical))
                        {
                            Trace.LogCritical(0, ex, "Unable to bind to '{url}' on config reload.", listenOption.EndpointConfig!.Url);
                        }
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
            _configChangedRegistration = reloadToken?.RegisterChangeCallback(TriggerRebind, this);
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
    }

    private static ConnectionDelegate EnforceConnectionLimit(ConnectionDelegate innerDelegate, long? connectionLimit, KestrelTrace trace, KestrelMetrics metrics)
    {
        if (!connectionLimit.HasValue)
        {
            return innerDelegate;
        }

        return new ConnectionLimitMiddleware<ConnectionContext>(c => innerDelegate(c), connectionLimit.Value, trace, metrics).OnConnectionAsync;
    }

    private static MultiplexedConnectionDelegate EnforceConnectionLimit(MultiplexedConnectionDelegate innerDelegate, long? connectionLimit, KestrelTrace trace, KestrelMetrics metrics)
    {
        if (!connectionLimit.HasValue)
        {
            return innerDelegate;
        }

        return new ConnectionLimitMiddleware<MultiplexedConnectionContext>(c => innerDelegate(c), connectionLimit.Value, trace, metrics).OnConnectionAsync;
    }
}
