// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Builder
{
    public class MapActionEndpointDataSourceBuilderExtensionsTest
    {
        private ModelEndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
        {
            return Assert.IsType<ModelEndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
        }

        private RouteEndpointBuilder GetRouteEndpointBuilder(IEndpointRouteBuilder endpointRouteBuilder)
        {
            return Assert.IsType<RouteEndpointBuilder>(Assert.Single(GetBuilderEndpointDataSource(endpointRouteBuilder).EndpointBuilders));
        }

        [Fact]
        public void MapEndpoint_PrecedenceOfMetadata_BuilderMetadataReturned()
        {
            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());

            [HttpMethod("ATTRIBUTE")]
            void TestAction()
            {
            }

            var endpointBuilder = builder.MapMethods("/", new[] { "METHOD" }, (Action)TestAction);
            endpointBuilder.WithMetadata(new HttpMethodMetadata(new[] { "BUILDER" }));

            var dataSource = Assert.Single(builder.DataSources);
            var endpoint = Assert.Single(dataSource.Endpoints);

            var metadataArray = endpoint.Metadata.Where(m => m is not CompilerGeneratedAttribute).ToArray();

            Assert.Equal(3, metadataArray.Length);
            Assert.Equal("ATTRIBUTE", GetMethod(metadataArray[0]));
            Assert.Equal("METHOD", GetMethod(metadataArray[1]));
            Assert.Equal("BUILDER", GetMethod(metadataArray[2]));

            Assert.Equal("BUILDER", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Single());

            string GetMethod(object metadata)
            {
                var httpMethodMetadata = Assert.IsAssignableFrom<IHttpMethodMetadata>(metadata);
                return Assert.Single(httpMethodMetadata.HttpMethods);
            }
        }

        [Fact]
        public void MapGet_BuildsEndpointWithCorrectMethod()
        {
            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            _ = builder.MapGet("/", (Action)(() => { }));

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal("GET", method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal("/ HTTP: GET", routeEndpointBuilder.DisplayName);
            Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
        }

        [Fact]
        public void MapPost_BuildsEndpointWithCorrectMethod()
        {
            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            _ = builder.MapPost("/", (Action)(() => { }));

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal("POST", method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal("/ HTTP: POST", routeEndpointBuilder.DisplayName);
            Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
        }

        [Fact]
        public void MapPut_BuildsEndpointWithCorrectMethod()
        {
            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            _ = builder.MapPut("/", (Action)(() => { }));

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal("PUT", method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal("/ HTTP: PUT", routeEndpointBuilder.DisplayName);
            Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
        }

        [Fact]
        public void MapDelete_BuildsEndpointWithCorrectMethod()
        {
            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            _ = builder.MapDelete("/", (Action)(() => { }));

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal("DELETE", method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal("/ HTTP: DELETE", routeEndpointBuilder.DisplayName);
            Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
        }

        private class HttpMethodAttribute : Attribute, IHttpMethodMetadata
        {
            public bool AcceptCorsPreflight => false;

            public IReadOnlyList<string> HttpMethods { get; }

            public HttpMethodAttribute(params string[] httpMethods)
            {
                HttpMethods = httpMethods;
            }
        }
    }
}
