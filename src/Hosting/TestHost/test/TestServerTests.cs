// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.TestHost;

public class TestServerTests
{
    [Fact]
    public async Task GenericRawCreateAndStartHost_GetTestServer()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IServer>(serviceProvider => new TestServer(serviceProvider));
                    })
                    .Configure(app => { });
            })
            .Build();
        await host.StartAsync();

        var response = await host.GetTestServer().CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenericCreateAndStartHost_GetTestServer()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app => { });
            })
            .StartAsync();

        var response = await host.GetTestServer().CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenericCreateAndStartHost_GetTestClient()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app => { });
            })
            .StartAsync();

        var response = await host.GetTestClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UseTestServerRegistersNoopHostLifetime()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app => { });
            })
            .StartAsync();

        Assert.IsType<NoopHostLifetime>(host.Services.GetService<IHostLifetime>());
    }

    [Fact]
    public void CreateWithDelegate()
    {
        // Arrange
        // Act & Assert (Does not throw)
        new TestServer(new WebHostBuilder().Configure(app => { }));
    }

    [Fact]
    public void CreateWithDelegate_DI()
    {
        var builder = new WebHostBuilder()
            .Configure(app => { })
            .UseTestServer();

        using var host = builder.Build();
        host.Start();
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
    public async Task ServicesCanBeOverridenForTestingAsync()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(s => s.AddSingleton<IServiceProviderFactory<ThirdPartyContainer>, ThirdPartyContainerServiceProviderFactory>())
            .UseStartup<ThirdPartyContainerStartup>()
            .ConfigureTestServices(services => services.AddSingleton(new SimpleService { Message = "OverridesConfigureServices" }))
            .ConfigureTestContainer<ThirdPartyContainer>(container => container.Services.AddSingleton(new TestService { Message = "OverridesConfigureContainer" }));

        var host = new TestServer(builder);

        var response = await host.CreateClient().GetStringAsync("/");

        Assert.Equal("OverridesConfigureServices, OverridesConfigureContainer", response);
    }

    public class ThirdPartyContainerStartup
    {
        public void ConfigureServices(IServiceCollection services) =>
            services.AddSingleton(new SimpleService { Message = "ConfigureServices" });

        public void ConfigureContainer(ThirdPartyContainer container) =>
            container.Services.AddSingleton(new TestService { Message = "ConfigureContainer" });

        public void Configure(IApplicationBuilder app) =>
            app.Run(ctx => ctx.Response.WriteAsync(
                $"{ctx.RequestServices.GetRequiredService<SimpleService>().Message}, {ctx.RequestServices.GetRequiredService<TestService>().Message}"));
    }

    public class ThirdPartyContainer
    {
        public IServiceCollection Services { get; set; }
    }

    public class ThirdPartyContainerServiceProviderFactory : IServiceProviderFactory<ThirdPartyContainer>
    {
        public ThirdPartyContainer CreateBuilder(IServiceCollection services) => new ThirdPartyContainer { Services = services };

        public IServiceProvider CreateServiceProvider(ThirdPartyContainer containerBuilder) => containerBuilder.Services.BuildServiceProvider();
    }

    [Fact]
    public void CaptureStartupErrorsSettingPreserved()
    {
        var builder = new WebHostBuilder()
            .CaptureStartupErrors(true)
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

    [Fact]
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

    [Fact]
    public async Task DispoingTheRequestBodyDoesNotDisposeClientStreams()
    {
        var builder = new WebHostBuilder().Configure(app =>
        {
            app.Run(async context =>
            {
                using (var sr = new StreamReader(context.Request.Body))
                {
                    await context.Response.WriteAsync(await sr.ReadToEndAsync());
                }
            });
        });
        var server = new TestServer(builder);

        var stream = new ThrowOnDisposeStream();
        stream.Write(Encoding.ASCII.GetBytes("Hello World"));
        stream.Seek(0, SeekOrigin.Begin);
        var response = await server.CreateClient().PostAsync("/", new StreamContent(stream));
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
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

    [Fact]
    public async Task CustomServiceProviderSetsApplicationServices()
    {
        var builder = new WebHostBuilder().UseStartup<CustomContainerStartup>();
        var server = new TestServer(builder);
        string result = await server.CreateClient().GetStringAsync("/path");
        Assert.Equal("ApplicationServicesEqual:True", result);
    }

    [Fact]
    public void TestServerConstructorWithFeatureCollectionAllowsInitializingServerFeatures()
    {
        // Arrange
        var url = "http://localhost:8000/appName/serviceName";
        var builder = new WebHostBuilder()
            .UseUrls(url)
            .Configure(applicationBuilder =>
            {
                var serverAddressesFeature = applicationBuilder.ServerFeatures.Get<IServerAddressesFeature>();
                Assert.Contains(serverAddressesFeature.Addresses, s => string.Equals(s, url, StringComparison.Ordinal));
            });

        var featureCollection = new FeatureCollection();
        featureCollection.Set<IServerAddressesFeature>(new ServerAddressesFeature());

        // Act
        new TestServer(builder, featureCollection);

        // Assert
        // Is inside configure callback
    }

    [Fact]
    public void TestServerConstructedWithoutFeatureCollectionHasServerAddressesFeature()
    {
        // Arrange
        var builder = new WebHostBuilder()
            .Configure(applicationBuilder =>
            {
                var serverAddressesFeature = applicationBuilder.ServerFeatures.Get<IServerAddressesFeature>();
                Assert.NotNull(serverAddressesFeature);
            });

        // Act
        new TestServer(builder);

        // Assert
        // Is inside configure callback
    }

    [Fact]
    public void TestServerConstructorWithNullFeatureCollectionThrows()
    {
        var builder = new WebHostBuilder()
            .Configure(b => { });

        Assert.Throws<ArgumentNullException>(() => new TestServer(builder, null));
    }

    [Fact]
    public void TestServerConstructorShouldProvideServicesFromPassedServiceProvider()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // Act
        var testServer = new TestServer(serviceProvider);

        // Assert
        Assert.Equal(serviceProvider, testServer.Services);
    }

    [Fact]
    public void TestServerConstructorShouldProvideServicesFromWebHost()
    {
        // Arrange
        var testService = new TestService();
        var builder = new WebHostBuilder()
            .ConfigureServices(services => services.AddSingleton(testService))
            .Configure(_ => { });

        // Act
        var testServer = new TestServer(builder);

        // Assert
        Assert.Equal(testService, testServer.Services.GetService<TestService>());
    }

    [Fact]
    public async Task TestServerConstructorShouldProvideServicesFromHostBuilder()
    {
        // Arrange
        var testService = new TestService();
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services.AddSingleton(testService))
                    .Configure(_ => { });
            })
            .StartAsync();

        // Act
        // By calling GetTestServer(), a new TestServer instance will be instantiated
        var testServer = host.GetTestServer();

        // Assert
        Assert.Equal(testService, testServer.Services.GetService<TestService>());
    }

    [Fact]
    public async Task TestServerConstructorSetOptions()
    {
        // Arrange
        var baseAddress = new Uri("http://localhost/test");
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer(options =>
                    {
                        options.AllowSynchronousIO = true;
                        options.PreserveExecutionContext = true;
                        options.BaseAddress = baseAddress;
                    })
                    .Configure(_ => { });
            })
            .StartAsync();

        // Act
        // By calling GetTestServer(), a new TestServer instance will be instantiated
        var testServer = host.GetTestServer();

        // Assert
        Assert.True(testServer.AllowSynchronousIO);
        Assert.True(testServer.PreserveExecutionContext);
        Assert.Equal(baseAddress, testServer.BaseAddress);
    }

    public class TestService { public string Message { get; set; } }

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

    [Fact]
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
                    await nxt(context);
                });
                next(app);
            };
        }
    }

    [Fact]
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
                    await nxt(context);
                });
                next(app);
            };
        }
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task CancelAborts()
    {
        var builder = new WebHostBuilder()
                              .Configure(app =>
                              {
                                  app.Run(context =>
                                  {
                                      TaskCompletionSource tcs = new TaskCompletionSource();
                                      tcs.SetCanceled();
                                      return tcs.Task;
                                  });
                              });
        var server = new TestServer(builder);

        await Assert.ThrowsAsync<TaskCanceledException>(async () => { string result = await server.CreateClient().GetStringAsync("/path"); });
    }

    [Fact]
    public async Task CanCreateViaStartupType()
    {
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>();
        var server = new TestServer(builder);
        HttpResponseMessage result = await server.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("FoundService:True", await result.Content.ReadAsStringAsync());
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Theory]
    [InlineData("http://localhost:12345")]
    [InlineData("http://localhost:12345/")]
    [InlineData("http://localhost:12345/hellohellohello")]
    [InlineData("/isthereanybodyinthere?")]
    public async Task ManuallySetHostWinsOverInferredHostFromRequestUri(string uri)
    {
        RequestDelegate appDelegate = ctx =>
            ctx.Response.WriteAsync(ctx.Request.Headers.Host);

        var builder = new WebHostBuilder().Configure(app => app.Run(appDelegate));
        var server = new TestServer(builder);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Host = "otherhost:5678";

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal("otherhost:5678", responseBody);
    }

    private class ThrowOnDisposeStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            throw new InvalidOperationException("Dispose should not happen!");
        }

        public override ValueTask DisposeAsync()
        {
            throw new InvalidOperationException("DisposeAsync should not happen!");
        }
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
