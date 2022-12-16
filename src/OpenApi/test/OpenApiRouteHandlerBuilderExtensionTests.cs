// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using static Microsoft.AspNetCore.OpenApi.Tests.OpenApiOperationGeneratorTests;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class OpenApiRouteHandlerBuilderExtensionTests
{
    [Fact]
    public void WithOpenApi_CanSetOperationInMetadata()
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        string GetString() => "Foo";
        _ = builder.MapDelete("/", GetString).WithOpenApi();

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var operation = endpoint.Metadata.GetMetadata<OpenApiOperation>();
        Assert.NotNull(operation);
        Assert.Single(operation.Responses); // Sanity check generated operation
    }

    [Fact]
    public void WithOpenApi_CanSetOperationInMetadataWithOverride()
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        string GetString() => "Foo";
        _ = builder.MapDelete("/", GetString).WithOpenApi(generatedOperation => new OpenApiOperation());

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var operation = endpoint.Metadata.GetMetadata<OpenApiOperation>();
        Assert.NotNull(operation);
        Assert.Empty(operation.Responses);
    }

    [Fact]
    public void WithOpenApi_CanSetSchemaInOperationWithOverride()
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        string GetString(string id) => "Foo";
        _ = builder.MapDelete("/{id}", GetString)
            .WithOpenApi(operation => new(operation)
            {
                Parameters = new List<OpenApiParameter>() { new() { Schema = new() { Type = "number" } } }
            });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var operation = endpoint.Metadata.GetMetadata<OpenApiOperation>();
        Assert.NotNull(operation);
        var parameter = Assert.Single(operation.Parameters);
        Assert.Equal("number", parameter.Schema.Type);
    }

    [Fact]
    public void WithOpenApi_WorksWithMapGroup()
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        string GetString() => "Foo";
        var myGroup = builder.MapGroup("/group");

        myGroup.MapDelete("/a", GetString);

        // The order WithOpenApi() is relative to the MapDelete() methods does not matter.
        myGroup.WithOpenApi();

        myGroup.MapDelete("/b", GetString);

        // The RotueGroupBuilder adds a single EndpointDataSource.
        var groupDataSource = Assert.Single(builder.DataSources);

        Assert.Collection(groupDataSource.Endpoints,
            e => Assert.NotNull(e.Metadata.GetMetadata<OpenApiOperation>()),
            e => Assert.NotNull(e.Metadata.GetMetadata<OpenApiOperation>()));
    }

    [Fact]
    public void WithOpenApi_WorksWithMapGroupAndEndpointAnnotations()
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        string GetString() => "Foo";
        var myGroup = builder.MapGroup("/group");
        myGroup.WithOpenApi();
        myGroup.MapDelete("/a", GetString).Produces<string>(201);

        // The RotueGroupBuilder adds a single EndpointDataSource.
        var groupDataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(groupDataSource.Endpoints);
        var operation = endpoint.Metadata.GetMetadata<OpenApiOperation>();

        Assert.NotNull(operation);
        Assert.Equal(2, operation.Responses.Count);

        var defaultOperation = operation.Responses["200"];
        Assert.True(defaultOperation.Content.ContainsKey("text/plain"));

        var annotatedOperation = operation.Responses["201"];
        // Produces doesn't special case string??
        Assert.True(annotatedOperation.Content.ContainsKey("application/json"));
    }

    [Fact]
    public void WithOpenApi_WorksWithGroupAndSpecificEndpoint()
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        string GetString() => "Foo";
        var myGroup = builder.MapGroup("/group");
        myGroup.WithOpenApi(o => new(o)
        {
            Summary = "Set from outer group"
        });
        myGroup.MapDelete("/a", GetString).WithOpenApi(o => new(o)
        {
            Summary = "Set from endpoint"
        });

        // The RotueGroupBuilder adds a single EndpointDataSource.
        var groupDataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(groupDataSource.Endpoints);
        var operation = endpoint.Metadata.GetMetadata<OpenApiOperation>();
        Assert.NotNull(operation);
        Assert.Equal("Set from outer group", operation.Summary);
    }

    [Fact]
    public void WithOpenApi_GroupMetadataCanExamineAndExtendMoreLocalMetadata()
    {
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var serviceProviderIsService = new ServiceProviderIsService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IServiceProviderIsService>(serviceProviderIsService)
            .AddSingleton<IHostEnvironment>(hostEnvironment)
            .BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        string GetString() => "Foo";

        static void WithLocalSummary(RouteHandlerBuilder builder)
        {
            builder.WithOpenApi(operation => new(operation)
            {
                Summary = $"| Local Summary | 200 Status Response Content-Type: {operation.Responses["200"].Content.Keys.Single()}"
            });
        }

        WithLocalSummary(builder.MapDelete("/root", GetString));

        var outerGroup = builder.MapGroup("/outer");
        var innerGroup = outerGroup.MapGroup("/inner");

        WithLocalSummary(outerGroup.MapDelete("/outer-a", GetString));

        // The order WithOpenApi() is relative to the MapDelete() methods does not matter.
        outerGroup.WithOpenApi(operation => new(operation)
        {
            Summary = $"Outer Group Summary {operation.Summary}"
        });

        WithLocalSummary(outerGroup.MapDelete("/outer-b", GetString));
        WithLocalSummary(innerGroup.MapDelete("/inner-a", GetString));

        innerGroup.WithOpenApi(operation => new(operation)
        {
            Summary = $"| Inner Group Summary {operation.Summary}"
        });

        WithLocalSummary(innerGroup.MapDelete("/inner-b", GetString));

        var summaries = builder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .ToDictionary(
                e => ((RouteEndpoint)e).RoutePattern.RawText,
                e => e.Metadata.GetMetadata<OpenApiOperation>().Summary);

        Assert.Equal(5, summaries.Count);

        Assert.Equal("| Local Summary | 200 Status Response Content-Type: text/plain",
            summaries["/root"]);

        Assert.Equal("Outer Group Summary | Local Summary | 200 Status Response Content-Type: text/plain",
            summaries["/outer/outer-a"]);
        Assert.Equal("Outer Group Summary | Local Summary | 200 Status Response Content-Type: text/plain",
            summaries["/outer/outer-b"]);

        Assert.Equal("Outer Group Summary | Inner Group Summary | Local Summary | 200 Status Response Content-Type: text/plain",
            summaries["/outer/inner/inner-a"]);
        Assert.Equal("Outer Group Summary | Inner Group Summary | Local Summary | 200 Status Response Content-Type: text/plain",
            summaries["/outer/inner/inner-b"]);
    }

    private RouteEndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return Assert.IsType<RouteEndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
    }
}
