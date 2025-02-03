// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// Kestrel server.
/// </summary>
public class KestrelServer : IServer
{
    private readonly KestrelServerImpl _innerKestrelServer;

    /// <summary>
    /// Initializes a new instance of <see cref="KestrelServer"/>.
    /// </summary>
    /// <param name="options">The Kestrel <see cref="IOptions{TOptions}"/>.</param>
    /// <param name="transportFactory">The <see cref="IConnectionListenerFactory"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public KestrelServer(IOptions<KestrelServerOptions> options, IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory)
    {
        _innerKestrelServer = new KestrelServerImpl(
            options,
            new[] { transportFactory ?? throw new ArgumentNullException(nameof(transportFactory)) },
            Array.Empty<IMultiplexedConnectionListenerFactory>(),
            new SimpleHttpsConfigurationService(),
            loggerFactory,
            diagnosticSource: null,
            new KestrelMetrics(new DummyMeterFactory()));
    }

    /// <inheritdoc />
    public IFeatureCollection Features => _innerKestrelServer.Features;

    /// <summary>
    /// Gets the <see cref="KestrelServerOptions"/>.
    /// </summary>
    public KestrelServerOptions Options => _innerKestrelServer.Options;

    /// <inheritdoc />
    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        return _innerKestrelServer.StartAsync(application, cancellationToken);
    }

    // Graceful shutdown if possible
    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _innerKestrelServer.StopAsync(cancellationToken);
    }

    // Ungraceful shutdown
    /// <inheritdoc />
    public void Dispose()
    {
        _innerKestrelServer.Dispose();
    }

    // This factory used when type is created without DI. For example, via KestrelServer.
    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new Meter(options);

        public void Dispose() { }
    }

    private sealed class SimpleHttpsConfigurationService : IHttpsConfigurationService
    {
        public bool IsInitialized => true;

        public void Initialize(IHostEnvironment hostEnvironment, ILogger<KestrelServer> serverLogger, ILogger<HttpsConnectionMiddleware> httpsLogger)
        {
            // Already initialized
        }

        public void PopulateMultiplexedTransportFeatures(FeatureCollection features, ListenOptions listenOptions)
        {
            throw new NotImplementedException(); // Not actually required by this impl, which never provides an IMultiplexedConnectionListenerFactory
        }

        public ListenOptions UseHttpsWithDefaults(ListenOptions listenOptions)
        {
            return HttpsConfigurationService.UseHttpsWithDefaultsWorker(listenOptions);
        }

        public void ApplyHttpsConfiguration(
            HttpsConnectionAdapterOptions httpsOptions,
            EndpointConfig endpoint,
            KestrelServerOptions serverOptions,
            CertificateConfig? defaultCertificateConfig,
            ConfigurationReader configurationReader)
        {
            throw new NotImplementedException(); // Not actually required by this impl
        }

        public ListenOptions UseHttpsWithSni(ListenOptions listenOptions, HttpsConnectionAdapterOptions httpsOptions, EndpointConfig endpoint)
        {
            throw new NotImplementedException(); // Not actually required by this impl
        }

        public CertificateAndConfig? LoadDefaultCertificate(ConfigurationReader configurationReader)
        {
            throw new NotImplementedException(); // Not actually required by this impl
        }
    }
}
