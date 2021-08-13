// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Builder
{
    public class MinimalActionEndpointDataSourceBuilderExtensionsTest
    {
        private ModelEndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
        {
            return Assert.IsType<ModelEndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
        }

        private RouteEndpointBuilder GetRouteEndpointBuilder(IEndpointRouteBuilder endpointRouteBuilder)
        {
            return Assert.IsType<RouteEndpointBuilder>(Assert.Single(GetBuilderEndpointDataSource(endpointRouteBuilder).EndpointBuilders));
        }

        public static object[][] MapMethods
        {
            get
            {
                void MapGet(IEndpointRouteBuilder routes, string template, Delegate action) =>
                    routes.MapGet(template, action);

                void MapPost(IEndpointRouteBuilder routes, string template, Delegate action) =>
                    routes.MapPost(template, action);

                void MapPut(IEndpointRouteBuilder routes, string template, Delegate action) =>
                    routes.MapPut(template, action);

                void MapDelete(IEndpointRouteBuilder routes, string template, Delegate action) =>
                    routes.MapDelete(template, action);

                return new object[][]
                {
                    new object[] { (Action<IEndpointRouteBuilder, string, Delegate>)MapGet, "GET" },
                    new object[] { (Action<IEndpointRouteBuilder, string, Delegate>)MapPost, "POST" },
                    new object[] { (Action<IEndpointRouteBuilder, string, Delegate>)MapPut, "PUT" },
                    new object[] { (Action<IEndpointRouteBuilder, string, Delegate>)MapDelete, "DELETE" },
                };
            }
        }

        [Fact]
        public void MapEndpoint_PrecedenceOfMetadata_BuilderMetadataReturned()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));

            [HttpMethod("ATTRIBUTE")]
            void TestAction()
            {
            }

            var endpointBuilder = builder.MapMethods("/", new[] { "METHOD" }, (Action)TestAction);
            endpointBuilder.WithMetadata(new HttpMethodMetadata(new[] { "BUILDER" }));

            var dataSource = Assert.Single(builder.DataSources);
            var endpoint = Assert.Single(dataSource.Endpoints);

            var metadataArray = endpoint.Metadata.OfType<IHttpMethodMetadata>().ToArray();

            static string GetMethod(IHttpMethodMetadata metadata) => Assert.Single(metadata.HttpMethods);

            Assert.Equal(3, metadataArray.Length);
            Assert.Equal("ATTRIBUTE", GetMethod(metadataArray[0]));
            Assert.Equal("METHOD", GetMethod(metadataArray[1]));
            Assert.Equal("BUILDER", GetMethod(metadataArray[2]));

            Assert.Equal("BUILDER", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Single());
        }

        [Fact]
        public void MapGet_BuildsEndpointWithCorrectMethod()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapGet("/", () => { });

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
        public async Task MapGetWithRouteParameter_BuildsEndpointWithRouteSpecificBinding()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapGet("/{id}", (int? id, HttpContext httpContext) =>
            {
                if (id is not null)
                {
                    httpContext.Items["input"] = id;
                }
            });

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal("GET", method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal("/{id} HTTP: GET", routeEndpointBuilder.DisplayName);
            Assert.Equal("/{id}", routeEndpointBuilder.RoutePattern.RawText);

            // Assert that we don't fallback to the query string
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = "42"
            });

            await endpoint.RequestDelegate!(httpContext);

            Assert.Null(httpContext.Items["input"]);
        }

        [Fact]
        public async Task MapGetWithoutRouteParameter_BuildsEndpointWithQuerySpecificBinding()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapGet("/", (int? id, HttpContext httpContext) =>
            {
                if (id is not null)
                {
                    httpContext.Items["input"] = id;
                }
            });

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

            // Assert that we don't fallback to the route values
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Query = new QueryCollection();
            httpContext.Request.RouteValues = new();
            httpContext.Request.RouteValues["id"] = "42";

            await endpoint.RequestDelegate!(httpContext);

            Assert.Null(httpContext.Items["input"]);
        }

        [Theory]
        [MemberData(nameof(MapMethods))]
        public async Task MapVerbWithExplicitRouteParameterIsCaseInsensitive(Action<IEndpointRouteBuilder, string, Delegate> map, string expectedMethod)
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));

            map(builder, "/{ID}", ([FromRoute] int? id, HttpContext httpContext) =>
            {
                if (id is not null)
                {
                    httpContext.Items["input"] = id;
                }
            });

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal($"/{{ID}} HTTP: {expectedMethod}", routeEndpointBuilder.DisplayName);
            Assert.Equal($"/{{ID}}", routeEndpointBuilder.RoutePattern.RawText);

            var httpContext = new DefaultHttpContext();

            httpContext.Request.RouteValues["id"] = "13";

            await endpoint.RequestDelegate!(httpContext);

            Assert.Equal(13, httpContext.Items["input"]);
        }

        [Theory]
        [MemberData(nameof(MapMethods))]
        public async Task MapVerbWithRouteParameterDoesNotFallbackToQuery(Action<IEndpointRouteBuilder, string, Delegate> map, string expectedMethod)
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));

            map(builder, "/{ID}", (int? id, HttpContext httpContext) =>
            {
                if (id is not null)
                {
                    httpContext.Items["input"] = id;
                }
            });

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal($"/{{ID}} HTTP: {expectedMethod}", routeEndpointBuilder.DisplayName);
            Assert.Equal($"/{{ID}}", routeEndpointBuilder.RoutePattern.RawText);

            // Assert that we don't fallback to the query string
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = "42"
            });

            await endpoint.RequestDelegate!(httpContext);

            Assert.Null(httpContext.Items["input"]);
        }

        [Fact]
        public void MapGetWithRouteParameter_ThrowsIfRouteParameterDoesNotExist()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            var ex = Assert.Throws<InvalidOperationException>(() => builder.MapGet("/", ([FromRoute] int id) => { }));
            Assert.Equal("id is not a route paramter.", ex.Message);
        }

        [Fact]
        public void MapPost_BuildsEndpointWithCorrectMethod()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapPost("/", () => { });

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
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapPut("/", () => { });

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
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapDelete("/", () => { });

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

        [Fact]
        public void MapFallback_BuildsEndpointWithLowestRouteOrder()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapFallback("/", () => { });

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal("Fallback /", routeEndpointBuilder.DisplayName);
            Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
            Assert.Equal(int.MaxValue, routeEndpointBuilder.Order);
        }

        [Fact]
        public void MapFallbackWithoutPath_BuildsEndpointWithLowestRouteOrder()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapFallback(() => { });

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal("Fallback {*path:nonfile}", routeEndpointBuilder.DisplayName);
            Assert.Equal("{*path:nonfile}", routeEndpointBuilder.RoutePattern.RawText);
            Assert.Single(routeEndpointBuilder.RoutePattern.Parameters);
            Assert.True(routeEndpointBuilder.RoutePattern.Parameters[0].IsCatchAll);
            Assert.Equal(int.MaxValue, routeEndpointBuilder.Order);
        }

        class FromRoute : Attribute, IFromRouteMetadata
        {
            public string? Name { get; set; }
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

        private class EmptyServiceProvdier : IServiceScope, IServiceProvider, IServiceScopeFactory
        {
            public IServiceProvider ServiceProvider => this;

            public IServiceScope CreateScope()
            {
                return new EmptyServiceProvdier();
            }

            public void Dispose()
            {

            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IServiceScopeFactory))
                {
                    return this;
                }
                return null;
            }
        }
    }
}
