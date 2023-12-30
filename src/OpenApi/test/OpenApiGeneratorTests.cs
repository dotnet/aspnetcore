// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

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
    public void UsesTagsFromMultipleCallsToWithTags()
    {
        var testBuilder = new TestEndpointConventionBuilder();
        var routeHandlerBuilder = new RouteHandlerBuilder(new[] { testBuilder });

        routeHandlerBuilder
            .WithTags("A")
            .WithTags("B");

        var operation = GetOpenApiOperation(() => { }, additionalMetadata: testBuilder.Metadata.ToArray());

        Assert.Collection(operation.Tags,
            tag => Assert.Equal("A", tag.Name),
            tag => Assert.Equal("B", tag.Name));
    }

    [Fact]
    public void ThrowsInvalidOperationExceptionGivenUnnamedParameter()
    {
        var unnamedParameter = Expression.Parameter(typeof(int));
        var lambda = Expression.Lambda(Expression.Block(), unnamedParameter);
        var ex = Assert.Throws<InvalidOperationException>(() => GetOpenApiOperation(lambda.Compile()));
        Assert.Equal("Encountered a parameter of type 'System.Runtime.CompilerServices.Closure' without a name. Parameters must have a name.", ex.Message);
    }

    [Fact]
    public void AddsRequestFormatFromMetadata()
    {
        static void AssertCustomRequestFormat(OpenApiOperation operation)
        {
            Assert.Empty(operation.Parameters);
            var content = operation.RequestBody.Content.Keys.FirstOrDefault();
            Assert.Equal("application/custom", content);
        }

        AssertCustomRequestFormat(GetOpenApiOperation(
            [Consumes("application/custom")] (InferredJsonClass fromBody) => { }));

        AssertCustomRequestFormat(GetOpenApiOperation(
            [Consumes("application/custom")] ([FromBody] int fromBody) => { }));
    }

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadata()
    {
        var operation = GetOpenApiOperation(
            [Consumes("application/custom0", "application/custom1")] (InferredJsonClass fromBody) => { });

        Assert.Empty(operation.Parameters);

        var content = operation.RequestBody.Content;
        Assert.Equal(2, content.Count);
        Assert.Equal(new[] { "application/custom0", "application/custom1" }, content.Keys);
    }

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadataWithRequestTypeAndOptionalBodyParameter()
    {
        var operation = GetOpenApiOperation(
            [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = true)] () => { });
        var request = operation.RequestBody;
        Assert.NotNull(request);
        Assert.Equal(2, request.Content.Count);
        Assert.Empty(operation.Parameters);

        Assert.Equal("application/custom0", request.Content.First().Key);
        Assert.Equal("application/custom1", request.Content.Last().Key);
        Assert.False(request.Required);
    }

