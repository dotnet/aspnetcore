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
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class OpenApiOperationGeneratorTests
{
    [Fact]
    public void OperationNotCreatedIfNoHttpMethods()
    {
        var operation = GetOpenApiOperation(() => { }, "/", Array.Empty<string>());

        Assert.Null(operation);
    }

    [Fact]
    public void UsesDeclaringTypeAsOperationTags()
    {
        var operation = GetOpenApiOperation(TestAction);

        var declaringTypeName = typeof(OpenApiOperationGeneratorTests).Name;
        var tag = Assert.Single(operation.Tags);

        Assert.Equal(declaringTypeName, tag.Name);

    }

    [Fact]
    public void UsesApplicationNameAsOperationTagsIfNoDeclaringType()
    {
        var operation = GetOpenApiOperation(() => { });

        var declaringTypeName = nameof(OpenApiOperationGeneratorTests);
        var tag = Assert.Single(operation.Tags);

        Assert.Equal(declaringTypeName, tag.Name);
    }

    [Fact]
    public void AddsRequestFormatFromMetadata()
    {
        static void AssertCustomRequestFormat(OpenApiOperation operation)
        {
            var request = Assert.Single(operation.Parameters);
            var content = Assert.Single(request.Content);
            Assert.Equal("application/custom", content.Key);
        }

        AssertCustomRequestFormat(GetOpenApiOperation(
            [Consumes("application/custom")]
        (InferredJsonClass fromBody) =>
            { }));

        AssertCustomRequestFormat(GetOpenApiOperation(
            [Consumes("application/custom")]
        ([FromBody] int fromBody) =>
            { }));
    }

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadata()
    {
        var operation = GetOpenApiOperation(
            [Consumes("application/custom0", "application/custom1")]
        (InferredJsonClass fromBody) =>
            { });

        var request = Assert.Single(operation.Parameters);

        Assert.Equal(2, request.Content.Count);
        Assert.Equal(new[] { "application/custom0", "application/custom1" }, request.Content.Keys);
    }

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadataWithRequestTypeAndOptionalBodyParameter()
    {
        var operation = GetOpenApiOperation(
            [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = true)]
        () =>
            { }); ;
        var request = operation.RequestBody;
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
        var operation = GetOpenApiOperation(
            [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = false)]
        (InferredJsonClass fromBody) =>
            { });

        var request = operation.RequestBody;
        Assert.NotNull(request);

        Assert.Equal("object", request.Content.First().Value.Schema.Type);
        Assert.True(request.Required);
    }

