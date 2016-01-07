// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Internal;
using Microsoft.AspNet.Routing.Tree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRouteTest
    {
        private static readonly RequestDelegate NullHandler = (c) => Task.FromResult(0);

        // This test verifies that AttributeRoute can respond to changes in the AD collection. It does this
        // by running a successful request, then removing that action and verifying the next route isn't
        // successful.
        [Fact]
        public async Task AttributeRoute_UsesUpdatedActionDescriptors()
        {
            // Arrange
            var handler = new Mock<IRouter>(MockBehavior.Strict);
            handler
                .Setup(h => h.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(c => c.Handler = NullHandler)
                .Returns(Task.FromResult(true))
                .Verifiable();

            var actionDescriptors = new List<ActionDescriptor>()
            {
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Blog/{id}"
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "1"),
                    },
                },
                new ActionDescriptor()
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "api/Store/Buy/{id}"
                    },
                    RouteConstraints = new List<RouteDataActionConstraint>()
                    {
                        new RouteDataActionConstraint(TreeRouter.RouteGroupKey, "2"),
                    },
                },
            };

            var actionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>(MockBehavior.Strict);
            actionDescriptorProvider
                .SetupGet(ad => ad.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actionDescriptors, version: 1));

            var policy = new UriBuilderContextPooledObjectPolicy(new UrlTestEncoder());
            var pool = new DefaultObjectPool<UriBuildingContext>(policy);

            var route = new AttributeRoute(
                handler.Object,
                actionDescriptorProvider.Object,
                Mock.Of<IInlineConstraintResolver>(),
                pool,
                new UrlTestEncoder(),
                NullLoggerFactory.Instance);

            var requestServices = new Mock<IServiceProvider>(MockBehavior.Strict);
            requestServices
                .Setup(s => s.GetService(typeof(ILoggerFactory)))
                .Returns(NullLoggerFactory.Instance);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = new PathString("/api/Store/Buy/5");
            httpContext.RequestServices = requestServices.Object;

            var context = new RouteContext(httpContext);

            // Act 1
            await route.RouteAsync(context);

            // Assert 1
            Assert.NotNull(context.Handler);
            Assert.Equal("5", context.RouteData.Values["id"]);
            Assert.Equal("2", context.RouteData.Values[TreeRouter.RouteGroupKey]);

            handler.Verify(h => h.RouteAsync(It.IsAny<RouteContext>()), Times.Once());

            // Arrange 2 - remove the action and update the collection
            actionDescriptors.RemoveAt(1);
            actionDescriptorProvider
                .SetupGet(ad => ad.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actionDescriptors, version: 2));

            context = new RouteContext(httpContext);

            // Act 2
            await route.RouteAsync(context);

            // Assert 2
            Assert.Null(context.Handler);
            Assert.Empty(context.RouteData.Values);

            handler.Verify(h => h.RouteAsync(It.IsAny<RouteContext>()), Times.Once());
        }
    }
}
