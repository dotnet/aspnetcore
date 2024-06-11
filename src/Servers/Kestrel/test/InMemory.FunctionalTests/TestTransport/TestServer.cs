// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;

/// <summary>
/// In-memory TestServer
/// </summary
internal class TestServer : IAsyncDisposable, IStartup
{
    private readonly MemoryPool<byte> _memoryPool;
    private readonly RequestDelegate _app;
    private readonly InMemoryTransportFactory _transportFactory;
    private readonly IHost _host;

    public TestServer(RequestDelegate app)
        : this(app, new TestServiceContext())
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context)
        : this(app, context, new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)))
    {
        // The endpoint is ignored, but this ensures no cert loading happens for HTTPS endpoints.
    }

    public TestServer(RequestDelegate app, TestServiceContext context, ListenOptions listenOptions)
        : this(app, context, options => options.CodeBackedListenOptions.Add(listenOptions), _ => { })
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context, Action<ListenOptions> configureListenOptions)
        : this(app, context, options =>
            {
                var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                {
                    KestrelServerOptions = options
                };

                configureListenOptions(listenOptions);
                options.CodeBackedListenOptions.Add(listenOptions);
            },
            _ => { })
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context, Action<KestrelServerOptions> configureKestrel, Action<IServiceCollection> configureServices)
    {
        _app = app;
        Context = context;
        _memoryPool = context.MemoryPoolFactory();
        _transportFactory = new InMemoryTransportFactory();
        HttpClientSlim = new InMemoryHttpClientSlim(this);

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSetting(WebHostDefaults.ShutdownTimeoutKey, context.ShutdownTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture))
                    .Configure(app => { app.Run(_app); });
            })
            .ConfigureServices(services =>
            {
                configureServices(services);

                services.AddSingleton<IStartup>(this);
                services.AddSingleton(context.LoggerFactory);
                services.AddSingleton(context.Metrics);
                services.AddSingleton<IHttpsConfigurationService, HttpsConfigurationService>();
                services.AddSingleton<HttpsConfigurationService.IInitializer, HttpsConfigurationService.Initializer>();

                services.AddSingleton<IServer>(sp =>
                {
                    context.ServerOptions.ApplicationServices = sp;
                    configureKestrel(context.ServerOptions);
                    return new KestrelServerImpl(
                        new IConnectionListenerFactory[] { _transportFactory },
                        sp.GetServices<IMultiplexedConnectionListenerFactory>(),
                        sp.GetRequiredService<IHttpsConfigurationService>(),
                        context);
                });
            });

        _host = hostBuilder.Build();
        _host.Start();
    }

    public int Port => 0;

    public TestServiceContext Context { get; }

    public InMemoryHttpClientSlim HttpClientSlim { get; }

    public InMemoryConnection CreateConnection(Encoding encoding = null, Action<IFeatureCollection> featuresAction = null)
    {
        var transportConnection = new InMemoryTransportConnection(_memoryPool, Context.Log, Context.Scheduler);
        featuresAction?.Invoke(transportConnection.Features);

        _transportFactory.AddConnection(transportConnection);
        return new InMemoryConnection(transportConnection, encoding);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _host.StopAsync(cancellationToken);
    }

    void IStartup.Configure(IApplicationBuilder app)
    {
        app.Run(_app);
    }

    IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
    {
        return services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync().ConfigureAwait(false);
        // The concrete Host implements IAsyncDisposable
        await ((IAsyncDisposable)_host).DisposeAsync().ConfigureAwait(false);
        _memoryPool.Dispose();
    }
}
