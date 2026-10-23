// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

public union UnionIntString(int, string);

public record Cat(string Name, int Lives);
public record Dog(string Name, string Breed);
public union UnionPet(Cat, Dog);

public partial class EndpointMetadataApiDescriptionProviderTest
{
    [Fact]
    public void AddsResponseType_ForUnionReturnType_InferredFromHandler()
    {
        var apiDescription = GetApiDescription(() => new UnionIntString(42));

        var responseType = Assert.Single(apiDescription.SupportedResponseTypes);

        Assert.Equal(StatusCodes.Status200OK, responseType.StatusCode);
        Assert.Equal(typeof(UnionIntString), responseType.Type);
        Assert.Equal(typeof(UnionIntString), responseType.ModelMetadata?.ModelType);

        var format = Assert.Single(responseType.ApiResponseFormats);
        Assert.Equal("application/json", format.MediaType);
    }

    [Fact]
    public void AddsResponseType_ForUnionInsideTypedResultsOk()
    {
        var apiDescription = GetApiDescription(() => TypedResults.Ok(new UnionIntString(42)));

        var responseType = Assert.Single(apiDescription.SupportedResponseTypes);

        Assert.Equal(StatusCodes.Status200OK, responseType.StatusCode);
        Assert.Equal(typeof(UnionIntString), responseType.Type);
        Assert.Equal(typeof(UnionIntString), responseType.ModelMetadata?.ModelType);

        var format = Assert.Single(responseType.ApiResponseFormats);
        Assert.Equal("application/json", format.MediaType);
    }

    [Fact]
    public void AddsResponseType_ForUnionInsideResultsTUnion()
    {
        var apiDescription = GetApiDescription(Results<Ok<UnionIntString>, NotFound> () =>
            TypedResults.Ok(new UnionIntString("hi")));

        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var okResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.StatusCode == StatusCodes.Status200OK);
        Assert.Equal(typeof(UnionIntString), okResponseType.Type);
        Assert.Equal(typeof(UnionIntString), okResponseType.ModelMetadata?.ModelType);
        Assert.Equal("application/json", Assert.Single(okResponseType.ApiResponseFormats).MediaType);

