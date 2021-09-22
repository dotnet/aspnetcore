// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class EndpointMetadataApiDescriptionProviderTest
    {
        [Fact]
        public void MultipleApiDescriptionsCreatedForMultipleHttpMethods()
        {
            var apiDescriptions = GetApiDescriptions(() => { }, "/", new string[] { "FOO", "BAR" });

            Assert.Equal(2, apiDescriptions.Count);
        }

        [Fact]
        public void ApiDescriptionNotCreatedIfNoHttpMethods()
        {
            var apiDescriptions = GetApiDescriptions(() => { }, "/", Array.Empty<string>());

            Assert.Empty(apiDescriptions);
        }

        [Fact]
        public void UsesDeclaringTypeAsControllerName()
        {
            var apiDescription = GetApiDescription(TestAction);

            var declaringTypeName = typeof(EndpointMetadataApiDescriptionProviderTest).Name;
            Assert.Equal(declaringTypeName, apiDescription.ActionDescriptor.RouteValues["controller"]);
        }

        [Fact]
        public void UsesApplicationNameAsControllerNameIfNoDeclaringType()
        {
            var apiDescription = GetApiDescription(() => { });

            Assert.Equal(nameof(EndpointMetadataApiDescriptionProviderTest), apiDescription.ActionDescriptor.RouteValues["controller"]);
        }

        [Fact]
        public void AddsRequestFormatFromMetadata()
        {
            static void AssertCustomRequestFormat(ApiDescription apiDescription)
            {
                var requestFormat = Assert.Single(apiDescription.SupportedRequestFormats);
                Assert.Equal("application/custom", requestFormat.MediaType);
                Assert.Null(requestFormat.Formatter);
            }

            AssertCustomRequestFormat(GetApiDescription(
                [Consumes("application/custom")]
            (InferredJsonClass fromBody) =>
                { }));

            AssertCustomRequestFormat(GetApiDescription(
                [Consumes("application/custom")]
            ([FromBody] int fromBody) =>
                { }));
        }

        [Fact]
        public void AddsMultipleRequestFormatsFromMetadata()
        {
            var apiDescription = GetApiDescription(
                [Consumes("application/custom0", "application/custom1")]
            (InferredJsonClass fromBody) =>
                { });

            Assert.Equal(2, apiDescription.SupportedRequestFormats.Count);

            var requestFormat0 = apiDescription.SupportedRequestFormats[0];
            Assert.Equal("application/custom0", requestFormat0.MediaType);
            Assert.Null(requestFormat0.Formatter);

            var requestFormat1 = apiDescription.SupportedRequestFormats[1];
            Assert.Equal("application/custom1", requestFormat1.MediaType);
            Assert.Null(requestFormat1.Formatter);
        }

        [Fact]
        public void AddsMultipleRequestFormatsFromMetadataWithRequestTypeAndOptionalBodyParameter()
        {
            var apiDescription = GetApiDescription(
                [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = true)]
            () =>
                { });

            Assert.Equal(2, apiDescription.SupportedRequestFormats.Count);

            var apiParameterDescription = apiDescription.ParameterDescriptions[0];
            Assert.Equal("InferredJsonClass", apiParameterDescription.Type.Name);
            Assert.False(apiParameterDescription.IsRequired);
        }

        [Fact]
        public void AddsMultipleRequestFormatsFromMetadataWithRequiredBodyParameter()
        {
            var apiDescription = GetApiDescription(
                [Consumes(typeof(InferredJsonClass), "application/custom0", "application/custom1", IsOptional = false)]
            (InferredJsonClass fromBody) =>
                { });

            Assert.Equal(2, apiDescription.SupportedRequestFormats.Count);

            var apiParameterDescription = apiDescription.ParameterDescriptions[0];
            Assert.Equal("InferredJsonClass", apiParameterDescription.Type.Name);
            Assert.True(apiParameterDescription.IsRequired);
        }

        [Fact]
        public void AddsJsonResponseFormatWhenFromBodyInferred()
        {
            static void AssertJsonResponse(ApiDescription apiDescription, Type expectedType)
            {
                var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(expectedType, responseType.Type);
                Assert.Equal(expectedType, responseType.ModelMetadata.ModelType);

                var responseFormat = Assert.Single(responseType.ApiResponseFormats);
                Assert.Equal("application/json", responseFormat.MediaType);
                Assert.Null(responseFormat.Formatter);
            }

            AssertJsonResponse(GetApiDescription(() => new InferredJsonClass()), typeof(InferredJsonClass));
            AssertJsonResponse(GetApiDescription(() => (IInferredJsonInterface)null), typeof(IInferredJsonInterface));
        }

        [Fact]
        public void AddsTextResponseFormatWhenFromBodyInferred()
        {
            var apiDescription = GetApiDescription(() => "foo");

            var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Equal(typeof(string), responseType.Type);
            Assert.Equal(typeof(string), responseType.ModelMetadata.ModelType);

            var responseFormat = Assert.Single(responseType.ApiResponseFormats);
            Assert.Equal("text/plain", responseFormat.MediaType);
            Assert.Null(responseFormat.Formatter);
        }

        [Fact]
        public void AddsNoResponseFormatWhenItCannotBeInferredAndTheresNoMetadata()
        {
            static void AssertVoid(ApiDescription apiDescription)
            {
                var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Equal(typeof(void), responseType.ModelMetadata.ModelType);

                Assert.Empty(responseType.ApiResponseFormats);
            }

            AssertVoid(GetApiDescription(() => { }));
            AssertVoid(GetApiDescription(() => Task.CompletedTask));
            AssertVoid(GetApiDescription(() => new ValueTask()));
        }

        [Fact]
        public void AddsResponseFormatFromMetadata()
        {
            var apiDescription = GetApiDescription(
                [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created)]
            [Produces("application/custom")]
            () => new InferredJsonClass());

            var responseType = Assert.Single(apiDescription.SupportedResponseTypes);

            Assert.Equal(201, responseType.StatusCode);
            Assert.Equal(typeof(TimeSpan), responseType.Type);
            Assert.Equal(typeof(TimeSpan), responseType.ModelMetadata.ModelType);

            var responseFormat = Assert.Single(responseType.ApiResponseFormats);
            Assert.Equal("application/custom", responseFormat.MediaType);
        }

        [Fact]
        public void AddsMultipleResponseFormatsFromMetadataWithPoco()
        {
            var apiDescription = GetApiDescription(
                [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status201Created)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            () => new InferredJsonClass());

            Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

            var createdResponseType = apiDescription.SupportedResponseTypes[0];

            Assert.Equal(201, createdResponseType.StatusCode);
            Assert.Equal(typeof(TimeSpan), createdResponseType.Type);
            Assert.Equal(typeof(TimeSpan), createdResponseType.ModelMetadata.ModelType);

            var createdResponseFormat = Assert.Single(createdResponseType.ApiResponseFormats);
            Assert.Equal("application/json", createdResponseFormat.MediaType);

            var badRequestResponseType = apiDescription.SupportedResponseTypes[1];

            Assert.Equal(400, badRequestResponseType.StatusCode);
            Assert.Equal(typeof(InferredJsonClass), badRequestResponseType.Type);
            Assert.Equal(typeof(InferredJsonClass), badRequestResponseType.ModelMetadata.ModelType);

            var badRequestResponseFormat = Assert.Single(badRequestResponseType.ApiResponseFormats);
            Assert.Equal("application/json", badRequestResponseFormat.MediaType);
        }

        [Fact]
        public void AddsMultipleResponseFormatsFromMetadataWithIResult()
        {
            var apiDescription = GetApiDescription(
                [ProducesResponseType(typeof(InferredJsonClass), StatusCodes.Status201Created)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            () => Results.Ok(new InferredJsonClass()));

            Assert.Equal(2, apiDescription.SupportedResponseTypes.Count);

            var createdResponseType = apiDescription.SupportedResponseTypes[0];

            Assert.Equal(201, createdResponseType.StatusCode);
            Assert.Equal(typeof(InferredJsonClass), createdResponseType.Type);
            Assert.Equal(typeof(InferredJsonClass), createdResponseType.ModelMetadata.ModelType);

            var createdResponseFormat = Assert.Single(createdResponseType.ApiResponseFormats);
            Assert.Equal("application/json", createdResponseFormat.MediaType);

            var badRequestResponseType = apiDescription.SupportedResponseTypes[1];

            Assert.Equal(400, badRequestResponseType.StatusCode);
            Assert.Equal(typeof(void), badRequestResponseType.Type);
            Assert.Equal(typeof(void), badRequestResponseType.ModelMetadata.ModelType);

            Assert.Empty(badRequestResponseType.ApiResponseFormats);
        }

        [Fact]
        public void AddsFromRouteParameterAsPath()
        {
            static void AssertPathParameter(ApiDescription apiDescription)
            {
                var param = Assert.Single(apiDescription.ParameterDescriptions);
                Assert.Equal(typeof(int), param.Type);
                Assert.Equal(typeof(int), param.ModelMetadata.ModelType);
                Assert.Equal(BindingSource.Path, param.Source);
            }

            AssertPathParameter(GetApiDescription((int foo) => { }, "/{foo}"));
            AssertPathParameter(GetApiDescription(([FromRoute] int foo) => { }));
        }

        [Fact]
        public void AddsFromRouteParameterAsPathWithCustomClassWithTryParse()
        {
            static void AssertPathParameter(ApiDescription apiDescription)
            {
                var param = Assert.Single(apiDescription.ParameterDescriptions);
                Assert.Equal(typeof(TryParseStringRecord), param.Type);
                Assert.Equal(typeof(string), param.ModelMetadata.ModelType);
                Assert.Equal(BindingSource.Path, param.Source);
            }

            AssertPathParameter(GetApiDescription((TryParseStringRecord foo) => { }, "/{foo}"));
        }

        [Fact]
        public void AddsFromRouteParameterAsPathWithPrimitiveType()
        {
            static void AssertPathParameter(ApiDescription apiDescription)
            {
                var param = Assert.Single(apiDescription.ParameterDescriptions);
                Assert.Equal(typeof(int), param.Type);
                Assert.Equal(typeof(int), param.ModelMetadata.ModelType);
                Assert.Equal(BindingSource.Path, param.Source);
            }

            AssertPathParameter(GetApiDescription((int foo) => { }, "/{foo}"));
        }

        [Fact]
        public void AddsFromRouteParameterAsPathWithNullablePrimitiveType()
        {
            static void AssertPathParameter(ApiDescription apiDescription)
            {
                var param = Assert.Single(apiDescription.ParameterDescriptions);
                Assert.Equal(typeof(int?), param.Type);
                Assert.Equal(typeof(int?), param.ModelMetadata.ModelType);
                Assert.Equal(BindingSource.Path, param.Source);
            }

            AssertPathParameter(GetApiDescription((int? foo) => { }, "/{foo}"));
        }

        [Fact]
        public void AddsFromRouteParameterAsPathWithStructTypeWithTryParse()
        {
            static void AssertPathParameter(ApiDescription apiDescription)
            {
                var param = Assert.Single(apiDescription.ParameterDescriptions);
                Assert.Equal(typeof(TryParseStringRecordStruct), param.Type);
                Assert.Equal(typeof(string), param.ModelMetadata.ModelType);
                Assert.Equal(BindingSource.Path, param.Source);
            }

            AssertPathParameter(GetApiDescription((TryParseStringRecordStruct foo) => { }, "/{foo}"));
        }

        [Fact]
        public void AddsFromQueryParameterAsQuery()
        {
            static void AssertQueryParameter(ApiDescription apiDescription)
            {
                var param = Assert.Single(apiDescription.ParameterDescriptions);
                Assert.Equal(typeof(int), param.Type);
                Assert.Equal(typeof(int), param.ModelMetadata.ModelType);
                Assert.Equal(BindingSource.Query, param.Source);
            }

            AssertQueryParameter(GetApiDescription((int foo) => { }, "/"));
            AssertQueryParameter(GetApiDescription(([FromQuery] int foo) => { }));
        }

        [Fact]
        public void AddsFromHeaderParameterAsHeader()
        {
            var apiDescription = GetApiDescription(([FromHeader] int foo) => { });
            var param = Assert.Single(apiDescription.ParameterDescriptions);

            Assert.Equal(typeof(int), param.Type);
            Assert.Equal(typeof(int), param.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Header, param.Source);
        }

        [Fact]
        public void DoesNotAddFromServiceParameterAsService()
        {
            Assert.Empty(GetApiDescription((IInferredServiceInterface foo) => { }).ParameterDescriptions);
            Assert.Empty(GetApiDescription(([FromServices] int foo) => { }).ParameterDescriptions);
            Assert.Empty(GetApiDescription((HttpContext context) => { }).ParameterDescriptions);
            Assert.Empty(GetApiDescription((HttpRequest request) => { }).ParameterDescriptions);
            Assert.Empty(GetApiDescription((HttpResponse response) => { }).ParameterDescriptions);
            Assert.Empty(GetApiDescription((ClaimsPrincipal user) => { }).ParameterDescriptions);
            Assert.Empty(GetApiDescription((CancellationToken token) => { }).ParameterDescriptions);
            Assert.Empty(GetApiDescription((BindAsyncRecord context) => { }).ParameterDescriptions);
        }

        [Fact]
        public void DoesNotAddFromBodyParameterInTheParameterDescription()
        {
            static void AssertBodyParameter(ApiDescription apiDescription, Type expectedType)
            {
                Assert.Empty(apiDescription.ParameterDescriptions);
            }

            AssertBodyParameter(GetApiDescription((InferredJsonClass foo) => { }), typeof(InferredJsonClass));
            AssertBodyParameter(GetApiDescription(([FromBody] int foo) => { }), typeof(int));
        }

        [Fact]
        public void AddsDefaultValueFromParameters()
        {
            var apiDescription = GetApiDescription(TestActionWithDefaultValue);

            var param = Assert.Single(apiDescription.ParameterDescriptions);
            Assert.Equal(42, param.DefaultValue);
        }

        [Fact]
        public void AddsMultipleParameters()
        {
            var apiDescription = GetApiDescription(([FromRoute] int foo, int bar, InferredJsonClass fromBody) => { });
            Assert.Equal(2, apiDescription.ParameterDescriptions.Count);

            var fooParam = apiDescription.ParameterDescriptions[0];
            Assert.Equal(typeof(int), fooParam.Type);
            Assert.Equal(typeof(int), fooParam.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Path, fooParam.Source);
            Assert.True(fooParam.IsRequired);

            var barParam = apiDescription.ParameterDescriptions[1];
            Assert.Equal(typeof(int), barParam.Type);
            Assert.Equal(typeof(int), barParam.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Query, barParam.Source);
            Assert.True(barParam.IsRequired);
        }

        [Fact]
        public void TestParameterIsRequired()
        {
            var apiDescription = GetApiDescription(([FromRoute] int foo, int? bar) => { });
            Assert.Equal(2, apiDescription.ParameterDescriptions.Count);

            var fooParam = apiDescription.ParameterDescriptions[0];
            Assert.Equal(typeof(int), fooParam.Type);
            Assert.Equal(typeof(int), fooParam.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Path, fooParam.Source);
            Assert.True(fooParam.IsRequired);

            var barParam = apiDescription.ParameterDescriptions[1];
            Assert.Equal(typeof(int?), barParam.Type);
            Assert.Equal(typeof(int?), barParam.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Query, barParam.Source);
            Assert.False(barParam.IsRequired);
        }

        [Fact]
        public void AddsDisplayNameFromRouteEndpoint()
        {
            var apiDescription = GetApiDescription(() => "foo", displayName: "FOO");

            Assert.Equal("FOO", apiDescription.ActionDescriptor.DisplayName);
        }

        [Fact]
        public void AddsMetadataFromRouteEndpoint()
        {
            var apiDescription = GetApiDescription([ApiExplorerSettings(IgnoreApi = true)]() => { });

            Assert.NotEmpty(apiDescription.ActionDescriptor.EndpointMetadata);

            var apiExplorerSettings = apiDescription.ActionDescriptor.EndpointMetadata
                .OfType<ApiExplorerSettingsAttribute>()
                .FirstOrDefault();

            Assert.NotNull(apiExplorerSettings);
            Assert.True(apiExplorerSettings.IgnoreApi);
        }

        [Fact]
        public void TestParameterIsRequiredForObliviousNullabilityContext()
        {
            // In an oblivious nullability context, reference type parameters without
            // annotations are optional. Value type parameters are always required.
            var apiDescription = GetApiDescription((string foo, int bar) => { });
            Assert.Equal(2, apiDescription.ParameterDescriptions.Count);

            var fooParam = apiDescription.ParameterDescriptions[0];
            Assert.Equal(typeof(string), fooParam.Type);
            Assert.Equal(typeof(string), fooParam.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Query, fooParam.Source);
            Assert.False(fooParam.IsRequired);

            var barParam = apiDescription.ParameterDescriptions[1];
            Assert.Equal(typeof(int), barParam.Type);
            Assert.Equal(typeof(int), barParam.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Query, barParam.Source);
            Assert.True(barParam.IsRequired);
        }

        [Fact]
        public void TestParameterAttributesCanBeInspected()
        {
            var apiDescription = GetApiDescription(([Description("The name.")] string name) => { });
            Assert.Equal(1, apiDescription.ParameterDescriptions.Count);

            var nameParam = apiDescription.ParameterDescriptions[0];
            Assert.Equal(typeof(string), nameParam.Type);
            Assert.Equal(typeof(string), nameParam.ModelMetadata.ModelType);
            Assert.Equal(BindingSource.Query, nameParam.Source);
            Assert.False(nameParam.IsRequired);

            Assert.NotNull(nameParam.ParameterDescriptor);
            Assert.Equal("name", nameParam.ParameterDescriptor.Name);
            Assert.Equal(typeof(string), nameParam.ParameterDescriptor.ParameterType);

            var descriptor = Assert.IsAssignableFrom<IParameterInfoParameterDescriptor>(nameParam.ParameterDescriptor);

            Assert.NotNull(descriptor.ParameterInfo);

            var description = Assert.Single(descriptor.ParameterInfo.GetCustomAttributes<DescriptionAttribute>());

            Assert.NotNull(description);
            Assert.Equal("The name.", description.Description);
        }

        [Fact]
        public void RespectsProducesProblemExtensionMethod()
        {
            // Arrange
            var builder = CreateBuilder();
            builder.MapGet("/api/todos", () => "").ProducesProblem(StatusCodes.Status400BadRequest);
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var apiDescription = Assert.Single(context.Results);
            var responseTypes = Assert.Single(apiDescription.SupportedResponseTypes);
            Assert.Equal(typeof(ProblemDetails), responseTypes.Type);
        }

        [Fact]
        public void RespectsProducesWithGroupNameExtensionMethod()
        {
            // Arrange
            var endpointGroupName = "SomeEndpointGroupName";
            var builder = CreateBuilder();
            builder.MapGet("/api/todos", () => "").Produces<InferredJsonClass>().WithGroupName(endpointGroupName);
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var apiDescription = Assert.Single(context.Results);
            var responseTypes = Assert.Single(apiDescription.SupportedResponseTypes);
            Assert.Equal(typeof(InferredJsonClass), responseTypes.Type);
            Assert.Equal(endpointGroupName, apiDescription.GroupName);
        }

        [Fact]
        public void RespectsExcludeFromDescription()
        {
            // Arrange
            var builder = CreateBuilder();
            builder.MapGet("/api/todos", () => "").Produces<InferredJsonClass>().ExcludeFromDescription();
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Empty(context.Results);
        }

        [Fact]
        public void HandlesProducesWithProducesProblem()
        {
            // Arrange
            var builder = CreateBuilder();
            builder.MapGet("/api/todos", () => "")
                .Produces<InferredJsonClass>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            Assert.Collection(
                context.Results.SelectMany(r => r.SupportedResponseTypes).OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(typeof(InferredJsonClass), responseType.Type);
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(new[] { "application/json" }, GetSortedMediaTypes(responseType));

                },
                responseType =>
                {
                    Assert.Equal(typeof(HttpValidationProblemDetails), responseType.Type);
                    Assert.Equal(400, responseType.StatusCode);
                    Assert.Equal(new[] { "application/problem+json" }, GetSortedMediaTypes(responseType));
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails), responseType.Type);
                    Assert.Equal(404, responseType.StatusCode);
                    Assert.Equal(new[] { "application/problem+json" }, GetSortedMediaTypes(responseType));
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails), responseType.Type);
                    Assert.Equal(409, responseType.StatusCode);
                    Assert.Equal(new[] { "application/problem+json" }, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public void HandleMultipleProduces()
        {
            // Arrange
            var builder = CreateBuilder();
            builder.MapGet("/api/todos", () => "")
                .Produces<InferredJsonClass>(StatusCodes.Status200OK)
                .Produces<InferredJsonClass>(StatusCodes.Status201Created);
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            Assert.Collection(
                context.Results.SelectMany(r => r.SupportedResponseTypes).OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(typeof(InferredJsonClass), responseType.Type);
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(new[] { "application/json" }, GetSortedMediaTypes(responseType));

                },
                responseType =>
                {
                    Assert.Equal(typeof(InferredJsonClass), responseType.Type);
                    Assert.Equal(201, responseType.StatusCode);
                    Assert.Equal(new[] { "application/json" }, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public void HandleAcceptsMetadata()
        {
            // Arrange
            var builder = CreateBuilder();
            builder.MapPost("/api/todos", () => "")
                .Accepts<string>("application/json", "application/xml");
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            Assert.Collection(
                context.Results.SelectMany(r => r.SupportedRequestFormats),
                requestType =>
                {
                    Assert.Equal("application/json", requestType.MediaType);
                },
                requestType =>
                {
                    Assert.Equal("application/xml", requestType.MediaType);
                });
        }

        [Fact]
        public void HandleAcceptsMetadataWithTypeParameter()
        {
            // Arrange
            var builder = new TestEndpointRouteBuilder(new ApplicationBuilder(null));
            builder.MapPost("/api/todos", (InferredJsonClass inferredJsonClass) => "")
                .Accepts(typeof(InferredJsonClass), "application/json");
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            var parameterDescriptions = context.Results.SelectMany(r => r.ParameterDescriptions);
            var bodyParameterDescription = parameterDescriptions.Single();
            Assert.Equal(typeof(InferredJsonClass), bodyParameterDescription.Type);
            Assert.Equal(typeof(InferredJsonClass).Name, bodyParameterDescription.Name);
            Assert.True(bodyParameterDescription.IsRequired);
        }

        [Fact]
        public void FavorsProducesMetadataOverAttribute()
        {
            // Arrange
            var builder = CreateBuilder();
            builder.MapGet("/api/todos", [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]() => "")
                .Produces<InferredJsonClass>(StatusCodes.Status200OK);
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            Assert.Collection(
                context.Results.SelectMany(r => r.SupportedResponseTypes).OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(typeof(InferredJsonClass), responseType.Type);
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(new[] { "application/json" }, GetSortedMediaTypes(responseType));

                });
        }

#nullable enable

        [Fact]
        public void HandleDefaultIAcceptsMetadataForRequiredBodyParameter()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var builder = new TestEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
            builder.MapPost("/api/todos", (InferredJsonClass inferredJsonClass) => "");
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            var parameterDescriptions = context.Results.SelectMany(r => r.ParameterDescriptions);
            var bodyParameterDescription = parameterDescriptions.Single();
            Assert.Equal(typeof(InferredJsonClass), bodyParameterDescription.Type);
            Assert.Equal(typeof(InferredJsonClass).Name, bodyParameterDescription.Name);
            Assert.True(bodyParameterDescription.IsRequired);

            // Assert
            var requestFormats = context.Results.SelectMany(r => r.SupportedRequestFormats);
            var defaultRequestFormat = requestFormats.Single();
            Assert.Equal("application/json", defaultRequestFormat.MediaType);
        }

#nullable restore

#nullable enable

        [Fact]
        public void HandleDefaultIAcceptsMetadataForOptionalBodyParameter()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var builder = new TestEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
            builder.MapPost("/api/todos", (InferredJsonClass? inferredJsonClass) => "");
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            var parameterDescriptions = context.Results.SelectMany(r => r.ParameterDescriptions);
            var bodyParameterDescription = parameterDescriptions.Single();
            Assert.Equal(typeof(InferredJsonClass), bodyParameterDescription.Type);
            Assert.Equal(typeof(InferredJsonClass).Name, bodyParameterDescription.Name);
            Assert.False(bodyParameterDescription.IsRequired);

            // Assert
            var requestFormats = context.Results.SelectMany(r => r.SupportedRequestFormats);
            var defaultRequestFormat = requestFormats.Single();
            Assert.Equal("application/json", defaultRequestFormat.MediaType);
        }

#nullable restore

#nullable enable

        [Fact]
        public void HandleIAcceptsMetadataWithConsumesAttributeAndInferredOptionalFromBodyType()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var builder = new TestEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
            builder.MapPost("/api/todos", [Consumes("application/xml")] (InferredJsonClass? inferredJsonClass) => "");
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var endpointDataSource = builder.DataSources.OfType<EndpointDataSource>().Single();
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };
            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            // Act
            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            // Assert
            var parameterDescriptions = context.Results.SelectMany(r => r.ParameterDescriptions);
            var bodyParameterDescription = parameterDescriptions.Single();
            Assert.Equal(typeof(void), bodyParameterDescription.Type);
            Assert.Equal(typeof(void).Name, bodyParameterDescription.Name);
            Assert.True(bodyParameterDescription.IsRequired);

            // Assert
            var requestFormats = context.Results.SelectMany(r => r.SupportedRequestFormats);
            var defaultRequestFormat = requestFormats.Single();
            Assert.Equal("application/xml", defaultRequestFormat.MediaType);
        }

