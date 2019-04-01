// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.AspNetCore.Http;

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

            // Assert
            Assert.NotNull(listener.DiagnosticUnhandledException?.HttpContext);
            Assert.NotNull(listener.DiagnosticUnhandledException?.Exception);
            Assert.Null(listener.DiagnosticHandledException?.HttpContext);
            Assert.Null(listener.DiagnosticHandledException?.Exception);
        }

        [Fact]
        public async Task ExceptionPageFiltersAreApplied()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionMessageFilter>();
                })
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new Exception("Test exception");
                    });
                });
            var server = new TestServer(builder);

            // Act
            var response = await server.CreateClient().GetAsync("/path");

            // Assert
            Assert.Equal("Test exception", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ExceptionFilterCallingNextWorks()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IDeveloperPageExceptionFilter, PassThroughExceptionFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, AlwaysBadFormatExceptionFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionMessageFilter>();
                })
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new Exception("Test exception");
                    });
                });
            var server = new TestServer(builder);

            // Act
            var response = await server.CreateClient().GetAsync("/path");

            // Assert
            Assert.Equal("Bad format exception!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ExceptionPageFiltersAreAppliedInOrder()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IDeveloperPageExceptionFilter, AlwaysThrowSameMessageFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionMessageFilter>();
                    services.AddSingleton<IDeveloperPageExceptionFilter, ExceptionToStringFilter>();
                })
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();
                    app.Run(context =>
                    {
                        throw new Exception("Test exception");
                    });
                });
            var server = new TestServer(builder);

            // Act
            var response = await server.CreateClient().GetAsync("/path");

            // Assert
            Assert.Equal("An error occurred", await response.Content.ReadAsStringAsync());
        }

        public static TheoryData CompilationExceptionData
        {
            get
            {
                var variations = new TheoryData<List<CompilationFailure>>();
                var failures = new List<CompilationFailure>();
                var diagnosticMessages = new List<DiagnosticMessage>();
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", diagnosticMessages)
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(null, "source file content", "compiled content", diagnosticMessages)
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", null, "compiled content", diagnosticMessages)
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", null, diagnosticMessages)
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(null, null, null, diagnosticMessages)
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", diagnosticMessages),
                    new CompilationFailure(@"c:\sourcefilepath.cs", null, "compiled content", diagnosticMessages)
                });
                variations.Add(null);
                variations.Add(new List<CompilationFailure>()
                {
                    null
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", diagnosticMessages),
                    null
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", null)
                });
                variations.Add(new List<CompilationFailure>()
                {
                    new CompilationFailure(@"c:\sourcefilepath.cs", "source file content", "compiled content", new List<DiagnosticMessage>(){ null })
                });
                return variations;
            }
        }

        [Theory]
        [MemberData(nameof(CompilationExceptionData))]
        public async Task NullInfoInCompilationException_ShouldNotThrowExceptionGeneratingExceptionPage(
            List<CompilationFailure> failures)
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
                        throw new CustomCompilationException(failures);
                    });
                });
            var server = new TestServer(builder);
            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

            // Act
            await server.CreateClient().GetAsync("/path");

            // Assert
            Assert.NotNull(listener.DiagnosticUnhandledException?.HttpContext);
            Assert.NotNull(listener.DiagnosticUnhandledException?.Exception);
            Assert.Null(listener.DiagnosticHandledException?.HttpContext);
            Assert.Null(listener.DiagnosticHandledException?.Exception);
        }

        public class CustomCompilationException : Exception, ICompilationException
        {
            public CustomCompilationException(IEnumerable<CompilationFailure> compilationFailures)
            {
                CompilationFailures = compilationFailures;
            }

            public IEnumerable<CompilationFailure> CompilationFailures { get; }
        }

        public class ExceptionMessageFilter : IDeveloperPageExceptionFilter
        {
            public Task HandleExceptionAsync(HttpContext context, Exception exception, Func<HttpContext, Exception, Task> next)
            {
                return context.Response.WriteAsync(exception.Message);
            }
        }

        public class ExceptionToStringFilter : IDeveloperPageExceptionFilter
        {
            public Task HandleExceptionAsync(HttpContext context, Exception exception, Func<HttpContext, Exception, Task> next)
            {
                return context.Response.WriteAsync(exception.ToString());
            }
        }

        public class AlwaysThrowSameMessageFilter : IDeveloperPageExceptionFilter
        {
            public Task HandleExceptionAsync(HttpContext context, Exception exception, Func<HttpContext, Exception, Task> next)
            {
                return context.Response.WriteAsync("An error occurred");
            }
        }

        public class AlwaysBadFormatExceptionFilter : IDeveloperPageExceptionFilter
        {
            public Task HandleExceptionAsync(HttpContext context, Exception exception, Func<HttpContext, Exception, Task> next)
            {
                return next(context, new FormatException("Bad format exception!"));
            }
        }

        public class PassThroughExceptionFilter : IDeveloperPageExceptionFilter
        {
            public Task HandleExceptionAsync(HttpContext context, Exception exception, Func<HttpContext, Exception, Task> next)
            {
                return next(context, exception);
            }
        }
    }
}
