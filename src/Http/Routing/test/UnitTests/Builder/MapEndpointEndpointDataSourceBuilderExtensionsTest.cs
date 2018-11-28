// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
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
    }
}
