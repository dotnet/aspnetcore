// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class OpenApiOperationGeneratorTests
{
    [Fact]
    public void MultipleOperationssCreatedForMultipleHttpMethods()
    {
        var pathItem = GetOpenApiPathItem(() => { }, "/", new string[] { "GET", "POST" });

        Assert.Equal(2, pathItem.Operations.Count);
    }

    [Fact]
    public void OperationNotCreatedIfNoHttpMethods()
    {
        var pathItem = GetOpenApiPathItem(() => { }, "/", Array.Empty<string>());

        Assert.Empty(pathItem.Operations);
    }

    [Fact]
    public void ThrowsIfInvalidHttpMethodIsProvided()
    {
        Assert.Throws<InvalidOperationException>(() => GetOpenApiPathItem(() => { }, "/", new string[] { "FOO" }));
    }

    [Fact]
    public void UsesDeclaringTypeAsOperationTags()
    {
        var pathItem = GetOpenApiPathItem(TestAction);

        var declaringTypeName = typeof(OpenApiOperationGeneratorTests).Name;
        var operation = Assert.Single(pathItem.Operations);
        var tag = Assert.Single(operation.Value.Tags);

        Assert.Equal(declaringTypeName, tag.Name);

    }

    [Fact]
    public void UsesApplicationNameAsOperationTagsIfNoDeclaringType()
    {
        var pathItem = GetOpenApiPathItem(() => { });

        var operation = Assert.Single(pathItem.Operations);

        var declaringTypeName = nameof(OpenApiOperationGeneratorTests);
        var tag = Assert.Single(operation.Value.Tags);

        Assert.Equal(declaringTypeName, tag.Name);
    }

    [Fact]
    public void AddsRequestFormatFromMetadata()
    {
        static void AssertCustomRequestFormat(OpenApiPathItem pathItem)
        {
            var operation = Assert.Single(pathItem.Operations);
            var request = Assert.Single(operation.Value.Parameters);
            var content = Assert.Single(request.Content);
            Assert.Equal("application/custom", content.Key);
        }

        AssertCustomRequestFormat(GetOpenApiPathItem(
            [Consumes("application/custom")]
        (InferredJsonClass fromBody) =>
            { }));

        AssertCustomRequestFormat(GetOpenApiPathItem(
            [Consumes("application/custom")]
        ([FromBody] int fromBody) =>
            { }));
    }

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadata()
    {
        var pathItem = GetOpenApiPathItem(
            [Consumes("application/custom0", "application/custom1")]
        (InferredJsonClass fromBody) =>
            { });

        var operation = Assert.Single(pathItem.Operations);
        var request = Assert.Single(operation.Value.Parameters);

        Assert.Equal(2, request.Content.Count);
        Assert.Equal(new[] { "application/custom0", "application/custom1" } , request.Content.Keys);
    }

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadataWithRequestTypeAndOptionalBodyParameter()
    {
        var pathItem = GetOpenApiPathItem(
            [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = true)]
        () =>
            { });
        var operation = Assert.Single(pathItem.Operations);
        var request = operation.Value.RequestBody;
        Assert.NotNull(request);

        Assert.Equal(2, request.Content.Count);

        Assert.Equal("InferredJsonClass", request.Content.First().Value.Schema.Type);
        Assert.False(request.Required);
    }

    private static OpenApiPathItem GetOpenApiPathItem(
        Delegate action,
        string pattern = null,
        IEnumerable<string> httpMethods = null,
        string displayName = null,
        object[] additionalMetadata  = null)
    {
        var methodInfo = action.Method;
        var attributes = methodInfo.GetCustomAttributes();

        var httpMethodMetadata = new HttpMethodMetadata(httpMethods ?? new[] { "GET" });
        var hostEnvironment = new HostEnvironment() { ApplicationName = nameof(OpenApiOperationGeneratorTests) };
        var metadataItems = new List<object>(attributes) { methodInfo, httpMethodMetadata, additionalMetadata,  hostEnvironment };
        var endpointMetadata = new EndpointMetadataCollection(metadataItems.ToArray());
        // var routePattern = RoutePatternFactory.Parse(pattern ?? "/");


        return OpenApiOperationExtensions.GetOpenApiPathItem(methodInfo, endpointMetadata);
    }

    private static void TestAction()
    {
    }

    private class HostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }

    private class InferredJsonClass
    {
    }
}
