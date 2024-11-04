// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class MvcTestFixture<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    private ILoggerFactory _loggerFactory;

    public MvcTestFixture(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ILoggerFactory loggerFactory = _loggerFactory;
        var testSink = new TestSink();
        if (_loggerFactory is null)
        {
            loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        }

        builder
            .UseRequestCulture<TStartup>("en-GB", "en-US")
            .UseEnvironment("Production")
            .ConfigureServices(
                services =>
                {
                    services.AddSingleton<ILoggerFactory>(loggerFactory);
                    services.AddSingleton<TestSink>(testSink);
                });
    }

    protected override TestServer CreateServer(IWebHostBuilder builder)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            return base.CreateServer(builder);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            return base.CreateHost(builder);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}
