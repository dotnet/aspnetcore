// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Builder
{
    public class MapEndpointEndpointDataSourceBuilderExtensionsTest
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
        public void MapEndpoint_StringPattern_BuildsEndpoint()
        {
            // Arrange
            var builder = new DefaultEndpointRouteBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.Map("/", "Display name!", requestDelegate);

            // Assert
            var endpointBuilder1 = GetRouteEndpointBuilder(builder);

            Assert.Equal(requestDelegate, endpointBuilder1.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder1.DisplayName);
            Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
        }

        [Fact]
        public void MapEndpoint_TypedPattern_BuildsEndpoint()
        {
            // Arrange
            var builder = new DefaultEndpointRouteBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), "Display name!", requestDelegate);

            // Assert
            var endpointBuilder1 = GetRouteEndpointBuilder(builder);

            Assert.Equal(requestDelegate, endpointBuilder1.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder1.DisplayName);
            Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
        }

        [Fact]
        public void MapEndpoint_StringPatternAndMetadata_BuildsEndpoint()
        {
            // Arrange
            var metadata = new object();
            var builder = new DefaultEndpointRouteBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.Map("/", "Display name!", requestDelegate, new[] { metadata });

            // Assert
            var endpointBuilder1 = GetRouteEndpointBuilder(builder);
            Assert.Equal(requestDelegate, endpointBuilder1.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder1.DisplayName);
            Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
            Assert.Equal(metadata, Assert.Single(endpointBuilder1.Metadata));
        }

        [Fact]
        public void MapEndpoint_TypedPatternAndMetadata_BuildsEndpoint()
        {
            // Arrange
            var metadata = new object();
            var builder = new DefaultEndpointRouteBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), "Display name!", requestDelegate, new[] { metadata });

            // Assert
            var endpointBuilder1 = GetRouteEndpointBuilder(builder);
            Assert.Equal(requestDelegate, endpointBuilder1.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder1.DisplayName);
            Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
            Assert.Equal(metadata, Assert.Single(endpointBuilder1.Metadata));
        }

        [Fact]
        public void MapEndpoint_AttributesCollectedAsMetadata()
        {
            // Arrange
            var builder = new DefaultEndpointRouteBuilder();

            // Act
            var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), "Display name!", Handle);

            // Assert
            var endpointBuilder1 = GetRouteEndpointBuilder(builder);
            Assert.Equal("Display name!", endpointBuilder1.DisplayName);
            Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
            Assert.Equal(2, endpointBuilder1.Metadata.Count);
            Assert.IsType<Attribute1>(endpointBuilder1.Metadata[0]);
            Assert.IsType<Attribute2>(endpointBuilder1.Metadata[1]);
        }

        [Fact]
        public void MapEndpoint_ExplicitMetadataAddedAfterAttributeMetadata()
        {
            // Arrange
            var builder = new DefaultEndpointRouteBuilder();

            // Act
            var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), "Display name!", Handle, new Metadata());

            // Assert
            var endpointBuilder1 = GetRouteEndpointBuilder(builder);
            Assert.Equal("Display name!", endpointBuilder1.DisplayName);
            Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
            Assert.Equal(3, endpointBuilder1.Metadata.Count);
            Assert.IsType<Attribute1>(endpointBuilder1.Metadata[0]);
            Assert.IsType<Attribute2>(endpointBuilder1.Metadata[1]);
            Assert.IsType<Metadata>(endpointBuilder1.Metadata[2]);
        }

        [Fact]
        public void MapEndpoint_GeneratedDelegateWorks()
        {
            // Arrange
            var builder = new DefaultEndpointRouteBuilder();

            Expression<RequestDelegate> handler = context => Task.CompletedTask;

            // Act
            var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), "Display name!", handler.Compile(), new Metadata());

            // Assert
            var endpointBuilder1 = GetRouteEndpointBuilder(builder);
            Assert.Equal("Display name!", endpointBuilder1.DisplayName);
            Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
            Assert.Equal(1, endpointBuilder1.Metadata.Count);
            Assert.IsType<Metadata>(endpointBuilder1.Metadata[0]);
        }

        [Fact]
        public void MapEndpoint_PrecedenceOfMetadata_BuilderMetadataReturned()
        {
            // Arrange
            var builder = new DefaultEndpointRouteBuilder();

            // Act
            var endpointBuilder = builder.MapGet("/", HandleHttpMetdata, new HttpMethodMetadata(new[] { "METHOD" }));
            endpointBuilder.Add(b =>
            {
                b.Metadata.Add(new HttpMethodMetadata(new[] { "BUILDER" }));
            });

            // Assert
            var dataSource = Assert.Single(builder.DataSources);
            var endpoint = Assert.Single(dataSource.Endpoints);

            Assert.Equal(4, endpoint.Metadata.Count);
            Assert.Equal("ATTRIBUTE", GetMethod(endpoint.Metadata[0]));
            Assert.Equal("GET", GetMethod(endpoint.Metadata[1]));
            Assert.Equal("METHOD", GetMethod(endpoint.Metadata[2]));
            Assert.Equal("BUILDER", GetMethod(endpoint.Metadata[3]));

            Assert.Equal("BUILDER", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>().HttpMethods.Single());

            string GetMethod(object metadata)
            {
                var httpMethodMetadata = Assert.IsAssignableFrom<IHttpMethodMetadata>(metadata);
                return Assert.Single(httpMethodMetadata.HttpMethods);
            }
        }

        [Attribute1]
        [Attribute2]
        private static Task Handle(HttpContext context) => Task.CompletedTask;

        [HttpMethod("ATTRIBUTE")]
        private static Task HandleHttpMetdata(HttpContext context) => Task.CompletedTask;

        private class HttpMethodAttribute : Attribute, IHttpMethodMetadata
        {
            public bool AcceptCorsPreflight => false;

            public IReadOnlyList<string> HttpMethods { get; }

            public HttpMethodAttribute(params string[] httpMethods)
            {
                HttpMethods = httpMethods;
            }
        }

        private class Attribute1 : Attribute
        {
        }

        private class Attribute2 : Attribute
        {
        }

        private class Metadata
        {

        }
    }
}
