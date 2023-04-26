// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests
{
    [Fact]
    public async Task MapAction_ReturnsString_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", () => "Hello, world!");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(200, metadata.StatusCode);
        Assert.Equal("text/plain", metadata.ContentTypes.Single());
        Assert.Null(metadata.Type);

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ReturnsVoid_Has_No_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", () => {});
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>();
        Assert.Empty(metadata);

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ReturnsTaskOfString_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", Task<string> () => Task.FromResult("Hello, world!"));
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(200, metadata.StatusCode);
        Assert.Equal("text/plain", metadata.ContentTypes.Single());
        Assert.Null(metadata.Type);
    }

    [Fact]
    public async Task MapAction_ReturnsTask_Has_No_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", Task () => Task.CompletedTask);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>();
        Assert.Empty(metadata);
    }

    [Fact]
    public async Task MapAction_ReturnsValueTaskOfString_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", ValueTask<string> () => ValueTask.FromResult("Hello, world!"));
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(200, metadata.StatusCode);
        Assert.Equal("text/plain", metadata.ContentTypes.Single());
        Assert.Null(metadata.Type);
    }

    [Fact]
    public async Task MapAction_ReturnsValueTask_Has_No_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", ValueTask () => ValueTask.CompletedTask);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>();
        Assert.Empty(metadata);
    }

    [Fact]
    public async Task MapAction_ReturnsValidationProblemResult_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", () => TypedResults.ValidationProblem(new Dictionary<string, string[]>()));
""");

        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(400, metadata.StatusCode);
        Assert.Equal("application/problem+json", metadata.ContentTypes.Single());

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_TakesCustomMetadataEmitter_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (CustomMetadataEmitter x) => {});
""");

        var endpoint = GetEndpointFromCompilation(compilation);

        _ = endpoint.Metadata.OfType<CustomMetadata>().Single(m => m.Value == 42);
        _ = endpoint.Metadata.OfType<CustomMetadata>().Single(m => m.Value == 24);

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ReturnsCustomMetadataEmitter_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", () => new CustomMetadataEmitter());
""");

        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<CustomMetadata>().Single();
        Assert.Equal(24, metadata.Value);
    }

    [Fact]
    public async Task Create_AddJsonResponseType_AsMetadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", () => new object());
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var responseMetadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();

        Assert.Equal("application/json", Assert.Single(responseMetadata.ContentTypes));
        Assert.Equal(typeof(object), responseMetadata.Type);
    }

    [Fact]
    public async Task Create_AddPlaintextResponseType_AsMetadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", () => "Hello");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var responseMetadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();

        Assert.Equal("text/plain", Assert.Single(responseMetadata.ContentTypes));
        Assert.Null(responseMetadata.Type);
    }

    [Fact]
    public async Task Create_DiscoversMetadata_FromParametersImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (AddsCustomParameterMetadataBindable param1, AddsCustomParameterMetadata param2) => { });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "param1" });
        Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "param2" });
    }

    [Fact]
    public async Task Create_DiscoversMetadata_FromParametersImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (AddsCustomParameterMetadata param1) => { });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    }

    [Fact]
    public async Task Create_DiscoversEndpointMetadata_FromReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", () => new AddsCustomEndpointMetadataResult());
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public async Task Create_DiscoversEndpointMetadata_FromTaskWrappedReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", () => Task.FromResult(new AddsCustomEndpointMetadataResult()));
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public async Task Create_DiscoversEndpointMetadata_FromValueTaskWrappedReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", () => ValueTask.FromResult(new AddsCustomEndpointMetadataResult()));
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public async Task Create_AllowsRemovalOfDefaultMetadata_ByReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (Todo todo) => new RemovesAcceptsMetadataResult();
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (Todo todo) => new RemovesAcceptsMetadataResult());
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public async Task Create_AllowsRemovalOfDefaultMetadata_ByTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (Todo todo) => Task.FromResult(new RemovesAcceptsMetadataResult()));
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public async Task Create_AllowsRemovalOfDefaultMetadata_ByValueTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (Todo todo) => ValueTask.FromResult(new RemovesAcceptsMetadataResult()));
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public async Task Create_AllowsRemovalOfDefaultMetadata_ByParameterTypesImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (RemovesAcceptsParameterMetadata param1) => "Hello");
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public async Task Create_AllowsRemovalOfDefaultMetadata_ByParameterTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (RemovesAcceptsParameterMetadata param1) => "Hello");
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public async Task Create_SetsApplicationServices_OnEndpointMetadataContext()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (Todo todo) => new AccessesServicesMetadataResult());
""");
        var serviceProvider = CreateServiceProvider((services) =>
        {
            var metadataService = new MetadataService();
            services.AddSingleton(metadataService).BuildServiceProvider();
        });

        // Act
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is MetadataService);
    }

    [Fact]
    public async Task Create_SetsApplicationServices_OnEndpointParameterMetadataContext()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (AccessesServicesMetadataBinder parameter1) => "Test");
""");
        var serviceProvider = CreateServiceProvider((services) =>
        {
            var metadataService = new MetadataService();
            services.AddSingleton(metadataService).BuildServiceProvider();
        });

        // Act
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is MetadataService);
    }
}
