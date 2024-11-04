// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder;

public class RequestDelegateEndpointRouteBuilderExtensionsTest
{
    private EndpointDataSource GetBuilderEndpointDataSource(IEndpointRouteBuilder endpointRouteBuilder) =>
        Assert.Single(endpointRouteBuilder.DataSources);

    private RouteEndpointBuilder GetRouteEndpointBuilder(IEndpointRouteBuilder endpointRouteBuilder) =>
        GetBuilderEndpointDataSource(endpointRouteBuilder) switch
        {
            RouteEndpointDataSource routeDataSource => routeDataSource.GetSingleRouteEndpointBuilder(),
            _ => throw new InvalidOperationException($"Unknown EndointDataSource type!"),
        };

    public static object[][] MapMethods
    {
        get
        {
            IEndpointConventionBuilder MapGet(IEndpointRouteBuilder routes, string template, RequestDelegate action) =>
                routes.MapGet(template, action);

            IEndpointConventionBuilder MapPost(IEndpointRouteBuilder routes, string template, RequestDelegate action) =>
                routes.MapPost(template, action);

            IEndpointConventionBuilder MapPut(IEndpointRouteBuilder routes, string template, RequestDelegate action) =>
                routes.MapPut(template, action);

            IEndpointConventionBuilder MapDelete(IEndpointRouteBuilder routes, string template, RequestDelegate action) =>
                routes.MapDelete(template, action);

            IEndpointConventionBuilder Map(IEndpointRouteBuilder routes, string template, RequestDelegate action) =>
                routes.Map(template, action);

            return new object[][]
            {
                new object[] { (Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder>)MapGet },
                new object[] { (Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder>)MapPost },
                new object[] { (Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder>)MapPut },
                new object[] { (Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder>)MapDelete },
                new object[] { (Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder>)Map },
            };
        }
    }

    [Fact]
    public void MapEndpoint_StringPattern_BuildsEndpoint()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        RequestDelegate requestDelegate = (d) => Task.CompletedTask;

        // Act
        var endpointBuilder = builder.Map("/", requestDelegate);

        // Assert
        var endpointBuilder1 = GetRouteEndpointBuilder(builder);

        Assert.Equal(requestDelegate, endpointBuilder1.RequestDelegate);
        Assert.Equal("/", endpointBuilder1.DisplayName);
        Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task MapEndpoint_ReturnGenericTypeTask_GeneratedDelegate(Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder> map)
    {
        var httpContext = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        static async Task<string> GenericTypeTaskDelegate(HttpContext context)
        {
            await context.Response.WriteAsync("Response string text");
            return await Task.FromResult("String Test");
        }

        // Act
        var endpointBuilder = map(builder, "/", GenericTypeTaskDelegate);

        // Assert
        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints); // Triggers build and construction of delegate

        Assert.NotNull(endpoint.RequestDelegate);
        var requestDelegate = endpoint.RequestDelegate!;
        await requestDelegate(httpContext);

        var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

        Assert.Equal("Response string text", responseBody);
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task MapEndpoint_CanBeFiltered_EndpointFilterFactory(Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder> map)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var httpContext = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        RequestDelegate initialRequestDelegate = static (context) => Task.CompletedTask;

        var endpointBuilder = map(builder, "/", initialRequestDelegate).AddEndpointFilterFactory(filterFactory: (routeHandlerContext, next) =>
        {
            return async invocationContext =>
            {
                Assert.IsAssignableFrom<HttpContext>(Assert.Single(invocationContext.Arguments));
                // Ignore thre result and write filtered because we can!
                _ = await next(invocationContext);
                return "filtered!";
            };
        });

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        Assert.NotSame(initialRequestDelegate, endpoint.RequestDelegate);

        Assert.NotNull(endpoint.RequestDelegate);
        var requestDelegate = endpoint.RequestDelegate!;
        await requestDelegate(httpContext);

        var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

