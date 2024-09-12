// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder;

public class RouteHandlerEndpointRouteBuilderExtensionsTest : LoggedTest
{
    private RouteEndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return Assert.IsType<RouteEndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
    }

    private RouteEndpointBuilder GetRouteEndpointBuilder(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return GetBuilderEndpointDataSource(endpointRouteBuilder).GetSingleRouteEndpointBuilder();
    }

    public static object?[]?[] MapMethods
    {
        get
        {
            IEndpointConventionBuilder MapGet(IEndpointRouteBuilder routes, string template, Delegate action) =>
                routes.MapGet(template, action);

            IEndpointConventionBuilder MapPost(IEndpointRouteBuilder routes, string template, Delegate action) =>
                routes.MapPost(template, action);

            IEndpointConventionBuilder MapPut(IEndpointRouteBuilder routes, string template, Delegate action) =>
                routes.MapPut(template, action);

            IEndpointConventionBuilder MapDelete(IEndpointRouteBuilder routes, string template, Delegate action) =>
                routes.MapDelete(template, action);

            IEndpointConventionBuilder MapPatch(IEndpointRouteBuilder routes, string template, Delegate action) =>
                routes.MapPatch(template, action);

            IEndpointConventionBuilder Map(IEndpointRouteBuilder routes, string template, Delegate action) =>
                routes.Map(template, action);

            return new object?[]?[]
            {
                    new object?[] { (Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder>)MapGet, "GET" },
                    new object?[] { (Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder>)MapPost, "POST" },
                    new object?[] { (Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder>)MapPut, "PUT" },
                    new object?[] { (Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder>)MapDelete, "DELETE" },
                    new object?[] { (Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder>)MapPatch, "PATCH" },
                    new object?[] { (Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder>)Map, null },
            };
        }
    }

    [Fact]
    public void MapEndpoint_PrecedenceOfMetadata_BuilderMetadataReturned()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));

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
        Assert.Equal("METHOD", GetMethod(metadataArray[0]));
        Assert.Equal("ATTRIBUTE", GetMethod(metadataArray[1]));
        Assert.Equal("BUILDER", GetMethod(metadataArray[2]));

        Assert.Equal("BUILDER", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Single());
    }

    [Fact]
    public void MapGet_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapGet("/", () => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: GET /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapPatch_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapPatch("/", () => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("PATCH", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: PATCH /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public async Task MapGet_WithRouteParameter_BuildsEndpointWithRouteSpecificBinding()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
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
        Assert.Equal("HTTP: GET /{id}", routeEndpointBuilder.DisplayName);
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
    public async Task MapGet_WithoutRouteParameter_BuildsEndpointWithQuerySpecificBinding()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
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
        Assert.Equal("HTTP: GET /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);

        // Assert that we don't fallback to the route values
        var httpContext = new DefaultHttpContext();

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>()
        {
            ["id"] = "41"
        });
        httpContext.Request.RouteValues = new();
        httpContext.Request.RouteValues["id"] = "42";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(41, httpContext.Items["input"]);
    }

    [Fact]
    public void MapGet_ThrowsWithImplicitFromBody()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapGet("/", (Todo todo) => { });
        var dataSource = GetBuilderEndpointDataSource(builder);
        var ex = Assert.Throws<InvalidOperationException>(() => dataSource.Endpoints);
        Assert.Contains("Body was inferred but the method does not allow inferred body parameters.", ex.Message);
        Assert.Contains("Did you mean to register the \"Body (Inferred)\" parameter(s) as a Service or apply the [FromServices] or [FromBody] attribute?", ex.Message);
    }

    [Fact]
    public void MapDelete_ThrowsWithImplicitFromBody()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapDelete("/", (Todo todo) => { });
        var dataSource = GetBuilderEndpointDataSource(builder);
        var ex = Assert.Throws<InvalidOperationException>(() => dataSource.Endpoints);
        Assert.Contains("Body was inferred but the method does not allow inferred body parameters.", ex.Message);
        Assert.Contains("Did you mean to register the \"Body (Inferred)\" parameter(s) as a Service or apply the [FromServices] or [FromBody] attribute?", ex.Message);
    }

    public static object[][] NonImplicitFromBodyMethods
    {
        get
        {
            return new[]
            {
                    new[] { HttpMethods.Delete },
                    new[] { HttpMethods.Connect },
                    new[] { HttpMethods.Trace },
                    new[] { HttpMethods.Get },
                    new[] { HttpMethods.Head },
                    new[] { HttpMethods.Options },
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonImplicitFromBodyMethods))]
    public void MapVerb_ThrowsWithImplicitFromBody(string method)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapMethods("/", new[] { method }, (Todo todo) => { });
        var dataSource = GetBuilderEndpointDataSource(builder);
        var ex = Assert.Throws<InvalidOperationException>(() => dataSource.Endpoints);
        Assert.Contains("Body was inferred but the method does not allow inferred body parameters.", ex.Message);
        Assert.Contains("Did you mean to register the \"Body (Inferred)\" parameter(s) as a Service or apply the [FromServices] or [FromBody] attribute?", ex.Message);
    }

    [Fact]
    public void MapGet_ImplicitFromService()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().AddSingleton<TodoService>().BuildServiceProvider()));
        _ = builder.MapGet("/", (TodoService todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: GET /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapDelete_ImplicitFromService()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().AddSingleton<TodoService>().BuildServiceProvider()));
        _ = builder.MapDelete("/", (TodoService todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("DELETE", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: DELETE /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapPatch_ImplicitFromService()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().AddSingleton<TodoService>().BuildServiceProvider()));
        _ = builder.MapPatch("/", (TodoService todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("PATCH", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: PATCH /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private class TestFromServiceAttribute : Attribute, IFromServiceMetadata
    { }

    [Fact]
    public void MapGet_ExplicitFromService()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().AddSingleton<TodoService>().BuildServiceProvider()));
        _ = builder.MapGet("/", ([TestFromServiceAttribute] TodoService todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: GET /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapDelete_ExplicitFromService()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().AddSingleton<TodoService>().BuildServiceProvider()));
        _ = builder.MapDelete("/", ([TestFromServiceAttribute] TodoService todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("DELETE", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: DELETE /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapPatch_ExplicitFromService()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().AddSingleton<TodoService>().BuildServiceProvider()));
        _ = builder.MapPatch("/", ([TestFromServiceAttribute] TodoService todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("PATCH", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: PATCH /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private class TestFromBodyAttribute : Attribute, IFromBodyMetadata
    { }

    [Fact]
    public void MapGet_ExplicitFromBody_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapGet("/", ([TestFromBody] Todo todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: GET /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapDelete_ExplicitFromBody_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapDelete("/", ([TestFromBody] Todo todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("DELETE", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: DELETE /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapPatch_ExplicitFromBody_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapPatch("/", ([TestFromBody] Todo todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("PATCH", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: PATCH /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public void MapVerbDoesNotDuplicateMetadata(Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder> map, string expectedMethod)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));

        map(builder, "/{ID}", () => { }).WithName("Foo");

        var dataSource = GetBuilderEndpointDataSource(builder);

        // Access endpoints a couple of times to make sure it gets built
        _ = dataSource.Endpoints;
        _ = dataSource.Endpoints;
        _ = dataSource.Endpoints;

        var endpoint = Assert.Single(dataSource.Endpoints);

        var endpointNameMetadata = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IEndpointNameMetadata>());
        var routeNameMetadata = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IRouteNameMetadata>());
        Assert.Equal("Foo", endpointNameMetadata.EndpointName);
        Assert.Equal("Foo", routeNameMetadata.RouteName);

        if (expectedMethod is not null)
        {
            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);
        }
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public void AddingMetadataAfterBuildingEndpointThrows(Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder> map, string expectedMethod)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));

        var endpointBuilder = map(builder, "/{ID}", () => { });

        var dataSource = GetBuilderEndpointDataSource(builder);

        var endpoint = Assert.Single(dataSource.Endpoints);

        if (expectedMethod is not null)
        {
            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);
        }

        Assert.Throws<InvalidOperationException>(() => endpointBuilder.WithMetadata(new RouteNameMetadata("Foo")));
        Assert.Throws<InvalidOperationException>(() => endpointBuilder.Finally(b => b.Metadata.Add(new RouteNameMetadata("Foo"))));
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task MapVerbWithExplicitRouteParameterIsCaseInsensitive(Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder> map, string expectedMethod)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));

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

        if (expectedMethod is not null)
        {
            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);
        }

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        if (expectedMethod is not null)
        {
            Assert.Equal($"HTTP: {expectedMethod} /{{ID}}", routeEndpointBuilder.DisplayName);
        }
        Assert.Equal($"/{{ID}}", routeEndpointBuilder.RoutePattern.RawText);

        var httpContext = new DefaultHttpContext();

        httpContext.Request.RouteValues["id"] = "13";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(13, httpContext.Items["input"]);
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task MapVerbWithRouteParameterDoesNotFallbackToQuery(Func<IEndpointRouteBuilder, string, Delegate, IEndpointConventionBuilder> map, string expectedMethod)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));

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
        if (expectedMethod is not null)
        {
            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);
        }

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        if (expectedMethod is not null)
        {
            Assert.Equal($"HTTP: {expectedMethod} /{{ID}}", routeEndpointBuilder.DisplayName);
        }
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
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapGet("/", ([FromRoute] int id) => { });
        var dataSource = GetBuilderEndpointDataSource(builder);
        var ex = Assert.Throws<InvalidOperationException>(() => dataSource.Endpoints);
        Assert.Equal("'id' is not a route parameter.", ex.Message);
    }

    [Fact]
    public async Task MapGetWithNamedFromRouteParameter_UsesFromRouteName()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapGet("/{value}", ([FromRoute(Name = "value")] int id, HttpContext httpContext) =>
        {
            httpContext.Items["value"] = id;
        });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        // Assert that we don't fallback to the query string
        var httpContext = new DefaultHttpContext();

        httpContext.Request.RouteValues["value"] = "42";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(42, httpContext.Items["value"]);
    }

    [Fact]
    public async Task MapGetWithNamedFromRouteParameter_FailsForParameterName()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapGet("/{value}", ([FromRoute(Name = "value")] int id, HttpContext httpContext) =>
        {
            httpContext.Items["value"] = id;
        });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        // Assert that we don't fallback to the query string
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        httpContext.Request.RouteValues["id"] = "42";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Null(httpContext.Items["value"]);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public void MapGetWithNamedFromRouteParameter_ThrowsForMismatchedPattern()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapGet("/{id}", ([FromRoute(Name = "value")] int id, HttpContext httpContext) => { });
        var dataSource = GetBuilderEndpointDataSource(builder);
        var ex = Assert.Throws<InvalidOperationException>(() => dataSource.Endpoints);
        Assert.Equal("'value' is not a route parameter.", ex.Message);
    }

    [Fact]
    public void MapPost_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapPost("/", () => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("POST", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: POST /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapPost_BuildsEndpointWithCorrectEndpointMetadata()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapPost("/", [TestConsumesAttribute(typeof(Todo), "application/xml")] (Todo todo) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var endpointMetadata = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();

        Assert.NotNull(endpointMetadata);
        Assert.False(endpointMetadata!.IsOptional);
        Assert.Equal(typeof(Todo), endpointMetadata.RequestType);
        Assert.Equal(new[] { "application/xml" }, endpointMetadata.ContentTypes);
    }

    [Fact]
    public void MapPut_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapPut("/", () => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("PUT", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: PUT /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapDelete_BuildsEndpointWithCorrectMethod()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapDelete("/", () => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("DELETE", method);

        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Equal("HTTP: DELETE /", routeEndpointBuilder.DisplayName);
        Assert.Equal("/", routeEndpointBuilder.RoutePattern.RawText);
    }

    [Fact]
    public void MapFallback_BuildsEndpointWithLowestRouteOrder()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
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
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
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
        Assert.Contains(FallbackMetadata.Instance, routeEndpointBuilder.Metadata);
    }

    [Fact]
    public void WithTags_CanSetTagsForEndpoint()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        string GetString() => "Foo";
        _ = builder.MapDelete("/", GetString).WithTags("Some", "Test", "Tags");

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var tagsMetadata = endpoint.Metadata.GetMetadata<ITagsMetadata>();
        Assert.Equal(new[] { "Some", "Test", "Tags" }, tagsMetadata?.Tags);
    }

    [Fact]
    public void MapMethod_DoesNotEndpointNameForMethodGroupByDefault()
    {
        string GetString() => "Foo";
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builder.MapDelete("/", GetString);

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
        var routeName = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
        var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
        Assert.Null(endpointName);
        Assert.Null(routeName);
        Assert.Equal("HTTP: DELETE / => GetString", routeEndpointBuilder.DisplayName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MapMethod_FlowsThrowOnBadHttpRequest(bool throwOnBadRequest)
    {
        var serviceProvider = new EmptyServiceProvider();
        serviceProvider.RouteHandlerOptions.ThrowOnBadRequest = throwOnBadRequest;

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        _ = builder.Map("/{id}", (int id) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        httpContext.Request.RouteValues["id"] = "invalid!";

        if (throwOnBadRequest)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => endpoint.RequestDelegate!(httpContext));
            Assert.Equal(400, ex.StatusCode);
        }
        else
        {
            await endpoint.RequestDelegate!(httpContext);
            Assert.Equal(400, httpContext.Response.StatusCode);
        }
    }

    [Fact]
    public async Task MapMethod_DefaultsToNotThrowOnBadHttpRequestIfItCannotResolveRouteHandlerOptions()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));

        _ = builder.Map("/{id}", (int id) => { });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        httpContext.Request.RouteValues["id"] = "invalid!";

        await endpoint.RequestDelegate!(httpContext);
        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    public static object[][] AddFiltersByClassData =
    {
        new object[] { (Action<IEndpointConventionBuilder>)((IEndpointConventionBuilder builder) => builder.AddEndpointFilter(new IncrementArgFilter())) },
        new object[] { (Action<IEndpointConventionBuilder>)((IEndpointConventionBuilder builder) => builder.AddEndpointFilter<IEndpointConventionBuilder, IncrementArgFilter>()) }
    };

    public static object[][] AddFiltersByDelegateData
    {
        get
        {
            void WithFilter(IEndpointConventionBuilder builder) =>
                builder.AddEndpointFilter(async (context, next) =>
                {
                    context.Arguments[0] = ((int)context.Arguments[0]!) + 1;
                    return await next(context);
                });

            void WithFilterFactory(IEndpointConventionBuilder builder) =>
                builder.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
                {
                    Assert.NotNull(routeHandlerContext.MethodInfo);
                    Assert.NotNull(routeHandlerContext.MethodInfo.DeclaringType);
                    Assert.NotNull(routeHandlerContext.ApplicationServices);
                    Assert.Equal("RouteHandlerEndpointRouteBuilderExtensionsTest", routeHandlerContext.MethodInfo.DeclaringType?.Name);
                    context.Arguments[0] = context.GetArgument<int>(0) + 1;
                    return await next(context);
                });

            return new object[][] {
                new object[] { (Action<IEndpointConventionBuilder>)WithFilter },
                new object[] { (Action<IEndpointConventionBuilder>)WithFilterFactory  }
            };
        }
    }

    private static async Task AssertIdAsync(Endpoint endpoint, string expectedPattern, int expectedId)
    {
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal(expectedPattern, routeEndpoint.RoutePattern.RawText);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        httpContext.Request.RouteValues["id"] = "2";
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        await routeEndpoint.RequestDelegate!(httpContext);

        // Assert;
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal($"ID: {expectedId}", body);
    }

    [Theory]
    [MemberData(nameof(AddFiltersByClassData))]
    [MemberData(nameof(AddFiltersByDelegateData))]
    public async Task AddEndpointFilterMethods_CanRegisterFilterWithClassAndDelegateImplementations(Action<IEndpointConventionBuilder> addFilter)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));

        string PrintId(int id) => $"ID: {id}";
        addFilter(builder.Map("/{id}", PrintId));

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);
        await AssertIdAsync(endpoint, "/{id}", 3);
    }

    [Theory]
    [MemberData(nameof(AddFiltersByClassData))]
    [MemberData(nameof(AddFiltersByDelegateData))]
    public async Task AddEndpointFilterMethods_WorkWithMapGroup(Action<IEndpointConventionBuilder> addFilter)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));

        string PrintId(int id) => $"ID: {id}";
        addFilter(builder.Map("/{id}", PrintId));

        var outerGroup = builder.MapGroup("/outer");
        addFilter(outerGroup);
        addFilter(outerGroup.Map("/{id}", PrintId));

        var innerGroup = outerGroup.MapGroup("/inner");
        addFilter(innerGroup);
        addFilter(innerGroup.Map("/{id}", PrintId));

        var endpoints = builder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .ToDictionary(e => ((RouteEndpoint)e).RoutePattern.RawText!);

        Assert.Equal(3, endpoints.Count);

        // For each layer of grouping, another filter is applies which increments the expectedId by 1 each time.
        await AssertIdAsync(endpoints["/{id}"], expectedPattern: "/{id}", expectedId: 3);
        await AssertIdAsync(endpoints["/outer/{id}"], expectedPattern: "/outer/{id}", expectedId: 4);
        await AssertIdAsync(endpoints["/outer/inner/{id}"], expectedPattern: "/outer/inner/{id}", expectedId: 5);
    }

    [Fact]
    public async Task RequestDelegateFactory_CanInvokeEndpointFilter_ThatAccessesServices()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));

        string? PrintLogger(HttpContext context) => $"loggerErrorIsEnabled: {context.Items["loggerErrorIsEnabled"]}, parentName: {context.Items["parentName"]}";
        var routeHandlerBuilder = builder.Map("/", PrintLogger);
        routeHandlerBuilder.AddEndpointFilter<ServiceAccessingEndpointFilter>();

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var httpContext = new DefaultHttpContext();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;
        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal("loggerErrorIsEnabled: True, parentName: RouteHandlerEndpointRouteBuilderExtensionsTest", body);
    }

    [Fact]
    public void RequestDelegateFactory_ProvidesAppServiceProvider_ToFilterFactory()
    {
        var appServiceCollection = new ServiceCollection();
        var appService = new MyService();
        appServiceCollection.AddSingleton(appService);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(appServiceCollection.BuildServiceProvider()));
        var filterFactoryRan = false;

        string? PrintLogger(HttpContext context) => $"loggerErrorIsEnabled: {context.Items["loggerErrorIsEnabled"]}, parentName: {context.Items["parentName"]}";
        var routeHandlerBuilder = builder.Map("/", PrintLogger);
        routeHandlerBuilder.AddEndpointFilterFactory((rhc, next) =>
        {
            Assert.NotNull(rhc.ApplicationServices);
            var myService = rhc.ApplicationServices.GetRequiredService<MyService>();
            Assert.Equal(appService, myService);
            filterFactoryRan = true;
            return next;
        });

        var dataSource = GetBuilderEndpointDataSource(builder);
        // Trigger Endpoint build by calling getter.
        Assert.Single(dataSource.Endpoints);
        Assert.True(filterFactoryRan);
    }

    [Fact]
    public void FinallyOnGroup_CanExamineFinallyOnEndpoint()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));

        var group = builder.MapGroup("/group");
        ((IEndpointConventionBuilder)group).Finally(b =>
        {
            if (b.Metadata.Any(md => md is string smd && smd == "added-from-endpoint"))
            {
                b.Metadata.Add("added-from-group");
            }
        });

        group.MapGet("/endpoint", () => { }).Finally(b => b.Metadata.Add("added-from-endpoint"));

        var endpoint = Assert.Single(builder.DataSources
            .SelectMany(ds => ds.Endpoints));

        Assert.Equal(new[] { "added-from-endpoint", "added-from-group" }, endpoint.Metadata.GetOrderedMetadata<string>());
    }

    [Fact]
    public void FinallyOnNestedGroups_OuterGroupCanExamineInnerGroup()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));

        var outerGroup = builder.MapGroup("/group");
        var innerGroup = outerGroup.MapGroup("/");
        ((IEndpointConventionBuilder)innerGroup).Finally(b =>
        {
            // Verifies that both endpoint-specific finally conventions have run
            if (b.Metadata.Any(md => md is string smd && smd == "added-from-endpoint-1")
                && b.Metadata.Any(md => md is string smd && smd == "added-from-endpoint-2"))
            {
                b.Metadata.Add("added-from-inner-group");
            }
        });
        ((IEndpointConventionBuilder)outerGroup).Finally(b =>
        {
            if (b.Metadata.Any(md => md is string smd && smd == "added-from-inner-group"))
            {
                b.Metadata.Add("added-from-outer-group");
            }
        });

        var handler = innerGroup.MapGet("/endpoint", () => { });
        handler.Finally(b => b.Metadata.Add("added-from-endpoint-1"));
        handler.Finally(b => b.Metadata.Add("added-from-endpoint-2"));

        var endpoint = Assert.Single(builder.DataSources
            .SelectMany(ds => ds.Endpoints));

        Assert.Equal(new[] { "added-from-endpoint-1", "added-from-endpoint-2", "added-from-inner-group", "added-from-outer-group" }, endpoint.Metadata.GetOrderedMetadata<string>());
    }

    class MyService { }

    class ServiceAccessingEndpointFilter : IEndpointFilter
    {
        private ILogger _logger;
        private EndpointFilterFactoryContext _routeHandlerContext;

        public ServiceAccessingEndpointFilter(ILoggerFactory loggerFactory, EndpointFilterFactoryContext routeHandlerContext)
        {
            _logger = loggerFactory.CreateLogger<ServiceAccessingEndpointFilter>();
            _routeHandlerContext = routeHandlerContext;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Items["loggerErrorIsEnabled"] = _logger.IsEnabled(LogLevel.Error);
            context.HttpContext.Items["parentName"] = _routeHandlerContext.MethodInfo.DeclaringType?.Name;
            return await next(context);
        }
    }

    class IncrementArgFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.Arguments[0] = ((int)context.Arguments[0]!) + 1;
            return await next(context);
        }
    }

    class FromRoute : Attribute, IFromRouteMetadata
    {
        public string? Name { get; set; }
    }

    class TestConsumesAttribute : Attribute, IAcceptsMetadata
    {
        public TestConsumesAttribute(Type requestType, string contentType, params string[] otherContentTypes)
        {
            ArgumentNullException.ThrowIfNull(contentType);

            var contentTypes = new List<string>()
                {
                    contentType
                };

            for (var i = 0; i < otherContentTypes.Length; i++)
            {
                contentTypes.Add(otherContentTypes[i]);
            }

            _requestType = requestType;
            _contentTypes = contentTypes;
        }

        IReadOnlyList<string> IAcceptsMetadata.ContentTypes => _contentTypes;
        Type? IAcceptsMetadata.RequestType => _requestType;

        bool IAcceptsMetadata.IsOptional => false;

        Type? _requestType;

        List<string> _contentTypes = new();
    }

    class Todo
    {

    }

    // Here to more easily disambiguate when ToDo is
    // intended to be validated as an implicit service in tests
    class TodoService
    {

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

    private class EmptyServiceProvider : IServiceScope, IServiceProvider, IServiceScopeFactory
    {
        public IServiceProvider ServiceProvider => this;

        public RouteHandlerOptions RouteHandlerOptions { get; set; } = new RouteHandlerOptions();

        public IServiceScope CreateScope()
        {
            return this;
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
            else if (serviceType == typeof(IOptions<RouteHandlerOptions>))
            {
                return Options.Create(RouteHandlerOptions);
            }

            return null;
        }
    }
}
