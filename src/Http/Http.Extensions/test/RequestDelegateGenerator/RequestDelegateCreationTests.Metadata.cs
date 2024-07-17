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
using System.CodeDom.Compiler;

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
        Assert.Equal(typeof(string), metadata.Type);

        await VerifyAgainstBaselineUsingFile(compilation);
    }

    [Fact]
    public async Task MapAction_ReturnsTodo_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", () => new Todo());
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(200, metadata.StatusCode);
        Assert.Equal("application/json", metadata.ContentTypes.Single());
        Assert.Equal(typeof(Todo), metadata.Type);

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
        Assert.Equal(typeof(string), metadata.Type);
    }

    [Fact]
    public async Task MapAction_ReturnsTask_ProducesInferredMetadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", Task () => Task.CompletedTask);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(200, metadata.StatusCode);
        Assert.Equal("text/plain", metadata.ContentTypes.Single());
        Assert.Equal(typeof(void), metadata.Type);
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
        Assert.Equal(typeof(string), metadata.Type);
    }

    [Fact]
    public async Task MapAction_ReturnsValueTask_ProducesInferredMetadata()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapGet("/", ValueTask () => ValueTask.CompletedTask);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(200, metadata.StatusCode);
        Assert.Equal("text/plain", metadata.ContentTypes.Single());
        Assert.Equal(typeof(void), metadata.Type);
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
        Assert.Equal(typeof(string), responseMetadata.Type);
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

    [Fact]
    public async Task Create_CombinesDefaultMetadata_AndMetadataFromReturnTypesImplementingIEndpointMetadataProvider()
    {
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", () => new CountsDefaultEndpointMetadataResult()).WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        // Differs from RDF test because we end up with more metadata in RDG.
        Assert.Contains(endpoint.Metadata, m => m is MetadataCountMetadata { Count: > 1 });
    }

    [Fact]
    public async Task Create_CombinesDefaultMetadata_AndMetadataFromTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", () => Task.FromResult(new CountsDefaultEndpointMetadataResult())).WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        // Differs from RDF test because we end up with more metadata in RDG.
        Assert.Contains(endpoint.Metadata, m => m is MetadataCountMetadata { Count: > 1 });
    }

    [Fact]
    public async Task AndMetadataFromValueTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", () => ValueTask.FromResult(new CountsDefaultEndpointMetadataResult())).WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        // Differs from RDF test because we end up with more metadata in RDG.
        Assert.Contains(endpoint.Metadata, m => m is MetadataCountMetadata { Count: > 1 });
    }

    [Fact]
    public async Task Create_CombinesDefaultMetadata_AndMetadataFromParameterTypesImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (AddsCustomParameterMetadata param1) => "Hello").WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "param1" });
    }

    [Fact]
    public async Task Create_CombinesDefaultMetadata_AndMetadataFromParameterTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (AddsCustomParameterMetadata param1) => "Hello").WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    }

    [Fact]
    public async Task Create_CombinesDefaultMetadata_AndMetadataFromParameterTypesImplementingIEndpointMetadataProvider_AndNonMetadataProviderParameter()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/", (AddsCustomParameterMetadata param1, HttpContext context) => "Hello").WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    }

    [Fact]
    public async Task Create_FlowsRoutePattern_ToMetadataProvider()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/test/pattern", (AddsRoutePatternMetadata param1) => {});
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is RoutePatternMetadata { RoutePattern: "/test/pattern" });
    }

    [Fact]
    public async Task InferMetadata_ThenCreate_CombinesAllMetadata_InCorrectOrder()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/test/pattern", [Attribute1, Attribute2] (AddsCustomParameterMetadata param1) => new CountsDefaultEndpointMetadataPoco())
   .WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");
        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        // NOTE: Depending on whether we are running under RDG or RDG, there are some generated types which
        //       don't have equivalents in the opposite. The two examples here are NullableContextAttribute which
        //       is generated by Roslyn depending on the context, and SourceKey which is RDF specific. So we filter
        //       them out so that the collection-based assertion below remains consistent with the original version
        //       of this test from RDF.
        var filteredMetadata = endpoint.Metadata.Where(
            m => m.GetType().Name != "NullableContextAttribute" &&
            m is not GeneratedCodeAttribute &&
            m is not MethodInfo &&
            m is not HttpMethodMetadata &&
            m is not Attribute1 &&
            m is not Attribute2 &&
            m is not IRouteDiagnosticsMetadata);

        Assert.Collection(filteredMetadata,
            // Inferred AcceptsMetadata from RDF for complex type
            m => Assert.True(m is IAcceptsMetadata am && am.RequestType == typeof(AddsCustomParameterMetadata)),
            // Parameter binding metadata inferred by RDF
            m => Assert.True(m is IParameterBindingMetadata { Name: "param1" }),
            // Inferred ProducesResopnseTypeMetadata from RDF for complex type
            m => Assert.Equal(typeof(CountsDefaultEndpointMetadataPoco), ((IProducesResponseTypeMetadata)m).Type),
            // Metadata provided by parameters implementing IEndpointParameterMetadataProvider
            m => Assert.True(m is ParameterNameMetadata { Name: "param1" }),
            // Metadata provided by parameters implementing IEndpointMetadataProvider
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Parameter }),
            // Metadata provided by return type implementing IEndpointMetadataProvider
            m => Assert.True(m is MetadataCountMetadata),
            // Entry-specific metadata added after a call to InferMetadata
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Caller }));
    }

    [Fact]
    public async Task Create_CombinesPropertiesAsParameterMetadata_AndTopLevelParameter()
    {
        // Arrange
        var (_, compilation) = await RunGeneratorAsync("""
app.MapPost("/test/pattern", ([AsParameters] AddsCustomParameterMetadata param1) => new CountsDefaultEndpointMetadataPoco())
   .WithMetadata(new CustomEndpointMetadata { Source = MetadataSource.Caller });
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
        Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "param1" });
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Property });
        Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: nameof(AddsCustomParameterMetadata.Data) });
    }
}
