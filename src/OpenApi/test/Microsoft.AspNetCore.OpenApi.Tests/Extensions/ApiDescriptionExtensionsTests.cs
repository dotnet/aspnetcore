// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

public class ApiDescriptionExtensionsTests
{
    [Theory]
    [InlineData("api/todos", "/api/todos")]
    [InlineData("api/todos/{id}", "/api/todos/{id}")]
    [InlineData("api/todos/{id:int:min(10)}", "/api/todos/{id}")]
    [InlineData("{a}/{b}/{c=19}", "/{a}/{b}/{c}")]
    [InlineData("{a}/{b}/{c?}", "/{a}/{b}/{c}")]
    [InlineData("{a:int}/{b}/{c:int}", "/{a}/{b}/{c}")]
    [InlineData("", "/")]
    [InlineData("api", "/api")]
    [InlineData("{p1}/{p2}.{p3?}", "/{p1}/{p2}.{p3}")]
    public void MapRelativePathToItemPath_ReturnsItemPathForApiDescription(string relativePath, string expectedItemPath)
    {
        // Arrange
        var apiDescription = new ApiDescription
        {
            RelativePath = relativePath
        };

        // Act
        var itemPath = apiDescription.MapRelativePathToItemPath();

        // Assert
        Assert.Equal(expectedItemPath, itemPath);
    }

    [Theory]
    [InlineData("GET", OperationType.Get)]
    [InlineData("POST", OperationType.Post)]
    [InlineData("PUT", OperationType.Put)]
    [InlineData("DELETE", OperationType.Delete)]
    [InlineData("PATCH", OperationType.Patch)]
    [InlineData("HEAD", OperationType.Head)]
    [InlineData("OPTIONS", OperationType.Options)]
    [InlineData("TRACE", OperationType.Trace)]
    [InlineData("gEt", OperationType.Get)]
    public void ToOperationType_ReturnsOperationTypeForApiDescription(string httpMethod, OperationType expectedOperationType)
    {
        // Arrange
        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod
        };

        // Act
        var operationType = apiDescription.GetOperationType();

        // Assert
        Assert.Equal(expectedOperationType, operationType);
    }

    [Theory]
    [InlineData("UNKNOWN")]
    [InlineData("unknown")]
    public void ToOperationType_ThrowsForUnknownHttpMethod(string methodName)
    {
        // Arrange
        var apiDescription = new ApiDescription
        {
            HttpMethod = methodName
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => apiDescription.GetOperationType());
        Assert.Equal($"Unsupported HTTP method: {methodName}", exception.Message);
    }
}
