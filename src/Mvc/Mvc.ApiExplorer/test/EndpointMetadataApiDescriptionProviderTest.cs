// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class EndpointMetadataApiDescriptionProviderTest
    {
        [Fact]
        public void ApiDescription_MultipleCreatedForMultipleHttpMethods()
        {
            var apiDescriptions = GetApiDescriptions(() => { }, "/", new string[] { "FOO", "BAR" });

            Assert.Equal(2, apiDescriptions.Count);
        }

        [Fact]
        public void ApiDescription_NotCreatedIfNoHttpMethods()
        {
            var apiDescriptions = GetApiDescriptions(() => { }, "/", Array.Empty<string>());

            Assert.Empty(apiDescriptions);
        }

        [Fact]
        public void ApiDescription_UsesDeclaringTypeAsControllerName()
        {
            var apiDescription = GetApiDescription(TestAction);

            var declaringTypeName = typeof(EndpointMetadataApiDescriptionProviderTest).Name;
            Assert.Equal(declaringTypeName, apiDescription.ActionDescriptor.RouteValues["controller"]);
        }

        [Fact]
        public void ApiDescription_UsesMapAsControllerNameIfNoDeclaringType()
        {
            var apiDescription = GetApiDescription(() => { });

            Assert.Equal("Map", apiDescription.ActionDescriptor.RouteValues["controller"]);
        }

        [Fact]
        public void ApiDescription_AddsJsonRequestFormatWhenFromBodyInferred()
        {
            static void AssertApiDescriptionHasJsonRequestFormat(ApiDescription apiDescription)
            {
                var requestFormat = Assert.Single(apiDescription.SupportedRequestFormats);
                Assert.Equal("application/json", requestFormat.MediaType);
                Assert.Null(requestFormat.Formatter);
            }

            AssertApiDescriptionHasJsonRequestFormat(GetApiDescription(
                (InferredJsonType fromBody) => { }));

            AssertApiDescriptionHasJsonRequestFormat(GetApiDescription((
                [FromBody] int fromBody) => { }));
        }

        [Fact]
        public void ApiDescription_UsesMetadadataInsteadOfDefaultJsonRequestFormat()
        {
            static void AssertApiDescriptionHasCustomRequestFormat(ApiDescription apiDescription)
            {
                var requestFormat = Assert.Single(apiDescription.SupportedRequestFormats);
                Assert.Equal("application/custom", requestFormat.MediaType);
                Assert.Null(requestFormat.Formatter);
            }

            AssertApiDescriptionHasCustomRequestFormat(GetApiDescription(
                [Consumes("application/custom")]
                (InferredJsonType fromBody) => { }));

            AssertApiDescriptionHasCustomRequestFormat(GetApiDescription(
                [Consumes("application/custom")]
                ([FromBody] int fromBody) => { }));
        }

        [Fact]
        public void ApiDescription_AddsJsonResponseFormatWhenFromBodyInferred()
        {
            var apiDescription = GetApiDescription(() => new InferredJsonType());

            var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
            Assert.Equal(typeof(InferredJsonType), responseType.Type);
            Assert.Equal(typeof(InferredJsonType), responseType.ModelMetadata.ModelType);

            var responseFormat = Assert.Single(responseType.ApiResponseFormats);
            Assert.Equal("application/json", responseFormat.MediaType);
            Assert.Null(responseFormat.Formatter);
        }

        [Fact]
        public void ApiDescription_AddsTextResponseFormatWhenFromBodyInferred()
        {
            var apiDescription = GetApiDescription(() => "foo");

            var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
            Assert.Equal(typeof(string), responseType.Type);
            Assert.Equal(typeof(string), responseType.ModelMetadata.ModelType);

            var responseFormat = Assert.Single(responseType.ApiResponseFormats);
            Assert.Equal("text/plain", responseFormat.MediaType);
            Assert.Null(responseFormat.Formatter);
        }

        [Fact]
        public void ApiDescription_AddsNoResponseFormatWhenItCannotBeInferredAndTheresNoMetadata()
        {
            static void AssertApiDescriptionIsVoid(ApiDescription apiDescription)
            {
                var responseType = Assert.Single(apiDescription.SupportedResponseTypes);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Equal(typeof(void), responseType.ModelMetadata.ModelType);

                Assert.Empty(responseType.ApiResponseFormats);
            }

            AssertApiDescriptionIsVoid(GetApiDescription(() => { }));
            AssertApiDescriptionIsVoid(GetApiDescription(() => Task.CompletedTask));
            AssertApiDescriptionIsVoid(GetApiDescription(() => new ValueTask()));
        }

        private IList<ApiDescription> GetApiDescriptions(
            Delegate action,
            string pattern = null,
            IEnumerable<string> httpMethods = null)
        {
            var methodInfo = action.Method;
            var attributes = methodInfo.GetCustomAttributes();
            var context = new ApiDescriptionProviderContext(Array.Empty<ActionDescriptor>());

            var httpMethodMetadata = new HttpMethodMetadata(httpMethods ?? new[] { "GET" });
            var metadataItems = new List<object>(attributes) { methodInfo, httpMethodMetadata };
            var endpointMetadata = new EndpointMetadataCollection(metadataItems.ToArray());
            var routePattern = RoutePatternFactory.Pattern(pattern ?? "/");

            var endpoint = new RouteEndpoint(httpContext => Task.CompletedTask, routePattern, 0, endpointMetadata, null);
            var endpointDataSource = new DefaultEndpointDataSource(endpoint);

            var provider = new EndpointMetadataApiDescriptionProvider(endpointDataSource);

            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            return context.Results;
        }

        private ApiDescription GetApiDescription(Delegate action, string pattern = null) =>
            Assert.Single(GetApiDescriptions(action, pattern));

        private static void TestAction()
        {
        }

        private class InferredJsonType
        {
        }

        private interface IInferredServiceType
        {
        }
    }
}
