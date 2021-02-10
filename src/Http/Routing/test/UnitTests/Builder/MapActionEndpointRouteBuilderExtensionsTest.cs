// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.TestObjects;
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
        public void MapAction_BuildsEndpointFromAttributes()
        {
            const string customPattern = "/CustomTemplate";
            const string customMethod = "CUSTOM_METHOD";

            [CustomRouteMetadata(Pattern = customPattern, Methods = new[] { customMethod })]
            void TestAction() { };

            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            _ = builder.MapAction((Action)TestAction);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal(customPattern, routeEndpointBuilder.RoutePattern.RawText);

            var dataSource = GetBuilderEndpointDataSource(builder);
            var endpoint = Assert.Single(dataSource.Endpoints);

            var httpMethodMetadata = Assert.Single(endpoint.Metadata.OfType<IHttpMethodMetadata>());
            var method = Assert.Single(httpMethodMetadata.HttpMethods);
            Assert.Equal(customMethod, method);
        }

        [Fact]
        public void MapAction_BuildsEndpointWithRouteNameAndOrder()
        {
            const string customName = "Custom Name";
            const int customOrder = 1337;

            [CustomRouteMetadata(Name = customName, Order = customOrder)]
            void TestAction() { };

            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            _ = builder.MapAction((Action)TestAction);

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal(customName, routeEndpointBuilder.DisplayName);
            Assert.Equal(customOrder, routeEndpointBuilder.Order);
        }
    }
}
