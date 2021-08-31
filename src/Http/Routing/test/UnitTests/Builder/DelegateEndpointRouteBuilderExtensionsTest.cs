// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder
{
    public class DelegateEndpointRouteBuilderExtensionsTest
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

        [Theory]
        [MemberData(nameof(MapMethods))]
        public async Task MapVerbWithExplicitRouteParameterIsCaseInsensitive(Action<IEndpointRouteBuilder, string, Delegate> map, string expectedMethod)
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

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal($"HTTP: {expectedMethod} /{{ID}}", routeEndpointBuilder.DisplayName);
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

            var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
            Assert.NotNull(methodMetadata);
            var method = Assert.Single(methodMetadata!.HttpMethods);
            Assert.Equal(expectedMethod, method);

            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal($"HTTP: {expectedMethod} /{{ID}}", routeEndpointBuilder.DisplayName);
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
            Assert.Equal("id is not a route paramter.", ex.Message);
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

            var endpointMetadata = endpoint.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
            Assert.NotNull(endpointMetadata);
            Assert.Equal(2, endpointMetadata.Count);

            var lastAddedMetadata = endpointMetadata[^1];
  
            Assert.Equal(typeof(Todo), lastAddedMetadata.RequestType);
            Assert.Equal(new[] { "application/xml" }, lastAddedMetadata.ContentTypes);
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
        // This test scenario simulates methods defined in a top-level program
        // which are compiler generated. We currently do some manually parsing leveraging
        // code in Roslyn to support this scenario. More info at https://github.com/dotnet/roslyn/issues/55651.
        public void MapMethod_DoesNotEndpointNameForInnerMethod()
        {
            var name = "InnerGetString";
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
            string InnerGetString() => "TestString";
            _ = builder.MapDelete("/", InnerGetString);

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
            var routeName = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal(name, endpointName?.EndpointName);
            Assert.Equal(name, routeName?.RouteName);
            Assert.Equal("HTTP: DELETE / => InnerGetString", routeEndpointBuilder.DisplayName);
        }

        [Fact]
        public void MapMethod_DoesNotEndpointNameForInnerMethodWithTarget()
        {
            var name = "InnerGetString";
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
            var testString = "TestString";
            string InnerGetString() => testString;
            _ = builder.MapDelete("/", InnerGetString);

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
            var routeName = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal(name, endpointName?.EndpointName);
            Assert.Equal(name, routeName?.RouteName);
            Assert.Equal("HTTP: DELETE / => InnerGetString", routeEndpointBuilder.DisplayName);
        }


        [Fact]
        public void MapMethod_SetsEndpointNameForMethodGroup()
        {
            var name = "GetString";
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
            _ = builder.MapDelete("/", GetString);

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
            var routeName = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal(name, endpointName?.EndpointName);
            Assert.Equal(name, routeName?.RouteName);
            Assert.Equal("HTTP: DELETE / => GetString", routeEndpointBuilder.DisplayName);
        }

        [Fact]
        public void WithNameOverridesDefaultEndpointName()
        {
            var name = "SomeCustomName";
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
            _ = builder.MapDelete("/", GetString).WithName(name);

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
            var routeName = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            var routeEndpointBuilder = GetRouteEndpointBuilder(builder);
            Assert.Equal(name, endpointName?.EndpointName);
            Assert.Equal(name, routeName?.RouteName);
            // Will still use the original method name, not the custom endpoint name
            Assert.Equal("HTTP: DELETE / => GetString", routeEndpointBuilder.DisplayName);
        }

        private string GetString() => "TestString";

        [Fact]
        public void MapMethod_DoesNotSetEndpointNameForLambda()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
            _ = builder.MapDelete("/", () => { });

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
            Assert.Null(endpointName);
        }

        [Fact]
        public void WithTags_CanSetTagsForEndpoint()
        {
            var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvdier()));
            _ = builder.MapDelete("/", GetString).WithTags("Some", "Test", "Tags");

            var dataSource = GetBuilderEndpointDataSource(builder);
            // Trigger Endpoint build by calling getter.
            var endpoint = Assert.Single(dataSource.Endpoints);

            var tagsMetadata = endpoint.Metadata.GetMetadata<ITagsMetadata>();
            Assert.Equal(new[] { "Some", "Test", "Tags" }, tagsMetadata?.Tags);
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

            Type? _requestType;

            List<string> _contentTypes = new();
            
        }

        class Todo
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
}
