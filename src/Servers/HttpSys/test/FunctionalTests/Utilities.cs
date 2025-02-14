// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static class Utilities
{
    // When tests projects are run in parallel, overlapping port ranges can cause a race condition when looking for free
    // ports during dynamic port allocation.
    private const int BaseHttpsPort = 44300;
    private const int MaxHttpsPort = 44399;
    private static int NextHttpsPort = BaseHttpsPort;
    private static object PortLock = new object();
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    internal static readonly int WriteRetryLimit = 1000;

    // Minimum support for Windows 7 is assumed.
    internal static readonly bool IsWin8orLater;

    static Utilities()
    {
        var win8Version = new Version(6, 2);
        IsWin8orLater = (Environment.OSVersion.Version >= win8Version);
    }

    internal static IServer CreateHttpServer(out string baseAddress, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        string root;
        return CreateDynamicHttpServer(string.Empty, out root, out baseAddress, options => { }, app, loggerFactory);
    }

    internal static IServer CreateHttpServer(out string baseAddress, RequestDelegate app, Action<HttpSysOptions> configureOptions, ILoggerFactory loggerFactory)
    {
        string root;
        return CreateDynamicHttpServer(string.Empty, out root, out baseAddress, configureOptions, app, loggerFactory);
    }

    internal static IServer CreateHttpServerReturnRoot(string path, out string root, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        string baseAddress;
        return CreateDynamicHttpServer(path, out root, out baseAddress, options => { }, app, loggerFactory);
    }

    internal static IServer CreateHttpAuthServer(AuthenticationSchemes authType, bool allowAnonymous, out string baseAddress, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        string root;
        return CreateDynamicHttpServer(string.Empty, out root, out baseAddress, options =>
        {
            options.Authentication.Schemes = authType;
            options.Authentication.AllowAnonymous = allowAnonymous;
        }, app, loggerFactory);
    }

    internal static IHost CreateDynamicHost(AuthenticationSchemes authType, bool allowAnonymous, out string root, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        return CreateDynamicHost(string.Empty, out root, out var baseAddress, options =>
        {
            options.Authentication.Schemes = authType;
            options.Authentication.AllowAnonymous = allowAnonymous;
        }, app, loggerFactory);
    }

    internal static IHost CreateDynamicHost(out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        return CreateDynamicHost(string.Empty, out var root, out baseAddress, configureOptions, app, loggerFactory);
    }

    internal static IHost CreateDynamicHost(string basePath, out string root, out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        var prefix = UrlPrefix.Create("http", "localhost", "0", basePath);

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseHttpSys(options =>
                    {
                        options.UrlPrefixes.Add(prefix);
                        configureOptions(options);
                    })
                    .ConfigureLogging(builder => builder.AddProvider(new ForwardingLoggerProvider(loggerFactory)))
                    .Configure(appBuilder => appBuilder.Run(app));
            });

        var host = builder.Build();

        host.Start();

        var options = host.Services.GetRequiredService<IOptions<HttpSysOptions>>();
        prefix = options.Value.UrlPrefixes.First(); // Has new port
        root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
        baseAddress = prefix.ToString();

        return host;
    }

    internal static MessagePump CreatePump(ILoggerFactory loggerFactory)
        => new MessagePump(Options.Create(new HttpSysOptions()), loggerFactory ?? new LoggerFactory(), new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())));

    internal static MessagePump CreatePump(Action<HttpSysOptions> configureOptions, ILoggerFactory loggerFactory)
    {
        var options = new HttpSysOptions();
        configureOptions(options);
        return new MessagePump(Options.Create(options), loggerFactory ?? new LoggerFactory(), new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())));
    }

    internal static IServer CreateDynamicHttpServer(string basePath, out string root, out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        var prefix = UrlPrefix.Create("http", "localhost", "0", basePath);

        var server = CreatePump(configureOptions, loggerFactory);
        server.Features.Get<IServerAddressesFeature>().Addresses.Add(prefix.ToString());
        server.StartAsync(new DummyApplication(app), CancellationToken.None).Wait();

        prefix = server.Listener.Options.UrlPrefixes.First(); // Has new port
        root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
        baseAddress = prefix.ToString();

        return server;
    }

    internal static IServer CreateDynamicHttpsServer(out string baseAddress, RequestDelegate app, ILoggerFactory loggerFactory)
    {
        return CreateDynamicHttpsServer("/", out var root, out baseAddress, options => { }, app, loggerFactory);
    }

    internal static IServer CreateDynamicHttpsServer(string basePath, out string root, out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app, ILoggerFactory loggerFactory = null)
    {
        lock (PortLock)
        {
            while (NextHttpsPort < MaxHttpsPort)
            {
                var port = NextHttpsPort++;
                var prefix = UrlPrefix.Create("https", "localhost", port, basePath);
                root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
                baseAddress = prefix.ToString();

                var server = CreatePump(loggerFactory);
                server.Features.Get<IServerAddressesFeature>().Addresses.Add(baseAddress);
                configureOptions(server.Listener.Options);
                try
                {
                    server.StartAsync(new DummyApplication(app), CancellationToken.None).Wait();
                    return server;
                }
                catch (HttpSysException)
                {
                }
            }
            NextHttpsPort = BaseHttpsPort;
        }
        throw new Exception("Failed to locate a free port.");
    }

    internal static bool? CanHaveBody(this HttpRequest request)
    {
        return request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
    }

    private sealed class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        public ForwardingLoggerProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }
    }
}
