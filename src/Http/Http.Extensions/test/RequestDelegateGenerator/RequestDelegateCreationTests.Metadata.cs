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
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/", () => "Hello, world!");
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(200, metadata.StatusCode);
        Assert.Equal("text/plain", metadata.ContentTypes.Single());
        Assert.Null(metadata.Type);
    }

    [Fact]
    public async Task MapAction_ReturnsVoid_Has_No_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/", () => {});
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>();
        Assert.Empty(metadata);
    }

    [Fact]
    public async Task MapAction_ReturnsTaskOfString_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/", Task () => Task.CompletedTask);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>();
        Assert.Empty(metadata);
    }

    [Fact]
    public async Task MapAction_ReturnsValueTaskOfString_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/", ValueTask () => ValueTask.CompletedTask);
""");
        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>();
        Assert.Empty(metadata);
    }

    [Fact]
    public async Task MapAction_ReturnsValidationProblemResult_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapGet("/", () => TypedResults.ValidationProblem(new Dictionary<string, string[]>()));
""");

        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().Single();
        Assert.Equal(400, metadata.StatusCode);
        Assert.Equal("application/problem+json", metadata.ContentTypes.Single());
    }

    [Fact]
    public async Task MapAction_TakesCustomMetadataEmitter_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapPost("/", (CustomMetadataEmitter x) => {});
