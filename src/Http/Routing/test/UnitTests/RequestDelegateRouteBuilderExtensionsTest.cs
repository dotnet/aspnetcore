// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Routing;

// These are really more like integration tests. They verify that these extensions
// add routes that behave as advertised.
public class RequestDelegateRouteBuilderExtensionsTest
{
    private static readonly RequestDelegate NullHandler = (c) => Task.CompletedTask;

    public static TheoryData<Action<IRouteBuilder>, Action<HttpContext>> MatchingActions
    {
        get
        {
            return new TheoryData<Action<IRouteBuilder>, Action<HttpContext>>()
                {
                    { b => { b.MapRoute("api/{id}", NullHandler); }, null },
                    { b => { b.MapMiddlewareRoute("api/{id}", app => { }); }, null },

                    { b => { b.MapDelete("api/{id}", NullHandler); }, c => { c.Request.Method = "DELETE"; } },
                    { b => { b.MapMiddlewareDelete("api/{id}", app => { }); }, c => { c.Request.Method = "DELETE"; }  },
                    { b => { b.MapGet("api/{id}", NullHandler); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapMiddlewareGet("api/{id}", app => { }); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapPost("api/{id}", NullHandler); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapMiddlewarePost("api/{id}", app => { }); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapPut("api/{id}", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapMiddlewarePut("api/{id}", app => { }); }, c => { c.Request.Method = "PUT"; }  },

                    { b => { b.MapVerb("PUT", "api/{id}", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapMiddlewareVerb("PUT", "api/{id}", app => { }); }, c => { c.Request.Method = "PUT"; }  },
                };
        }
    }

    [Theory]
    [MemberData(nameof(MatchingActions))]
    public async Task Map_MatchesRequest(
        Action<IRouteBuilder> routeSetup,
        Action<HttpContext> requestSetup)
    {
        // Arrange
        var services = CreateServices();

        var context = CreateRouteContext(services);
        context.HttpContext.Request.Path = new PathString("/api/5");
        requestSetup?.Invoke(context.HttpContext);

        var builder = CreateRouteBuilder(services);
        routeSetup(builder);
        var route = builder.Build();

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Same(NullHandler, context.Handler);
    }

    public static TheoryData<Action<IRouteBuilder>, Action<HttpContext>> NonmatchingActions
    {
        get
        {
            return new TheoryData<Action<IRouteBuilder>, Action<HttpContext>>()
                {
                    { b => { b.MapRoute("api/{id}/extra", NullHandler); }, null },
                    { b => { b.MapMiddlewareRoute("api/{id}/extra", app => { }); }, null },

                    { b => { b.MapDelete("api/{id}", NullHandler); }, c => { c.Request.Method = "GET"; } },
                    { b => { b.MapMiddlewareDelete("api/{id}", app => { }); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapDelete("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "DELETE"; } },
                    { b => { b.MapMiddlewareDelete("api/{id}/extra", app => { }); }, c => { c.Request.Method = "DELETE"; }  },
                    { b => { b.MapGet("api/{id}", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapMiddlewareGet("api/{id}", app => { }); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapGet("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapMiddlewareGet("api/{id}/extra", app => { }); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapPost("api/{id}", NullHandler); }, c => { c.Request.Method = "MEH"; }  },
                    { b => { b.MapMiddlewarePost("api/{id}", app => { }); }, c => { c.Request.Method = "DELETE"; }  },
                    { b => { b.MapPost("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapMiddlewarePost("api/{id}/extra", app => { }); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapPut("api/{id}", NullHandler); }, c => { c.Request.Method = "BLEH"; }  },
                    { b => { b.MapMiddlewarePut("api/{id}", app => { }); }, c => { c.Request.Method = "HEAD"; }  },
                    { b => { b.MapPut("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapMiddlewarePut("api/{id}/extra", app => { }); }, c => { c.Request.Method = "PUT"; }  },

                    { b => { b.MapVerb("PUT", "api/{id}", NullHandler); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapMiddlewareVerb("PUT", "api/{id}", app => { }); }, c => { c.Request.Method = "HEAD"; }  },
                    { b => { b.MapVerb("PUT", "api/{id}/extra", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapMiddlewareVerb("PUT", "api/{id}/extra", app => { }); }, c => { c.Request.Method = "PUT"; }  },
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonmatchingActions))]
    public async Task Map_DoesNotMatchRequest(
        Action<IRouteBuilder> routeSetup,
        Action<HttpContext> requestSetup)
    {
        // Arrange
        var services = CreateServices();

        var context = CreateRouteContext(services);
        context.HttpContext.Request.Path = new PathString("/api/5");
        requestSetup?.Invoke(context.HttpContext);

        var builder = CreateRouteBuilder(services);
        routeSetup(builder);
        var route = builder.Build();

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRouting();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static RouteContext CreateRouteContext(IServiceProvider services)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services;
        return new RouteContext(httpContext);
    }

    private static IRouteBuilder CreateRouteBuilder(IServiceProvider services)
    {
        var applicationBuilder = new Mock<IApplicationBuilder>();
        applicationBuilder.SetupAllProperties();

        applicationBuilder
            .Setup(b => b.New().Build())
            .Returns(NullHandler);

        applicationBuilder.Object.ApplicationServices = services;

        var routeBuilder = new RouteBuilder(applicationBuilder.Object);
        return routeBuilder;
    }
}
