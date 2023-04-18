// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.Extensions.Primitives;

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
}
