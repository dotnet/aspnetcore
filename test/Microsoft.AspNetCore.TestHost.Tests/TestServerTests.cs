// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DiagnosticAdapter;
using Xunit;

namespace Microsoft.AspNetCore.TestHost
{
    public class TestServerTests
    {
        [Fact]
        public void CreateWithDelegate()
        {
            // Arrange
            // Act & Assert (Does not throw)
            new TestServer(new WebHostBuilder().Configure(app => { }));
        }


        [Fact]
        public void DoesNotCaptureStartupErrorsByDefault()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    throw new InvalidOperationException();
                });

            Assert.Throws<InvalidOperationException>(() => new TestServer(builder));
        }


        [Fact]
        public void CaptureStartupErrorsSettingPreserved()
        {
            var builder = new WebHostBuilder()
                .UseCaptureStartupErrors(true)
                .Configure(app =>
                {
                    throw new InvalidOperationException();
                });

            // Does not throw
            new TestServer(builder);
        }

        [Fact]
        public void ApplicationServicesAvailableFromTestServer()
        {
            var testService = new TestService();
            var builder = new WebHostBuilder()
                .Configure(app => { })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(testService);
                });
            var server = new TestServer(builder);

            Assert.Equal(testService, server.Host.Services.GetRequiredService<TestService>());
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task RequestServicesAutoCreated()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    return context.Response.WriteAsync("RequestServices:" + (context.RequestServices != null));
                });
            });
            var server = new TestServer(builder);

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("RequestServices:True", result);
        }

        public class CustomContainerStartup
        {
            public IServiceProvider Services;
            public IServiceProvider ConfigureServices(IServiceCollection services)
            {
                Services = services.BuildServiceProvider();
                return Services;
            }

            public void Configure(IApplicationBuilder app)
            {
                var applicationServices = app.ApplicationServices;
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("ApplicationServicesEqual:" + (applicationServices == Services));
                });
            }

        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CustomServiceProviderSetsApplicationServices()
        {
            var builder = new WebHostBuilder().UseStartup<CustomContainerStartup>();
            var server = new TestServer(builder);
            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("ApplicationServicesEqual:True", result);
        }

        public class TestService { }

        public class TestRequestServiceMiddleware
        {
            private RequestDelegate _next;

            public TestRequestServiceMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public Task Invoke(HttpContext httpContext)
            {
                var services = new ServiceCollection();
                services.AddTransient<TestService>();
                httpContext.RequestServices = services.BuildServiceProvider();

                return _next.Invoke(httpContext);
            }
        }

        public class RequestServicesFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return builder =>
                {
                    builder.UseMiddleware<TestRequestServiceMiddleware>();
                    next(builder);
                };
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task ExistingRequestServicesWillNotBeReplaced()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    var service = context.RequestServices.GetService<TestService>();
                    return context.Response.WriteAsync("Found:" + (service != null));
                });
            })
            .ConfigureServices(services =>
            {
                services.AddTransient<IStartupFilter, RequestServicesFilter>();
            });
            var server = new TestServer(builder);

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("Found:True", result);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CanSetCustomServiceProvider()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    context.RequestServices = new ServiceCollection()
                    .AddTransient<TestService>()
                    .BuildServiceProvider();

                    var s = context.RequestServices.GetRequiredService<TestService>();

                    return context.Response.WriteAsync("Success");
                });
            });
            var server = new TestServer(builder);

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("Success", result);
        }

        public class ReplaceServiceProvidersFeatureFilter : IStartupFilter, IServiceProvidersFeature
        {
            public ReplaceServiceProvidersFeatureFilter(IServiceProvider appServices, IServiceProvider requestServices)
            {
                ApplicationServices = appServices;
                RequestServices = requestServices;
            }

            public IServiceProvider ApplicationServices { get; set; }

            public IServiceProvider RequestServices { get; set; }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.Use(async (context, nxt) =>
                    {
                        context.Features.Set<IServiceProvidersFeature>(this);
                        await nxt();
                    });
                    next(app);
                };
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task ExistingServiceProviderFeatureWillNotBeReplaced()
        {
            var appServices = new ServiceCollection().BuildServiceProvider();
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    Assert.Equal(appServices, context.RequestServices);
                    return context.Response.WriteAsync("Success");
                });
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new ReplaceServiceProvidersFeatureFilter(appServices, appServices));
            });
            var server = new TestServer(builder);

            var result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("Success", result);
        }

        public class NullServiceProvidersFeatureFilter : IStartupFilter, IServiceProvidersFeature
        {
            public IServiceProvider ApplicationServices { get; set; }

            public IServiceProvider RequestServices { get; set; }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.Use(async (context, nxt) =>
                    {
                        context.Features.Set<IServiceProvidersFeature>(this);
                        await nxt();
                    });
                    next(app);
                };
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task WillReplaceServiceProviderFeatureWithNullRequestServices()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    Assert.Null(context.RequestServices);
                    return context.Response.WriteAsync("Success");
                });
            })
            .ConfigureServices(services =>
            {
                services.AddTransient<IStartupFilter, NullServiceProvidersFeatureFilter>();
            });
            var server = new TestServer(builder);

            var result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("Success", result);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CanAccessLogger()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    var logger = app.ApplicationServices.GetRequiredService<ILogger<HttpContext>>();
                    return context.Response.WriteAsync("FoundLogger:" + (logger != null));
                });
            });
            var server = new TestServer(builder);

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("FoundLogger:True", result);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CanAccessHttpContext()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    var accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
                    return context.Response.WriteAsync("HasContext:" + (accessor.HttpContext != null));
                });
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            });
            var server = new TestServer(builder);

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("HasContext:True", result);
        }

        public class ContextHolder
        {
            public ContextHolder(IHttpContextAccessor accessor)
            {
                Accessor = accessor;
            }

            public IHttpContextAccessor Accessor { get; set; }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CanAddNewHostServices()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    var accessor = app.ApplicationServices.GetRequiredService<ContextHolder>();
                    return context.Response.WriteAsync("HasContext:" + (accessor.Accessor.HttpContext != null));
                });
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddSingleton<ContextHolder>();
            });
            var server = new TestServer(builder);

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("HasContext:True", result);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CreateInvokesApp()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(context =>
                {
                    return context.Response.WriteAsync("CreateInvokesApp");
                });
            });
            var server = new TestServer(builder);

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("CreateInvokesApp", result);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task DisposeStreamIgnored()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("Response");
                    context.Response.Body.Dispose();
                });
            });
            var server = new TestServer(builder);

            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("Response", await result.Content.ReadAsStringAsync());
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task DisposedServerThrows()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("Response");
                    context.Response.Body.Dispose();
                });
            });
            var server = new TestServer(builder);

            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            server.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => server.CreateClient().GetAsync("/"));
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CancelAborts()
        {
            var builder = new WebHostBuilder()
                                  .Configure(app =>
                                  {
                                      app.Run(context =>
                                      {
                                          TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                                          tcs.SetCanceled();
                                          return tcs.Task;
                                      });
                                  });
            var server = new TestServer(builder);

            await Assert.ThrowsAsync<TaskCanceledException>(async () => { string result = await server.CreateClient().GetStringAsync("/path"); });
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CanCreateViaStartupType()
        {
            var builder = new WebHostBuilder()
                .UseStartup<TestStartup>();
            var server = new TestServer(builder);
            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("FoundService:True", await result.Content.ReadAsStringAsync());
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task CanCreateViaStartupTypeAndSpecifyEnv()
        {
            var builder = new WebHostBuilder()
                            .UseStartup<TestStartup>()
                            .UseEnvironment("Foo");
            var server = new TestServer(builder);

            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("FoundFoo:False", await result.Content.ReadAsStringAsync());
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task BeginEndDiagnosticAvailable()
        {
            DiagnosticListener diagnosticListener = null;

            var builder = new WebHostBuilder()
                            .Configure(app =>
                            {
                                diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();
                                app.Run(context =>
                                {
                                    return context.Response.WriteAsync("Hello World");
                                });
                            });
            var server = new TestServer(builder);

            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);
            var result = await server.CreateClient().GetStringAsync("/path");

            // This ensures that all diagnostics are completely written to the diagnostic listener
            Thread.Sleep(1000);

            Assert.Equal("Hello World", result);
            Assert.NotNull(listener.BeginRequest?.HttpContext);
            Assert.NotNull(listener.EndRequest?.HttpContext);
            Assert.Null(listener.UnhandledException);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono, SkipReason = "Hangs randomly (issue #507)")]
        public async Task ExceptionDiagnosticAvailable()
        {
            DiagnosticListener diagnosticListener = null;
            var builder = new WebHostBuilder().Configure(app =>
            {
                diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();
                app.Run(context =>
                {
                    throw new Exception("Test exception");
                });
            });
            var server = new TestServer(builder);

            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);
            await Assert.ThrowsAsync<Exception>(() => server.CreateClient().GetAsync("/path"));

            // This ensures that all diagnostics are completely written to the diagnostic listener
            Thread.Sleep(1000);

            Assert.NotNull(listener.BeginRequest?.HttpContext);
            Assert.Null(listener.EndRequest?.HttpContext);
            Assert.NotNull(listener.UnhandledException?.HttpContext);
            Assert.NotNull(listener.UnhandledException?.Exception);
        }

        public class TestDiagnosticListener
        {
            public class OnBeginRequestEventData
            {
                public IProxyHttpContext HttpContext { get; set; }
            }

            public OnBeginRequestEventData BeginRequest { get; set; }

            [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
            public virtual void OnBeginRequest(IProxyHttpContext httpContext)
            {
                BeginRequest = new OnBeginRequestEventData()
                {
                    HttpContext = httpContext,
                };
            }

            public class OnEndRequestEventData
            {
                public IProxyHttpContext HttpContext { get; set; }
            }

            public OnEndRequestEventData EndRequest { get; set; }

            [DiagnosticName("Microsoft.AspNetCore.Hosting.EndRequest")]
            public virtual void OnEndRequest(IProxyHttpContext httpContext)
            {
                EndRequest = new OnEndRequestEventData()
                {
                    HttpContext = httpContext,
                };
            }

            public class OnUnhandledExceptionEventData
            {
                public IProxyHttpContext HttpContext { get; set; }
                public IProxyException Exception { get; set; }
            }

            public OnUnhandledExceptionEventData UnhandledException { get; set; }

            [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
            public virtual void OnUnhandledException(IProxyHttpContext httpContext, IProxyException exception)
            {
                UnhandledException = new OnUnhandledExceptionEventData()
                {
                    HttpContext = httpContext,
                    Exception = exception,
                };
            }
        }

        public interface IProxyHttpContext
        {
        }

        public interface IProxyException
        {
        }

        public class Startup
        {
            public void Configure(IApplicationBuilder builder)
            {
                builder.Run(ctx => ctx.Response.WriteAsync("Startup"));
            }
        }

        public class SimpleService
        {
            public SimpleService()
            {
            }

            public string Message { get; set; }
        }

        public class TestStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton<SimpleService>();
            }

            public void ConfigureFooServices(IServiceCollection services)
            {
            }

            public void Configure(IApplicationBuilder app)
            {
                app.Run(context =>
                {
                    var service = app.ApplicationServices.GetRequiredService<SimpleService>();
                    return context.Response.WriteAsync("FoundService:" + (service != null));
                });
            }

            public void ConfigureFoo(IApplicationBuilder app)
            {
                app.Run(context =>
                {
                    var service = app.ApplicationServices.GetService<SimpleService>();
                    return context.Response.WriteAsync("FoundFoo:" + (service != null));
                });
            }
        }
    }
}
