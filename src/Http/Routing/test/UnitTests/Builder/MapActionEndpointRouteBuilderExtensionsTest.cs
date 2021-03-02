// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
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

            // This is tested separately because MapAction requires a Pattern and the other overloads forbit it.
            [CustomRouteMetadata(Pattern = "/", Name = customName, Order = customOrder)]
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

        [Theory]
        [MemberData(nameof(MapActionMethods))]
        public void MapOverloads_BuildsEndpointWithRouteNameAndOrder(Action<IEndpointRouteBuilder, Delegate> mapOverload)
        {
            const string customName = "Custom Name";
            const int customOrder = 1337;

            [CustomRouteMetadata(Name = customName, Order = customOrder)]
            void TestAction() { };

            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            mapOverload(builder, (Action)TestAction);

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal(customName, routeEndpointBuilder.DisplayName);
            Assert.Equal(customOrder, routeEndpointBuilder.Order);
        }

        [Fact]
        public void MapGet_BuildsEndpointWithRouteNameAndOrder()
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
        }

        [Fact]
        public void MapPost_BuildsEndpointWithRouteNameAndOrder()
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
        }

        [Fact]
        public void MapPut_BuildsEndpointWithRouteNameAndOrder()
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
        }

        [Fact]
        public void MapDelete_BuildsEndpointWithRouteNameAndOrder()
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
        }
 
        [Theory]
        [MemberData(nameof(MapActionMethods))]
        public void MapOverloads_RejectActionsWithPatternMetadata(Action<IEndpointRouteBuilder, Delegate> mapOverload)
        {
            [CustomRouteMetadata(Pattern = "/")]
            void TestAction() { };

            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            var ex = Assert.Throws<InvalidOperationException>(() => mapOverload(builder, (Action)TestAction));
            Assert.Contains(nameof(IRoutePatternMetadata), ex.Message);
        }

        [Theory]
        [MemberData(nameof(MapActionMethods))]
        public void MapOverloads_RejectActionsWithMethodMetadata(Action<IEndpointRouteBuilder, Delegate> mapOverload)
        {
            [CustomRouteMetadata(Methods = new[] { "GET" })]
            void TestAction() { };

            var builder = new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>());
            var ex = Assert.Throws<InvalidOperationException>(() => mapOverload(builder, (Action)TestAction));
            Assert.Contains(nameof(IHttpMethodMetadata), ex.Message);
        }

        public static IEnumerable<object[]> MapActionMethods => new object[][]
        {
            new object[]
            {
                (Action<IEndpointRouteBuilder, Delegate>)(
                    (builder, action) => builder.MapGet("/", action))
            },
            new object[]
            {
                (Action<IEndpointRouteBuilder, Delegate>)(
                    (builder, action) => builder.MapPost("/", action))
            },
            new object[]
            {
                (Action<IEndpointRouteBuilder, Delegate>)(
                    (builder, action) => builder.MapPut("/", action))
            },
            new object[]
            {
                (Action<IEndpointRouteBuilder, Delegate>)(
                    (builder, action) => builder.MapDelete("/", action))
            },
            new object[]
            {
                (Action<IEndpointRouteBuilder, Delegate>)(
                    (builder, action) => builder.MapMethods("/", Array.Empty<string>(), action))
            },
            new object[]
            {
                (Action<IEndpointRouteBuilder, Delegate>)(
                    (builder, action) => builder.Map("/", action))
            },
            new object[]
            {
                (Action<IEndpointRouteBuilder, Delegate>)(
                    (builder, action) => builder.Map(RoutePatternFactory.Parse("/"), action))
            },
        };

    }
}
