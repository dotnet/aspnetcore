// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class UnitTest1
{
    [Fact]
    public void MultipleApiDescriptionsCreatedForMultipleHttpMethods()
    {
        var pathItem = GetOpenApiPathItem(() => { }, "/", new string[] { "FOO", "BAR" });

        Assert.Equal(2, pathItem.Operations.Count);
    }

    private static OpenApiPathItem GetOpenApiPathItem(
Delegate action,
string pattern = null,
IEnumerable<string> httpMethods = null,
string displayName = null)
    {
        var methodInfo = action.Method;
        var attributes = methodInfo.GetCustomAttributes();

        var httpMethodMetadata = new HttpMethodMetadata(httpMethods ?? new[] { "GET" });
        var metadataItems = new List<object>(attributes) { methodInfo, httpMethodMetadata };
        var endpointMetadata = new EndpointMetadataCollection(metadataItems.ToArray());
        // var routePattern = RoutePatternFactory.Parse(pattern ?? "/");


        return OpenApiOperationExtensions.GetOpenApiPathItem(methodInfo, endpointMetadata);
    }
}
