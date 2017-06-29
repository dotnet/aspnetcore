// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class RequestSizeLimitResourceFilterTest
    {
        [Fact]
        public void SetsMaxRequestBodySize()
        {
            // Arrange
            var requestSizeLimitResourceFilter = new RequestSizeLimitResourceFilter(NullLoggerFactory.Instance);
            requestSizeLimitResourceFilter.Bytes = 12345;
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            resourceExecutingContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            requestSizeLimitResourceFilter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.Equal(12345, httpMaxRequestBodySize.MaxRequestBodySize);
        }

        [Fact]
        public void SkipsWhenOverridden()
        {
            // Arrange
            var requestSizeLimitResourceFilter = new RequestSizeLimitResourceFilter(NullLoggerFactory.Instance);
            requestSizeLimitResourceFilter.Bytes = 12345;
            var requestSizeLimitResourceFilterFinal = new RequestSizeLimitResourceFilter(NullLoggerFactory.Instance);
            requestSizeLimitResourceFilterFinal.Bytes = 0;
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilterMetadata[] { requestSizeLimitResourceFilter, requestSizeLimitResourceFilterFinal });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            resourceExecutingContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            requestSizeLimitResourceFilter.OnResourceExecuting(resourceExecutingContext);
            requestSizeLimitResourceFilterFinal.OnResourceExecuting(resourceExecutingContext);

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

            var requestSizeLimitResourceFilter = new RequestSizeLimitResourceFilter(loggerFactory);
            requestSizeLimitResourceFilter.Bytes = 12345;
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

            // Act
            requestSizeLimitResourceFilter.OnResourceExecuting(resourceExecutingContext);

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

            var requestSizeLimitResourceFilter = new RequestSizeLimitResourceFilter(loggerFactory);
            requestSizeLimitResourceFilter.Bytes = 12345;
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            httpMaxRequestBodySize.IsReadOnly = true;
            resourceExecutingContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            requestSizeLimitResourceFilter.OnResourceExecuting(resourceExecutingContext);

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

            var requestSizeLimitResourceFilter = new RequestSizeLimitResourceFilter(loggerFactory);
            requestSizeLimitResourceFilter.Bytes = 12345;
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilterMetadata[] { requestSizeLimitResourceFilter });

            var httpMaxRequestBodySize = new TestHttpMaxRequestBodySizeFeature();
            resourceExecutingContext.HttpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(httpMaxRequestBodySize);

            // Act
            requestSizeLimitResourceFilter.OnResourceExecuting(resourceExecutingContext);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal($"The maximum request body size has been set to 12345.", write.State.ToString());
        }

        private static ResourceExecutingContext CreateResourceExecutingContext(IFilterMetadata[] filters)
        {
            return new ResourceExecutingContext(
                CreateActionContext(),
                filters,
                new List<IValueProviderFactory>());
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
