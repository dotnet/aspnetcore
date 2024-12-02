// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

/// <summary>
/// Summary description for TestServer
/// </summary>
internal class TestServer : IAsyncDisposable, IStartup
{
    private readonly IHost _host;
    private ListenOptions _listenOptions;
    private readonly RequestDelegate _app;

    public TestServer(RequestDelegate app)
        : this(app, new TestServiceContext())
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context)
        : this(app, context, new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)))
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context, ListenOptions listenOptions)
        : this(app, context, options => options.CodeBackedListenOptions.Add(listenOptions), _ => { })
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context, Action<ListenOptions> configureListenOptions, Action<IServiceCollection> configureServices = null)
        : this(app, context, options =>
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                KestrelServerOptions = options
            };
            configureListenOptions(listenOptions);
            options.CodeBackedListenOptions.Add(listenOptions);
        }, s =>
        {
            configureServices?.Invoke(s);
        })
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context, Action<KestrelServerOptions> configureKestrel)
        : this(app, context, configureKestrel, _ => { })
    {
    }

    public TestServer(RequestDelegate app, TestServiceContext context, Action<KestrelServerOptions> configureKestrel, Action<IServiceCollection> configureServices)
    {
        _app = app;
        Context = context;

        _host = TransportSelector.GetHostBuilder(context.MemoryPoolFactory, context.ServerOptions.Limits.MaxRequestBufferSize)
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(options =>
                    {
                        configureKestrel(options);
                        _listenOptions = options.GetListenOptions().First();
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IStartup>(this);
                        services.AddSingleton(context.LoggerFactory);
                        services.AddSingleton<IHttpsConfigurationService, HttpsConfigurationService>();
                        services.AddSingleton<HttpsConfigurationService.IInitializer, HttpsConfigurationService.Initializer>();
                        services.AddSingleton<IServer>(sp =>
                        {
                            // Manually configure options on the TestServiceContext.
                            // We're doing this so we can use the same instance that was passed in
                            var configureOptions = sp.GetServices<IConfigureOptions<KestrelServerOptions>>();
                            foreach (var c in configureOptions)
                            {
                                c.Configure(context.ServerOptions);
                            }

                            return new KestrelServerImpl(sp.GetServices<IConnectionListenerFactory>(), Array.Empty<IMultiplexedConnectionListenerFactory>(), sp.GetRequiredService<IHttpsConfigurationService>(), context);
                        });
                        configureServices(services);
                    })
                    .UseSetting(WebHostDefaults.ApplicationKey, typeof(TestServer).Assembly.FullName)
                    .UseSetting(WebHostDefaults.ShutdownTimeoutKey, TestConstants.DefaultTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture))
                    .Configure(app => { app.Run(_app); });
            })
            .ConfigureServices(services =>
            {
                services.Configure<HostOptions>(option =>
                {
                    option.ShutdownTimeout = TestConstants.DefaultTimeout;
                });
            })
            .Build();

        _host.Start();

        Context.Log.LogDebug($"TestServer is listening on port {Port}");
    }

    // Avoid NullReferenceException in the CanListenToOpenTcpSocketHandle test
    public int Port => _listenOptions.IPEndPoint?.Port ?? 0;

    public TestServiceContext Context { get; }

    void IStartup.Configure(IApplicationBuilder app)
    {
        app.Run(_app);
    }

    IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
    {
        // Unfortunately, this needs to be replaced in IStartup.ConfigureServices
        services.AddSingleton<IHostApplicationLifetime, LifetimeNotImplemented>();
        return services.BuildServiceProvider();
    }

    public TestConnection CreateConnection()
    {
        return new TestConnection(Port, _listenOptions.IPEndPoint.AddressFamily);
    }

    public Task StopAsync(CancellationToken token = default)
    {
        return _host.StopAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync().ConfigureAwait(false);
        // The concrete Host implements IAsyncDisposable
        await ((IAsyncDisposable)_host).DisposeAsync().ConfigureAwait(false);
    }
}
