// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class ActionEndpointDataSourceTest : ActionEndpointDataSourceBaseTest
    {
        [Fact]
        public void Endpoints_MultipledActions_MultipleRoutes()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new ActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "/test",
                    },
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Test" },
                        { "controller", "Test" },
                    },
                },
                new ActionDescriptor
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Index" },
                        { "controller", "Home" },
                    },
                }
            };

            var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockDescriptorProvider
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actions, 0));

            var dataSource = (ActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);
            dataSource.AddRoute(new ConventionalRouteEntry("1", "/1/{controller}/{action}/{id?}", null, null, null));
            dataSource.AddRoute(new ConventionalRouteEntry("2", "/2/{controller}/{action}/{id?}", null, null, null));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.Cast<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                },
                e =>
                {
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                });
        }

        [Fact]
        public void Endpoints_AppliesConventions()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new ActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "/test",
                    },
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Test" },
                        { "controller", "Test" },
                    },
                },
                new ActionDescriptor
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Index" },
                        { "controller", "Home" },
                    },
                }
            };

            var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockDescriptorProvider.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(actions, 0));

            var dataSource = (ActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);
            dataSource.AddRoute(new ConventionalRouteEntry("1", "/1/{controller}/{action}/{id?}", null, null, null));
            dataSource.AddRoute(new ConventionalRouteEntry("2", "/2/{controller}/{action}/{id?}", null, null, null));

            dataSource.Add((b) =>
            {
                b.Metadata.Add("Hi there");
            });

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                },
                e =>
                {
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                });
        }

        private protected override ActionEndpointDataSourceBase CreateDataSource(IActionDescriptorCollectionProvider actions, ActionEndpointFactory endpointFactory)
        {
            return new ActionEndpointDataSource(actions, endpointFactory);
        }

        protected override ActionDescriptor CreateActionDescriptor(
            object values,
            string pattern = null,
            IList<object> metadata = null)
        {
            var action = new ActionDescriptor();

            foreach (var kvp in new RouteValueDictionary(values))
            {
                action.RouteValues[kvp.Key] = kvp.Value?.ToString();
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                action.AttributeRouteInfo = new AttributeRouteInfo
                {
                    Name = "test",
                    Template = pattern,
                };
            }

            action.EndpointMetadata = metadata;
            return action;
        }
    }
}
