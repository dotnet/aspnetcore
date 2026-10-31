// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class CorsMiddlewareTests
{
    private const string OriginUrl = "http://api.example.com";

    [Theory]
    [InlineData("PuT")]
    [InlineData("PUT")]
    public async Task CorsRequest_MatchesPolicy_OnCaseInsensitiveAccessControlRequestMethod(string accessControlRequestMethod)
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors(builder =>
                        builder.WithOrigins(OriginUrl)
                               .WithMethods("PUT"));
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services => services.AddCors());
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Actual request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .SendAsync(accessControlRequestMethod);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Single(response.Headers);
            Assert.Equal("Cross origin response", await response.Content.ReadAsStringAsync());
            Assert.Equal(OriginUrl, response.Headers.GetValues(CorsConstants.AccessControlAllowOrigin).FirstOrDefault());
        }
    }

    [Fact]
    public async Task CorsRequest_MatchPolicy_SetsResponseHeaders()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors(builder =>
                        builder.WithOrigins(OriginUrl)
                               .WithMethods("PUT")
                               .WithHeaders("Header1")
                               .WithExposedHeaders("AllowedHeader"));
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services => services.AddCors());
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Actual request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .SendAsync("PUT");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(2, response.Headers.Count());
            Assert.Equal("Cross origin response", await response.Content.ReadAsStringAsync());
            Assert.Equal(OriginUrl, response.Headers.GetValues(CorsConstants.AccessControlAllowOrigin).FirstOrDefault());
            Assert.Equal("AllowedHeader", response.Headers.GetValues(CorsConstants.AccessControlExposeHeaders).FirstOrDefault());
        }
    }

    [Theory]
    [InlineData("OpTions")]
    [InlineData("OPTIONS")]
    public async Task PreFlight_MatchesPolicy_OnCaseInsensitiveOptionsMethod(string preflightMethod)
    {
        // Arrange
        var policy = new CorsPolicy();
        policy.Origins.Add(OriginUrl);
        policy.Methods.Add("PUT");

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors("customPolicy");
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddCors(options =>
                    {
                        options.AddPolicy("customPolicy", policy);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Preflight request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .SendAsync(preflightMethod);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Single(response.Headers);
            Assert.Equal(OriginUrl, response.Headers.GetValues(CorsConstants.AccessControlAllowOrigin).FirstOrDefault());
        }
    }

    [Fact]
    public async Task PreFlight_MatchesPolicy_SetsResponseHeaders()
    {
        // Arrange
        var policy = new CorsPolicy();
        policy.Origins.Add(OriginUrl);
        policy.Methods.Add("PUT");
        policy.Headers.Add("Header1");
        policy.ExposedHeaders.Add("AllowedHeader");

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors("customPolicy");
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddCors(options =>
                    {
                        options.AddPolicy("customPolicy", policy);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Preflight request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .AddHeader(CorsConstants.AccessControlRequestMethod, "PUT")
                .SendAsync(CorsConstants.PreflightHttpMethod);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Collection(
                response.Headers.OrderBy(h => h.Key),
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowHeaders, kvp.Key);
                    Assert.Equal(new[] { "Header1" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowMethods, kvp.Key);
                    Assert.Equal(new[] { "PUT" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowOrigin, kvp.Key);
                    Assert.Equal(new[] { OriginUrl }, kvp.Value);
                });
        }
    }

    [Fact]
    public async Task PreFlight_WithCredentialsAllowed_ReflectsRequestHeaders()
    {
        // Arrange
        var policy = new CorsPolicyBuilder(OriginUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors("customPolicy");
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddCors(options =>
                    {
                        options.AddPolicy("customPolicy", policy);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Preflight request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .AddHeader(CorsConstants.AccessControlRequestMethod, "PUT")
                .AddHeader(CorsConstants.AccessControlRequestHeaders, "X-Test1,X-Test2")
                .SendAsync(CorsConstants.PreflightHttpMethod);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Collection(
                response.Headers.OrderBy(h => h.Key),
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowCredentials, kvp.Key);
                    Assert.Equal(new[] { "true" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowHeaders, kvp.Key);
                    Assert.Equal(new[] { "X-Test1,X-Test2" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowMethods, kvp.Key);
                    Assert.Equal(new[] { "PUT" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowOrigin, kvp.Key);
                    Assert.Equal(new[] { OriginUrl }, kvp.Value);
                });
        }
    }

    [Fact]
    public async Task PreFlightRequest_DoesNotMatchPolicy_SetsResponseHeadersAndReturnsNoContent()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors(builder =>
                        builder.WithOrigins(OriginUrl)
                               .WithMethods("PUT")
                               .WithHeaders("Header1")
                               .WithExposedHeaders("AllowedHeader"));
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services => services.AddCors());
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Preflight request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, "http://test.example.com")
                .AddHeader(CorsConstants.AccessControlRequestMethod, "PUT")
                .SendAsync(CorsConstants.PreflightHttpMethod);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(response.Headers);
        }

        await host.StartAsync();
    }

    [Fact]
    public async Task CorsRequest_DoesNotMatchPolicy_DoesNotSetHeaders()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors(builder =>
                        builder.WithOrigins(OriginUrl)
                               .WithMethods("PUT")
                               .WithHeaders("Header1")
                               .WithExposedHeaders("AllowedHeader"));
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services => services.AddCors());
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Actual request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, "http://test.example.com")
                .SendAsync("PUT");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);
        }
    }

    [Fact]
    public async Task Uses_PolicyProvider_AsFallback()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            policyName: null);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task DoesNotSetHeaders_ForNoPolicy()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            policyName: null);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Empty(httpContext.Response.Headers);
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task PreFlight_MatchesDefaultPolicy_SetsResponseHeaders()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors();
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(policyBuilder =>
                        {
                            policyBuilder
                            .WithOrigins(OriginUrl)
                            .WithMethods("PUT")
                            .WithHeaders("Header1")
                            .WithExposedHeaders("AllowedHeader")
                            .Build();
                        });
                        options.AddPolicy("policy2", policyBuilder =>
                        {
                            policyBuilder
                            .WithOrigins("http://test.example.com")
                            .Build();
                        });
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Preflight request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .AddHeader(CorsConstants.AccessControlRequestMethod, "PUT")
                .SendAsync(CorsConstants.PreflightHttpMethod);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Collection(
                response.Headers.OrderBy(h => h.Key),
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowHeaders, kvp.Key);
                    Assert.Equal(new[] { "Header1" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowMethods, kvp.Key);
                    Assert.Equal(new[] { "PUT" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowOrigin, kvp.Key);
                    Assert.Equal(new[] { OriginUrl }, kvp.Value);
                });
        }
    }

    [Fact]
    public async Task CorsRequest_SetsResponseHeaders()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseCors(builder =>
                        builder.WithOrigins(OriginUrl)
                            .WithMethods("PUT")
                            .WithHeaders("Header1")
                            .WithExposedHeaders("AllowedHeader"));
                    app.Run(async context =>
                    {
                        context.Response.Headers.Add("Test", "Should-Appear");
                        await context.Response.WriteAsync("Cross origin response");
                    });
                })
                .ConfigureServices(services => services.AddCors());
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Actual request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .SendAsync("PUT");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Collection(
                response.Headers.OrderBy(o => o.Key),
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowOrigin, kvp.Key);
                    Assert.Equal(OriginUrl, Assert.Single(kvp.Value));
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlExposeHeaders, kvp.Key);
                    Assert.Equal("AllowedHeader", Assert.Single(kvp.Value));
                },
                kvp =>
                {
                    Assert.Equal("Test", kvp.Key);
                    Assert.Equal("Should-Appear", Assert.Single(kvp.Value));
                });

            Assert.Equal("Cross origin response", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task CorsRequest_SetsResponseHeader_IfExceptionHandlerClearsResponse()
    {
        // Arrange
        var exceptionSeen = true;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    // Simulate ExceptionHandler middleware
                    app.Use(async (context, next) =>
                    {
                        try
                        {
                            await next(context);
                        }
                        catch (Exception)
                        {
                            exceptionSeen = true;
                            context.Response.Clear();
                            context.Response.StatusCode = 500;
                        }
                    });

                    app.UseCors(builder =>
                        builder.WithOrigins(OriginUrl)
                            .WithMethods("PUT")
                            .WithHeaders("Header1")
                            .WithExposedHeaders("AllowedHeader"));

                    app.Run(context =>
                    {
                        context.Response.Headers.Add("Test", "Should-Not-Exist");
                        throw new Exception("Runtime error");
                    });
                })
                .ConfigureServices(services => services.AddCors());
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            // Act
            // Actual request.
            var response = await server.CreateRequest("/")
                .AddHeader(CorsConstants.Origin, OriginUrl)
                .SendAsync("PUT");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.True(exceptionSeen, "We expect exception middleware to have executed");

            Assert.Collection(
                response.Headers.OrderBy(o => o.Key),
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlAllowOrigin, kvp.Key);
                    Assert.Equal(OriginUrl, Assert.Single(kvp.Value));
                },
                kvp =>
                {
                    Assert.Equal(CorsConstants.AccessControlExposeHeaders, kvp.Key);
                    Assert.Equal("AllowedHeader", Assert.Single(kvp.Value));
                });
        }
    }

    [Fact]
    public async Task Invoke_WithCustomPolicyProviderThatReturnsAsynchronously_Works()
    {
        // Arrange
        var corsService = new CorsService(Options.Create(new CorsOptions()), NullLoggerFactory.Instance);
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        var policy = new CorsPolicyBuilder()
            .WithOrigins(OriginUrl)
            .WithHeaders("AllowedHeader")
            .Build();
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(policy, TimeSpan.FromMilliseconds(10));

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "OPTIONS";
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { OriginUrl });
        httpContext.Request.Headers.Add(CorsConstants.AccessControlRequestMethod, new[] { "PUT" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        var response = httpContext.Response;
        Assert.Collection(
            response.Headers.OrderBy(o => o.Key),
            kvp =>
            {
                Assert.Equal(CorsConstants.AccessControlAllowHeaders, kvp.Key);
                Assert.Equal("AllowedHeader", Assert.Single(kvp.Value));
            },
            kvp =>
            {
                Assert.Equal(CorsConstants.AccessControlAllowOrigin, kvp.Key);
                Assert.Equal(OriginUrl, Assert.Single(kvp.Value));
            });
    }

    [Fact]
    public async Task Invoke_HasEndpointWithNoMetadata_RunsCors()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), "DefaultPolicyName"),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_HasEndpointWithEnableMetadata_MiddlewareHasPolicyName_RunsCorsWithPolicyName()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute("MetadataPolicyName")), "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), "MetadataPolicyName"),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_HasEndpointWithEnableMetadata_HasSignificantDisableCors_ReturnsNoContentForPreflightRequest()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var policyProvider = Mock.Of<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;

        var middleware = new CorsMiddleware(
            c => { throw new Exception("Should not be called."); },
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute(), new DisableCorsAttribute()), "Test endpoint"));
        httpContext.Request.Method = "OPTIONS";
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });
        httpContext.Request.Headers.Add(CorsConstants.AccessControlRequestMethod, new[] { "GET" });

        // Act
        await middleware.Invoke(httpContext, policyProvider);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_HasEndpointWithEnableMetadata_HasSignificantDisableCors_ExecutesNextMiddleware()
    {
        // Arrange
        var executed = false;
        var corsService = Mock.Of<ICorsService>();
        var policyProvider = Mock.Of<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;

        var middleware = new CorsMiddleware(
            c =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute(), new DisableCorsAttribute()), "Test endpoint"));
        httpContext.Request.Method = "GET";
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });
        httpContext.Request.Headers.Add(CorsConstants.AccessControlRequestMethod, new[] { "GET" });

        // Act
        await middleware.Invoke(httpContext, policyProvider);

        // Assert
        Assert.True(executed);
        Mock.Get(policyProvider).Verify(v => v.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()), Times.Never());
        Mock.Get(corsService).Verify(v => v.EvaluatePolicy(It.IsAny<HttpContext>(), It.IsAny<CorsPolicy>()), Times.Never());
    }

    [Fact]
    public async Task Invoke_HasEndpointWithEnableMetadata_MiddlewareHasPolicy_RunsCorsWithPolicyName()
    {
        // Arrange
        var policy = new CorsPolicyBuilder().Build();
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            policy,
            loggerFactory);

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute("MetadataPolicyName")), "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), "MetadataPolicyName"),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_HasEndpointRequireCorsMetadata_MiddlewareHasPolicy_RunsCorsWithPolicyName()
    {
        // Arrange
        var defaultPolicy = new CorsPolicyBuilder().Build();
        var metadataPolicy = new CorsPolicyBuilder().Build();
        var mockCorsService = new Mock<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();
        mockCorsService.Setup(o => o.EvaluatePolicy(It.IsAny<HttpContext>(), It.IsAny<CorsPolicy>()))
            .Returns(new CorsResult())
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            mockCorsService.Object,
            defaultPolicy,
            loggerFactory);

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new CorsPolicyMetadata(metadataPolicy)), "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);
        mockCorsService.Verify(
            o => o.EvaluatePolicy(It.IsAny<HttpContext>(), metadataPolicy),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_HasEndpointWithEnableMetadataWithNoName_RunsCorsWithStaticPolicy()
    {
        // Arrange
        var policy = new CorsPolicyBuilder().Build();
        var mockCorsService = new Mock<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();
        mockCorsService.Setup(o => o.EvaluatePolicy(It.IsAny<HttpContext>(), It.IsAny<CorsPolicy>()))
            .Returns(new CorsResult())
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            mockCorsService.Object,
            policy,
            loggerFactory);

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute()), "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);
        mockCorsService.Verify(
            o => o.EvaluatePolicy(It.IsAny<HttpContext>(), policy),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_HasEndpointWithDisableMetadata_SkipCors()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new DisableCorsAttribute()), "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Invoke_HasEndpointWithMutlipleMetadata_SkipCorsBecauseOfMetadataOrder()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = new Mock<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;
        mockProvider.Setup(o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult<CorsPolicy>(null))
            .Verifiable();

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute("MetadataPolicyName"), new DisableCorsAttribute()), "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider.Object);

        // Assert
        mockProvider.Verify(
            o => o.GetPolicyAsync(It.IsAny<HttpContext>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Invoke_InvokeFlagSet()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = Mock.Of<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute("MetadataPolicyName"), new DisableCorsAttribute()), "Test endpoint"));
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider);

        // Assert
        Assert.Contains(httpContext.Items, item => string.Equals(item.Key as string, "__CorsMiddlewareWithEndpointInvoked"));
    }

    [Fact]
    public async Task Invoke_WithoutOrigin_InvokeFlagSet()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = Mock.Of<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableCorsAttribute("MetadataPolicyName"), new DisableCorsAttribute()), "Test endpoint"));

        // Act
        await middleware.Invoke(httpContext, mockProvider);

        // Assert
        Assert.Contains(httpContext.Items, item => string.Equals(item.Key as string, "__CorsMiddlewareWithEndpointInvoked"));
    }

    [Fact]
    public async Task Invoke_WithoutEndpoint_InvokeFlagSet()
    {
        // Arrange
        var corsService = Mock.Of<ICorsService>();
        var mockProvider = Mock.Of<ICorsPolicyProvider>();
        var loggerFactory = NullLoggerFactory.Instance;

        var middleware = new CorsMiddleware(
            Mock.Of<RequestDelegate>(),
            corsService,
            loggerFactory,
            "DefaultPolicyName");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Add(CorsConstants.Origin, new[] { "http://example.com" });

        // Act
        await middleware.Invoke(httpContext, mockProvider);

        // Assert
        Assert.DoesNotContain(httpContext.Items, item => string.Equals(item.Key as string, "__CorsMiddlewareWithEndpointInvoked"));
    }
}
