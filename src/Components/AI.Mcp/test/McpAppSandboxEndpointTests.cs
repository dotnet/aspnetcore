// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpAppSandboxEndpointTests
{
    [Theory]
    [InlineData("/app", "https://host.example/path", "https://sandbox.example")]
    [InlineData("/app", "https://host.example", "https://sandbox.example/#fragment")]
    [InlineData("/app", "https://user:pass@host.example", "https://sandbox.example")]
    [InlineData("/app", "https://host.example", "https://host.example:443")]
    [InlineData("/app", "https://b\u00fccher.example", "https://xn--bcher-kva.example:443")]
    public void DeploymentAddressesOnlyRequireDistinctEffectiveOrigins(string path, string host, string sandbox)
    {
        using var services = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        var routes = new Routes(services);

        var hostUri = new Uri(host);
        var sandboxUri = new Uri(sandbox);
        if (hostUri.Scheme == sandboxUri.Scheme && hostUri.IdnHost == sandboxUri.IdnHost &&
            hostUri.Port == sandboxUri.Port)
        {
            Assert.Throws<ArgumentException>(() =>
                routes.MapMcpAppSandbox<McpSandboxBase>(path, hostUri, sandboxUri));
        }
        else
        {
            routes.MapMcpAppSandbox<McpSandboxBase>(path, hostUri, sandboxUri);
            Assert.Single(routes.DataSources.SelectMany(source => source.Endpoints));
        }
    }

    [Fact]
    public void NonHttpDeploymentAddressesAreNotRejectedByAdditionalValidation()
    {
        using var services = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        var routes = new Routes(services);

        routes.MapMcpAppSandbox<McpSandboxBase>("/sandbox", new("ftp://host.example"), new("ftp://sandbox.example"));

        Assert.Single(routes.DataSources.SelectMany(source => source.Endpoints));
    }

    [Fact]
    public async Task MapperDerivesOriginWithoutCredentialsOrDocumentParts()
    {
        await using var services = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        var routes = new Routes(services);
        var host = new UriBuilder("https://host.example/document?query#fragment")
        {
            UserName = "example", Password = "example"
        }.Uri;
        object? captured = null;
        routes.MapMcpAppSandbox<McpSandboxBase>("/sandbox", host, new("https://sandbox.example"))
            .AddEndpointFilter(async (context, next) =>
            {
                captured = await next(context);
                return Results.NoContent();
            });
        var endpoint = Assert.Single(routes.DataSources.SelectMany(source => source.Endpoints));
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = "https";
        context.Request.Host = new("sandbox.example");

        await endpoint.RequestDelegate!(context);

        Assert.Equal("https://host.example",
            Assert.IsType<RazorComponentResult<McpSandboxBase>>(captured).Parameters[nameof(McpSandboxBase.HostOrigin)]);
    }

    [Theory]
    [InlineData("HostOrigin")]
    [InlineData("hostorigin")]
    [InlineData("HOSTORIGIN")]
    public void MapperOwnedOriginCannotBeOverridden(string parameter)
    {
        using var services = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        var routes = new Routes(services);

        Assert.Throws<ArgumentException>(() => routes.MapMcpAppSandbox<McpSandboxBase>(
            "/sandbox", new("https://host.example"), new("https://sandbox.example"),
            new Dictionary<string, object?> { [parameter] = "https://different.example" }));
    }

    [Theory]
    [InlineData("https", "sandbox.example:5002", true)]
    [InlineData("http", "sandbox.example:5002", false)]
    [InlineData("https", "sandbox.example:5001", false)]
    [InlineData("https", "other.example:5002", false)]
    public async Task MappingSnapshotsParametersAndRestrictsExactOrigin(string scheme, string host, bool allowed)
    {
        await using var services = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        var routes = new Routes(services);
        var parameters = new Dictionary<string, object?> { ["FrameTitle"] = "Original" };
        object? captured = null;
        var marker = new object();
        routes.MapMcpAppSandbox<TitledSandbox>("/sandbox",
                new("https://host.example:5001"), new("https://sandbox.example:5002"), parameters)
            .WithMetadata(marker)
            .AddEndpointFilter(async (context, next) =>
            {
                captured = await next(context);
                return Results.NoContent();
            });
        parameters["FrameTitle"] = "Changed";
        var endpoint = Assert.Single(routes.DataSources.SelectMany(source => source.Endpoints));
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = scheme;
        context.Request.Host = new(host);

        await endpoint.RequestDelegate!(context);

        Assert.Contains(marker, endpoint.Metadata);
        Assert.Equal("Changed", parameters["FrameTitle"]);
        Assert.Single(parameters);
        if (allowed)
        {
            var result = Assert.IsType<RazorComponentResult<TitledSandbox>>(captured);
            Assert.Equal("Original", result.Parameters["FrameTitle"]);
            Assert.Equal("https://host.example:5001", result.Parameters[nameof(McpSandboxBase.HostOrigin)]);
            Assert.True(result.PreventStreamingRendering);
            Assert.Equal("text/html; charset=utf-8", result.ContentType);
            Assert.Equal("no-store", context.Response.Headers.CacheControl);
        }
        else
        {
            Assert.IsType<NotFound>(captured);
            Assert.False(context.Response.Headers.ContainsKey("Cache-Control"));
        }
    }

    private sealed class Routes(IServiceProvider services) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider => services;
        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(services);
    }

    [Theory]
    [InlineData("http://sandbox.example", "http", "sandbox.example")]
    [InlineData("http://sandbox.example", "http", "sandbox.example:80")]
    [InlineData("https://sandbox.example", "https", "sandbox.example")]
    [InlineData("https://sandbox.example", "https", "sandbox.example:443")]
    [InlineData("https://[::1]:5002", "https", "[::1]:5002")]
    [InlineData("https://b\u00fccher.example", "https", "xn--bcher-kva.example")]
    public async Task DefaultPortsIpv6AndInternationalizedOriginsUseCanonicalAuthority(string origin, string scheme, string host)
    {
        await using var services = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        var routes = new Routes(services);
        object? captured = null;
        routes.MapMcpAppSandbox<McpSandboxBase>("/sandbox", new("https://host.example"), new(origin))
            .AddEndpointFilter(async (context, next) =>
            {
                captured = await next(context);
                return Results.NoContent();
            });
        var endpoint = Assert.Single(routes.DataSources.SelectMany(source => source.Endpoints));
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = scheme;
        context.Request.Host = new(host);

        await endpoint.RequestDelegate!(context);

        Assert.IsType<RazorComponentResult<McpSandboxBase>>(captured);
        Assert.Contains(new Uri(origin).IsDefaultPort ? context.Request.Host.Host : context.Request.Host.Value,
            endpoint.Metadata.GetMetadata<IHostMetadata>()!.Hosts);
    }

    private sealed class TitledSandbox : McpSandboxBase
    {
        [Parameter] public string FrameTitle { get; set; } = "";
    }
}