#nullable disable

    [Fact]
    public void AddsJsonResponseFormatWhenFromBodyInferred()
    {
        static void AssertJsonResponse(OpenApiOperation operation, string expectedType)
        {
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            var formats = Assert.Single(response.Value.Content);
            Assert.Equal(expectedType, formats.Value.Schema.Type);

            Assert.Equal("application/json", formats.Key);
        }

        AssertJsonResponse(GetOpenApiOperation(() => new InferredJsonClass()), "object");
        AssertJsonResponse(GetOpenApiOperation(() => (IInferredJsonInterface)null), "object");
    }

    [Fact]
    public void AddsTextResponseFormatWhenFromBodyInferred()
    {
        var operation = GetOpenApiOperation(() => "foo");

        var response = Assert.Single(operation.Responses);
        Assert.Equal("200", response.Key);
        var formats = Assert.Single(response.Value.Content);
        Assert.Equal("string", formats.Value.Schema.Type);
        Assert.Equal("text/plain", formats.Key);
    }

    [Fact]
    public void AddsNoResponseFormatWhenItCannotBeInferredAndTheresNoMetadata()
    {
        static void AssertVoid(OpenApiOperation operation)
        {
            ;
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Empty(response.Value.Content);
        }

        AssertVoid(GetOpenApiOperation(() => { }));
        AssertVoid(GetOpenApiOperation(() => Task.CompletedTask));
        AssertVoid(GetOpenApiOperation(() => new ValueTask()));
    }

    [Fact]
    public void AddsMultipleResponseFormatsFromMetadataWithPoco()
    {
        var operation = GetOpenApiOperation(
            [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        () => new InferredJsonClass());

        var responses = operation.Responses;

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
        var operation = GetOpenApiOperation(
            [ProducesResponseType(typeof(InferredJsonClass), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        () => Results.Ok(new InferredJsonClass()));

        Assert.Equal(2, operation.Responses.Count);

        var createdResponseType = operation.Responses["201"];
        var createdResponseContent = Assert.Single(createdResponseType.Content);

        Assert.NotNull(createdResponseType);
        Assert.Equal("object", createdResponseContent.Value.Schema.Type);
        Assert.Equal("application/json", createdResponseContent.Key);

        var badRequestResponseType = operation.Responses["400"];

        Assert.NotNull(badRequestResponseType);
        Assert.Empty(badRequestResponseType.Content);
    }

    [Fact]
    public void AddsFromRouteParameterAsPath()
    {
        static void AssertPathParameter(OpenApiOperation operation)
        {
            var param = Assert.Single(operation.Parameters);
            Assert.Equal("number", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }

        AssertPathParameter(GetOpenApiOperation((int foo) => { }, "/{foo}"));
        AssertPathParameter(GetOpenApiOperation(([FromRoute] int foo) => { }));
    }

    [Fact]
    public void AddsFromRouteParameterAsPathWithCustomClassWithTryParse()
    {
        static void AssertPathParameter(OpenApiOperation operation)
        {
            var param = Assert.Single(operation.Parameters);
            Assert.Equal("object", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }
        AssertPathParameter(GetOpenApiOperation((TryParseStringRecord foo) => { }, pattern: "/{foo}"));
    }

    [Fact]
    public void AddsFromRouteParameterAsPathWithNullablePrimitiveType()
    {
        static void AssertPathParameter(OpenApiOperation operation)
        {
            var param = Assert.Single(operation.Parameters);
            Assert.Equal("number", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }

        AssertPathParameter(GetOpenApiOperation((int? foo) => { }, "/{foo}"));
        AssertPathParameter(GetOpenApiOperation(([FromRoute] int? foo) => { }));
    }

    [Fact]
    public void AddsFromRouteParameterAsPathWithStructTypeWithTryParse()
    {
        static void AssertPathParameter(OpenApiOperation operation)
        {
            var param = Assert.Single(operation.Parameters);
            Assert.Equal("object", param.Schema.Type);
            Assert.Equal(ParameterLocation.Path, param.In);
        }
        AssertPathParameter(GetOpenApiOperation((TryParseStringRecordStruct foo) => { }, pattern: "/{foo}"));
    }

    [Fact]
    public void AddsFromQueryParameterAsQuery()
    {
        static void AssertQueryParameter(OpenApiOperation operation, string type)
        {
            var param = Assert.Single(operation.Parameters); ;
            Assert.Equal(type, param.Schema.Type);
            Assert.Equal(ParameterLocation.Query, param.In);
        }

        AssertQueryParameter(GetOpenApiOperation((int foo) => { }, "/"), "number");
        AssertQueryParameter(GetOpenApiOperation(([FromQuery] int foo) => { }), "number");
        AssertQueryParameter(GetOpenApiOperation(([FromQuery] TryParseStringRecordStruct foo) => { }), "object");
        AssertQueryParameter(GetOpenApiOperation((int[] foo) => { }, "/"), "array");
        AssertQueryParameter(GetOpenApiOperation((string[] foo) => { }, "/"), "array");
        AssertQueryParameter(GetOpenApiOperation((StringValues foo) => { }, "/"), "object");
        AssertQueryParameter(GetOpenApiOperation((TryParseStringRecordStruct[] foo) => { }, "/"), "array");
    }

    [Theory]
    [InlineData("Put")]
    [InlineData("Post")]
    public void BodyIsInferredForArraysInsteadOfQuerySomeHttpMethods(string httpMethod)
    {
        static void AssertBody(OpenApiOperation operation, string expectedType)
        {
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal(expectedType, content.Value.Schema.Type);
        }

        AssertBody(GetOpenApiOperation((int[] foo) => { }, "/", httpMethods: new[] { httpMethod }), "array");
        AssertBody(GetOpenApiOperation((string[] foo) => { }, "/", httpMethods: new[] { httpMethod }), "array");
        AssertBody(GetOpenApiOperation((TryParseStringRecordStruct[] foo) => { }, "/", httpMethods: new[] { httpMethod }), "array");
    }

    [Fact]
    public void AddsFromHeaderParameterAsHeader()
    {
        var operation = GetOpenApiOperation(([FromHeader] int foo) => { });
        var param = Assert.Single(operation.Parameters);

        Assert.Equal("number", param.Schema.Type);
        Assert.Equal(ParameterLocation.Header, param.In);
    }

    [Fact]
    public void DoesNotAddFromServiceParameterAsService()
    {
        Assert.Empty(GetOpenApiOperation((IInferredServiceInterface foo) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation(([FromServices] int foo) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation((HttpContext context) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation((HttpRequest request) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation((HttpResponse response) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation((ClaimsPrincipal user) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation((CancellationToken token) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation((BindAsyncRecord context) => { }).Parameters);
    }

    [Fact]
    public void AddsBodyParameterInTheParameterDescription()
    {
        static void AssertBodyParameter(OpenApiOperation operation, string expectedName, string expectedType)
        {
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal(expectedType, content.Value.Schema.Type);
        }

        AssertBodyParameter(GetOpenApiOperation((InferredJsonClass foo) => { }), "foo", "object");
        AssertBodyParameter(GetOpenApiOperation(([FromBody] int bar) => { }), "bar", "number");
    }

#nullable enable

    [Fact]
    public void AddsMultipleParameters()
    {
        var operation = GetOpenApiOperation(([FromRoute] int foo, int bar, InferredJsonClass fromBody) => { });
        Assert.Equal(3, operation.Parameters.Count);

        var fooParam = operation.Parameters[0];
        Assert.Equal("foo", fooParam.Name);
        Assert.Equal("number", fooParam.Schema.Type);
        Assert.Equal(ParameterLocation.Path, fooParam.In);
        Assert.True(fooParam.Required);

        var barParam = operation.Parameters[1];
        Assert.Equal("bar", barParam.Name);
        Assert.Equal("number", barParam.Schema.Type);
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.True(barParam.Required);

        var fromBodyParam = operation.RequestBody;
        Assert.Equal("object", fromBodyParam.Content.First().Value.Schema.Type);
        Assert.True(fromBodyParam.Required);
    }

#nullable disable

    [Fact]
    public void TestParameterIsRequired()
    {
        var operation = GetOpenApiOperation(([FromRoute] int foo, int? bar) => { });
        Assert.Equal(2, operation.Parameters.Count);

        var fooParam = operation.Parameters[0];
        Assert.Equal("foo", fooParam.Name);
        Assert.Equal("number", fooParam.Schema.Type);
        Assert.Equal(ParameterLocation.Path, fooParam.In);
        Assert.True(fooParam.Required);

        var barParam = operation.Parameters[1];
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
        var operation = GetOpenApiOperation((string foo, int bar) => { });
        Assert.Equal(2, operation.Parameters.Count);

        var fooParam = operation.Parameters[0];
        Assert.Equal("string", fooParam.Schema.Type);
        Assert.Equal(ParameterLocation.Query, fooParam.In);
        Assert.False(fooParam.Required);

        var barParam = operation.Parameters[1];
        Assert.Equal("number", barParam.Schema.Type);
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.True(barParam.Required);
    }

    [Fact]
    public void RespectProducesProblemMetadata()
    {
        // Arrange
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new[] {
                new ProducesResponseTypeMetadata(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/json+problem") });

        // Assert
        var responses = Assert.Single(operation.Responses);
        var content = Assert.Single(responses.Value.Content);
        Assert.Equal("object", content.Value.Schema.Type);
    }

    [Fact]
    public void RespectsProducesWithGroupNameExtensionMethod()
    {
        // Arrange
        var endpointGroupName = "SomeEndpointGroupName";
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new object[]
            {
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                new EndpointNameMetadata(endpointGroupName)
            });

        var responses = Assert.Single(operation.Responses);
        var content = Assert.Single(responses.Value.Content);
        Assert.Equal("object", content.Value.Schema.Type);
    }

    [Fact]
    public void RespectsExcludeFromDescription()
    {
        // Arrange
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new object[]
            {
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                new ExcludeFromDescriptionAttribute()
            });

        Assert.Null(operation);
    }

    [Fact]
    public void HandlesProducesWithProducesProblem()
    {
        // Arrange
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new[]
            {
                    new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                    new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json"),
                    new ProducesResponseTypeMetadata(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json"),
                    new ProducesResponseTypeMetadata(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")
            });
        var responses = operation.Responses;

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
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new[]
            {
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status200OK, "application/json"),
                new ProducesResponseTypeMetadata(typeof(InferredJsonClass), StatusCodes.Status201Created, "application/json")
            });

        var responses = operation.Responses;

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
        var operation = GetOpenApiOperation(() => "",
                additionalMetadata: new[]
                {
                new AcceptsMetadata(typeof(string), true, new string[] { "application/json", "application/xml"})
                });

        var requestBody = operation.RequestBody;

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
        var operation = GetOpenApiOperation((InferredJsonClass inferredJsonClass) => "",
                additionalMetadata: new[]
                {
                    new AcceptsMetadata(typeof(InferredJsonClass), true, new string[] { "application/json"})
                });

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

#nullable enable

    [Fact]
    public void HandleDefaultIAcceptsMetadataForRequiredBodyParameter()
    {
        // Arrange
        var operation = GetOpenApiOperation((InferredJsonClass inferredJsonClass) => "");

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("application/json", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.True(requestBody.Required);
    }

    [Fact]
    public void HandleDefaultIAcceptsMetadataForOptionalBodyParameter()
    {
        // Arrange
        var operation = GetOpenApiOperation((InferredJsonClass? inferredJsonClass) => "");

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("application/json", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

    [Fact]
    public void HandleIAcceptsMetadataWithConsumesAttributeAndInferredOptionalFromBodyType()
    {
        // Arrange
        var operation = GetOpenApiOperation([Consumes("application/xml")] (InferredJsonClass? inferredJsonClass) => "");

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("application/xml", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

    [Fact]
    public void HandleDefaultIAcceptsMetadataForRequiredFormFileParameter()
    {
        // Arrange
        var operation = GetOpenApiOperation((IFormFile inferredFormFile) => "");

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("multipart/form-data", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.True(requestBody.Required);
    }

    [Fact]
    public void HandleDefaultIAcceptsMetadataForOptionalFormFileParameter()
    {
        // Arrange
        var operation = GetOpenApiOperation((IFormFile? inferredFormFile) => "");

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("multipart/form-data", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.False(requestBody.Required);
    }

    [Fact]
    public void AddsMultipartFormDataRequestFormatWhenFormFileSpecified()
    {
        // Arrange
        var operation = GetOpenApiOperation((IFormFile file) => Results.NoContent());

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.Equal("multipart/form-data", content.Key);
        Assert.Equal("object", content.Value.Schema.Type);
        Assert.True(requestBody.Required);
    }

    [Fact]
    public void HasMultipleRequestFormatsWhenFormFileSpecifiedWithConsumesAttribute()
    {
        var operation = GetOpenApiOperation(
            [Consumes("application/custom0", "application/custom1")] (IFormFile file) => Results.NoContent());

        var requestBody = operation.RequestBody;
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
        var operation0 = GetOpenApiOperation((IFormFile fromFile) => { });
        var operation1 = GetOpenApiOperation((IFormFile? fromFile) => { });
        Assert.NotNull(operation0.RequestBody);
        Assert.NotNull(operation1.RequestBody);

        var fromFileParam0 = operation0.RequestBody;
        Assert.Equal("object", fromFileParam0.Content.Values.Single().Schema.Type);
        Assert.True(fromFileParam0.Required);

        var fromFileParam1 = operation1.RequestBody;
        Assert.Equal("object", fromFileParam1.Content.Values.Single().Schema.Type);
        Assert.False(fromFileParam1.Required);
    }

    [Fact]
    public void AddsFromFormParameterAsFormFile()
    {
        static void AssertFormFileParameter(OpenApiOperation operation, string expectedType, string expectedName)
        {
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal(expectedType, content.Value.Schema.Type);
            Assert.Equal("multipart/form-data", content.Key);
        }

        AssertFormFileParameter(GetOpenApiOperation((IFormFile file) => { }), "object", "file");
        AssertFormFileParameter(GetOpenApiOperation(([FromForm(Name = "file_name")] IFormFile file) => { }), "object", "file_name");
    }

    [Fact]
    public void AddsMultipartFormDataResponseFormatWhenFormFileCollectionSpecified()
    {
        AssertFormFileCollection((IFormFileCollection files) => Results.NoContent(), "files");
        AssertFormFileCollection(([FromForm] IFormFileCollection uploads) => Results.NoContent(), "uploads");

        static void AssertFormFileCollection(Delegate handler, string expectedName)
        {
            // Arrange
            var operation = GetOpenApiOperation(handler);

            // Assert
            var requestBody = operation.RequestBody;
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
        var operation = GetOpenApiOperation(
            [EndpointSummary("A summary")][EndpointDescription("A description")] (int id) => "");

        // Assert
        Assert.Equal("A description", operation.Description);
        Assert.Equal("A summary", operation.Summary);
    }

    private static OpenApiOperation GetOpenApiOperation(
        Delegate action,
        string pattern = null,
        IEnumerable<string> httpMethods = null,
        string displayName = null,
        object[] additionalMetadata = null)
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

        return generator.GetOpenApiOperation(methodInfo, endpointMetadata, routePattern);
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
