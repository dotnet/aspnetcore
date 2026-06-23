// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing.Patterns;

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
    [InlineData("/~health", "~health", "/~health")]
    [InlineData("/~api/todos", "~api/todos", "/~api/todos")]
    [InlineData("/~api/todos/{id}", "~api/todos/{id}", "/~api/todos/{id}")]
    [InlineData("~/health", "health", "/health")]
    [InlineData("~/api/todos", "api/todos", "/api/todos")]
    [InlineData("~/api/todos/{id}", "api/todos/{id}", "/api/todos/{id}")]
    public void MapRelativePathToItemPath_WithRoutePattern_HandlesRoutesThatStartWithTilde(string rawPattern, string relativePath, string expectedItemPath)
    {
        // Arrange
        var routePattern = RoutePatternFactory.Parse(rawPattern);
        var apiDescription = new ApiDescription
        {
            RelativePath = relativePath,
            RoutePattern = routePattern
        };

        // Act
        var itemPath = apiDescription.MapRelativePathToItemPath();

        // Assert
        Assert.Equal(expectedItemPath, itemPath);
    }

    public static class HttpMethodTestData
    {
        public static IEnumerable<object[]> KnownMethods => new List<object[]>
        {
            new object[] { "GET", HttpMethod.Get },
            new object[] { "POST", HttpMethod.Post },
            new object[] { "PUT", HttpMethod.Put },
            new object[] { "DELETE", HttpMethod.Delete },
            new object[] { "PATCH", HttpMethod.Patch },
            new object[] { "HEAD", HttpMethod.Head },
            new object[] { "OPTIONS", HttpMethod.Options },
            new object[] { "TRACE", HttpMethod.Trace },
            new object[] { "QUERY", HttpMethod.Query },
            new object[] { "gEt", HttpMethod.Get },
            new object[] { "pOsT", HttpMethod.Post },
            new object[] { "QuErY", HttpMethod.Query },
            new object[] { " GET ", HttpMethod.Get },
        };

        public static IEnumerable<object[]> UnsupportedMethods => new List<object[]>
        {
            new object[] { "foo", "foo" },
            new object[] { "Foo", "Foo" },
            new object[] { "FOO", "FOO" },
            new object[] { "customMethod", "customMethod" },
            new object[] { " FOO ", "FOO" },
            new object[] { " FooBar ", "FooBar" },
        };

        public static IEnumerable<object[]> InvalidMethods => new List<object[]>
        {
            new object[] { "FOO BAR" },
        };
    }

    [Theory]
    [MemberData(nameof(HttpMethodTestData.KnownMethods), MemberType = typeof(HttpMethodTestData))]
    public void GetHttpMethod_ReturnsKnownHttpMethodForApiDescription(string httpMethod, HttpMethod expectedHttpMethod)
    {
        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod
        };

        var result = apiDescription.GetHttpMethod();

        Assert.Equal(expectedHttpMethod, result);
        Assert.Equal(expectedHttpMethod.Method, result?.Method);
    }

    [Theory]
    [MemberData(nameof(HttpMethodTestData.UnsupportedMethods), MemberType = typeof(HttpMethodTestData))]
    public void GetHttpMethod_PreservesUnsupportedMethodCasingAfterTrimming(string httpMethod, string expectedHttpMethod)
    {
        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod
        };

        var result = apiDescription.GetHttpMethod();

        Assert.Equal(expectedHttpMethod, result?.Method);
    }

    [Theory]
    [MemberData(nameof(HttpMethodTestData.InvalidMethods), MemberType = typeof(HttpMethodTestData))]
    public void GetHttpMethod_ReturnsNullForInvalidHttpMethodToken(string httpMethod)
    {
        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod
        };

        var result = apiDescription.GetHttpMethod();

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetHttpMethod_ReturnsNullWhenApiDescriptionHasNoHttpMethod(string httpMethod)
    {
        var apiDescription = new ApiDescription
        {
            HttpMethod = httpMethod
        };

        var result = apiDescription.GetHttpMethod();

        Assert.Null(result);
    }
}
