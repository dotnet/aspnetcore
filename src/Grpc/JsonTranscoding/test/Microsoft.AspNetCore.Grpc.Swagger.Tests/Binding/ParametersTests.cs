// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.OpenApi.Models;
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
        Assert.True(path.Operations.TryGetValue(OperationType.Get, out var operation));
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
        Assert.True(path.Operations.TryGetValue(OperationType.Get, out var operation));
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
        Assert.True(path.Operations.TryGetValue(OperationType.Post, out var operation));
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
        Assert.True(path.Operations.TryGetValue(OperationType.Post, out var operation));
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
        Assert.True(path.Operations.TryGetValue(OperationType.Get, out var operation));
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
        Assert.True(path.Operations.TryGetValue(OperationType.Get, out var operation));
        Assert.Single(operation.Parameters);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[0].In);
        Assert.Equal("parameterOne", operation.Parameters[0].Name);
        Assert.Equal("array", operation.Parameters[0].Schema.Type);
        Assert.Equal("integer", operation.Parameters[0].Schema.Items.Type);
    }

    [Fact]
    public void MultipleRouteParameter_NestedFields_MissingFieldsAreQuery()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ParametersService>(_testOutputHelper);

        // Assert
        var path = swagger.Paths["/v1/parameters7/{parameterOne.nestedParameterOne}/{parameterOne.nestedParameterTwo}"];
        Assert.True(path.Operations.TryGetValue(OperationType.Get, out var operation));
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
        Assert.True(path.Operations.TryGetValue(OperationType.Get, out var operation));
        Assert.Equal(3, operation.Parameters.Count);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[0].In);
        Assert.Equal("fieldMaskValue", operation.Parameters[0].Name);
        Assert.Equal("string", operation.Parameters[0].Schema.Type);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[1].In);
        Assert.Equal("stringValue", operation.Parameters[1].Name);
        Assert.Equal("string", operation.Parameters[1].Schema.Type);
        Assert.Equal(ParameterLocation.Query, operation.Parameters[2].In);
        Assert.Equal("int32Value", operation.Parameters[2].Name);
        Assert.Equal("integer", operation.Parameters[2].Schema.Type);
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

        static void AssertParams(OpenApiPathItem path)
        {
            Assert.True(path.Operations.TryGetValue(OperationType.Get, out var operation));
            Assert.Equal(2, operation.Parameters.Count);
            Assert.Equal(ParameterLocation.Path, operation.Parameters[0].In);
            Assert.Equal("parameterInt", operation.Parameters[0].Name);
            Assert.Equal(ParameterLocation.Query, operation.Parameters[1].In);
            Assert.Equal("parameterString", operation.Parameters[1].Name);
        }
    }
}
