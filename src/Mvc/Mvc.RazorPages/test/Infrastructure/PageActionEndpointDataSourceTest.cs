// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionEndpointDataSourceTest : ActionEndpointDataSourceBaseTest
    {
        [Fact]
        public void Endpoints_Ignores_NonPage()
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
            };

            var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockDescriptorProvider.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(actions, 0));

            var dataSource = (PageActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }
        [Fact]
        public void Endpoints_AppliesConventions()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new PageActionDescriptor
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
            };

            var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockDescriptorProvider.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(actions, 0));

            var dataSource = (PageActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);

            dataSource.DefaultBuilder.Add((b) =>
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
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                });
        }

        private protected override ActionEndpointDataSourceBase CreateDataSource(IActionDescriptorCollectionProvider actions, ActionEndpointFactory endpointFactory)
        {
            return new PageActionEndpointDataSource(actions, endpointFactory);
        }

        protected override ActionDescriptor CreateActionDescriptor(
            object values,
            string pattern = null,
            IList<object> metadata = null)
        {
            var action = new PageActionDescriptor();

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
