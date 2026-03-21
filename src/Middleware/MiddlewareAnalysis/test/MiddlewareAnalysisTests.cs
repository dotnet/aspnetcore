// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.MiddlewareAnalysis;

public class MiddlewareAnalysisTests
{
    [Fact]
    public async Task ExceptionWrittenToDiagnostics()
    {
        DiagnosticListener diagnosticListener = null;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();

                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new Exception("Test exception");
                    });
                })
                .ConfigureServices(services => services.AddMiddlewareAnalysis());
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var listener = new TestDiagnosticListener();
        diagnosticListener.SubscribeWithAdapter(listener);

        await server.CreateClient().GetAsync(string.Empty);

        // "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware",
        // "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareAnalysisTests.+<>c"
        Assert.Equal(2, listener.MiddlewareStarting.Count);
        Assert.Equal("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareAnalysisTests+<>c", listener.MiddlewareStarting[1]);
        // reversed "RunInlineMiddleware"
        Assert.Single(listener.MiddlewareException);
        Assert.Equal("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareAnalysisTests+<>c", listener.MiddlewareException[0]);
        // reversed "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware"
        Assert.Single(listener.MiddlewareFinished);
        Assert.Equal("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", listener.MiddlewareFinished[0]);
    }
}
