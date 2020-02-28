// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    /// <summary>
    /// In-memory TestServer
    /// </summary
    internal class TestServer : IAsyncDisposable, IDisposable, IStartup
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly RequestDelegate _app;
        private readonly InMemoryTransportFactory _transportFactory;
        private readonly IWebHost _host;

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
            : this(app, context, options => options.ListenOptions.Add(listenOptions), _ => { })
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
                    options.ListenOptions.Add(listenOptions);
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

            var hostBuilder = new WebHostBuilder()
                .UseSetting(WebHostDefaults.ShutdownTimeoutKey, TestConstants.DefaultTimeout.TotalSeconds.ToString())
                .ConfigureServices(services =>
                {
                    configureServices(services);

                    services.AddSingleton<IStartup>(this);
                    services.AddSingleton(context.LoggerFactory);

                    services.AddSingleton<IServer>(sp =>
                    {
                        context.ServerOptions.ApplicationServices = sp;
                        configureKestrel(context.ServerOptions);
                        return new KestrelServer(new List<IConnectionListenerFactory>() { _transportFactory }, context);
                    });
                });

            _host = hostBuilder.Build();
            _host.Start();
        }

        public int Port => 0;

        public TestServiceContext Context { get; }

        public InMemoryHttpClientSlim HttpClientSlim { get; }

        public InMemoryConnection CreateConnection()
        {
            var transportConnection = new InMemoryTransportConnection(_memoryPool, Context.Log, Context.Scheduler);
            _transportFactory.AddConnection(transportConnection);
            return new InMemoryConnection(transportConnection);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return _host.StopAsync(cancellationToken);
        }

        public void Dispose()
        {
            _host.Dispose();
            _memoryPool.Dispose();
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
            // The concrete WebHost implements IAsyncDisposable
            await ((IAsyncDisposable)_host).ConfigureAwait(false).DisposeAsync();
            _memoryPool.Dispose();
        }
    }
}
