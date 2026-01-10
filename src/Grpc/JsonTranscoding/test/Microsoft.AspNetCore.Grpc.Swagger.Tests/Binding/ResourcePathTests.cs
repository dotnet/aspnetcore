// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Grpc.Swagger.Tests.Infrastructure;
using Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;
using Microsoft.OpenApi.Models;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Binding;

public class ResourcePathTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ResourcePathTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ConflictingPaths_DifferentLiteralSegments_GenerateUniquePaths()
    {
        // Arrange & Act
        var swagger = OpenApiTestHelpers.GetOpenApiDocument<ResourceService>(_testOutputHelper);

        // Assert - Should have 3 distinct paths
        Assert.Equal(3, swagger.Paths.Count);

        // Path 1: /v1/widgets/{name}
        Assert.True(swagger.Paths.ContainsKey("/v1/widgets/{name}"));
        var widgetPath = swagger.Paths["/v1/widgets/{name}"];
        Assert.True(widgetPath.Operations.TryGetValue(OperationType.Get, out var widgetOperation));
        Assert.Single(widgetOperation.Parameters);
        Assert.Equal(ParameterLocation.Path, widgetOperation.Parameters[0].In);
        Assert.Equal("name", widgetOperation.Parameters[0].Name);

        // Path 2: /v1/things/{name}
        Assert.True(swagger.Paths.ContainsKey("/v1/things/{name}"));
        var thingPath = swagger.Paths["/v1/things/{name}"];
        Assert.True(thingPath.Operations.TryGetValue(OperationType.Get, out var thingOperation));
        Assert.Single(thingOperation.Parameters);
        Assert.Equal(ParameterLocation.Path, thingOperation.Parameters[0].In);
        Assert.Equal("name", thingOperation.Parameters[0].Name);

        // Path 3: /v1/gadgets/{name}/items/{name} (nested path)
        Assert.True(swagger.Paths.ContainsKey("/v1/gadgets/{name}/items/{name}"));
        var gadgetPath = swagger.Paths["/v1/gadgets/{name}/items/{name}"];
        Assert.True(gadgetPath.Operations.TryGetValue(OperationType.Get, out var gadgetOperation));
        Assert.Single(gadgetOperation.Parameters);
        Assert.Equal(ParameterLocation.Path, gadgetOperation.Parameters[0].In);
        Assert.Equal("name", gadgetOperation.Parameters[0].Name);
    }
}
