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

    private ModelEndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return Assert.IsType<ModelEndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
    }
}