#nullable restore

        private static IEnumerable<string> GetSortedMediaTypes(ApiResponseType apiResponseType)
        {
            return apiResponseType.ApiResponseFormats
                .OrderBy(format => format.MediaType)
                .Select(format => format.MediaType);
        }

        private static IList<ApiDescription> GetApiDescriptions(
            Delegate action,
            string pattern = null,
            IEnumerable<string> httpMethods = null,
            string displayName = null)
        {
            var methodInfo = action.Method;
            var attributes = methodInfo.GetCustomAttributes();
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var httpMethodMetadata = new HttpMethodMetadata(httpMethods ?? new[] { "GET" });
            var metadataItems = new List<object>(attributes) { methodInfo, httpMethodMetadata };
            var endpointMetadata = new EndpointMetadataCollection(metadataItems.ToArray());
            var routePattern = RoutePatternFactory.Parse(pattern ?? "/");

            var endpoint = new RouteEndpoint(httpContext => Task.CompletedTask, routePattern, 0, endpointMetadata, displayName);
            var endpointDataSource = new DefaultEndpointDataSource(endpoint);
            var hostEnvironment = new HostEnvironment
            {
                ApplicationName = nameof(EndpointMetadataApiDescriptionProviderTest)
            };

            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource, hostEnvironment, new ServiceProviderIsService());

            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            return context.Results;
        }

        private static TestEndpointRouteBuilder CreateBuilder() =>
            new TestEndpointRouteBuilder(new ApplicationBuilder(new TestServiceProvider()));


        private static ApiDescription GetApiDescription(Delegate action, string pattern = null, string displayName = null) =>
            Assert.Single(GetApiDescriptions(action, pattern, displayName: displayName));

        private static void TestAction()
        {
        }

        private static void TestActionWithDefaultValue(int foo = 42)
        {
        }

        private class InferredJsonClass
        {
        }

        private interface IInferredServiceInterface
        {
        }

        private interface IInferredJsonInterface
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

        private class TestEndpointRouteBuilder : IEndpointRouteBuilder
        {
            public TestEndpointRouteBuilder(IApplicationBuilder applicationBuilder)
            {
                ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
                DataSources = new List<EndpointDataSource>();
            }

            public IApplicationBuilder ApplicationBuilder { get; }

            public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();

            public ICollection<EndpointDataSource> DataSources { get; }

            public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
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

        private record BindAsyncRecord(int Value)
        {
            public static ValueTask<BindAsyncRecord> BindAsync(HttpContext context, ParameterInfo parameter) =>
                throw new NotImplementedException();
            public static bool TryParse(string value, out BindAsyncRecord result) =>
                throw new NotImplementedException();
        }

        private class TestServiceProvider : IServiceProvider
        {
            public void Dispose()
            {
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IOptions<RouteHandlerOptions>))
                {
                    return Options.Create(new RouteHandlerOptions());
                }

                return null;
            }
        }
    }
}