        var notFoundResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.StatusCode == StatusCodes.Status404NotFound);
        Assert.Equal(typeof(void), notFoundResponseType.Type);
        Assert.Empty(notFoundResponseType.ApiResponseFormats);
    }

    [Fact]
    public void AddsResponseType_ForUnion_WithProducesResponseTypeAttribute()
    {
        var apiDescription = GetApiDescription(
            [ProducesResponseType<UnionIntString>(StatusCodes.Status200OK)]
            () => Results.Ok());

        var responseType = Assert.Single(apiDescription.SupportedResponseTypes);

        Assert.Equal(StatusCodes.Status200OK, responseType.StatusCode);
        Assert.Equal(typeof(UnionIntString), responseType.Type);
        Assert.Equal(typeof(UnionIntString), responseType.ModelMetadata?.ModelType);

        var format = Assert.Single(responseType.ApiResponseFormats);
        Assert.Equal("application/json", format.MediaType);
    }

    [Fact]
    public void AddsResponseType_ForUnion_WithProducesBuilderExtension()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api/union", () => Results.Ok())
            .Produces<UnionIntString>(StatusCodes.Status200OK);

        var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());
        var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
        var provider = CreateEndpointMetadataApiDescriptionProvider(endpointDataSource);

        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var apiDescription = Assert.Single(context.Results);
        var responseType = Assert.Single(apiDescription.SupportedResponseTypes);

        Assert.Equal(StatusCodes.Status200OK, responseType.StatusCode);
        Assert.Equal(typeof(UnionIntString), responseType.Type);
        Assert.Equal(typeof(UnionIntString), responseType.ModelMetadata?.ModelType);

        var format = Assert.Single(responseType.ApiResponseFormats);
        Assert.Equal("application/json", format.MediaType);
    }

    [Fact]
    public void PreservesUnionAndNonUnion_AtSameStatusCode_DefaultContentType()
    {
        var apiDescription = GetApiDescription(
            [ProducesResponseType<UnionIntString>(StatusCodes.Status200OK)]
            [ProducesResponseType<InferredJsonClass>(StatusCodes.Status200OK)]
            () => Results.Ok());

        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var unionResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(UnionIntString));
        Assert.Equal(StatusCodes.Status200OK, unionResponseType.StatusCode);

        var classResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(InferredJsonClass));
        Assert.Equal(StatusCodes.Status200OK, classResponseType.StatusCode);
    }

    [Fact]
    public void PreservesTwoUnions_AtSameStatusCode_DefaultContentType()
    {
        var apiDescription = GetApiDescription(
            [ProducesResponseType<UnionIntString>(StatusCodes.Status200OK)]
            [ProducesResponseType<UnionPet>(StatusCodes.Status200OK)]
            () => Results.Ok());

        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var intStringResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(UnionIntString));
        Assert.Equal(StatusCodes.Status200OK, intStringResponseType.StatusCode);

        var petResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(UnionPet));
        Assert.Equal(StatusCodes.Status200OK, petResponseType.StatusCode);
    }

    [Fact]
    public void PreservesUnionAndNonUnion_AtSameStatusCode_SameContentType()
    {
        var apiDescription = GetApiDescription(
            [ProducesResponseType(typeof(UnionIntString), StatusCodes.Status200OK, "application/json")]
            [ProducesResponseType(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json")]
            () => Results.Ok());

        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var unionResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(UnionIntString));
        Assert.Equal(StatusCodes.Status200OK, unionResponseType.StatusCode);
        Assert.Equal("application/json", Assert.Single(unionResponseType.ApiResponseFormats).MediaType);

        var classResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(InferredJsonClass));
        Assert.Equal(StatusCodes.Status200OK, classResponseType.StatusCode);
        Assert.Equal("application/json", Assert.Single(classResponseType.ApiResponseFormats).MediaType);
    }

    [Fact]
    public void PreservesUnionAndNonUnion_AtSameStatusCode_DifferentContentTypes()
    {
        var apiDescription = GetApiDescription(
            [ProducesResponseType(typeof(UnionIntString), StatusCodes.Status200OK, "application/json")]
            [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/html")]
            () => Results.Ok());

        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var unionResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(UnionIntString));
        Assert.Equal(StatusCodes.Status200OK, unionResponseType.StatusCode);
        Assert.Equal("application/json", Assert.Single(unionResponseType.ApiResponseFormats).MediaType);

        var htmlResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(string));
        Assert.Equal(StatusCodes.Status200OK, htmlResponseType.StatusCode);
        Assert.Equal("text/html", Assert.Single(htmlResponseType.ApiResponseFormats).MediaType);
    }

    [Fact]
    public void DedupesSameUnionDeclaredTwice_AtSameStatusCode()
    {
        var apiDescription = GetApiDescription(
            [ProducesResponseType<UnionIntString>(StatusCodes.Status200OK)]
            [ProducesResponseType<UnionIntString>(StatusCodes.Status200OK)]
            () => Results.Ok());

        var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
        Assert.Equal(StatusCodes.Status200OK, responseType.StatusCode);
        Assert.Equal(typeof(UnionIntString), responseType.Type);
    }

    [Fact]
    public void ProducesBuilder_PreservesUnionAndNonUnion_AtSameStatusCode()
    {
        var builder = CreateBuilder();
        builder.MapGet("/api/union", () => Results.Ok())
            .Produces<UnionIntString>(StatusCodes.Status200OK)
            .Produces<InferredJsonClass>(StatusCodes.Status200OK);

        var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());
        var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
        var provider = CreateEndpointMetadataApiDescriptionProvider(endpointDataSource);

        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var apiDescription = Assert.Single(context.Results);
        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var unionResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(UnionIntString));
        Assert.Equal(StatusCodes.Status200OK, unionResponseType.StatusCode);

        var classResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(InferredJsonClass));
        Assert.Equal(StatusCodes.Status200OK, classResponseType.StatusCode);
    }

    [Fact]
    public void TypedResultsUnionWinsOverProducesResponseTypeAttribute_ForSameStatusCode()
    {
        // TypedResults contributes IProducesResponseTypeMetadata; [ProducesResponseType] contributes
        // IApiResponseMetadataProvider. IProducesResponseTypeMetadata has higher priority
        var apiDescription = GetApiDescription(
            [ProducesResponseType<InferredJsonClass>(StatusCodes.Status200OK)]
            () => TypedResults.Ok(new UnionIntString(42)));

        var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
        Assert.Equal(StatusCodes.Status200OK, responseType.StatusCode);
        Assert.Equal(typeof(UnionIntString), responseType.Type);
        Assert.DoesNotContain(apiDescription.SupportedResponseTypes, r => r.Type == typeof(InferredJsonClass));
    }

    [Fact]
    public void TypedResultsNonUnionAndProducesBuilderUnion_AtSameStatusCode_BothCoexist()
    {
        // Both TypedResults.Ok<T>() and .Produces<T>() contribute IProducesResponseTypeMetadata,
        // so they coexist for the same status with different types. The "TypedResults wins" rule
        // only drops attribute-driven entries, not other endpoint metadata.
        var builder = CreateBuilder();
        builder.MapGet("/api/union", () => TypedResults.Ok(new InferredJsonClass()))
            .Produces<UnionIntString>(StatusCodes.Status200OK);

        var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());
        var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
        var provider = CreateEndpointMetadataApiDescriptionProvider(endpointDataSource);

        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var apiDescription = Assert.Single(context.Results);
        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var classResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(InferredJsonClass));
        Assert.Equal(StatusCodes.Status200OK, classResponseType.StatusCode);

        var unionResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.Type == typeof(UnionIntString));
        Assert.Equal(StatusCodes.Status200OK, unionResponseType.StatusCode);
    }

    [Fact]
    public void UsesUnionAsDefaultClientErrorResponseType_FromProducesErrorResponseType()
    {
        // [ProducesErrorResponseType(typeof(UnionPet))] supplies the default Type for client-error
        // status codes that did not specify their own. Here, [ProducesResponseType(400)] declares
        // the 400 status without a type — it should resolve to UnionPet.
        var apiDescription = GetApiDescription(
            [ProducesErrorResponseType(typeof(UnionPet))]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            () => new UnionIntString(42));

        Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

        var okResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.StatusCode == StatusCodes.Status200OK);
        Assert.Equal(typeof(UnionIntString), okResponseType.Type);

        var badRequestResponseType = apiDescription.SupportedResponseTypes
            .Single(r => r.StatusCode == StatusCodes.Status400BadRequest);
        Assert.Equal(typeof(UnionPet), badRequestResponseType.Type);
    }
}

