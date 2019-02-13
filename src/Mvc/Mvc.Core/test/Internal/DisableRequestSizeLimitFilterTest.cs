// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class DisableRequestSizeLimitFilterTest
    {
        [Fact]
        public void SetsMaxRequestBodySizeToNull()
        {
            // Arrange
            var disableRequestSizeLimitResourceFilter = new DisableRequestSizeLimitFilter(NullLoggerFactory.Instance);
            var authorizationFilterContext = CreateauthorizationFilterContext(new IFilterMetadata[] { disableRequestSizeLimitResourceFilter });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            disableRequestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            Assert.Null(httpMaxRequestBodySize.MaxRequestBodySize);
        }

        [Fact]
        public void SkipsWhenOverridden()
        {
            // Arrange
            var disableRequestSizeLimitResourceFilter = new DisableRequestSizeLimitFilter(NullLoggerFactory.Instance);
            var disableRequestSizeLimitResourceFilterFinal = new DisableRequestSizeLimitFilter(NullLoggerFactory.Instance);
            var authorizationFilterContext = CreateauthorizationFilterContext(
                new IFilterMetadata[] { disableRequestSizeLimitResourceFilter, disableRequestSizeLimitResourceFilterFinal });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            disableRequestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);
            disableRequestSizeLimitResourceFilterFinal.OnAuthorization(authorizationFilterContext);

            // Assert
            Assert.Null(httpMaxRequestBodySize.MaxRequestBodySize);
            Assert.Equal(1, httpMaxRequestBodySize.Count);
        }

        [Fact]
        public void LogsFeatureNotFound()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var disableRequestSizeLimitResourceFilter = new DisableRequestSizeLimitFilter(loggerFactory);
            var authorizationFilterContext = CreateauthorizationFilterContext(new IFilterMetadata[] { disableRequestSizeLimitResourceFilter });

            // Act
            disableRequestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

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

            var disableRequestSizeLimitResourceFilter = new DisableRequestSizeLimitFilter(loggerFactory);
            var authorizationFilterContext = CreateauthorizationFilterContext(new IFilterMetadata[] { disableRequestSizeLimitResourceFilter });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            httpMaxRequestBodySize.IsReadOnly = true;
            authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            disableRequestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal($"A request body size limit could not be applied. The IHttpRequestBodySizeFeature for the server is read-only.", write.State.ToString());
        }

        [Fact]
        public void LogsMaxRequestBodySizeSetToNull()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var disableRequestSizeLimitResourceFilter = new DisableRequestSizeLimitFilter(loggerFactory);
            var authorizationFilterContext = CreateauthorizationFilterContext(new IFilterMetadata[] { disableRequestSizeLimitResourceFilter });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            authorizationFilterContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            disableRequestSizeLimitResourceFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal($"The request body size limit has been disabled.", write.State.ToString());
        }

        private static AuthorizationFilterContext CreateauthorizationFilterContext(IFilterMetadata[] filters)
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
}

