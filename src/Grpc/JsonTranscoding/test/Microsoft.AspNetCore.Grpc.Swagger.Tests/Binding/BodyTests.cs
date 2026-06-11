// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.OpenApi;
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
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Null(OpenApiTestHelpers.GetSchemaId(bodySchema));
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Array, bodySchema);
        Assert.Equal("RequestBody", OpenApiTestHelpers.GetSchemaId(bodySchema.Items));

        var messageSchema = OpenApiTestHelpers.ResolveSchema(swagger, bodySchema.Items);
        Assert.NotNull(messageSchema);
    }

    [Fact]
    public void PostMap()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<BodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/body2"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Null(OpenApiTestHelpers.GetSchemaId(bodySchema));
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, bodySchema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Integer, bodySchema.AdditionalProperties);
    }

    [Fact]
    public void PostMessage()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<BodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/body3"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Equal("RequestBody", OpenApiTestHelpers.GetSchemaId(bodySchema));
    }

    [Fact]
    public void PostRoot()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<BodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/body4"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.RequestBody.Content["application/json"].Schema;
        Assert.Equal("RequestOne", OpenApiTestHelpers.GetSchemaId(bodySchema));
    }
}
