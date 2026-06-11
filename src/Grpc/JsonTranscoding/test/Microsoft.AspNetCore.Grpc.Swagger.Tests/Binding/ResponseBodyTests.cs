// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.OpenApi;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Binding;

public class ResponseBodyTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ResponseBodyTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ResponseBodyString()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ResponseBodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/responsebody1"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.Responses["200"].Content["application/json"].Schema;
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, bodySchema);
    }

    [Fact]
    public void ResponseBodyRepeatedString()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ResponseBodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/responsebody2"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.Responses["200"].Content["application/json"].Schema;
        Assert.Null(OpenApiTestHelpers.GetSchemaId(bodySchema));
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Array, bodySchema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, bodySchema.Items);
    }

    [Fact]
    public void ResponseBodyEnum()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ResponseBodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/responsebody3"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.Responses["200"].Content["application/json"].Schema;

        var enumSchema = OpenApiTestHelpers.ResolveSchema(swagger, bodySchema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, enumSchema);
        Assert.Equal(5, enumSchema.Enum.Count);

        var enumValues = enumSchema.Enum.Select(e => e.GetValue<string>()).OrderBy(s => s).ToList();
        Assert.Collection(enumValues,
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_BAR", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_BAZ", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_FOO", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_NEG", v),
            v => Assert.Equal("ENUM_WITHOUT_MESSAGE_UNSPECIFIED", v));
    }

    [Fact]
    public void ResponseBodyMessage()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ResponseBodyService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/responsebody4"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);

        var bodySchema = operation.Responses["200"].Content["application/json"].Schema;

        var enumSchema = OpenApiTestHelpers.ResolveSchema(swagger, bodySchema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Object, enumSchema);
        Assert.False(enumSchema.AdditionalPropertiesAllowed);
    }
}
