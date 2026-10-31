// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests;

public class TestFixture<TStartup> : IDisposable
{
    private readonly TestServer _server;
    private readonly IHost _host;

    public TestFixture()
    {
        // RequestLocalizationOptions saves the current culture when constructed, potentially changing response
        // localization i.e. RequestLocalizationMiddleware behavior. Ensure the saved culture
        // (DefaultRequestCulture) is consistent regardless of system configuration or personal preferences.
        using (new CultureReplacer())
        {
            _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .UseStartup(typeof(TStartup));
            }).Build();

            _host.Start();
            _server = _host.GetTestServer();
        }

        Client = _server.CreateClient();
        Client.BaseAddress = new Uri("http://localhost");
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        _server.Dispose();
        _host.Dispose();
    }
}
