// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Microsoft.AspNetCore.Dispatcher
{
    // Not getting too in-depth with the tests here, the core URL generation is already tested elsewhere
    public class RoutePatternTemplateTest
    {
        public RoutePatternTemplateTest()
        {
            BinderFactory = new RoutePatternBinderFactory(UrlEncoder.Default, new DefaultObjectPoolProvider());
        }

        public RoutePatternBinderFactory BinderFactory { get; }

        [Fact]
        public void GetUrl_WithAllRequiredValues_GeneratesUrl()
        {
            // Arrange
            var template = new RoutePatternTemplate(BinderFactory.Create("api/products/{id}"));

            // Act
            var url = template.GetUrl(new DispatcherValueCollection(new { id = 17 }));

            // Assert
            Assert.Equal("/api/products/17", url);
        }

        [Fact]
        public void GetUrl_WithoutAllRequiredValues_GeneratesUrl()
        {
            // Arrange
            var template = new RoutePatternTemplate(BinderFactory.Create("api/products/{id}"));

            // Act
            var url = template.GetUrl(new DispatcherValueCollection(new { name = "billy" }));

            // Assert
            Assert.Null(url);
        }

        [Fact]
        public void GetUrl_WithAmbientValues_GeneratesUrl()
        {
            // Arrange
            var template = new RoutePatternTemplate(BinderFactory.Create("api/products/{id}/{name}"));

            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IDispatcherFeature>(new DispatcherFeature()
            {
                Values = new DispatcherValueCollection(new { id = 17 }),
            });

            // Act
            var url = template.GetUrl(httpContext, new DispatcherValueCollection(new { name = "billy" }));

            // Assert
            Assert.Equal("/api/products/17/billy", url);
        }
    }
}
