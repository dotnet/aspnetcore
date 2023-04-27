// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.OpenApi.Models;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Binding;

public class BodyTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public BodyTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void PostRepeated()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<BodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/body1"];
        Assert.True(path.Operations.TryGetValue(OperationType.Post, out var operation));

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Null(bodySchema.Reference);
        Assert.Equal("array", bodySchema.Type);
        Assert.Equal("RequestBody", bodySchema.Items.Reference.Id);

        var messageSchema = swagger.ResolveReference(bodySchema.Items.Reference);
        Assert.NotNull(messageSchema);
    }

    [Fact]
    public void PostMap()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<BodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/body2"];
        Assert.True(path.Operations.TryGetValue(OperationType.Post, out var operation));

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Null(bodySchema.Reference);
        Assert.Equal("object", bodySchema.Type);
        Assert.Equal("integer", bodySchema.AdditionalProperties.Type);
    }

    [Fact]
    public void PostMessage()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<BodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/body3"];
        Assert.True(path.Operations.TryGetValue(OperationType.Post, out var operation));

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Equal("RequestBody", bodySchema.Reference.Id);
    }

    [Fact]
    public void PostRoot()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<BodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/body4"];
        Assert.True(path.Operations.TryGetValue(OperationType.Post, out var operation));

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Equal("RequestOne", bodySchema.Reference.Id);
    }
}
