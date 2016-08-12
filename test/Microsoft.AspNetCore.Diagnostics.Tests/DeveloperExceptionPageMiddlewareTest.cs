// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics
{
    public class DeveloperExceptionPageMiddlewareTest
    {
        [Fact]
        public async Task UnhandledErrorsWriteToDiagnosticWhenUsingExceptionPage()
        {
            // Arrange
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
                });
            var server = new TestServer(builder);
            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

            // Act
            await server.CreateClient().GetAsync("/path");

            // This ensures that all diagnostics are completely written to the diagnostic listener
            Thread.Sleep(1000);

            // Assert
            Assert.NotNull(listener.EndRequest?.HttpContext);
            Assert.Null(listener.HostingUnhandledException?.HttpContext);
            Assert.Null(listener.HostingUnhandledException?.Exception);
            Assert.NotNull(listener.DiagnosticUnhandledException?.HttpContext);
            Assert.NotNull(listener.DiagnosticUnhandledException?.Exception);
            Assert.Null(listener.DiagnosticHandledException?.HttpContext);
            Assert.Null(listener.DiagnosticHandledException?.Exception);
        }
    }
}
