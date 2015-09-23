// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.TelemetryAdapter;
using Xunit;

namespace Microsoft.AspNet.TestHost
{
    public class TestServerTests
    {
        [Fact]
        public void CreateWithDelegate()
        {
            // Arrange
            // Act & Assert (Does not throw)
            TestServer.Create(app => { });
        }

        [Fact]
        public void ThrowsIfNoApplicationEnvironmentIsRegisteredWithTheProvider()
        {
            // Arrange
            var services = new ServiceCollection().BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => TestServer.Create(
                    services,
                    new ConfigurationBuilder().Build(), new Startup().Configure, configureServices: null));
        }

        [Fact]
        public async Task RequestServicesAutoCreated()
        {
            var server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    return context.Response.WriteAsync("RequestServices:" + (context.RequestServices != null));
                });
            });

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
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("ApplicationServicesEqual:" + (context.ApplicationServices == Services));
                });
            }

        }

        [Fact]
        public async Task CustomServiceProviderReplacesApplicationServices()
        {
            var server = new TestServer(TestServer.CreateBuilder().UseStartup<CustomContainerStartup>());
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

        [Fact]
        public async Task ExistingRequestServicesWillNotBeReplaced()
        {
            var server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    var service = context.RequestServices.GetService<TestService>();
                    return context.Response.WriteAsync("Found:" + (service != null));
                });
            },
            services => services.AddTransient<IStartupFilter, RequestServicesFilter>());
            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("Found:True", result);
        }

        [Fact]
        public async Task SettingApplicationServicesOnFeatureToNullThrows()
        {
            var server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    var feature = context.Features.Get<IServiceProvidersFeature>();
                    Assert.Throws<ArgumentNullException>(() => feature.ApplicationServices = null);
                    return context.Response.WriteAsync("Success");
                });
            });
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

        [Fact]
        public async Task ExistingServiceProviderFeatureWillNotBeReplaced()
        {
            var appServices = new ServiceCollection().BuildServiceProvider();
            var server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    Assert.Equal(appServices, context.ApplicationServices);
                    Assert.Equal(appServices, context.RequestServices);
                    return context.Response.WriteAsync("Success");
                });
            },
            services => services.AddInstance<IStartupFilter>(new ReplaceServiceProvidersFeatureFilter(appServices, appServices)));
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

        [Fact]
        public async Task WillReplaceServiceProviderFeatureWithNullRequestServices()
        {
            var server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    Assert.NotNull(context.ApplicationServices);
                    Assert.NotNull(context.RequestServices);
                    return context.Response.WriteAsync("Success");
                });
            },
            services => services.AddTransient<IStartupFilter, NullServiceProvidersFeatureFilter>());
            var result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("Success", result);
        }

        public class EnsureApplicationServicesFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return builder =>
                {
                    builder.Run(context => {
                        Assert.NotNull(context.ApplicationServices);
                        return context.Response.WriteAsync("Done");
                    });
                };
            }
        }

        [Fact]
        public async Task ApplicationServicesShouldSetBeforeStatupFilters()
        {
            var server = TestServer.Create(app => { },
                services => services.AddTransient<IStartupFilter, EnsureApplicationServicesFilter>());
            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("Done", result);
        }


        [Fact]
        public async Task CanAccessLogger()
        {
            var server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    var logger = app.ApplicationServices.GetRequiredService<ILogger<HttpContext>>();
                    return context.Response.WriteAsync("FoundLogger:" + (logger != null));
                });
            });

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("FoundLogger:True", result);
        }

        [Fact]
        public async Task CanAccessHttpContext()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    var accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
                    return context.Response.WriteAsync("HasContext:"+(accessor.HttpContext != null));
                });
            });

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

        [Fact]
        public async Task CanAddNewHostServices()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    var accessor = app.ApplicationServices.GetRequiredService<ContextHolder>();
                    return context.Response.WriteAsync("HasContext:" + (accessor.Accessor.HttpContext != null));
                });
            },
            services => services.AddSingleton<ContextHolder>());

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("HasContext:True", result);
        }

        [Fact]
        public async Task CreateInvokesApp()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    return context.Response.WriteAsync("CreateInvokesApp");
                });
            });

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("CreateInvokesApp", result);
        }

        [Fact]
        public void WebRootCanBeResolvedWhenNotInTheProjectJson()
        {
            TestServer server = TestServer.Create(app =>
            {
                var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.Equal(Directory.GetCurrentDirectory(), env.WebRootPath);
            });
        }

        [Fact]
        public async Task DisposeStreamIgnored()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("Response");
                    context.Response.Body.Dispose();
                });
            });

            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("Response", await result.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task DisposedServerThrows()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("Response");
                    context.Response.Body.Dispose();
                });
            });

            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            server.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => server.CreateClient().GetAsync("/"));
        }

        [Fact]
        public void CancelAborts()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                    tcs.SetCanceled();
                    return tcs.Task;
                });
            });

            Assert.Throws<AggregateException>(() => { string result = server.CreateClient().GetStringAsync("/path").Result; });
        }

        [Fact]
        public async Task CanCreateViaStartupType()
        {
            TestServer server = new TestServer(TestServer.CreateBuilder().UseStartup<TestStartup>());
            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("FoundService:True", await result.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanCreateViaStartupTypeAndSpecifyEnv()
        {
            TestServer server = new TestServer(TestServer.CreateBuilder()
                    .UseStartup<TestStartup>()
                    .UseEnvironment("Foo"));
            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("FoundFoo:False", await result.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BeginEndTelemetryAvailable()
        {
            TelemetryListener telemetryListener = null;
            var server = TestServer.Create(app =>
            {
                telemetryListener = app.ApplicationServices.GetRequiredService<TelemetryListener>();
                app.Run(context =>
                {
                    return context.Response.WriteAsync("Hello World");
                });
            });
            var listener = new TestTelemetryListener();
            telemetryListener.SubscribeWithAdapter(listener);
            var result = await server.CreateClient().GetStringAsync("/path");

            Assert.Equal("Hello World", result);
            Assert.NotNull(listener.BeginRequest?.HttpContext);
            Assert.NotNull(listener.EndRequest?.HttpContext);
            Assert.Null(listener.UnhandledException);
        }

        [Fact]
        public async Task ExceptionTelemetryAvailable()
        {
            TelemetryListener telemetryListener = null;
            var server = TestServer.Create(app =>
            {
                telemetryListener = app.ApplicationServices.GetRequiredService<TelemetryListener>();
                app.Run(context =>
                {
                    throw new Exception("Test exception");
                });
            });
            var listener = new TestTelemetryListener();
            telemetryListener.SubscribeWithAdapter(listener);
            await Assert.ThrowsAsync<Exception>(() => server.CreateClient().GetAsync("/path"));

            Assert.NotNull(listener.BeginRequest?.HttpContext);
            Assert.Null(listener.EndRequest?.HttpContext);
            Assert.NotNull(listener.UnhandledException?.HttpContext);
            Assert.NotNull(listener.UnhandledException?.Exception);
        }

        public class TestTelemetryListener
        {
            public class OnBeginRequestEventData
            {
                public IProxyHttpContext HttpContext { get; set; }
            }

            public OnBeginRequestEventData BeginRequest { get; set; }

            [TelemetryName("Microsoft.AspNet.Hosting.BeginRequest")]
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

            [TelemetryName("Microsoft.AspNet.Hosting.EndRequest")]
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

            [TelemetryName("Microsoft.AspNet.Hosting.UnhandledException")]
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