""");

        var endpoint = GetEndpointFromCompilation(compilation);

        _ = endpoint.Metadata.OfType<CustomMetadata>().Single(m => m.Value == 42);
        _ = endpoint.Metadata.OfType<CustomMetadata>().Single(m => m.Value == 24);
    }

    [Fact]
    public async Task MapAction_ReturnsCustomMetadataEmitter_Has_Metadata()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapPost("/", () => new CustomMetadataEmitter());
""");

        var endpoint = GetEndpointFromCompilation(compilation);

        var metadata = endpoint.Metadata.OfType<CustomMetadata>().Single();
        Assert.Equal(24, metadata.Value);
    }

    [Fact]
    public async Task Create_AddJsonResponseType_AsMetadata()
    {
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
app.MapPost("/", () => ValueTask.FromResult(new AddsCustomEndpointMetadataResult()));
""");

        // Act
        var endpoint = GetEndpointFromCompilation(compilation);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    //[Fact]
    //public void Create_CombinesDefaultMetadata_AndMetadataFromReturnTypesImplementingIEndpointMetadataProvider()
    //{
    //    // Arrange
    //    var @delegate = () => new CountsDefaultEndpointMetadataResult();
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(new List<object>
    //        {
    //            new CustomEndpointMetadata { Source = MetadataSource.Caller }
    //        }),
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
    //    // Expecting '1' because only initial metadata will be in the metadata list when this metadata item is added
    //    Assert.Contains(result.EndpointMetadata, m => m is MetadataCountMetadata { Count: 1 });
    //}

    //[Fact]
    //public void Create_CombinesDefaultMetadata_AndMetadataFromTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    //{
    //    // Arrange
    //    var @delegate = () => Task.FromResult(new CountsDefaultEndpointMetadataResult());
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(new List<object>
    //        {
    //            new CustomEndpointMetadata { Source = MetadataSource.Caller }
    //        }),
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
    //    // Expecting '1' because only initial metadata will be in the metadata list when this metadata item is added
    //    Assert.Contains(result.EndpointMetadata, m => m is MetadataCountMetadata { Count: 1 });
    //}

    //[Fact]
    //public void Create_CombinesDefaultMetadata_AndMetadataFromValueTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    //{
    //    // Arrange
    //    var @delegate = () => ValueTask.FromResult(new CountsDefaultEndpointMetadataResult());
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(new List<object>
    //        {
    //            new CustomEndpointMetadata { Source = MetadataSource.Caller }
    //        }),
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
    //    // Expecting '1' because only initial metadata will be in the metadata list when this metadata item is added
    //    Assert.Contains(result.EndpointMetadata, m => m is MetadataCountMetadata { Count: 1 });
    //}

    //[Fact]
    //public void Create_CombinesDefaultMetadata_AndMetadataFromParameterTypesImplementingIEndpointParameterMetadataProvider()
    //{
    //    // Arrange
    //    var @delegate = (AddsCustomParameterMetadata param1) => "Hello";
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(new List<object>
    //        {
    //            new CustomEndpointMetadata { Source = MetadataSource.Caller }
    //        }),
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
    //    Assert.Contains(result.EndpointMetadata, m => m is ParameterNameMetadata { Name: "param1" });
    //}

    //[Fact]
    //public void Create_CombinesDefaultMetadata_AndMetadataFromParameterTypesImplementingIEndpointMetadataProvider()
    //{
    //    // Arrange
    //    var @delegate = (AddsCustomParameterMetadata param1) => "Hello";
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(new List<object>
    //        {
    //            new CustomEndpointMetadata { Source = MetadataSource.Caller }
    //        }),
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    //}

    //[Fact]
    //public void Create_CombinesPropertiesAsParameterMetadata_AndTopLevelParameter()
    //{
    //    // Arrange
    //    var @delegate = ([AsParameters] AddsCustomParameterMetadata param1) => new CountsDefaultEndpointMetadataResult();
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(new List<object>
    //        {
    //            new CustomEndpointMetadata { Source = MetadataSource.Caller }
    //        }),
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    //    Assert.Contains(result.EndpointMetadata, m => m is ParameterNameMetadata { Name: "param1" });
    //    Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Property });
    //    Assert.Contains(result.EndpointMetadata, m => m is ParameterNameMetadata { Name: nameof(AddsCustomParameterMetadata.Data) });
    //}

    //[Fact]
    //public void Create_CombinesAllMetadata_InCorrectOrder()
    //{
    //    // Arrange
    //    var @delegate = [Attribute1, Attribute2] (AddsCustomParameterMetadata param1) => new CountsDefaultEndpointMetadataPoco();
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(new List<object>
    //        {
    //            new CustomEndpointMetadata { Source = MetadataSource.Caller }
    //        }),
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Collection(result.EndpointMetadata,
    //        // Initial metadata from RequestDelegateFactoryOptions.EndpointBuilder. If the caller want to override inferred metadata,
    //        // They need to call InferMetadata first, then add the overriding metadata, and then call Create with InferMetadata's result.
    //        // This is demonstrated in the following tests.
    //        m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Caller }),
    //        // Inferred AcceptsMetadata from RDF for complex type
    //        m => Assert.True(m is AcceptsMetadata am && am.RequestType == typeof(AddsCustomParameterMetadata)),
    //        // Inferred ProducesResopnseTypeMetadata from RDF for complex type
    //        m => Assert.Equal(typeof(CountsDefaultEndpointMetadataPoco), ((IProducesResponseTypeMetadata)m).Type),
    //        // Metadata provided by parameters implementing IEndpointParameterMetadataProvider
    //        m => Assert.True(m is ParameterNameMetadata { Name: "param1" }),
    //        // Metadata provided by parameters implementing IEndpointMetadataProvider
    //        m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Parameter }),
    //        // Metadata provided by return type implementing IEndpointMetadataProvider
    //        m => Assert.True(m is MetadataCountMetadata { Count: 5 }));
    //}

    //[Fact]
    //public void Create_FlowsRoutePattern_ToMetadataProvider()
    //{
    //    // Arrange
    //    var @delegate = (AddsRoutePatternMetadata param1) => { };
    //    var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/test/pattern"), order: 0);
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = builder,
    //    };

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options);

    //    // Assert
    //    Assert.Contains(result.EndpointMetadata, m => m is RoutePatternMetadata { RoutePattern: "/test/pattern" });
    //}

    //[Fact]
    //public void Create_DoesNotInferMetadata_GivenManuallyConstructedMetadataResult()
    //{
    //    var invokeCount = 0;

    //    // Arrange
    //    var @delegate = [Attribute1, Attribute2] (AddsCustomParameterMetadata param1) =>
    //    {
    //        invokeCount++;
    //        return new CountsDefaultEndpointMetadataResult();
    //    };

    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(),
    //    };
    //    var metadataResult = new RequestDelegateMetadataResult { EndpointMetadata = new List<object>() };
    //    var httpContext = CreateHttpContext();

    //    // An empty object should deserialize to AddsCustomParameterMetadata since it has no required properties.
    //    var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new object());
    //    var stream = new MemoryStream(requestBodyBytes);
    //    httpContext.Request.Body = stream;

    //    httpContext.Request.Headers["Content-Type"] = "application/json";
    //    httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
    //    httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

    //    // Act
    //    var result = RequestDelegateFactory.Create(@delegate, options, metadataResult);

    //    // Assert
    //    Assert.Empty(result.EndpointMetadata);
    //    Assert.Same(options.EndpointBuilder.Metadata, result.EndpointMetadata);

    //    // Make extra sure things are running as expected, as this non-InferMetadata path is no longer exercised by RouteEndpointDataSource,
    //    // and most of the other unit tests don't pass in a metadataResult without a cached factory context.
    //    Assert.True(result.RequestDelegate(httpContext).IsCompletedSuccessfully);
    //    Assert.Equal(1, invokeCount);
    //}

    //[Fact]
    //public void InferMetadata_ThenCreate_CombinesAllMetadata_InCorrectOrder()
    //{
    //    // Arrange
    //    var @delegate = [Attribute1, Attribute2] (AddsCustomParameterMetadata param1) => new CountsDefaultEndpointMetadataPoco();
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(),
    //    };

    //    // Act
    //    var metadataResult = RequestDelegateFactory.InferMetadata(@delegate.Method, options);
    //    options.EndpointBuilder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Caller });
    //    var result = RequestDelegateFactory.Create(@delegate, options, metadataResult);

    //    // Assert
    //    Assert.Collection(result.EndpointMetadata,
    //        // Inferred AcceptsMetadata from RDF for complex type
    //        m => Assert.True(m is AcceptsMetadata am && am.RequestType == typeof(AddsCustomParameterMetadata)),
    //        // Inferred ProducesResopnseTypeMetadata from RDF for complex type
    //        m => Assert.Equal(typeof(CountsDefaultEndpointMetadataPoco), ((IProducesResponseTypeMetadata)m).Type),
    //        // Metadata provided by parameters implementing IEndpointParameterMetadataProvider
    //        m => Assert.True(m is ParameterNameMetadata { Name: "param1" }),
    //        // Metadata provided by parameters implementing IEndpointMetadataProvider
    //        m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Parameter }),
    //        // Metadata provided by return type implementing IEndpointMetadataProvider
    //        m => Assert.True(m is MetadataCountMetadata { Count: 4 }),
    //        // Entry-specific metadata added after a call to InferMetadata
    //        m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Caller }));
    //}

    //[Fact]
    //public void InferMetadata_PopulatesAcceptsMetadata_WhenReadFromForm()
    //{
    //    // Arrange
    //    var @delegate = void (IFormCollection formCollection) => { };
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(),
    //    };

    //    // Act
    //    var metadataResult = RequestDelegateFactory.InferMetadata(@delegate.Method, options);

    //    // Assert
    //    var allAcceptsMetadata = metadataResult.EndpointMetadata.OfType<IAcceptsMetadata>();
    //    var acceptsMetadata = Assert.Single(allAcceptsMetadata);

    //    Assert.NotNull(acceptsMetadata);
    //    Assert.Equal(new[] { "multipart/form-data", "application/x-www-form-urlencoded" }, acceptsMetadata.ContentTypes);
    //}

    //[Fact]
    //public void InferMetadata_PopulatesCachedContext()
    //{
    //    // Arrange
    //    var @delegate = void () => { };
    //    var options = new RequestDelegateFactoryOptions
    //    {
    //        EndpointBuilder = CreateEndpointBuilder(),
    //    };

    //    // Act
    //    var metadataResult = RequestDelegateFactory.InferMetadata(@delegate.Method, options);

    //    // Assert
    //    Assert.NotNull(metadataResult.CachedFactoryContext);
    //}

    [Fact]
    public async Task Create_AllowsRemovalOfDefaultMetadata_ByReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (Todo todo) => new RemovesAcceptsMetadataResult();
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
        var (_, compilation) = await RunGeneratorAsync($$"""
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
