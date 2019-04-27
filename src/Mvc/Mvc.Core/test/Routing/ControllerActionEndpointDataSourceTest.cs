// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class ControllerActionEndpointDataSourceTest : ActionEndpointDataSourceBaseTest
    {
        [Fact]
        public void Endpoints_Ignores_NonController()
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

            var dataSource = (ControllerActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void Endpoints_MultipledActions_MultipleRoutes()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new ControllerActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "/test",
                        Name = "Test",
                    },
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Test" },
                        { "controller", "Test" },
                    },
                },
                new ControllerActionDescriptor
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

            var dataSource = (ControllerActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);
            dataSource.AddRoute("1", "/1/{controller}/{action}/{id?}", null, null, null);
            dataSource.AddRoute("2", "/2/{controller}/{action}/{id?}", null, null, null);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => !SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                });

            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("1", e.Metadata.GetMetadata<IRouteNameMetadata>().RouteName);
                    Assert.Equal("1", e.Metadata.GetMetadata<IEndpointNameMetadata>().EndpointName);
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("2", e.Metadata.GetMetadata<IRouteNameMetadata>().RouteName);
                    Assert.Equal("2", e.Metadata.GetMetadata<IEndpointNameMetadata>().EndpointName);
                },
                e =>
                {
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Test", e.Metadata.GetMetadata<IRouteNameMetadata>().RouteName);
                    Assert.Equal("Test", e.Metadata.GetMetadata<IEndpointNameMetadata>().EndpointName);
                });
        }

        [Fact]
        public void Endpoints_AppliesConventions()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new ControllerActionDescriptor
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
                new ControllerActionDescriptor
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

            var dataSource = (ControllerActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);
            dataSource.AddRoute("1", "/1/{controller}/{action}/{id?}", null, null, null);
            dataSource.AddRoute("2", "/2/{controller}/{action}/{id?}", null, null, null);

            dataSource.DefaultBuilder.Add((b) =>
            {
                b.Metadata.Add("Hi there");
            });

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => !SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
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
                });

            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                },
                e =>
                {
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                });
        }

        [Fact]
        public void Endpoints_AppliesConventions_CanOverideEndpointName()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new ControllerActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "/test",
                        Name = "Test",
                    },
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Test" },
                        { "controller", "Test" },
                    },
                },
                new ControllerActionDescriptor
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

            var dataSource = (ControllerActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);
            dataSource.AddRoute("1", "/1/{controller}/{action}/{id?}", null, null, null);
            dataSource.AddRoute("2", "/2/{controller}/{action}/{id?}", null, null, null);
            
            
            dataSource.DefaultBuilder.Add(b =>
            {
                if (b.Metadata.OfType<ActionDescriptor>().FirstOrDefault()?.AttributeRouteInfo != null)
                {
                    b.Metadata.Add(new EndpointNameMetadata("NewName"));
                }
            });

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => !SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                });

            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("1", e.Metadata.GetMetadata<IRouteNameMetadata>().RouteName);
                    Assert.Equal("1", e.Metadata.GetMetadata<IEndpointNameMetadata>().EndpointName);
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("2", e.Metadata.GetMetadata<IRouteNameMetadata>().RouteName);
                    Assert.Equal("2", e.Metadata.GetMetadata<IEndpointNameMetadata>().EndpointName);
                },
                e =>
                {
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Test", e.Metadata.GetMetadata<IRouteNameMetadata>().RouteName);
                    Assert.Equal("NewName", e.Metadata.GetMetadata<IEndpointNameMetadata>().EndpointName);
                });
        }

        [Fact]
        public void Endpoints_AppliesConventions_RouteSpecificMetadata()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new ControllerActionDescriptor
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
                new ControllerActionDescriptor
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

            var dataSource = (ControllerActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);
            dataSource.AddRoute("1", "/1/{controller}/{action}/{id?}", null, null, null).Add(b => b.Metadata.Add("A"));
            dataSource.AddRoute("2", "/2/{controller}/{action}/{id?}", null, null, null).Add(b => b.Metadata.Add("B"));

            dataSource.DefaultBuilder.Add((b) =>
            {
                b.Metadata.Add("Hi there");
            });

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => !SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal(new[] { "Hi there", "A" }, e.Metadata.GetOrderedMetadata<string>());
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal(new[] { "Hi there", "B" }, e.Metadata.GetOrderedMetadata<string>());
                });

            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().Where(e => SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
                e =>
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal(new[] { "Hi there", "A" }, e.Metadata.GetOrderedMetadata<string>());
                },
                e =>
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal(new[] { "Hi there", "B" }, e.Metadata.GetOrderedMetadata<string>());
                },
                e =>
                {
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                    Assert.Equal("Hi there", e.Metadata.GetMetadata<string>());
                });
        }

        private static bool SupportsLinkGeneration(RouteEndpoint endpoint)
        {
            return !(endpoint.Metadata.GetMetadata<ISuppressLinkGenerationMetadata>()?.SuppressLinkGeneration == true);
        }

        private protected override ActionEndpointDataSourceBase CreateDataSource(IActionDescriptorCollectionProvider actions, ActionEndpointFactory endpointFactory)
        {
            return new ControllerActionEndpointDataSource(actions, endpointFactory);
        }

        protected override ActionDescriptor CreateActionDescriptor(
            object values,
            string pattern = null,
            IList<object> metadata = null)
        {
            var action = new ControllerActionDescriptor();

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
