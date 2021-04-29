// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    public class KestrelWebSocketHelpers
    {
        public static IDisposable CreateServer(ILoggerFactory loggerFactory, out int port, Func<HttpContext, Task> app, Action<WebSocketOptions> configure = null)
        {
            Exception exceptionFromApp = null;
            configure = configure ?? (o => { });
            Action<IApplicationBuilder> startup = builder =>
            {
                builder.Use(async (ct, next) =>
                {
                    try
                    {
                        // Kestrel does not return proper error responses:
                        // https://github.com/aspnet/KestrelHttpServer/issues/43
                        await next(ct);
                    }
                    catch (Exception ex)
                    {
                        // capture the exception from the app, we'll throw this at the end of the test when the server is disposed
                        exceptionFromApp = ex;
                        if (ct.Response.HasStarted)
                        {
                            throw;
                        }

                        ct.Response.StatusCode = 500;
                        ct.Response.Headers.Clear();
                        await ct.Response.WriteAsync(ex.ToString());
                    }
                });
                builder.UseWebSockets();
                builder.Run(c => app(c));
            };

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection();
            var config = configBuilder.Build();

            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .ConfigureServices(s =>
                    {
                        s.AddWebSockets(configure);
                        s.AddSingleton(loggerFactory);
                    })
                    .UseConfiguration(config)
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0);
                    })
                    .Configure(startup);
                }).ConfigureHostOptions(o =>
                {
                    o.ShutdownTimeout = TimeSpan.FromSeconds(30);
                }).Build();

            host.Start();
            port = host.GetPort();

            return new Disposable(() =>
            {
                host.Dispose();
                if (exceptionFromApp is not null)
                {
                    ExceptionDispatchInfo.Throw(exceptionFromApp);
                }
            });
        }

        private class Disposable : IDisposable
        {
            private readonly Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }
        }
    }
}

