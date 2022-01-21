// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder;

public class RouteHandlerEndpointRouteBuilderExtensionsTest
{
    private ModelEndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return Assert.IsType<ModelEndpointDataSource>(Assert.Single(endpointRouteBuilder.DataSources));
    }

    private RouteEndpointBuilder GetRouteEndpointBuilder(IEndpointRouteBuilder endpointRouteBuilder)
    {
        return Assert.IsType<RouteEndpointBuilder>(Assert.Single(GetBuilderEndpointDataSource(endpointRouteBuilder).EndpointBuilders));
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
        Assert.Equal("ATTRIBUTE", GetMethod(metadataArray[0]));
        Assert.Equal("METHOD", GetMethod(metadataArray[1]));
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
    public async Task MapGetWithRouteParameter_BuildsEndpointWithRouteSpecificBinding()
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
    public async Task MapGetWithoutRouteParameter_BuildsEndpointWithQuerySpecificBinding()
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
        var ex = Assert.Throws<InvalidOperationException>(() => builder.MapGet("/", (Todo todo) => { }));
        Assert.Contains("Body was inferred but the method does not allow inferred body parameters.", ex.Message);
        Assert.Contains("Did you mean to register the \"Body (Inferred)\" parameter(s) as a Service or apply the [FromServices] or [FromBody] attribute?", ex.Message);
    }

    [Fact]
    public void MapDelete_ThrowsWithImplicitFromBody()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        var ex = Assert.Throws<InvalidOperationException>(() => builder.MapDelete("/", (Todo todo) => { }));
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
        var ex = Assert.Throws<InvalidOperationException>(() => builder.MapMethods("/", new[] { method }, (Todo todo) => { }));
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
        var ex = Assert.Throws<InvalidOperationException>(() => builder.MapGet("/", ([FromRoute] int id) => { }));
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
        var ex = Assert.Throws<InvalidOperationException>(() => builder.MapGet("/{id}", ([FromRoute(Name = "value")] int id, HttpContext httpContext) => { }));
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

    class FromRoute : Attribute, IFromRouteMetadata
    {
        public string? Name { get; set; }
    }

    class TestConsumesAttribute : Attribute, IAcceptsMetadata
    {
        public TestConsumesAttribute(Type requestType, string contentType, params string[] otherContentTypes)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

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
