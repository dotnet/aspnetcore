// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Tests;

public class WebApplicationGlobalConventionTests
{
    [Fact]
    public async Task SupportsApplyingConventionsOnAllEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Conventions.WithMetadata(new EndpointGroupNameAttribute("global"));

        app.MapGet("/1", () => "Hello, world!").WithName("One");
        app.MapGet("/2", () => "Hello, world!").WithName("Two");

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Collection(endpointDataSource.Endpoints,
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                var nameMetadata = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
                Assert.Equal("One", nameMetadata.EndpointName);
            },
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                var nameMetadata = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
                Assert.Equal("Two", nameMetadata.EndpointName);
            }
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task LocalConventionsOverrideGlobalConventions()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Conventions.WithMetadata(new EndpointGroupNameAttribute("one"));

        var group = app.MapGroup("/hello")
            .WithMetadata(new EndpointGroupNameAttribute("two"));

        group.MapGet("/", () => "Hello world!")
            .WithMetadata(new EndpointGroupNameAttribute("three"));

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Collection(endpointDataSource.Endpoints,
            endpoint =>
            {
                var metadata = endpoint.Metadata.OfType<IEndpointGroupNameMetadata>();
                Assert.Collection(metadata,
                    metadata => Assert.Equal("one", metadata.EndpointGroupName),
                    metadata => Assert.Equal("two", metadata.EndpointGroupName),
                    metadata => Assert.Equal("three", metadata.EndpointGroupName));
                var targetMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("three", targetMetadata.EndpointGroupName);
            }
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task CanAccessCorrectBuilderInConvention()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Conventions.Add(builder =>
        {
            if (builder is RouteEndpointBuilder { RoutePattern.RawText: "/1" })
            {
                builder.Metadata.Add(new EndpointGroupNameAttribute("global"));
            }
        });

        app.MapGet("/1", () => "One");
        app.MapGet("/2", () => " Two");

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Collection(endpointDataSource.Endpoints,
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
            },
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Null(groupNameMetadata);

            }
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task CanAccessCorrectServiceProviderInConvention()
    {
        IServiceProvider globalConventionServiceProvider = null;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Conventions.Add(builder =>
        {
            globalConventionServiceProvider = builder.ApplicationServices;
        });

        app.MapGet("/", () => "Hello, world!");

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.NotEmpty(endpointDataSource.Endpoints);
        Assert.Equal(app.Services, globalConventionServiceProvider);

        await app.StopAsync();
    }

    [Fact]
    public async Task BranchedPipelinesExemptFromGlobalConventions()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Conventions.WithMetadata(new EndpointGroupNameAttribute("global"));

        app.UseRouting();

        app.MapGet("/1", () => "Hello, world!").WithName("One");

        app.UseEndpoints(e =>
        {
            e.MapGet("/2", () => "Hello, world!").WithName("Two");
        });

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Collection(endpointDataSource.Endpoints,
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                var nameMetadata = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
                Assert.Equal("One", nameMetadata.EndpointName);
            },
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                var nameMetadata = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
                Assert.Null(groupNameMetadata);
                Assert.Equal("Two", nameMetadata.EndpointName);
            }
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task SupportsGlobalConventionsOnRouteEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSignalR();
        builder.Services.AddRazorComponents();
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(apm =>
                {
                    apm.FeatureProviders.Clear();
                    apm.FeatureProviders.Add(new TestControllerFeatureProvider());
                });

        var app = builder.Build();

        app.Conventions.WithMetadata(new EndpointGroupNameAttribute("global"));

        app.MapGet("/", () => "Hello, world!");
        app.MapHub<TestHub>("/test-hub");
        app.MapRazorComponents<TestComponent>();
        app.MapControllers();

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Collection(endpointDataSource.Endpoints,
            // Route handler endpoints
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
            },
            // SignalR produces two endpoints per hub
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
            },
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
            },
            // Razor component endpoint
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
            },
            // MapController endpoint
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
            },
            // Controller-based endpoints
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Equal("global", groupNameMetadata.EndpointGroupName);
            }
        );

        await app.StopAsync();
    }

    [Fact]
    public async Task ThrowsExceptionOnNonRouteEndpointsAtTopLevel()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Conventions.WithMetadata(new EndpointGroupNameAttribute("global"));

        app.DataSources.Add(new CustomEndpointDataSource());

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        var ex = Assert.Throws<NotSupportedException>(() => endpointDataSource.Endpoints);
        Assert.Equal("MapGroup does not support custom Endpoint type 'Microsoft.AspNetCore.Tests.WebApplicationGlobalConventionTests+TestCustomEndpoint'. Only RouteEndpoints can be grouped.", ex.Message);

        await app.StopAsync();
    }

    [Fact]
    public async Task DoesNotThrowExceptionOnNonRouteEndpointsInBranchedPipeline()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Conventions.WithMetadata(new EndpointGroupNameAttribute("global"));

        app.UseRouting();

        app.UseEndpoints(e =>
        {
            e.DataSources.Add(new CustomEndpointDataSource());
        });

        await app.StartAsync();

        var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
        Assert.Collection(endpointDataSource.Endpoints,
            endpoint =>
            {
                var groupNameMetadata = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
                Assert.Null(groupNameMetadata);
            }
        );

        await app.StopAsync();
    }

    private class TestHub : Hub { }
    private class TestComponent { }

    private class TestController : Controller
    {
        [HttpGet("/")]
        public void Index() { }
    }

    [ApiController]
    private class MyApiController : ControllerBase
    {
        [HttpGet("other")]
        public void Index() { }
    }

    private class TestControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            feature.Controllers.Clear();
            feature.Controllers.Add(typeof(TestController).GetTypeInfo());
            feature.Controllers.Add(typeof(MyApiController).GetTypeInfo());
        }
    }

    private sealed class TestCustomEndpoint : Endpoint
    {
        public TestCustomEndpoint() : base(null, null, null) { }
    }

    private sealed class CustomEndpointDataSource : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints => [new TestCustomEndpoint()];
        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;
    }
}
