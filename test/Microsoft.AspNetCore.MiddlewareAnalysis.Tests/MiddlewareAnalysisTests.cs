// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.MiddlewareAnalysis
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

            // "Microsoft.AspnNet.Hosting.RequestServicesContainerMiddleware","Microsoft.AspnNet.Diagnostics.DeveloperExceptionPageMiddleware",
            // "Microsoft.AspNet.MiddlewareAnalysis.MiddlewareAnalysisTests.+<>c"
            Assert.Equal(3, listener.MiddlewareStarting.Count);
            Assert.Equal("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareAnalysisTests+<>c", listener.MiddlewareStarting[2]);
            // reversed "RunInlineMiddleware"
            Assert.Equal(1, listener.MiddlewareException.Count);
            Assert.Equal("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareAnalysisTests+<>c", listener.MiddlewareException[0]);
            // reversed "Microsoft.AspnNet.Diagnostics.DeveloperExceptionPageMiddleware","Microsoft.AspnNet.Hosting.RequestServicesContainerMiddleware"
            Assert.Equal(2, listener.MiddlewareFinished.Count);
            Assert.Equal("Microsoft.AspNet.Diagnostics.DeveloperExceptionPageMiddleware", listener.MiddlewareFinished[0]);
        }
    }
}
