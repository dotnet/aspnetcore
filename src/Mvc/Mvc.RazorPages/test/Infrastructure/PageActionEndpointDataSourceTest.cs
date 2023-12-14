// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

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

    [Fact]
    public void GroupedEndpoints_AppliesConventions_RouteSpecificMetadata()
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
                EndpointMetadata = new List<object>() { "A" }
            },
            new PageActionDescriptor
            {
                AttributeRouteInfo = new AttributeRouteInfo()
                {
                    Template = "/test2",
                },
                RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "action", "Test" },
                    { "controller", "Test" },
                },
                EndpointMetadata = new List<object>() { "B" }
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
            endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText),
            e =>
            {
                Assert.Equal("/group1/test", e.RoutePattern.RawText);
                Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                Assert.Equal(new[] { "group", "A", "Hi there" }, e.Metadata.GetOrderedMetadata<string>());
                Assert.NotNull(e.Metadata.GetMetadata<GroupMetadata>());
            },
            e =>
            {
                Assert.Equal("/group1/test2", e.RoutePattern.RawText);
                Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                Assert.Equal(new[] { "group", "B", "Hi there" }, e.Metadata.GetOrderedMetadata<string>());
                Assert.NotNull(e.Metadata.GetMetadata<GroupMetadata>());
            });
    }

    [Fact]
    public void Endpoints_AppliesFinallyConventions_InFIFOOrder_Last()
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

        dataSource.DefaultBuilder.Finally((b) => b.Metadata.Add("A1"));
        dataSource.DefaultBuilder.Finally((b) => b.Metadata.Add("A2"));

        // Act
        var endpoints = dataSource.Endpoints;

        // Assert
        Assert.Collection(
            endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText),
            e =>
            {
                Assert.Equal("/test", e.RoutePattern.RawText);
                Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                Assert.Equal(new[] { "A1", "A2" }, e.Metadata.GetOrderedMetadata<string>());
            });
    }

    [Fact]
    public void Endpoints_FinallyConvention_CanObserveMetadata()
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
                    EndpointMetadata = new List<object>() { "initial-metadata" }
                },
            };

        var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
        mockDescriptorProvider.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(actions, 0));

        var dataSource = (PageActionEndpointDataSource)CreateDataSource(mockDescriptorProvider.Object);

        dataSource.DefaultBuilder.Finally((b) =>
        {
            if (b.Metadata.Any(md => md is string smd && smd == "initial-metadata"))
            {
                b.Metadata.Add("initial-metadata-observed");
            }
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
                Assert.Equal(new[] { "initial-metadata", "initial-metadata-observed" }, e.Metadata.GetOrderedMetadata<string>());
            });
    }

    private class GroupMetadata { }

    private protected override ActionEndpointDataSourceBase CreateDataSource(IActionDescriptorCollectionProvider actions, ActionEndpointFactory endpointFactory)
    {
        return new PageActionEndpointDataSource(new PageActionEndpointDataSourceIdProvider(), actions, endpointFactory, new OrderedEndpointsSequenceProvider());
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
