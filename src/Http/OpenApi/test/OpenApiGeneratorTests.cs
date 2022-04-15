// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class OpenApiOperationGeneratorTests
{
    [Fact]
    public void MultipleOperationsCreatedForMultipleHttpMethods()
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

        Assert.Equal("object", request.Content.First().Value.Schema.Type);
        Assert.Equal("object", request.Content.Last().Value.Schema.Type);
        Assert.False(request.Required);
    }

#nullable enable

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadataWithRequiredBodyParameter()
    {
        var pathItem = GetOpenApiPathItem(
            [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = false)]
        (InferredJsonClass fromBody) =>
            { });

        var operation = Assert.Single(pathItem.Operations);
        var request = operation.Value.RequestBody;
        Assert.NotNull(request);

        Assert.Equal("object", request.Content.First().Value.Schema.Type);
        Assert.True(request.Required);
    }

#nullable disable

    [Fact]
    public void AddsJsonResponseFormatWhenFromBodyInferred()
    {
        static void AssertJsonResponse(OpenApiPathItem pathItem, string expectedType)
        {
            var operation = Assert.Single(pathItem.Operations);
            var response = Assert.Single(operation.Value.Responses);
            Assert.Equal("200", response.Key);
            var formats = Assert.Single(response.Value.Content);
            Assert.Equal(expectedType, formats.Value.Schema.Type);

            Assert.Equal("application/json", formats.Key);
        }

        AssertJsonResponse(GetOpenApiPathItem(() => new InferredJsonClass()), "object");
        AssertJsonResponse(GetOpenApiPathItem(() => (IInferredJsonInterface)null), "object");
    }

    [Fact]
    public void AddsTextResponseFormatWhenFromBodyInferred()
    {
        var pathItem = GetOpenApiPathItem(() => "foo");

        var operation = Assert.Single(pathItem.Operations);
        var response = Assert.Single(operation.Value.Responses);
        Assert.Equal("200", response.Key);
        var formats = Assert.Single(response.Value.Content);
        Assert.Equal("string", formats.Value.Schema.Type);
        Assert.Equal("text/plain", formats.Key);
    }

    [Fact]
    public void AddsNoResponseFormatWhenItCannotBeInferredAndTheresNoMetadata()
    {
        static void AssertVoid(OpenApiPathItem pathItem)
        {
            var operation = Assert.Single(pathItem.Operations);
            var response = Assert.Single(operation.Value.Responses);
            Assert.Equal("200", response.Key);
            Assert.Empty(response.Value.Content);
        }

        AssertVoid(GetOpenApiPathItem(() => { }));
        AssertVoid(GetOpenApiPathItem(() => Task.CompletedTask));
        AssertVoid(GetOpenApiPathItem(() => new ValueTask()));
    }

    [Fact]
    public void AddsMultipleResponseFormatsFromMetadataWithPoco()
    {
        var pathItem = GetOpenApiPathItem(
            [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
        () => new InferredJsonClass());

        var operation = Assert.Single(pathItem.Operations);
        var responses = operation.Value.Responses;

        Assert.Equal(2, responses.Count);

        var createdResponseType = responses["201"];
        var content = Assert.Single(createdResponseType.Content);

        Assert.NotNull(createdResponseType);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.Equal("application/json", createdResponseType.Content.Keys.First());

        var badRequestResponseType = responses["400"];

        Assert.NotNull(badRequestResponseType);
        Assert.Equal("object", badRequestResponseType.Content.Values.First().Schema.Type);
        Assert.Equal("application/json", badRequestResponseType.Content.Keys.First());
    }

    [Fact]
    public void AddsMultipleResponseFormatsFromMetadataWithIResult()
    {
        var pathItem = GetOpenApiPathItem(
            [ProducesResponseType(typeof(InferredJsonClass), StatusCodes.Status201Created)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            () => Results.Ok(new InferredJsonClass()));
        var operation = Assert.Single(pathItem.Operations);

        Assert.Equal(2, operation.Value.Responses.Count);

        var createdResponseType = operation.Value.Responses["201"];
        var createdResponseContent = Assert.Single(createdResponseType.Content);

        Assert.NotNull(createdResponseType);
        Assert.Equal("object", createdResponseContent.Value.Schema.Type);
        Assert.Equal("application/json", createdResponseContent.Key);

        var badRequestResponseType = operation.Value.Responses["400"];

        Assert.NotNull(badRequestResponseType);
        Assert.Empty(badRequestResponseType.Content);
    }

    [Fact]
    public void AddsFromRouteParameterAsPath()
    {
        static void AssertPathParameter(OpenApiPathItem pathItem)
        {
            var operation = Assert.Single(pathItem.Operations);
            var param = Assert.Single(operation.Value.Parameters);
            Assert.Equal("number", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }

        AssertPathParameter(GetOpenApiPathItem((int foo) => { }, "/{foo}"));
        AssertPathParameter(GetOpenApiPathItem(([FromRoute] int foo) => { }));
    }

    [Fact]
    public void AddsFromRouteParameterAsPathWithCustomClassWithTryParse()
    {
        static void AssertPathParameter(OpenApiPathItem pathItem)
        {
            var operation = Assert.Single(pathItem.Operations);
            var param = Assert.Single(operation.Value.Parameters);
            Assert.Equal("object", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }
        AssertPathParameter(GetOpenApiPathItem((TryParseStringRecord foo) => { }, pattern: "/{foo}"));
    }

    [Fact]
    public void AddsFromRouteParameterAsPathWithNullablePrimitiveType()
    {
        static void AssertPathParameter(OpenApiPathItem pathItem)
        {
            var operation = Assert.Single(pathItem.Operations);
            var param = Assert.Single(operation.Value.Parameters);
            Assert.Equal("number", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }

        AssertPathParameter(GetOpenApiPathItem((int? foo) => { }, "/{foo}"));
        AssertPathParameter(GetOpenApiPathItem(([FromRoute] int? foo) => { }));
    }

    [Fact]
    public void AddsFromRouteParameterAsPathWithStructTypeWithTryParse()
    {
        static void AssertPathParameter(OpenApiPathItem pathItem)
        {
            var operation = Assert.Single(pathItem.Operations);
            var param = Assert.Single(operation.Value.Parameters);
            Assert.Equal("object", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }
        AssertPathParameter(GetOpenApiPathItem((TryParseStringRecordStruct foo) => { }, pattern: "/{foo}"));
    }

    [Fact]
    public void AddsFromQueryParameterAsQuery()
    {
        static void AssertQueryParameter(OpenApiPathItem pathItem, string type)
        {
            var operation = Assert.Single(pathItem.Operations);
            var param = Assert.Single(operation.Value.Parameters); ;
            Assert.Equal(type, param.Schema.Type);
            Assert.Equal(ParameterLocation.Query, param.In);
        }

        AssertQueryParameter(GetOpenApiPathItem((int foo) => { }, "/"), "number");
        AssertQueryParameter(GetOpenApiPathItem(([FromQuery] int foo) => { }), "number");
        AssertQueryParameter(GetOpenApiPathItem(([FromQuery] TryParseStringRecordStruct foo) => { }), "object");
        AssertQueryParameter(GetOpenApiPathItem((int[] foo) => { }, "/"), "array");
        AssertQueryParameter(GetOpenApiPathItem((string[] foo) => { }, "/"), "array");
        AssertQueryParameter(GetOpenApiPathItem((StringValues foo) => { }, "/"), "object");
        AssertQueryParameter(GetOpenApiPathItem((TryParseStringRecordStruct[] foo) => { }, "/"), "array");
    }

    [Theory]
    [InlineData("Put")]
    [InlineData("Post")]
    public void BodyIsInferredForArraysInsteadOfQuerySomeHttpMethods(string httpMethod)
    {
        static void AssertBody(OpenApiPathItem pathItem, string expectedType)
        {
            var operation = Assert.Single(pathItem.Operations);
            var requestBody = operation.Value.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal(expectedType, content.Value.Schema.Type);
        }

        AssertBody(GetOpenApiPathItem((int[] foo) => { }, "/", httpMethods: new[] { httpMethod }), "array");
        AssertBody(GetOpenApiPathItem((string[] foo) => { }, "/", httpMethods: new[] { httpMethod }), "array");
        AssertBody(GetOpenApiPathItem((TryParseStringRecordStruct[] foo) => { }, "/", httpMethods: new[] { httpMethod }), "array");
    }

    [Fact]
    public void AddsFromHeaderParameterAsHeader()
    {
        var pathItem = GetOpenApiPathItem(([FromHeader] int foo) => { });
        var operation = Assert.Single(pathItem.Operations);
        var param = Assert.Single(operation.Value.Parameters);

        Assert.Equal("number", param.Schema.Type);
        Assert.Equal(ParameterLocation.Header, param.In);
    }

    [Fact]
    public void DoesNotAddFromServiceParameterAsService()
    {
        Assert.Empty(GetOpenApiPathItem((IInferredServiceInterface foo) => { }).Operations.First().Value.Parameters);
        Assert.Empty(GetOpenApiPathItem(([FromServices] int foo) => { }).Operations.First().Value.Parameters);
        Assert.Empty(GetOpenApiPathItem((HttpContext context) => { }).Operations.First().Value.Parameters);
        Assert.Empty(GetOpenApiPathItem((HttpRequest request) => { }).Operations.First().Value.Parameters);
        Assert.Empty(GetOpenApiPathItem((HttpResponse response) => { }).Operations.First().Value.Parameters);
        Assert.Empty(GetOpenApiPathItem((ClaimsPrincipal user) => { }).Operations.First().Value.Parameters);
        Assert.Empty(GetOpenApiPathItem((CancellationToken token) => { }).Operations.First().Value.Parameters);
        Assert.Empty(GetOpenApiPathItem((BindAsyncRecord context) => { }).Operations.First().Value.Parameters);
    }

    [Fact]
    public void AddsBodyParameterInTheParameterDescription()
    {
        static void AssertBodyParameter(OpenApiPathItem pathItem, string expectedName, string expectedType)
        {
            var operation = Assert.Single(pathItem.Operations);
            var requestBody = operation.Value.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal(expectedType, content.Value.Schema.Type);
        }

        AssertBodyParameter(GetOpenApiPathItem((InferredJsonClass foo) => { }), "foo", "object");
        AssertBodyParameter(GetOpenApiPathItem(([FromBody] int bar) => { }), "bar", "number");
    }

#nullable enable

    [Fact]
    public void AddsMultipleParameters()
    {
        var pathItem = GetOpenApiPathItem(([FromRoute] int foo, int bar, InferredJsonClass fromBody) => { });
        var operation = Assert.Single(pathItem.Operations);
        Assert.Equal(3, operation.Value.Parameters.Count);

        var fooParam = operation.Value.Parameters[0];
        Assert.Equal("foo", fooParam.Name);
        Assert.Equal("number", fooParam.Schema.Type);
        Assert.Equal(ParameterLocation.Path, fooParam.In);
        Assert.True(fooParam.Required);

        var barParam = operation.Value.Parameters[1];
        Assert.Equal("bar", barParam.Name);
        Assert.Equal("number", barParam.Schema.Type);
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.True(barParam.Required);

        var fromBodyParam = operation.Value.RequestBody;
        Assert.Equal("object", fromBodyParam.Content.First().Value.Schema.Type);
        Assert.True(fromBodyParam.Required);
    }

#nullable disable

    [Fact]
    public void TestParameterIsRequired()
    {
        var pathItem = GetOpenApiPathItem(([FromRoute] int foo, int? bar) => { });
        var operation = Assert.Single(pathItem.Operations);
        Assert.Equal(2, operation.Value.Parameters.Count);

        var fooParam = operation.Value.Parameters[0];
        Assert.Equal("foo", fooParam.Name);
        Assert.Equal("number", fooParam.Schema.Type);
        Assert.Equal(ParameterLocation.Path, fooParam.In);
        Assert.True(fooParam.Required);

        var barParam = operation.Value.Parameters[1];
        Assert.Equal("bar", barParam.Name);
        Assert.Equal("number", barParam.Schema.Type);
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.False(barParam.Required);
    }

    [Fact]
    public void TestParameterIsRequiredForObliviousNullabilityContext()
    {
        // In an oblivious nullability context, reference type parameters without
        // annotations are optional. Value type parameters are always required.
        var pathItem = GetOpenApiPathItem((string foo, int bar) => { });
        var operation = Assert.Single(pathItem.Operations);
        Assert.Equal(2, operation.Value.Parameters.Count);

        var fooParam = operation.Value.Parameters[0];
        Assert.Equal("string", fooParam.Schema.Type);
        Assert.Equal(ParameterLocation.Query, fooParam.In);
        Assert.False(fooParam.Required);

        var barParam = operation.Value.Parameters[1];
        Assert.Equal("number", barParam.Schema.Type);
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.True(barParam.Required);
    }

    [Fact]
    public void RespectProducesProblemMetadata()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem(() => "",
            additionalMetadata: new[] {
                new ProducesResponseTypeMetadata(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/json+problem") });

        // Assert
        var operation = Assert.Single(pathItem.Operations);
        var responses = Assert.Single(operation.Value.Responses);
        var content = Assert.Single(responses.Value.Content);
        Assert.Equal("object", content.Value.Schema.Type);
    }

    [Fact]
    public void RespectsProducesWithGroupNameExtensionMethod()
    {
        // Arrange
        var endpointGroupName = "SomeEndpointGroupName";
        var pathItem = GetOpenApiPathItem(() => "",
            additionalMetadata: new object[]
            {
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                new EndpointNameMetadata(endpointGroupName)
            });

        var operation = Assert.Single(pathItem.Operations);
        var responses = Assert.Single(operation.Value.Responses);
        var content = Assert.Single(responses.Value.Content);
        Assert.Equal("object", content.Value.Schema.Type);
    }

    [Fact]
    public void RespectsExcludeFromDescription()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem(() => "",
            additionalMetadata: new object[]
            {
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                new ExcludeFromDescriptionAttribute()
            });

        Assert.Empty(pathItem.Operations);
    }

    [Fact]
    public void HandlesProducesWithProducesProblem()
    {
            // Arrange
            var pathItem = GetOpenApiPathItem(() => "",
                additionalMetadata: new[]
                {
                    new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                    new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json"),
                    new ProducesResponseTypeMetadata(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json"),
                    new ProducesResponseTypeMetadata(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")
                });
            var operation = Assert.Single(pathItem.Operations);
            var responses = operation.Value.Responses;

        // Assert
        Assert.Collection(
            responses.OrderBy(response => response.Key),
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("object", content.Value.Schema.Type);
                Assert.Equal("200", responseType.Key);
                Assert.Equal("application/json", content.Key);
            },
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("object", content.Value.Schema.Type);
                Assert.Equal("400", responseType.Key);
                Assert.Equal("application/problem+json", content.Key);
            },
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("object", content.Value.Schema.Type);
                Assert.Equal("404", responseType.Key);
                Assert.Equal("application/problem+json", content.Key);
            },
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("object", content.Value.Schema.Type);
                Assert.Equal("409", responseType.Key);
                Assert.Equal("application/problem+json", content.Key);
            });
    }

    [Fact]
    public void HandleMultipleProduces()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem(() => "",
            additionalMetadata: new[]
            {
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status201Created, "application/json")
            });

        var operation = Assert.Single(pathItem.Operations);
        var responses = operation.Value.Responses;

        // Assert
        Assert.Collection(
        responses.OrderBy(response => response.Key),
        responseType =>
        {
            var content = Assert.Single(responseType.Value.Content);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.Equal("200", responseType.Key);
            Assert.Equal("application/json", content.Key);
        },
        responseType =>
        {
            var content = Assert.Single(responseType.Value.Content);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.Equal("201", responseType.Key);
            Assert.Equal("application/json", content.Key);
        });
    }

    [Fact]
    public void HandleAcceptsMetadata()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem(() => "",
                additionalMetadata: new[]
                {
                new AcceptsMetadata(typeof(string), true, new string[] { "application/json", "application/xml"})
                });

        var operation = Assert.Single(pathItem.Operations);
        var requestBody = operation.Value.RequestBody;

        // Assert
        Assert.Collection(
        requestBody.Content,
        parameter =>
        {
            Assert.Equal("application/json", parameter.Key);
        },
        parameter =>
        {
            Assert.Equal("application/xml", parameter.Key);
        });
    }

    [Fact]
    public void HandleAcceptsMetadataWithTypeParameter()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem((InferredJsonClass inferredJsonClass) => "",
                additionalMetadata: new[]
                {
                    new AcceptsMetadata(typeof(InferredJsonClass), true, new string[] { "application/json"})
                });

        // Assert
        var operation = Assert.Single(pathItem.Operations);
        var requestBody = operation.Value.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

#nullable enable

    [Fact]
    public void HandleDefaultIAcceptsMetadataForRequiredBodyParameter()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem((InferredJsonClass inferredJsonClass) => "");
        var operation = Assert.Single(pathItem.Operations);

        // Assert
        var requestBody = operation.Value.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("application/json", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.True(requestBody.Required);
    }

    [Fact]
    public void HandleDefaultIAcceptsMetadataForOptionalBodyParameter()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem((InferredJsonClass? inferredJsonClass) => "");
        var operation = Assert.Single(pathItem.Operations);

        // Assert
        var requestBody = operation.Value.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("application/json", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

    [Fact]
    public void HandleIAcceptsMetadataWithConsumesAttributeAndInferredOptionalFromBodyType()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem([Consumes("application/xml")] (InferredJsonClass? inferredJsonClass) => "");
        var operation = Assert.Single(pathItem.Operations);

        // Assert
        var requestBody = operation.Value.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("application/xml", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

    [Fact]
    public void HandleDefaultIAcceptsMetadataForRequiredFormFileParameter()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem((IFormFile inferredFormFile) => "");
        var operation = Assert.Single(pathItem.Operations);

        // Assert
        var requestBody = operation.Value.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("multipart/form-data", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.True(requestBody.Required);
    }

    [Fact]
    public void HandleDefaultIAcceptsMetadataForOptionalFormFileParameter()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem((IFormFile? inferredFormFile) => "");
        var operation = Assert.Single(pathItem.Operations);

        // Assert
        var requestBody = operation.Value.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("multipart/form-data", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

    [Fact]
    public void AddsMultipartFormDataRequestFormatWhenFormFileSpecified()
    {
        // Arrange
        var pathItem = GetOpenApiPathItem((IFormFile file) => Results.NoContent());
        var operation = Assert.Single(pathItem.Operations);

        // Assert
        var requestBody = operation.Value.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("multipart/form-data", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.True(requestBody.Required);
    }

    [Fact]
    public void HasMultipleRequestFormatsWhenFormFileSpecifiedWithConsumesAttribute()
    {
        var pathItem = GetOpenApiPathItem(
            [Consumes("application/custom0", "application/custom1")] (IFormFile file) => Results.NoContent());
        var operation = Assert.Single(pathItem.Operations);

        var requestBody = operation.Value.RequestBody;
        var content = requestBody.Content;

        Assert.Equal(2, content.Count);

        var requestFormat0 = content["application/custom0"];
        Assert.NotNull(requestFormat0);

        var requestFormat1 = content["application/custom1"];
        Assert.NotNull(requestFormat1);
    }

    [Fact]
    public void TestIsRequiredFromFormFile()
    {
        var operation0 = Assert.Single(GetOpenApiPathItem((IFormFile fromFile) => { }).Operations);
        var operation1 = Assert.Single(GetOpenApiPathItem((IFormFile? fromFile) => { }).Operations);
        Assert.NotNull(operation0.Value.RequestBody);
        Assert.NotNull(operation1.Value.RequestBody);

        var fromFileParam0 = operation0.Value.RequestBody;
        Assert.Equal("object", fromFileParam0.Content.Values.Single().Schema.Type);
        Assert.True(fromFileParam0.Required);

        var fromFileParam1 = operation1.Value.RequestBody;
        Assert.Equal("object", fromFileParam1.Content.Values.Single().Schema.Type);
        Assert.False(fromFileParam1.Required);
    }

    [Fact]
    public void AddsFromFormParameterAsFormFile()
    {
        static void AssertFormFileParameter(OpenApiPathItem pathItem, string expectedType, string expectedName)
        {
            var operation = Assert.Single(pathItem.Operations);
            var requestBody = operation.Value.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal(expectedType, content.Value.Schema.Type);
            Assert.Equal("multipart/form-data", content.Key);
        }

        AssertFormFileParameter(GetOpenApiPathItem((IFormFile file) => { }), "object", "file");
        AssertFormFileParameter(GetOpenApiPathItem(([FromForm(Name = "file_name")] IFormFile file) => { }), "object", "file_name");
    }

    [Fact]
    public void AddsMultipartFormDataResponseFormatWhenFormFileCollectionSpecified()
    {
        AssertFormFileCollection((IFormFileCollection files) => Results.NoContent(), "files");
        AssertFormFileCollection(([FromForm] IFormFileCollection uploads) => Results.NoContent(), "uploads");

        static void AssertFormFileCollection(Delegate handler, string expectedName)
        {
            // Arrange
            var pathItem = GetOpenApiPathItem(handler);
            var operation = Assert.Single(pathItem.Operations);

            // Assert
            var requestBody = operation.Value.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Equal("object", content.Value.Schema.Type);
            Assert.True(requestBody.Required);
        }
    }

#nullable restore

    [Fact]
    public void HandlesEndpointWithDescriptionAndSummary_WithAttributes()
    {
        var pathItem = GetOpenApiPathItem(
            [EndpointSummary("A summary")][EndpointDescription("A description")] (int id) => "");

        var operation = Assert.Single(pathItem.Operations);

        // Assert
        Assert.Equal("A description", operation.Value.Description);
        Assert.Equal("A summary", operation.Value.Summary);
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
        var metadataItems = new List<object>(attributes) { methodInfo, httpMethodMetadata };
        metadataItems.AddRange(additionalMetadata ?? Array.Empty<object>());
        var endpointMetadata = new EndpointMetadataCollection(metadataItems.ToArray());
        var routePattern = RoutePatternFactory.Parse(pattern ?? "/");

        var generator = new OpenApiGenerator(
            hostEnvironment,
            new ServiceProviderIsService());

        return generator.GetOpenApiPathItem(methodInfo, endpointMetadata, routePattern);
    }

    private static void TestAction()
    {
    }

    private class ServiceProviderIsService : IServiceProviderIsService
    {
        public bool IsService(Type serviceType) => serviceType == typeof(IInferredServiceInterface);
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

    private interface IInferredJsonInterface
    {
    }

    private record TryParseStringRecord(int Value)
    {
        public static bool TryParse(string value, out TryParseStringRecord result) =>
            throw new NotImplementedException();
    }

    private record struct TryParseStringRecordStruct(int Value)
    {
        public static bool TryParse(string value, out TryParseStringRecordStruct result) =>
            throw new NotImplementedException();
    }

    private interface IInferredServiceInterface
    {
    }

    private record BindAsyncRecord(int Value)
    {
        public static ValueTask<BindAsyncRecord> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();
        public static bool TryParse(string value, out BindAsyncRecord result) =>
            throw new NotImplementedException();
    }
}
