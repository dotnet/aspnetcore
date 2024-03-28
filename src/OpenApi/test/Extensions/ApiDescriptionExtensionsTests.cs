// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;

public class ApiDescriptionExtensionsTests
{
    [Theory]
    [InlineData("api/todos", "/api/todos")]
    [InlineData("api/todos/{id}", "/api/todos/{id}")]
    [InlineData("api/todos/{id:int:min(10)}", "/api/todos/{id}")]
    [InlineData("{a}/{b}/{c=19}", "/{a}/{b}/{c}")]
    [InlineData("{a}/{b}/{c?}", "/{a}/{b}/{c}")]
    [InlineData("{a:int}/{b}/{c:int}", "/{a}/{b}/{c}")]
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
}
