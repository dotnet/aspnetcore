// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class RequestSizeLimitFilterTest
{
    [Fact]
    public void SetsMaxRequestBodySize()
    {
        // Arrange
        var requestSizeLimitResourceFilter = new RequestSizeLimitFilter(NullLoggerFactory.Instance);
        requestSizeLimitResourceFilter.Bytes = 12345;
        var authorizationFilterContext = CreateAuthorizationFilterContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

        var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
        authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

        // Act
        requestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

        // Assert
        Assert.Equal(12345, httpMaxRequestBodySize.MaxRequestBodySize);
    }

    [Fact]
    public void SkipsWhenOverridden()
    {
        // Arrange
        var requestSizeLimitResourceFilter = new RequestSizeLimitFilter(NullLoggerFactory.Instance);
        requestSizeLimitResourceFilter.Bytes = 12345;
        var requestSizeLimitResourceFilterFinal = new RequestSizeLimitFilter(NullLoggerFactory.Instance);
        requestSizeLimitResourceFilterFinal.Bytes = 0;
        var authorizationFilterContext = CreateAuthorizationFilterContext(
            new IFilterMetadata[] { requestSizeLimitResourceFilter, requestSizeLimitResourceFilterFinal });

        var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
        authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

        // Act
        requestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);
        requestSizeLimitResourceFilterFinal.OnAuthorization(authorizationFilterContext);

        // Assert
        Assert.Equal(0, httpMaxRequestBodySize.MaxRequestBodySize);
        Assert.Equal(1, httpMaxRequestBodySize.Count);
    }

    [Fact]
    public void LogsFeatureNotFound()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var requestSizeLimitResourceFilter = new RequestSizeLimitFilter(loggerFactory);
        requestSizeLimitResourceFilter.Bytes = 12345;
        var authorizationFilterContext = CreateAuthorizationFilterContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

        // Act
        requestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

        // Assert
        var write = Assert.Single(sink.Writes);
        Assert.Equal($"A request body size limit could not be applied. This server does not support the IHttpRequestBodySizeFeature.",
            write.State.ToString());
    }

    [Fact]
    public void LogsFeatureIsReadOnly()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var requestSizeLimitResourceFilter = new RequestSizeLimitFilter(loggerFactory);
        requestSizeLimitResourceFilter.Bytes = 12345;
        var authorizationFilterContext = CreateAuthorizationFilterContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

        var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
        httpMaxRequestBodySize.IsReadOnly = true;
        authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

        // Act
        requestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

        // Assert
        var write = Assert.Single(sink.Writes);
        Assert.Equal($"A request body size limit could not be applied. The IHttpRequestBodySizeFeature for the server is read-only.", write.State.ToString());
    }

    [Fact]
    public void LogsMaxRequestBodySizeSet()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var requestSizeLimitResourceFilter = new RequestSizeLimitFilter(loggerFactory);
        requestSizeLimitResourceFilter.Bytes = 12345;
        var authorizationFilterContext = CreateAuthorizationFilterContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

        var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
        authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

        // Act
        requestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

        // Assert
        var write = Assert.Single(sink.Writes);
        Assert.Equal($"The maximum request body size has been set to 12345.", write.State.ToString());
    }

    private static AuthorizationFilterContext CreateAuthorizationFilterContext(IFilterMetadata[] filters)
    {
        return new AuthorizationFilterContext(CreateActionContext(), filters);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
    }

    private class TestHttpMaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        private long? _maxRequestBodySize;

        public bool IsReadOnly { get; set; }

        public long? MaxRequestBodySize
        {
            get
            {
                return _maxRequestBodySize;
            }
            set
            {
                _maxRequestBodySize = value;
                Count++;
            }
        }

        public int Count { get; set; }
    }
}
