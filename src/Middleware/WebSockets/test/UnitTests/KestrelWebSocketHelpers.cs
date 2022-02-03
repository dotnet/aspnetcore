// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebSockets.Test;

public class KestrelWebSocketHelpers
{
    public static IAsyncDisposable CreateServer(ILoggerFactory loggerFactory, out int port, Func<HttpContext, Task> app, Action<WebSocketOptions> configure = null)
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

        return new Disposable(async () =>
        {
            await host.StopAsync();
            host.Dispose();
            if (exceptionFromApp is not null)
            {
                ExceptionDispatchInfo.Throw(exceptionFromApp);
            }
        });
    }

    private class Disposable : IAsyncDisposable
    {
        private readonly Func<ValueTask> _dispose;

        public Disposable(Func<ValueTask> dispose)
        {
            _dispose = dispose;
        }

        public ValueTask DisposeAsync()
        {
            return _dispose();
        }
    }
}

