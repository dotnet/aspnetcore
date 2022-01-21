// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static class Utilities
{
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

    internal static MessagePump CreatePump(ILoggerFactory loggerFactory = null)
        => new MessagePump(Options.Create(new HttpSysOptions()), loggerFactory ?? new LoggerFactory(), new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())));

    internal static MessagePump CreatePump(Action<HttpSysOptions> configureOptions, ILoggerFactory loggerFactory = null)
    {
        var options = new HttpSysOptions();
        configureOptions(options);
        return new MessagePump(Options.Create(options), loggerFactory ?? new LoggerFactory(), new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())));
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
}
