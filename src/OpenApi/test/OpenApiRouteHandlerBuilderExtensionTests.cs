// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
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
    public void WithOpenApi_GroupMetadataCanBeSeenByAndOverriddenByMoreLocalMetadata()
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
            builder.WithOpenApi(operation =>
            {
                operation.Summary += $" | Local Summary | 200 Status Response Content-Type: {operation.Responses["200"].Content.Keys.Single()}";
                return operation;
            });
        }

        WithLocalSummary(builder.MapDelete("/root", GetString));

        var outerGroup = builder.MapGroup("/outer");
        var innerGroup = outerGroup.MapGroup("/inner");

        WithLocalSummary(outerGroup.MapDelete("/outer-a", GetString));

        // The order WithOpenApi() is relative to the MapDelete() methods does not matter.
        outerGroup.WithOpenApi(operation =>
        {
            operation.Summary = "Outer Group Summary";
            return operation;
        });

        WithLocalSummary(outerGroup.MapDelete("/outer-b", GetString));
        WithLocalSummary(innerGroup.MapDelete("/inner-a", GetString));

        innerGroup.WithOpenApi(operation =>
        {
            operation.Summary += " | Inner Group Summary";
            return operation;
        });

        WithLocalSummary(innerGroup.MapDelete("/inner-b", GetString));

        var summaries = builder.DataSources.SelectMany(ds => ds.Endpoints).Select(e => e.Metadata.GetMetadata<OpenApiOperation>().Summary).ToArray();

        Assert.Equal(5, summaries.Length);
        Assert.Contains(" | Local Summary | 200 Status Response Content-Type: text/plain", summaries);
        Assert.Contains("Outer Group Summary | Local Summary | 200 Status Response Content-Type: text/plain", summaries);
        Assert.Contains("Outer Group Summary | Local Summary | 200 Status Response Content-Type: text/plain", summaries);
        Assert.Contains("Outer Group Summary | Inner Group Summary | Local Summary | 200 Status Response Content-Type: text/plain", summaries);
        Assert.Contains("Outer Group Summary | Inner Group Summary | Local Summary | 200 Status Response Content-Type: text/plain", summaries);
    }

    private RouteEndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return Assert.IsType<RouteEndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
    }
}
