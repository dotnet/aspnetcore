// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    /// <summary>
    /// Summary description for TestServer
    /// </summary>
    public class TestServer : IDisposable, IStartup
    {
        private IWebHost _host;
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
            : this(app, context, listenOptions, _ => { })
        {
        }

        public TestServer(RequestDelegate app, TestServiceContext context, ListenOptions listenOptions, Action<IServiceCollection> configureServices)
        {
            _app = app;
            _listenOptions = listenOptions;
            Context = context;

            _host = TransportSelector.GetWebHostBuilder()
                .UseKestrel(o =>
                {
                    o.ListenOptions.Add(_listenOptions);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IStartup>(this);
                    services.AddSingleton(context.LoggerFactory);
                    services.AddSingleton<IServer>(sp =>
                    {
                        // Manually configure options on the TestServiceContext.
                        // We're doing this so we can use the same instance that was passed in
                        var configureOptions = sp.GetServices<IConfigureOptions<KestrelServerOptions>>();
                        foreach (var c in configureOptions)
                        {
                            c.Configure(context.ServerOptions);
                        }
                        return new KestrelServer(sp.GetRequiredService<ITransportFactory>(), context);
                    });
                    RemoveDevCert(services);
                    configureServices(services);
                })
                .UseSetting(WebHostDefaults.ApplicationKey, typeof(TestServer).GetTypeInfo().Assembly.FullName)
                .Build();

            _host.Start();
        }

        public static void RemoveDevCert(IServiceCollection services)
        {
            // KestrelServerOptionsSetup would scan all system certificates on every test server creation
            // making test runs very slow
            foreach (var descriptor in services.ToArray())
            {
                if (descriptor.ImplementationType == typeof(KestrelServerOptionsSetup))
                {
                    services.Remove(descriptor);
                }
            }
        }

        public IPEndPoint EndPoint => _listenOptions.IPEndPoint;
        public int Port => _listenOptions.IPEndPoint.Port;
        public AddressFamily AddressFamily => _listenOptions.IPEndPoint.AddressFamily;

        public TestServiceContext Context { get; }

        void IStartup.Configure(IApplicationBuilder app)
        {
            app.Run(_app);
        }

        IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
        {
            // Unfortunately, this needs to be replaced in IStartup.ConfigureServices
            services.AddSingleton<IApplicationLifetime, LifetimeNotImplemented>();
            return services.BuildServiceProvider();
        }

        public TestConnection CreateConnection()
        {
            return new TestConnection(Port, AddressFamily);
        }

        public Task StopAsync()
        {
            return _host.StopAsync();
        }

        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