#nullable enable

    [Fact]
    public void AddsMultipleRequestFormatsFromMetadataWithRequiredBodyParameter()
    {
        var operation = GetOpenApiOperation(
            [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = false)] (InferredJsonClass fromBody) => { });

        var request = operation.RequestBody;
        Assert.NotNull(request);

        Assert.Equal("application/custom0", request.Content.First().Key);
        Assert.Equal("application/custom1", request.Content.Last().Key);
        Assert.True(request.Required);
        Assert.Empty(operation.Parameters);
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

            Assert.Equal("application/json", formats.Key);
        }

        AssertJsonResponse(GetOpenApiOperation(() => new InferredJsonClass()), "object");
        AssertJsonResponse(GetOpenApiOperation(() => (IInferredJsonInterface)null), "object");
        AssertJsonResponse(GetOpenApiOperation(() => Task.FromResult(new InferredJsonClass())), "object");
        AssertJsonResponse(GetOpenApiOperation(() => Task.FromResult((IInferredJsonInterface)null)), "object");
        AssertJsonResponse(GetOpenApiOperation(() => ValueTask.FromResult(new InferredJsonClass())), "object");
        AssertJsonResponse(GetOpenApiOperation(() => ValueTask.FromResult((IInferredJsonInterface)null)), "object");
        AssertJsonResponse(GetOpenApiOperation(() => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new InferredJsonClass())), "object");
        AssertJsonResponse(GetOpenApiOperation(() => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return((IInferredJsonInterface)null)), "object");
    }

    [Fact]
    public void AddsTextResponseFormatWhenFromBodyInferred()
    {
        var operation = GetOpenApiOperation(() => "foo");

        var response = Assert.Single(operation.Responses);
        Assert.Equal("200", response.Key);
        var formats = Assert.Single(response.Value.Content);
        Assert.Equal("text/plain", formats.Key);
    }

    [Fact]
    public void AddsNoResponseFormatWhenItCannotBeInferredAndTheresNoMetadata()
    {
        static void AssertVoid(OpenApiOperation operation)
        {
            var response = Assert.Single(operation.Responses);
            Assert.Equal("200", response.Key);
            Assert.Empty(response.Value.Content);
        }

        AssertVoid(GetOpenApiOperation(() => { }));
        AssertVoid(GetOpenApiOperation(() => Task.CompletedTask));
        AssertVoid(GetOpenApiOperation(() => Task.FromResult(default(FSharp.Core.Unit))));
        AssertVoid(GetOpenApiOperation(() => new ValueTask()));
        AssertVoid(GetOpenApiOperation(() => ValueTask.FromResult(default(FSharp.Core.Unit))));
        AssertVoid(GetOpenApiOperation(() => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(default(FSharp.Core.Unit))));
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
        Assert.Equal("application/json", content.Key);

        var badRequestResponseType = responses["400"];

        Assert.NotNull(badRequestResponseType);
        var badRequestContent = Assert.Single(badRequestResponseType.Content);
        Assert.Equal("application/json", badRequestContent.Key);
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
        Assert.Equal("application/json", createdResponseContent.Key);

        var badRequestResponseType = operation.Responses["400"];

        Assert.NotNull(badRequestResponseType);
        Assert.Empty(badRequestResponseType.Content);
    }

    [Fact]
    public void DefaultResponseDescriptionIsCorrect()
    {
        var operation = GetOpenApiOperation(
        [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        () => new InferredJsonClass());

        Assert.Equal(2, operation.Responses.Count);

        var successResponse = operation.Responses["201"];
        Assert.Equal("Created", successResponse.Description);

        var clientErrorResponse = operation.Responses["400"];
        Assert.Equal("Bad Request", clientErrorResponse.Description);
    }

    [Fact]
    public void DefaultResponseDescriptionIsCorrectForTwoSimilarResponses()
    {
        var operation = GetOpenApiOperation(
        [ProducesResponseType(StatusCodes.Status100Continue)]
        [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
        () => new InferredJsonClass());

        Assert.Equal(2, operation.Responses.Count);

        var continueResponse = operation.Responses["100"];
        Assert.Equal("Continue", continueResponse.Description);

        var switchingProtocolsResponse = operation.Responses["101"];
        Assert.Equal("Switching Protocols", switchingProtocolsResponse.Description);
    }

    [Fact]
    public void AllDefaultResponseDescriptions()
    {
        var operation = GetOpenApiOperation(
        [ProducesResponseType(StatusCodes.Status100Continue)]
        [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status300MultipleChoices)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        () => new InferredJsonClass());

        Assert.Equal(5, operation.Responses.Count);

        var continueResponse = operation.Responses["100"];
        Assert.Equal("Continue", continueResponse.Description);

        var createdResponse = operation.Responses["201"];
        Assert.Equal("Created", createdResponse.Description);

        var multipleChoicesResponse = operation.Responses["300"];
        Assert.Equal("Multiple Choices", multipleChoicesResponse.Description);

        var badRequestResponse = operation.Responses["400"];
        Assert.Equal("Bad Request", badRequestResponse.Description);

        var InternalServerErrorResponse = operation.Responses["500"];
        Assert.Equal("Internal Server Error", InternalServerErrorResponse.Description);
    }

    [Fact]
    public void UnregisteredStatusCodeDescriptions()
    {
        var operation = GetOpenApiOperation(
        [ProducesResponseType(46)]
        [ProducesResponseType(654)]
        [ProducesResponseType(1111)]
        () => new InferredJsonClass());

        Assert.Equal(3, operation.Responses.Count);

        var unregisteredResponse1 = operation.Responses["46"];
        Assert.Equal("", unregisteredResponse1.Description);

        var unregisteredResponse2 = operation.Responses["654"];
        Assert.Equal("", unregisteredResponse2.Description);

        var unregisteredResponse3 = operation.Responses["1111"];
        Assert.Equal("", unregisteredResponse3.Description);
    }

    [Fact]
    public void AddsFromRouteParameterAsPath()
    {
        static void AssertPathParameter(OpenApiOperation operation)
        {
            var param = Assert.Single(operation.Parameters);
            Assert.Equal(ParameterLocation.Path, param.In);
            Assert.Empty(param.Content);
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
            Assert.Equal(ParameterLocation.Path, param.In);
            Assert.Empty(param.Content);
        }
        AssertPathParameter(GetOpenApiOperation((TryParseStringRecord foo) => { }, pattern: "/{foo}"));
    }

    [Fact]
    public void AddsFromRouteParameterAsPathWithNullablePrimitiveType()
    {
        static void AssertPathParameter(OpenApiOperation operation)
        {
            var param = Assert.Single(operation.Parameters);
            Assert.Equal(ParameterLocation.Path, param.In);
            Assert.Empty(param.Content);
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
            Assert.Equal(ParameterLocation.Path, param.In);
            Assert.Empty(param.Content);
        }
        AssertPathParameter(GetOpenApiOperation((TryParseStringRecordStruct foo) => { }, pattern: "/{foo}"));
    }

    [Fact]
    public void AddsFromQueryParameterAsQuery()
    {
        static void AssertQueryParameter(OpenApiOperation operation, string type)
        {
            var param = Assert.Single(operation.Parameters);
            Assert.Equal(ParameterLocation.Query, param.In);
            Assert.Empty(param.Content);
        }

        AssertQueryParameter(GetOpenApiOperation((int foo) => { }, "/"), "integer");
        AssertQueryParameter(GetOpenApiOperation(([FromQuery] int foo) => { }), "integer");
        AssertQueryParameter(GetOpenApiOperation(([FromQuery] TryParseStringRecordStruct foo) => { }), "object");
        AssertQueryParameter(GetOpenApiOperation((int[] foo) => { }, "/"), "array");
        AssertQueryParameter(GetOpenApiOperation((string[] foo) => { }, "/"), "array");
        AssertQueryParameter(GetOpenApiOperation((StringValues foo) => { }, "/"), "array");
        AssertQueryParameter(GetOpenApiOperation((TryParseStringRecordStruct[] foo) => { }, "/"), "array");
    }

    [Fact]
    public void AddsFromHeaderParameterAsHeader()
    {
        var operation = GetOpenApiOperation(([FromHeader] int foo) => { });
        var param = Assert.Single(operation.Parameters);

        Assert.Equal(ParameterLocation.Header, param.In);
        Assert.Empty(param.Content);
    }

    [Fact]
    public void DoesNotAddFromServiceParameterAsService()
    {
        Assert.Empty(GetOpenApiOperation((IInferredServiceInterface foo) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation(([FromServices] int foo) => { }).Parameters);
        Assert.Empty(GetOpenApiOperation(([FromKeyedServices("foo")] int foo) => { }).Parameters);
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
            Assert.Equal("application/json", content.Key);
            Assert.Empty(operation.Parameters);
        }

        AssertBodyParameter(GetOpenApiOperation((InferredJsonClass foo) => { }), "foo", "object");
        AssertBodyParameter(GetOpenApiOperation(([FromBody] int bar) => { }), "bar", "integer");
    }

#nullable enable

    [Fact]
    public void AddsMultipleParameters()
    {
        var operation = GetOpenApiOperation(([FromRoute] int foo, int bar, InferredJsonClass fromBody) => { });
        Assert.Equal(2, operation.Parameters.Count);

        var fooParam = operation.Parameters[0];
        Assert.Equal("foo", fooParam.Name);
        Assert.Equal(ParameterLocation.Path, fooParam.In);
        Assert.True(fooParam.Required);
        Assert.Empty(fooParam.Content);

        var barParam = operation.Parameters[1];
        Assert.Equal("bar", barParam.Name);
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.True(barParam.Required);
        Assert.Empty(barParam.Content);

        var fromBodyParam = operation.RequestBody;
        var fromBodyContent = Assert.Single(fromBodyParam.Content);
        Assert.Equal("application/json", fromBodyContent.Key);
        Assert.True(fromBodyParam.Required);
    }
#nullable disable

    [Fact]
    public void AddsMultipleParametersFromParametersAttribute()
    {
        static void AssertParameters(OpenApiOperation operation, string capturedName = "Foo")
        {
            Assert.Collection(
                operation.Parameters,
                param =>
                {
                    Assert.Equal(capturedName, param.Name);
                    Assert.Equal(ParameterLocation.Path, param.In);
                    Assert.True(param.Required);
                    Assert.Empty(param.Content);
                },
                param =>
                {
                    Assert.Equal("Bar", param.Name);
                    Assert.Equal(ParameterLocation.Query, param.In);
                    Assert.True(param.Required);
                    Assert.Empty(param.Content);
                }
            );
        }

        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListClass req) => { }));
        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListClassWithReadOnlyProperties req) => { }));
        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListStruct req) => { }));
        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListRecord req) => { }));
        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListRecordStruct req) => { }));
        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListRecordWithoutPositionalParameters req) => { }));
        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListRecordWithoutAttributes req) => { }, "/{foo}"), "foo");
        AssertParameters(GetOpenApiOperation(([AsParameters] ArgumentListRecordWithoutAttributes req) => { }, "/{Foo}"));
    }

    [Fact]
    public void TestParameterIsRequired()
    {
        var operation = GetOpenApiOperation(([FromRoute] int foo, int? bar) => { });
        Assert.Equal(2, operation.Parameters.Count);

        var fooParam = operation.Parameters[0];
        Assert.Equal("foo", fooParam.Name);
        Assert.Equal(ParameterLocation.Path, fooParam.In);
        Assert.True(fooParam.Required);
        Assert.Empty(fooParam.Content);

        var barParam = operation.Parameters[1];
        Assert.Equal("bar", barParam.Name);
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.False(barParam.Required);
        Assert.Empty(barParam.Content);
    }

    [Fact]
    public void TestParameterIsRequiredForObliviousNullabilityContext()
    {
        // In an oblivious nullability context, reference type parameters without
        // annotations are optional. Value type parameters are always required.
        var operation = GetOpenApiOperation((string foo, int bar) => { });
        Assert.Equal(2, operation.Parameters.Count);

        var fooParam = operation.Parameters[0];
        Assert.Equal(ParameterLocation.Query, fooParam.In);
        Assert.False(fooParam.Required);

        var barParam = operation.Parameters[1];
        Assert.Equal(ParameterLocation.Query, barParam.In);
        Assert.True(barParam.Required);
    }

    [Fact]
    public void RespectProducesProblemMetadata()
    {
        // Arrange
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new[] {
                new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(ProblemDetails), new [] { "application/json+problem" })
            });

        // Assert
        var responses = Assert.Single(operation.Responses);
        var content = Assert.Single(responses.Value.Content);
        Assert.Equal("application/json+problem", content.Key);
    }

    [Fact]
    public void RespectsProducesWithGroupNameExtensionMethod()
    {
        // Arrange
        var endpointGroupName = "SomeEndpointGroupName";
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new object[]
            {
                new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(InferredJsonClass), new[] { "application/json" }),
                new EndpointNameMetadata(endpointGroupName)
            });

        var responses = Assert.Single(operation.Responses);
        var content = Assert.Single(responses.Value.Content);
        Assert.Equal("application/json", content.Key);
    }

    [Fact]
    public void RespectsExcludeFromDescription()
    {
        // Arrange
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new object[]
            {
                new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(InferredJsonClass), new[] { "application/json" }),
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
                    new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(InferredJsonClass), new[] { "application/json" }),
                    new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(HttpValidationProblemDetails), new[] { "application/problem+json" }),
                    new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(ProblemDetails), new[] { "application/problem+json" }),
                    new ProducesResponseTypeMetadata(StatusCodes.Status409Conflict, typeof(ProblemDetails), new[] { "application/problem+json" })
            });
        var responses = operation.Responses;

        // Assert
        Assert.Collection(
            responses.OrderBy(response => response.Key),
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("200", responseType.Key);
                Assert.Equal("application/json", content.Key);
            },
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("400", responseType.Key);
                Assert.Equal("application/problem+json", content.Key);
            },
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("404", responseType.Key);
                Assert.Equal("application/problem+json", content.Key);
            },
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
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
                new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(InferredJsonClass), new[] { "application/json" }),
                new ProducesResponseTypeMetadata(StatusCodes.Status201Created, typeof(InferredJsonClass), new[] { "application/json" })
            });

        var responses = operation.Responses;

        // Assert
        Assert.Collection(
            responses.OrderBy(response => response.Key),
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("200", responseType.Key);
                Assert.Equal("application/json", content.Key);
            },
            responseType =>
            {
                var content = Assert.Single(responseType.Value.Content);
                Assert.Equal("201", responseType.Key);
                Assert.Equal("application/json", content.Key);
            });
    }

    [Fact]
    public void HandleAcceptsMetadataWithNoParams()
    {
        // Arrange
        var operation = GetOpenApiOperation(() => "",
            additionalMetadata: new[]
            {
                new AcceptsMetadata(new string[] { "application/json", "application/xml"}, typeof(string), true)
            });

        var requestBody = operation.RequestBody;

        // Assert
        Assert.Empty(operation.Parameters);
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
                    new AcceptsMetadata(new string[] { "application/json" }, typeof(InferredJsonClass), true)
                });

        // Assert
        var requestBody = operation.RequestBody;
        var content = Assert.Single(requestBody.Content);
        Assert.False(requestBody.Required);
        Assert.Empty(operation.Parameters);
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
        Assert.True(requestBody.Required);
        Assert.Empty(operation.Parameters);
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
        Assert.False(requestBody.Required);
        Assert.Empty(operation.Parameters);
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
        Assert.False(requestBody.Required);
        Assert.Empty(operation.Parameters);
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
        Assert.True(requestBody.Required);
        Assert.Empty(operation.Parameters);
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
        Assert.False(requestBody.Required);
        Assert.Empty(operation.Parameters);
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
        Assert.True(requestBody.Required);
        Assert.Empty(operation.Parameters);
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

        Assert.Empty(operation.Parameters);
    }

    [Fact]
    public void TestIsRequiredFromFormFile()
    {
        var operation0 = GetOpenApiOperation((IFormFile fromFile) => { });
        var operation1 = GetOpenApiOperation((IFormFile? fromFile) => { });
        Assert.NotNull(operation0.RequestBody);
        Assert.NotNull(operation1.RequestBody);

        var fromFileParam0 = operation0.RequestBody;
        var fromFileParam0ContentType = Assert.Single(fromFileParam0.Content.Values);
        Assert.Equal("multipart/form-data", fromFileParam0.Content.Keys.SingleOrDefault());
        Assert.True(fromFileParam0.Required);

        var fromFileParam1 = operation1.RequestBody;
        var fromFileParam1ContentType = Assert.Single(fromFileParam1.Content.Values);
        Assert.Equal("multipart/form-data", fromFileParam1.Content.Keys.SingleOrDefault());
        Assert.False(fromFileParam1.Required);
    }

    [Fact]
    public void AddsFromFormParameterAsFormFile()
    {
        static void AssertFormFileParameter(OpenApiOperation operation, string expectedType, string expectedName)
        {
            var requestBody = operation.RequestBody;
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("multipart/form-data", content.Key);
            Assert.Empty(operation.Parameters);
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
            Assert.True(requestBody.Required);
            Assert.Empty(operation.Parameters);
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

    // Test case for https://github.com/dotnet/aspnetcore/issues/41622
    [Fact]
    public void HandlesEndpointWithMultipleResponses()
    {
        var operation = GetOpenApiOperation(() => TypedResults.Ok(new InferredJsonClass()),
            additionalMetadata: new[]
            {
                // Metadata added by the `IEndpointMetadataProvider` on `TypedResults.Ok`
                new ProducesResponseTypeMetadata(StatusCodes.Status200OK),
                // Metadata added by the `Produces<Type>` extension method
                new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(InferredJsonClass), new[] { "application/json" }),
            });

        var response = Assert.Single(operation.Responses);
        var content = Assert.Single(response.Value.Content);
        Assert.Equal("200", response.Key);
        Assert.Equal("application/json", content.Key);

    }

    [Fact]
    public void OnlyAddParametersWithCorrectLocations()
    {
        var operation = GetOpenApiOperation(([FromBody] int fromBody, [FromRoute] int fromRoute, [FromServices] int fromServices) => { });

        Assert.Single(operation.Parameters);
    }

    [Theory]
    [InlineData("/todos/{id}", "id")]
    [InlineData("/todos/{Id}", "Id")]
    [InlineData("/todos/{id:minlen(2)}", "id")]
    public void FavorsParameterCasingInRoutePattern(string pattern, string expectedName)
    {
        var operation = GetOpenApiOperation((int Id) => "", pattern);

        var param = Assert.Single(operation.Parameters);
        Assert.Equal(expectedName, param.Name);
    }

    [Fact]
    public void HandlesEndpointWithNoRequestBodyOrParams()
    {
        var operationWithNoParams = GetOpenApiOperation(() => "", "/");

        Assert.Empty(operationWithNoParams.Parameters);
        Assert.Null(operationWithNoParams.RequestBody);
    }

    [Fact]
    public void HandlesEndpointWithNoRequestBody()
    {
        var operationWithNoBodyParams = GetOpenApiOperation((int id) => "", "/", httpMethods: new[] { "PUT" });

        Assert.Single(operationWithNoBodyParams.Parameters);
        Assert.Null(operationWithNoBodyParams.RequestBody);
    }

    [Fact]
    public void HandlesParameterWithNameInAttribute()
    {
        static void ValidateParameter(OpenApiOperation operation, string expectedName)
        {
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(expectedName, parameter.Name);
        }

        ValidateParameter(GetOpenApiOperation(([FromRoute(Name = "routeName")] string param) => ""), "routeName");
        ValidateParameter(GetOpenApiOperation(([FromRoute(Name = "routeName")] string param) => "", "/{param}"), "routeName");
        ValidateParameter(GetOpenApiOperation(([FromQuery(Name = "queryName")] string param) => ""), "queryName");
        ValidateParameter(GetOpenApiOperation(([FromHeader(Name = "headerName")] string param) => ""), "headerName");
    }

#nullable enable
    public class AsParametersWithRequiredMembers
    {
        public required string RequiredStringMember { get; set; }
        public required string? RequiredNullableStringMember { get; set; }
        public string NonNullableStringMember { get; set; } = string.Empty;
        public string? NullableStringMember { get; set; }
    }

    [Fact]
    public void SupportsRequiredMembersInAsParametersAttribute()
    {
        var operation = GetOpenApiOperation(([AsParameters] AsParametersWithRequiredMembers foo) => { });
        Assert.Equal(4, operation.Parameters.Count);

        Assert.Collection(operation.Parameters,
            param => Assert.True(param.Required),
            param => Assert.False(param.Required),
            param => Assert.True(param.Required),
            param => Assert.False(param.Required));
    }
#nullable disable

    public class AsParametersWithRequiredMembersObliviousContext
    {
        public required string RequiredStringMember { get; set; }
        public string OptionalStringMember { get; set; }
    }

    [Fact]
    public void SupportsRequiredMembersInAsParametersObliviousContextAttribute()
    {
        var operation = GetOpenApiOperation(([AsParameters] AsParametersWithRequiredMembersObliviousContext foo) => { });
        Assert.Equal(2, operation.Parameters.Count);

        Assert.Collection(operation.Parameters,
            param => Assert.True(param.Required),
            param => Assert.False(param.Required));
    }

    [Fact]
    public void DoesNotGenerateRequestBodyWhenInferredBodyDisabled()
    {
        var operation = GetOpenApiOperation((string[] names) => { }, httpMethods: new[] { "GET" });

        var parameter = Assert.Single(operation.Parameters);

        Assert.Equal("names", parameter.Name);
        Assert.Equal(ParameterLocation.Query, parameter.In);
        Assert.Null(operation.RequestBody);
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

    // Shared with OpenApiRouteHandlerExtensionsTests
    internal class ServiceProviderIsService : IServiceProviderIsService
    {
        public bool IsService(Type serviceType) => serviceType == typeof(IInferredServiceInterface);
    }

    internal class HostEnvironment : IHostEnvironment
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

    private record ArgumentListRecord([FromRoute] int Foo, int Bar, InferredJsonClass FromBody, HttpContext context);

    private record struct ArgumentListRecordStruct([FromRoute] int Foo, int Bar, InferredJsonClass FromBody, HttpContext context);

    private record ArgumentListRecordWithoutAttributes(int Foo, int Bar, InferredJsonClass FromBody, HttpContext context);

    private record ArgumentListRecordWithoutPositionalParameters
    {
        [FromRoute]
        public int Foo { get; set; }
        public int Bar { get; set; }
        public InferredJsonClass FromBody { get; set; }
        public HttpContext Context { get; set; }
    }

    private class ArgumentListClass
    {
        [FromRoute]
        public int Foo { get; set; }
        public int Bar { get; set; }
        public InferredJsonClass FromBody { get; set; }
        public HttpContext Context { get; set; }
    }

    private class ArgumentListClassWithReadOnlyProperties : ArgumentListClass
    {
        public int ReadOnly { get; }
    }

    private struct ArgumentListStruct
    {
        [FromRoute]
        public int Foo { get; set; }
        public int Bar { get; set; }
        public InferredJsonClass FromBody { get; set; }
        public HttpContext Context { get; set; }
    }

    private class TestEndpointConventionBuilder : EndpointBuilder, IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention)
        {
            convention(this);
        }

        public override Endpoint Build()
        {
            throw new NotImplementedException();
        }
    }
}
