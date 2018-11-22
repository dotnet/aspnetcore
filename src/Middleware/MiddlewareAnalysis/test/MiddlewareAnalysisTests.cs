// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.MiddlewareAnalysis
{
    public class MiddlewareAnalysisTests
    {
        [Fact]
        public async Task ExceptionWrittenToDiagnostics()
        {
            DiagnosticListener diagnosticListener = null;

            var builder = new WebHostBuilder()
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
            var server = new TestServer(builder);

            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);
            
            await server.CreateClient().GetAsync(string.Empty);

            // "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware",
            // "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareAnalysisTests.+<>c"
            Assert.Equal(2, listener.MiddlewareStarting.Count);
            Assert.Equal("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareAnalysisTests+<>c", listener.MiddlewareStarting[1]);
            // reversed "RunInlineMiddleware"
            Assert.Equal(1, listener.MiddlewareException.Count);
            Assert.Equal("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareAnalysisTests+<>c", listener.MiddlewareException[0]);
            // reversed "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware"
            Assert.Equal(1, listener.MiddlewareFinished.Count);
            Assert.Equal("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", listener.MiddlewareFinished[0]);
        }
    }
}
