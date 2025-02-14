// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing;

public class EndpointRoutingMiddlewareTest
{
    [Fact]
    public async Task Invoke_OnCall_SetsEndpointFeature()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var middleware = CreateMiddleware();

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        var endpointFeature = httpContext.Features.Get<IEndpointFeature>();
        Assert.NotNull(endpointFeature);
    }

    [Fact]
    public async Task Invoke_SkipsRouting_IfEndpointSet()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(), "myapp"));

        var middleware = CreateMiddleware();

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        var endpoint = httpContext.GetEndpoint();
        Assert.NotNull(endpoint);
        Assert.Equal("myapp", endpoint.DisplayName);
    }

    [Fact]
    public async Task Invoke_OnCall_WritesToConfiguredLogger()
    {
        // Arrange
        var expectedMessage = "Request matched endpoint 'Test endpoint'";
        bool eventFired = false;

        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var listener = new DiagnosticListener("TestListener");

        using var subscription = listener.Subscribe(new DelegateObserver(pair =>
        {
            eventFired = true;

            Assert.Equal("Microsoft.AspNetCore.Routing.EndpointMatched", pair.Key);
            Assert.IsAssignableFrom<HttpContext>(pair.Value);
        }));

        var httpContext = CreateHttpContext();

        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);
        var middleware = CreateMiddleware(logger, listener: listener);

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        Assert.Empty(sink.Scopes);
        var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "MatchSuccess"));
        Assert.Equal(expectedMessage, write.State?.ToString());
        Assert.True(eventFired);
    }

    [Fact]
    public async Task Invoke_BackCompatGetRouteValue_ValueUsedFromEndpointFeature()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var middleware = CreateMiddleware();

        // Act
        await middleware.Invoke(httpContext);
        var routeData = httpContext.GetRouteData();
        var routeValue = httpContext.GetRouteValue("controller");
        var routeValuesFeature = httpContext.Features.Get<IRouteValuesFeature>();

        // Assert
        Assert.NotNull(routeData);
        Assert.Equal("Home", (string)routeValue);

        // changing route data value is reflected in endpoint feature values
        routeData.Values["testKey"] = "testValue";
        Assert.Equal("testValue", routeValuesFeature.RouteValues["testKey"]);
    }

    [Fact]
    public async Task Invoke_BackCompatGetDataTokens_ValueUsedFromEndpointMetadata()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var middleware = CreateMiddleware();

        // Act
        await middleware.Invoke(httpContext);
        var routeData = httpContext.GetRouteData();
        var routeValue = httpContext.GetRouteValue("controller");
        var routeValuesFeature = httpContext.Features.Get<IRouteValuesFeature>();

        // Assert
        Assert.NotNull(routeData);
        Assert.Equal("Home", (string)routeValue);

        // changing route data value is reflected in endpoint feature values
        routeData.Values["testKey"] = "testValue";
        Assert.Equal("testValue", routeValuesFeature.RouteValues["testKey"]);
    }

    [Fact]
    public async Task Invoke_InitializationFailure_AllowsReinitialization()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var matcherFactory = new Mock<MatcherFactory>();
        matcherFactory
            .Setup(f => f.CreateMatcher(It.IsAny<EndpointDataSource>()))
            .Throws(new InvalidTimeZoneException())
            .Verifiable();

        var middleware = CreateMiddleware(matcherFactory: matcherFactory.Object);

        // Act
        await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await middleware.Invoke(httpContext));
        await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await middleware.Invoke(httpContext));

        // Assert
        matcherFactory
            .Verify(f => f.CreateMatcher(It.IsAny<EndpointDataSource>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ShortCircuitWithoutStatusCode()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var middleware = CreateMiddleware(
            matcherFactory: new ShortCircuitMatcherFactory(null, false, false),
            next: context =>
            {
                // should not be reached
                throw new Exception();
            });

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        Assert.True((bool)httpContext.Items["ShortCircuit"]);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ShortCircuitWithStatusCode()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var middleware = CreateMiddleware(
            matcherFactory: new ShortCircuitMatcherFactory(404, false, false),
            next: context =>
            {
                // should not be reached
                throw new Exception();
            });

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        Assert.True((bool)httpContext.Items["ShortCircuit"]);
        Assert.Equal(404, httpContext.Response.StatusCode);
    }

    [InlineData(404, true, true)]
    [InlineData(404, false, true)]
    [InlineData(404, true, false)]
    [InlineData(null, true, true)]
    [InlineData(null, false, true)]
    [InlineData(null, true, false)]
    [Theory]
    public async Task ThrowIfSecurityMetadataPresent(int? statusCode, bool hasAuthMetadata, bool hasCorsMetadata)
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var middleware = CreateMiddleware(
            matcherFactory: new ShortCircuitMatcherFactory(statusCode, hasAuthMetadata, hasCorsMetadata),
            next: context =>
            {
                // should not be reached
                throw new Exception();
            });

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(httpContext));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Invoke_CheckForFallbackMetadata_LogIfPresent(bool hasFallbackMetadata)
    {
        // Arrange
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var metadata = new List<object>();
        if (hasFallbackMetadata)
        {
            metadata.Add(FallbackMetadata.Instance);
        }

        var httpContext = CreateHttpContext();

        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(metadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        if (hasFallbackMetadata)
        {
            var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "FallbackMatch"));
            Assert.Equal("Matched endpoint 'myapp' is a fallback endpoint.", write.Message);
        }
        else
        {
            Assert.DoesNotContain(sink.Writes, w => w.EventId.Name == "FallbackMatch");
        }
    }

    [Fact]
    public async Task Endpoint_BodySizeFeatureIsReadOnly()
    {
        // Arrange
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var httpContext = CreateHttpContext();
        var expectedRequestSizeLimit = 50;
        var maxRequestBodySizeFeature = new FakeHttpMaxRequestBodySizeFeature(expectedRequestSizeLimit, true);
        httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(maxRequestBodySizeFeature);

        var metadata = new List<object> { new RequestSizeLimitMetadata(100) };
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(metadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "RequestSizeFeatureIsReadOnly"));
        Assert.Equal($"A request body size limit could not be applied. The {nameof(IHttpMaxRequestBodySizeFeature)} for the server is read-only.", write.Message);

        var actualRequestSizeLimit = maxRequestBodySizeFeature.MaxRequestBodySize;
        Assert.Equal(expectedRequestSizeLimit, actualRequestSizeLimit);
    }

    [Fact]
    public async Task Endpoint_DoesNotHaveBodySizeFeature()
    {
        // Arrange
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var httpContext = CreateHttpContext();

        var metadata = new List<object> { new RequestSizeLimitMetadata(100) };
        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(metadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "RequestSizeFeatureNotFound"));
        Assert.Equal($"A request body size limit could not be applied. This server does not support the {nameof(IHttpMaxRequestBodySizeFeature)}.", write.Message);
    }

    [Fact]
    public async Task Endpoint_DoesNotHaveSizeLimitMetadata()
    {
        // Arrange
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var httpContext = CreateHttpContext();
        var expectedRequestSizeLimit = 50;
        var maxRequestBodySizeFeature = new FakeHttpMaxRequestBodySizeFeature(expectedRequestSizeLimit, true);
        httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(maxRequestBodySizeFeature);

        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        // Assert
        var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "RequestSizeLimitMetadataNotFound"));
        Assert.Equal($"The endpoint does not specify the {nameof(IRequestSizeLimitMetadata)}.", write.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Endpoint_HasBodySizeFeature_SetUsingSizeLimitMetadata(bool isRequestSizeLimitDisabled)
    {
        // Arrange
        var sink = new TestSink(TestSink.EnableWithTypeName<EndpointRoutingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var logger = new Logger<EndpointRoutingMiddleware>(loggerFactory);

        var httpContext = CreateHttpContext();
        long? expectedRequestSizeLimit = isRequestSizeLimitDisabled ? null : 500L;
        var maxRequestBodySizeFeature = new FakeHttpMaxRequestBodySizeFeature(expectedRequestSizeLimit, false);
        httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(maxRequestBodySizeFeature);
        var metadata = new RequestSizeLimitMetadata(expectedRequestSizeLimit);

        var middleware = CreateMiddleware(
            logger: logger,
            matcherFactory: new TestMatcherFactory(isHandled: true, setEndpointCallback: c =>
            {
                c.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(metadata), "myapp"));
            }));

        // Act
        await middleware.Invoke(httpContext);

        // Assert

        if (isRequestSizeLimitDisabled)
        {
            var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "MaxRequestBodySizeDisabled"));
            Assert.Equal("The maximum request body size has been disabled.", write.Message);
        }
        else
        {
            var write = Assert.Single(sink.Writes.Where(w => w.EventId.Name == "MaxRequestBodySizeSet"));
            Assert.Equal($"The maximum request body size has been set to {expectedRequestSizeLimit}.", write.Message);
        }

        var actualRequestSizeLimit = maxRequestBodySizeFeature.MaxRequestBodySize;
        Assert.Equal(expectedRequestSizeLimit, actualRequestSizeLimit);
    }

    [Fact]
    public async Task Create_WithoutHostBuilder_Success()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<DiagnosticListener>(s => new DiagnosticListener("Test"));
        services.AddRouting();

        var applicationBuilder = new ApplicationBuilder(services.BuildServiceProvider());

        applicationBuilder.UseRouting();
        applicationBuilder.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", (HttpContext c) => Task.CompletedTask);
        });

        var requestDelegate = applicationBuilder.Build();

        // Act
        await requestDelegate(httpContext);

        // Assert
        var endpointFeature = httpContext.Features.Get<IEndpointFeature>();
        Assert.NotNull(endpointFeature);
    }

    private class RequestSizeLimitMetadata(long? maxRequestBodySize) : IRequestSizeLimitMetadata
    {

        public long? MaxRequestBodySize => maxRequestBodySize;
    }

    private class FakeHttpMaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public FakeHttpMaxRequestBodySizeFeature(
            long? maxRequestBodySize = null,
            bool isReadOnly = false)
        {
            MaxRequestBodySize = maxRequestBodySize;
            IsReadOnly = isReadOnly;
        }

        public bool IsReadOnly { get; }

        public long? MaxRequestBodySize { get; set; }
    }

    private static void AssertLog(WriteContext log, LogLevel level, string message)
    {
        Assert.Equal(level, log.LogLevel);
        Assert.Equal(message, log.State.ToString());
    }

    private HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new TestServiceProvider()
        };

        return httpContext;
    }

    private EndpointRoutingMiddleware CreateMiddleware(
        ILogger<EndpointRoutingMiddleware> logger = null,
        MatcherFactory matcherFactory = null,
        DiagnosticListener listener = null,
        RequestDelegate next = null)
    {
        next ??= c => Task.CompletedTask;
        logger ??= new Logger<EndpointRoutingMiddleware>(NullLoggerFactory.Instance);
        matcherFactory ??= new TestMatcherFactory(true);
        listener ??= new DiagnosticListener("Microsoft.AspNetCore");
        var metrics = new RoutingMetrics(new TestMeterFactory());

        var middleware = new EndpointRoutingMiddleware(
            matcherFactory,
            logger,
            new DefaultEndpointRouteBuilder(Mock.Of<IApplicationBuilder>()),
            new DefaultEndpointDataSource(),
            listener,
            Options.Create(new RouteOptions()),
            metrics,
            next);

        return middleware;
    }

    private class DelegateObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly Action<KeyValuePair<string, object>> _onNext;

        public DelegateObserver(Action<KeyValuePair<string, object>> onNext)
        {
            _onNext = onNext;
        }
        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            _onNext(value);
        }
    }
}
