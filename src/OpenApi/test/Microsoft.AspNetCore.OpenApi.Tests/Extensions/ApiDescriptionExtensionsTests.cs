// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
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

    public static class HttpMethodTestData
    {
        public static IEnumerable<object[]> TestCases => new List<object[]>
        {
            new object[] { "GET", HttpMethod.Get },
            new object[] { "POST", HttpMethod.Post },
            new object[] { "PUT", HttpMethod.Put },
            new object[] { "DELETE", HttpMethod.Delete },
            new object[] { "PATCH", HttpMethod.Patch },
            new object[] { "HEAD", HttpMethod.Head },
            new object[] { "OPTIONS", HttpMethod.Options },
            new object[] { "TRACE", HttpMethod.Trace },
            new object[] { "gEt", HttpMethod.Get }, // Test case-insensitivity
        };
    }

    [Theory]
    [MemberData(nameof(HttpMethodTestData.TestCases), MemberType = typeof(HttpMethodTestData))]
    public void GetHttpMethod_ReturnsHttpMethodForApiDescription(string httpMethod, HttpMethod expectedHttpMethod)
    {
        // Arrange
        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod
        };

        // Act
        var result = apiDescription.GetHttpMethod();

        // Assert
        Assert.Equal(expectedHttpMethod, result);
    }

    [Theory]
    [InlineData("UNKNOWN")]
    [InlineData("unknown")]
    public void GetHttpMethod_ThrowsForUnknownHttpMethod(string methodName)
    {
        // Arrange
        var apiDescription = new ApiDescription
        {
            HttpMethod = methodName
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => apiDescription.GetHttpMethod());
        Assert.Equal($"Unsupported HTTP method: {methodName}", exception.Message);
    }
}