        Assert.Equal("filtered!", responseBody);
    }

    [Fact]
    public async Task MapEndpoint_CanBeFiltered_EndpointFilter()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var httpContext = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        RequestDelegate initialRequestDelegate = static (context) => Task.CompletedTask;

        var endpointBuilder = builder.Map("/", initialRequestDelegate)
            .AddEndpointFilter(new HttpContextArgFilter("First"))
            .AddEndpointFilter(new HttpContextArgFilter("Second"));

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        Assert.NotSame(initialRequestDelegate, endpoint.RequestDelegate);

        Assert.NotNull(endpoint.RequestDelegate);
        var requestDelegate = endpoint.RequestDelegate!;
        await requestDelegate(httpContext);

        var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

        Assert.Equal("filtered!", responseBody);
        Assert.Equal(1, (int)httpContext.Items["First-Order"]!);
        Assert.Equal(2, (int)httpContext.Items["Second-Order"]!);
    }

    [Fact]
    public async Task MapEndpoint_Filtered_DontExecuteEndpointWhenErrorResponseStatus()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var httpContext = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        RequestDelegate initialRequestDelegate = static (context) =>
        {
            context.Items["ExecutedEndpoint"] = true;
            throw new Exception("Shouldn't reach here.");
        };

        var endpointBuilder = builder.Map("/", initialRequestDelegate)
            .AddEndpointFilterFactory(filterFactory: (routeHandlerContext, next) =>
            {
                return async invocationContext =>
                {
                    var httpContext = Assert.IsAssignableFrom<HttpContext>(Assert.Single(invocationContext.Arguments));
                    httpContext.Items["First"] = true;
                    httpContext.Response.StatusCode = 400;
                    return await next(invocationContext);
                };
            })
            .AddEndpointFilterFactory(filterFactory: (routeHandlerContext, next) =>
            {
                return invocationContext =>
                {
                    var httpContext = Assert.IsAssignableFrom<HttpContext>(Assert.Single(invocationContext.Arguments));
                    httpContext.Items["Second"] = true;
                    return next(invocationContext);
                };
            });

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        Assert.NotSame(initialRequestDelegate, endpoint.RequestDelegate);

        Assert.NotNull(endpoint.RequestDelegate);
        var requestDelegate = endpoint.RequestDelegate!;
        await requestDelegate(httpContext);

        Assert.True((bool)httpContext.Items["First"]!);
        Assert.True((bool)httpContext.Items["Second"]!);
        Assert.False(httpContext.Items.ContainsKey("ExecutedEndpoint"));
    }

    [Fact]
    public async Task RequestFilters_CanAssertOnEmptyResult()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var httpContext = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var @delegate = (HttpContext context) => context.Items.Add("param", "Value");

        object? response = null;
        var endpointBuilder = builder.Map("/", @delegate)
            .AddEndpointFilterFactory(filterFactory: (routeHandlerContext, next) =>
            {
                return async invocationContext =>
                {
                    response = await next(invocationContext);
                    return response;
                };
            });

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "Tester"
        });

        await endpoint.RequestDelegate!(httpContext);

        Assert.IsType<EmptyHttpResult>(response);
        Assert.Same(Results.Empty, response);
    }

    [Fact]
    public async Task RequestFilters_ReturnValue_SerializeJson()
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var httpContext = new DefaultHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        RequestDelegate requestDelegate = (HttpContext context) => Task.CompletedTask;

        var endpointBuilder = builder.Map("/", requestDelegate)
            .AddEndpointFilterFactory(filterFactory: (routeHandlerContext, next) =>
            {
                return async invocationContext =>
                {
                    await next(invocationContext);
                    return new MyCoolType(Name: "你好"); // serialized as JSON
                };
            });

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        await endpoint.RequestDelegate!(httpContext);

        var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(@"{""name"":""你好""}", responseBody);
    }

    private record struct MyCoolType(string Name);

    private sealed class HttpContextArgFilter : IEndpointFilter
    {
        private readonly string _name;

        public HttpContextArgFilter(string name)
        {
            _name = name;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            if (context.Arguments[0] is HttpContext httpContext)
            {
                int order;
                if (httpContext.Items["CurrentOrder"] is int)
                {
                    order = (int)httpContext.Items["CurrentOrder"]!;
                    order++;
                    httpContext.Items["CurrentOrder"] = order;
                }
                else
                {
                    order = 1;
                    httpContext.Items["CurrentOrder"] = order;
                }
                httpContext.Items[$"{_name}-Order"] = order;
            }

            // Ignore thre result and write filtered because we can!
            _ = await next(context);
            return "filtered!";
        }
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public void MapEndpoint_UsesOriginalRequestDelegateInstance_IfFilterDoesNotChangePerRequestBehavior(Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder> map)
    {
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        RequestDelegate initialRequestDelegate = static (context) => Task.CompletedTask;
        var runCount = 0;

        var endpointBuilder = map(builder, "/", initialRequestDelegate).AddEndpointFilterFactory((routeHandlerContext, next) =>
        {
            runCount++;
            return next;
        });

        var dataSource = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(dataSource.Endpoints);

        Assert.Same(initialRequestDelegate, endpoint.RequestDelegate);
        Assert.Equal(1, runCount);
    }

    [Fact]
    public void MapEndpoint_TypedPattern_BuildsEndpoint()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        RequestDelegate requestDelegate = (d) => Task.CompletedTask;

        // Act
        var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), requestDelegate);

        // Assert
        var endpointBuilder1 = GetRouteEndpointBuilder(builder);

        Assert.Equal(requestDelegate, endpointBuilder1.RequestDelegate);
        Assert.Equal("/", endpointBuilder1.DisplayName);
        Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
    }

    [Fact]
    public void MapEndpoint_AttributesCollectedAsMetadata()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        // Act
        var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), Handle);

        // Assert
        var endpointBuilder1 = GetRouteEndpointBuilder(builder);
        Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
        Assert.Equal(2, endpointBuilder1.Metadata.Count);
        Assert.IsType<Attribute1>(endpointBuilder1.Metadata[0]);
        Assert.IsType<Attribute2>(endpointBuilder1.Metadata[1]);
    }

    [Fact]
    public void MapEndpoint_GeneratedDelegateWorks()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        Expression<RequestDelegate> handler = context => Task.CompletedTask;

        // Act
        var endpointBuilder = builder.Map(RoutePatternFactory.Parse("/"), handler.Compile());

        // Assert
        var endpointBuilder1 = GetRouteEndpointBuilder(builder);
        Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
    }

    [Fact]
    public void MapEndpoint_PrecedenceOfMetadata_BuilderMetadataReturned()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        // Act
        var endpointBuilder = builder.MapMethods("/", new[] { "METHOD" }, HandleHttpMetdata);
        endpointBuilder.WithMetadata(new HttpMethodMetadata(new[] { "BUILDER" }));

        // Assert
        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        // As with the Delegate Map method overloads for route handlers, the attributes on the RequestDelegate
        // can override the HttpMethodMetadata. Extension methods could already do this.
        Assert.Equal(4, endpoint.Metadata.Count);
        Assert.Equal("METHOD", GetMethod(endpoint.Metadata[0]));
        Assert.Equal("ATTRIBUTE", GetMethod(endpoint.Metadata[1]));
        Assert.Equal("BUILDER", GetMethod(endpoint.Metadata[2]));
        Assert.IsAssignableFrom<IRouteDiagnosticsMetadata>(endpoint.Metadata[3]);

        Assert.Equal("BUILDER", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());

        string GetMethod(object metadata)
        {
            var httpMethodMetadata = Assert.IsAssignableFrom<IHttpMethodMetadata>(metadata);
            return Assert.Single(httpMethodMetadata.HttpMethods);
        }
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public void Map_EndpointMetadataNotDuplicated(Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder> map)
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        // Act
        var endpointBuilder = map(builder, "/", context => Task.CompletedTask).WithMetadata(new EndpointNameMetadata("MapMe"));

        // Assert
        var ds = GetBuilderEndpointDataSource(builder);

        _ = ds.Endpoints;
        _ = ds.Endpoints;
        _ = ds.Endpoints;

        Assert.Single(ds.Endpoints);
        var endpoint = ds.Endpoints.Single();

        Assert.Single(endpoint.Metadata.GetOrderedMetadata<IEndpointNameMetadata>());
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public void AddingMetadataAfterBuildingEndpointThrows(Func<IEndpointRouteBuilder, string, RequestDelegate, IEndpointConventionBuilder> map)
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        // Act
        var endpointBuilder = map(builder, "/", context => Task.CompletedTask).WithMetadata(new EndpointNameMetadata("MapMe"));

        // Assert
        var ds = GetBuilderEndpointDataSource(builder);

        var endpoint = Assert.Single(ds.Endpoints);

        Assert.Single(endpoint.Metadata.GetOrderedMetadata<IEndpointNameMetadata>());

        Assert.Throws<InvalidOperationException>(() => endpointBuilder.WithMetadata(new RouteNameMetadata("Foo")));
        Assert.Throws<InvalidOperationException>(() => endpointBuilder.Finally(b => b.Metadata.Add(new RouteNameMetadata("Foo"))));
    }

    [Fact]
    public void Map_AddsMetadata_InCorrectOrder()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));
        var @delegate = [Attribute1, Attribute2] (AddsCustomParameterMetadata param1) => new AddsCustomEndpointMetadataResult();

        // Act
        builder.Map("/test", @delegate);

        // Assert
        var ds = GetBuilderEndpointDataSource(builder);
        var endpoint = Assert.Single(ds.Endpoints);
        var metadata = endpoint.Metadata;

        Assert.Collection(metadata,
            m => Assert.IsAssignableFrom<MethodInfo>(m),
            m => Assert.IsAssignableFrom<IParameterBindingMetadata>(m),
            m => Assert.IsAssignableFrom<ParameterNameMetadata>(m),
            m =>
            {
                Assert.IsAssignableFrom<CustomEndpointMetadata>(m);
                Assert.Equal(MetadataSource.Parameter, ((CustomEndpointMetadata)m).Source);
            },
            m =>
            {
                Assert.IsAssignableFrom<CustomEndpointMetadata>(m);
                Assert.Equal(MetadataSource.ReturnType, ((CustomEndpointMetadata)m).Source);
            },
            m => Assert.Equal("System.Runtime.CompilerServices.NullableContextAttribute", m.ToString()),
            m => Assert.IsAssignableFrom<Attribute1>(m),
            m => Assert.IsAssignableFrom<Attribute2>(m),
            m => Assert.IsAssignableFrom<IRouteDiagnosticsMetadata>(m));
    }

    [Fact]
    public void MapEndpoint_Filter()
    {
        // Arrange
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(EmptyServiceProvider.Instance));

        // Act
        var endpointBuilder = builder
            .Map(RoutePatternFactory.Parse("/"), context => Task.CompletedTask)
            .AddEndpointFilter(new HttpContextArgFilter(""));

        // Assert
        var endpointBuilder1 = GetRouteEndpointBuilder(builder);
        Assert.Equal("/", endpointBuilder1.RoutePattern.RawText);
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

    private class AddsCustomEndpointMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.ReturnType });
        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class AddsCustomParameterMetadata : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
    {
        public static ValueTask<AddsCustomParameterMetadata?> BindAsync(HttpContext context, ParameterInfo parameter) => default;

        public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
        {
            builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name ?? string.Empty });
        }

        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Parameter });
        }
    }

    private class ParameterNameMetadata
    {
        public string Name { get; init; } = string.Empty;
    }

    private class CustomEndpointMetadata
    {
        public string Data { get; init; } = string.Empty;

        public MetadataSource Source { get; init; }
    }

    private enum MetadataSource
    {
        Parameter,
        ReturnType
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
