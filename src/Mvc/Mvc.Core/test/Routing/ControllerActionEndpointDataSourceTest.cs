// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Routing;

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

    [Fact]
    public void GroupedEndpoints_AppliesConventions_RouteSpecificMetadata()
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
                EndpointMetadata = new[] { "A" },
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
        var groupConventions = new List<Action<EndpointBuilder>>()
        {
            b => b.Metadata.Add(new GroupMetadata()),
            b => b.Metadata.Add("group")
        };
        var sp = Mock.Of<IServiceProvider>();
        var groupPattern = RoutePatternFactory.Parse("/group1");
        var endpoints = dataSource.GetGroupedEndpoints(new RouteGroupContext
        {
            Prefix = groupPattern,
            Conventions = groupConventions,
            ApplicationServices = sp
        });

        // Assert
        Assert.Collection(
            endpoints.OfType<RouteEndpoint>().Where(e => !SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
            e =>
            {
                Assert.Equal("/group1/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                Assert.Equal(new[] { "group", "Hi there", "A" }, e.Metadata.GetOrderedMetadata<string>());
                Assert.NotNull(e.Metadata.GetMetadata<GroupMetadata>());
            },
            e =>
            {
                Assert.Equal("/group1/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                Assert.Equal(new[] { "group", "Hi there", "B" }, e.Metadata.GetOrderedMetadata<string>());
                Assert.NotNull(e.Metadata.GetMetadata<GroupMetadata>());
            });

        Assert.Collection(
            endpoints.OfType<RouteEndpoint>().Where(e => SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
            e =>
            {
                Assert.Equal("/group1/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                // Group conventions are applied first, then endpoint specific metadata, then normal conventions, then per route conventions
                Assert.Equal(new[] { "group", "Hi there", "A" }, e.Metadata.GetOrderedMetadata<string>());
                Assert.NotNull(e.Metadata.GetMetadata<GroupMetadata>());
            },
            e =>
            {
                Assert.Equal("/group1/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                Assert.Null(e.Metadata.GetMetadata<ActionDescriptor>());
                Assert.Equal(new[] { "group", "Hi there", "B" }, e.Metadata.GetOrderedMetadata<string>());
                Assert.NotNull(e.Metadata.GetMetadata<GroupMetadata>());
            },
            e =>
            {
                Assert.Equal("/group1/test", e.RoutePattern.RawText);
                Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                // Group conventions are applied first, then endpoint specific metadata, then normal conventions
                Assert.Equal(new[] { "group", "A", "Hi there" }, e.Metadata.GetOrderedMetadata<string>());
                Assert.NotNull(e.Metadata.GetMetadata<GroupMetadata>());
            });
    }

    [Fact]
    public void Endpoints_AppliesFinallyConventions_InFIFOOrder_Last()
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
        var builder1 = dataSource.AddRoute("1", "/1/{controller}/{action}/{id?}", null, null, null);
        builder1.Finally(b => b.Metadata.Add("A1"));
        builder1.Finally(b => b.Metadata.Add("A2"));
        var builder2 = dataSource.AddRoute("2", "/2/{controller}/{action}/{id?}", null, null, null);
        builder2.Finally(b => b.Metadata.Add("B1"));
        builder2.Finally(b => b.Metadata.Add("B2"));

        dataSource.DefaultBuilder.Finally(b => b.Metadata.Add("C1"));
        dataSource.DefaultBuilder.Finally(b => b.Metadata.Add("C2"));

        // Act
        var endpoints = dataSource.Endpoints;

        // Assert
        Assert.Collection(
            endpoints.OfType<RouteEndpoint>().Where(e => SupportsLinkGeneration(e)).OrderBy(e => e.RoutePattern.RawText),
            e =>
            {
                Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                Assert.Equal(new[] { "A1", "A2", "C1", "C2" }, e.Metadata.GetOrderedMetadata<string>());
            },
            e =>
            {
                Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                Assert.Equal(new[] { "B1", "B2", "C1", "C2" }, e.Metadata.GetOrderedMetadata<string>());
            },
            e =>
            {
                Assert.Equal("/test", e.RoutePattern.RawText);
                Assert.Equal(new[] { "C1", "C2" }, e.Metadata.GetOrderedMetadata<string>());
            });
    }

    private class GroupMetadata { }

    private static bool SupportsLinkGeneration(RouteEndpoint endpoint)
    {
        return !(endpoint.Metadata.GetMetadata<ISuppressLinkGenerationMetadata>()?.SuppressLinkGeneration == true);
    }

    private protected override ActionEndpointDataSourceBase CreateDataSource(IActionDescriptorCollectionProvider actions, ActionEndpointFactory endpointFactory)
    {
        return new ControllerActionEndpointDataSource(
            new ControllerActionEndpointDataSourceIdProvider(),
            actions,
            endpointFactory,
            new OrderedEndpointsSequenceProvider());
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
