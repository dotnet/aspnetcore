// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.OpenApi;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Binding;

public class ParametersTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ParametersTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void NoRouteOrBody_AllQueryFields()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters1"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Get);
        Assert.Equal(2, operation.Parameters.Count);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[0].In);
        Assert.Equal("parameterInt", operation.Parameters[0].Name);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[1].In);
        Assert.Equal("parameterString", operation.Parameters[1].Name);
    }

    [Fact]
    public void RouteFields_FilterRouteQueryFields()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters2/{parameterInt}"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Get);
        Assert.Equal(2, operation.Parameters.Count);
        Assert.Equal(ParameterLocation.Path, operation.Parameters[0].In);
        Assert.Equal("parameterInt", operation.Parameters[0].Name);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[1].In);
        Assert.Equal("parameterString", operation.Parameters[1].Name);
    }

    [Fact]
    public void RouteAndBodyFields_FilterRouteAndBodyQueryFields()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters3/{parameterOne}"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);
        Assert.Equal(3, operation.Parameters.Count);
        Assert.Equal(ParameterLocation.Path, operation.Parameters[0].In);
        Assert.Equal("parameterOne", operation.Parameters[0].Name);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[1].In);
        Assert.Equal("parameterTwo", operation.Parameters[1].Name);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[2].In);
        Assert.Equal("parameterThree", operation.Parameters[2].Name);
        // body with one parameter
        Assert.NotNull(operation.RequestBody);
        Assert.Single(swagger.Components.Schemas["RequestBody"].Properties);
    }

    [Fact]
    public void CatchAllBody_NoQueryFields()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters4/{parameterTwo}"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Post);
        Assert.Single(operation.Parameters);
        Assert.Equal(ParameterLocation.Path, operation.Parameters[0].In);
        Assert.Equal("parameterTwo", operation.Parameters[0].Name);
        // body with four parameters
        Assert.NotNull(operation.RequestBody);
        Assert.Equal(4, swagger.Components.Schemas["RequestTwo"].Properties.Count);
    }

    [Fact]
    public void NoBodyComplexType_NestedQueryField()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters5/{parameterOne}"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Get);
        Assert.Equal(4, operation.Parameters.Count);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[3].In);
        Assert.Equal("parameterFour.requestBody", operation.Parameters[3].Name);
    }

    [Fact]
    public void RepeatedStringField_ArrayQueryField()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters6"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Get);
        Assert.Single(operation.Parameters);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[0].In);
        Assert.Equal("parameterOne", operation.Parameters[0].Name);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Array, operation.Parameters[0].Schema);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Integer, operation.Parameters[0].Schema.Items);
    }

    [Fact]
    public void MultipleRouteParameter_NestedFields_MissingFieldsAreQuery()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters7/{parameterOne.nestedParameterOne}/{parameterOne.nestedParameterTwo}"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Get);
        Assert.Equal(5, operation.Parameters.Count);
        Assert.Equal(ParameterLocation.Path, operation.Parameters[0].In);
        Assert.Equal("parameterOne.nestedParameterOne", operation.Parameters[0].Name);
        Assert.Equal(ParameterLocation.Path, operation.Parameters[1].In);
        Assert.Equal("parameterOne.nestedParameterTwo", operation.Parameters[1].Name);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[2].In);
        Assert.Equal("parameterOne.nestedParameterThree", operation.Parameters[2].Name);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[3].In);
        Assert.Equal("parameterOne.nestedParameterFour", operation.Parameters[3].Name);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[4].In);
        Assert.Equal("parameterTwo", operation.Parameters[4].Name);
    }

    [Fact]
    public void KnownTypes_AllQueryFields()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters9"];
        var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Get);
        Assert.Equal(3, operation.Parameters.Count);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[0].In);
        Assert.Equal("fieldMaskValue", operation.Parameters[0].Name);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, operation.Parameters[0].Schema);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[1].In);
        Assert.Equal("stringValue", operation.Parameters[1].Name);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.String, operation.Parameters[1].Schema);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[2].In);
        Assert.Equal("int32Value", operation.Parameters[2].Name);
        OpenApiTestHelpers.AssertSchemaType(JsonSchemaType.Integer, operation.Parameters[2].Schema);
        Assert.Equal("int32", operation.Parameters[2].Schema.Format);
    }

    [Fact]
    public void Verb()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path1 = swagger.Paths["/v1/parameters10/{parameterInt}:one"];
        AssertParams(path1);
        var path2 = swagger.Paths["/v1/parameters10/{parameterInt}:two"];
        AssertParams(path2);

        static void AssertParams(IOpenApiPathItem path)
        {
            var operation = OpenApiTestHelpers.GetOperation(path, HttpMethod.Get);
            Assert.Equal(2, operation.Parameters.Count);
            Assert.Equal(ParameterLocation.Path, operation.Parameters[0].In);
            Assert.Equal("parameterInt", operation.Parameters[0].Name);
            Assert.Equal(ParameterLocation.Query, operation.Parameters[1].In);
            Assert.Equal("parameterString", operation.Parameters[1].Name);
        }
    }
}
