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
        [Fact]
        public void MapEndpoint_StringPattern_BuildsEndpoint()
        {
            // Arrange
            var builder = new DefaultEndpointDataSourceBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.MapEndpoint(requestDelegate, "/", "Display name!");

            // Assert
            Assert.Equal(endpointBuilder, Assert.Single(builder.Endpoints));
            Assert.Equal(requestDelegate, endpointBuilder.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder.DisplayName);
            Assert.Equal("/", endpointBuilder.RoutePattern.RawText);
        }

        [Fact]
        public void MapEndpoint_TypedPattern_BuildsEndpoint()
        {
            // Arrange
            var builder = new DefaultEndpointDataSourceBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.MapEndpoint(requestDelegate, RoutePatternFactory.Parse("/"), "Display name!");

            // Assert
            Assert.Equal(endpointBuilder, Assert.Single(builder.Endpoints));
            Assert.Equal(requestDelegate, endpointBuilder.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder.DisplayName);
            Assert.Equal("/", endpointBuilder.RoutePattern.RawText);
        }

        [Fact]
        public void MapEndpoint_StringPatternAndMetadata_BuildsEndpoint()
        {
            // Arrange
            var metadata = new object();
            var builder = new DefaultEndpointDataSourceBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.MapEndpoint(requestDelegate, "/", "Display name!", new[] { metadata });

            // Assert
            Assert.Equal(endpointBuilder, Assert.Single(builder.Endpoints));
            Assert.Equal(requestDelegate, endpointBuilder.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder.DisplayName);
            Assert.Equal("/", endpointBuilder.RoutePattern.RawText);
            Assert.Equal(metadata, Assert.Single(endpointBuilder.Metadata));
        }

        [Fact]
        public void MapEndpoint_TypedPatternAndMetadata_BuildsEndpoint()
        {
            // Arrange
            var metadata = new object();
            var builder = new DefaultEndpointDataSourceBuilder();
            RequestDelegate requestDelegate = (d) => null;

            // Act
            var endpointBuilder = builder.MapEndpoint(requestDelegate, RoutePatternFactory.Parse("/"), "Display name!", new[] { metadata });

            // Assert
            Assert.Equal(endpointBuilder, Assert.Single(builder.Endpoints));
            Assert.Equal(requestDelegate, endpointBuilder.RequestDelegate);
            Assert.Equal("Display name!", endpointBuilder.DisplayName);
            Assert.Equal("/", endpointBuilder.RoutePattern.RawText);
            Assert.Equal(metadata, Assert.Single(endpointBuilder.Metadata));
        }
    }
}
