// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.HttpSys
{
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

        internal static IServer CreateHttpServer(out string baseAddress, RequestDelegate app)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, out root, out baseAddress, options => { }, app);
        }

        internal static IServer CreateHttpServer(out string baseAddress, RequestDelegate app, Action<HttpSysOptions> configureOptions)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, out root, out baseAddress, configureOptions, app);
        }

        internal static IServer CreateHttpServerReturnRoot(string path, out string root, RequestDelegate app)
        {
            string baseAddress;
            return CreateDynamicHttpServer(path, out root, out baseAddress, options => { }, app);
        }

        internal static IServer CreateHttpAuthServer(AuthenticationSchemes authType, bool allowAnonymous, out string baseAddress, RequestDelegate app)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, out root, out baseAddress, options =>
            {
                options.Authentication.Schemes = authType;
                options.Authentication.AllowAnonymous = allowAnonymous;
            }, app);
        }

        internal static IWebHost CreateDynamicHost(AuthenticationSchemes authType, bool allowAnonymous, out string root, RequestDelegate app)
        {
            return CreateDynamicHost(string.Empty, out root, out var baseAddress, options =>
            {
                options.Authentication.Schemes = authType;
                options.Authentication.AllowAnonymous = allowAnonymous;
            }, app);
        }

        internal static IWebHost CreateDynamicHost(out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app)
        {
            return CreateDynamicHost(string.Empty, out var root, out baseAddress, configureOptions, app);
        }

        internal static IWebHost CreateDynamicHost(string basePath, out string root, out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app)
        {
            var prefix = UrlPrefix.Create("http", "localhost", "0", basePath);

            var builder = new WebHostBuilder()
                .UseHttpSys(options =>
                {
                    options.UrlPrefixes.Add(prefix);
                    configureOptions(options);
                })
                .Configure(appBuilder => appBuilder.Run(app));

            var host = builder.Build();

            host.Start();

            var options = host.Services.GetRequiredService<IOptions<HttpSysOptions>>();
            prefix = options.Value.UrlPrefixes.First(); // Has new port
            root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
            baseAddress = prefix.ToString();

            return host;
        }

        internal static MessagePump CreatePump()
            => new MessagePump(Options.Create(new HttpSysOptions()), new LoggerFactory(), new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())));

        internal static MessagePump CreatePump(Action<HttpSysOptions> configureOptions)
        {
            var options = new HttpSysOptions();
            configureOptions(options);
            return new MessagePump(Options.Create(options), new LoggerFactory(), new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())));
        }

        internal static IServer CreateDynamicHttpServer(string basePath, out string root, out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app)
        {
            var prefix = UrlPrefix.Create("http", "localhost", "0", basePath);

            var server = CreatePump(configureOptions);
            server.Features.Get<IServerAddressesFeature>().Addresses.Add(prefix.ToString());
            server.StartAsync(new DummyApplication(app), CancellationToken.None).Wait();

            prefix = server.Listener.Options.UrlPrefixes.First(); // Has new port
            root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
            baseAddress = prefix.ToString();

            return server;
        }

        internal static IServer CreateDynamicHttpsServer(out string baseAddress, RequestDelegate app)
        {
            return CreateDynamicHttpsServer("/", out var root, out baseAddress, options => { }, app);
        }

        internal static IServer CreateDynamicHttpsServer(string basePath, out string root, out string baseAddress, Action<HttpSysOptions> configureOptions, RequestDelegate app)
        {
            lock (PortLock)
            {
                while (NextHttpsPort < MaxHttpsPort)
                {
                    var port = NextHttpsPort++;
                    var prefix = UrlPrefix.Create("https", "localhost", port, basePath);
                    root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
                    baseAddress = prefix.ToString();

                    var server = CreatePump();
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

        internal static Task WithTimeout(this Task task) => task.TimeoutAfter(DefaultTimeout);

        internal static Task<T> WithTimeout<T>(this Task<T> task) => task.TimeoutAfter(DefaultTimeout);
    }
}
