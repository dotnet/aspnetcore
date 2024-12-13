// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentEndpointFactoryTest
{
    [Fact]
    public void AddEndpoints_CreatesEndpointWithExpectedMetadata()
    {
        var endpoints = new List<Endpoint>();
        var factory = new RazorComponentEndpointFactory();
        var conventions = new List<Action<EndpointBuilder>>();
        var finallyConventions = new List<Action<EndpointBuilder>>();
        var testRenderMode = new TestRenderMode();
        var configuredRenderModes = new ConfiguredRenderModesMetadata(new[] { testRenderMode });
        factory.AddEndpoints(
            endpoints,
            typeof(App), new PageComponentInfo(
            "App",
            typeof(App),
            "/",
            new object[] { new AuthorizeAttribute() }),
            conventions,
            finallyConventions,
            configuredRenderModes);

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("/ (App)", endpoint.DisplayName);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal(0, routeEndpoint.Order);
        Assert.Equal("/", routeEndpoint.RoutePattern.RawText);
        Assert.Contains(endpoint.Metadata, m => m is RootComponentMetadata);
        Assert.Contains(endpoint.Metadata, m => m is ComponentTypeMetadata);
        Assert.Contains(endpoint.Metadata, m => m is SuppressLinkGenerationMetadata);
        Assert.Contains(endpoint.Metadata, m => m is AuthorizeAttribute);
        Assert.Contains(endpoint.Metadata, m => m is ConfiguredRenderModesMetadata c
            && c.ConfiguredRenderModes.Single() == testRenderMode);
        Assert.NotNull(endpoint.RequestDelegate);

        var methods = Assert.Single(endpoint.Metadata.GetOrderedMetadata<HttpMethodMetadata>());
        Assert.Collection(methods.HttpMethods,
            method => Assert.Equal("GET", method),
            method => Assert.Equal("POST", method)
            );
    }

    [Fact]
    public void AddEndpoints_RunsConventions()
    {
        var endpoints = new List<Endpoint>();
        var factory = new RazorComponentEndpointFactory();
        var conventions = new List<Action<EndpointBuilder>>() {
            builder => builder.Metadata.Add(new AuthorizeAttribute())
        };

        var finallyConventions = new List<Action<EndpointBuilder>>();
        factory.AddEndpoints(
            endpoints,
            typeof(App),
            new PageComponentInfo(
                "App",
                typeof(App),
                "/",
                Array.Empty<object>()),
            conventions,
            finallyConventions,
            new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>()));

        var endpoint = Assert.Single(endpoints);
        Assert.Contains(endpoint.Metadata, m => m is AuthorizeAttribute);
    }

    [Fact]
    public void AddEndpoints_RunsFinallyConventions()
    {
        var endpoints = new List<Endpoint>();
        var factory = new RazorComponentEndpointFactory();
        var conventions = new List<Action<EndpointBuilder>>();

        var finallyConventions = new List<Action<EndpointBuilder>>()
        {
            builder => builder.Metadata.Add(new AuthorizeAttribute())
        };

        factory.AddEndpoints(
            endpoints,
            typeof(App),
            new PageComponentInfo(
                "App",
                typeof(App),
                "/",
                Array.Empty<object>()),
            conventions,
            finallyConventions,
            new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>()));

        var endpoint = Assert.Single(endpoints);
        Assert.Contains(endpoint.Metadata, m => m is AuthorizeAttribute);
    }

    [Fact]
    public void AddEndpoints_RouteOrderCanNotBeChanged()
    {
        var endpoints = new List<Endpoint>();
        var factory = new RazorComponentEndpointFactory();
        var conventions = new List<Action<EndpointBuilder>>();

        var finallyConventions = new List<Action<EndpointBuilder>>()
        {
            builder => ((RouteEndpointBuilder)builder).Order = -1
        };

        factory.AddEndpoints(
            endpoints,
            typeof(App),
            new PageComponentInfo(
                "App",
                typeof(App),
                "/",
                Array.Empty<object>()),
            conventions,
            finallyConventions,
            new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>()));

        var endpoint = Assert.Single(endpoints);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal(0, routeEndpoint.Order);
    }

    [Fact]
    public void AddEndpoints_RunsFinallyConventionsAfterRegularConventions()
    {
        var endpoints = new List<Endpoint>();
        var factory = new RazorComponentEndpointFactory();
        var conventions = new List<Action<EndpointBuilder>>()
        {
            builder => builder.Metadata.Add(new AuthorizeAttribute())
        };

        var finallyConventions = new List<Action<EndpointBuilder>>()
        {
            builder => builder.Metadata.Remove(builder.Metadata.OfType<AuthorizeAttribute>().Single())
        };

        factory.AddEndpoints(
            endpoints,
            typeof(App),
            new PageComponentInfo(
                "App",
                typeof(App),
                "/",
                Array.Empty<object>()),
            conventions,
            finallyConventions,
            new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>()));

        var endpoint = Assert.Single(endpoints);
        Assert.DoesNotContain(endpoint.Metadata, m => m is AuthorizeAttribute);
    }

    class TestRenderMode : IComponentRenderMode { }
}
