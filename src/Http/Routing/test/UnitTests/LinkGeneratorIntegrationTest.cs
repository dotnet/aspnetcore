// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

// This is a set of integration tests that are similar to a typical MVC configuration.
//
// We're doing this here because it's relatively expensive to test these scenarios
// inside MVC - it requires creating actual controllers and pages.
public class LinkGeneratorIntegrationTest : LinkGeneratorTestBase
{
    public LinkGeneratorIntegrationTest()
    {
        var endpoints = new List<Endpoint>()
            {
                // Attribute routed endpoint 1
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "api/Pets/{id}",
                        defaults: new { controller = "Pets", action = "GetById", },
                        parameterPolicies: null,
                        requiredValues: new { controller = "Pets", action = "GetById", area = (string)null, page = (string)null, }),
                    order: 0),

                // Attribute routed endpoint 2
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "api/Pets",
                        defaults: new { controller = "Pets", action = "GetAll", },
                        parameterPolicies: null,
                        requiredValues: new { controller = "Pets", action = "GetAll", area = (string)null, page = (string)null, }),
                    order: 0),

                // Attribute routed endpoint 2
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "api/Pets/{id}",
                        defaults: new { controller = "Pets", action = "Update", },
                        parameterPolicies: null,
                        requiredValues: new { controller = "Pets", action = "Update", area = (string)null, page = (string)null, }),
                    order: 0),

                // Attribute routed endpoint 4
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "api/Inventory/{searchTerm}/{page}",
                        defaults: new { controller = "Inventory", action = "Search", },
                        parameterPolicies: null,
                        requiredValues: new { controller = "Inventory", action = "Search", area = (string)null, page = (string)null, }),
                    order: 0),

                // Conventional routed endpoint 1
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "{controller=Home}/{action=Index}/{id?}",
                        defaults: null,
                        parameterPolicies: null,
                        requiredValues: new { controller = "Home", action = "Index", area = (string)null, page = (string)null, }),
                    order: 2000,
                    metadata: new object[] { new SuppressLinkGenerationMetadata(), }),

                // Conventional routed endpoint 2
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "{controller=Home}/{action=Index}/{id?}",
                        defaults: null,
                        parameterPolicies: null,
                        requiredValues: new { controller = "Home", action = "About", area = (string)null, page = (string)null, }),
                    order: 2000,
                    metadata: new object[] { new SuppressLinkGenerationMetadata(), }),

                // Conventional routed endpoint 3
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "{controller=Home}/{action=Index}/{id?}",
                        defaults: null,
                        parameterPolicies: null,
                        requiredValues: new { controller = "Store", action = "Browse", area = (string)null, page = (string)null, }),
                    order: 2000,
                    metadata: new object[] { new SuppressLinkGenerationMetadata(), }),

                // Conventional routed link generation route 1
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "{controller=Home}/{action=Index}/{id?}",
                        defaults: null,
                        parameterPolicies: null,
                        requiredValues: new { controller = RoutePattern.RequiredValueAny, action = RoutePattern.RequiredValueAny, area = (string)null, page = (string)null, }),
                    order: 2000,
                    metadata: new object[] { new SuppressMatchingMetadata(), }),

                // Conventional routed endpoint 4 (with area)
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Admin/{controller=Home}/{action=Index}/{id?}",
                        defaults: new { area = "Admin", },
                        parameterPolicies: new { controller = "Admin", },
                        requiredValues: new { area = "Admin", controller = "Users", action = "Add", page = (string)null, }),
                    order: 1000,
                    metadata: new object[] { new SuppressLinkGenerationMetadata(), }),

                // Conventional routed endpoint 5 (with area)
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Admin/{controller=Home}/{action=Index}/{id?}",
                        defaults: new { area = "Admin", },
                        parameterPolicies: new { controller = "Admin", },
                        requiredValues: new { area = "Admin", controller = "Users", action = "Remove", page = (string)null, }),
                    order: 1000,
                    metadata: new object[] { new SuppressLinkGenerationMetadata(), }),

                // Conventional routed link generation route 2
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Admin/{controller=Home}/{action=Index}/{id?}",
                        defaults: new { area = "Admin", },
                        parameterPolicies: new { area = "Admin", },
                        requiredValues: new { controller = RoutePattern.RequiredValueAny, action = RoutePattern.RequiredValueAny, area = "Admin", page = (string)null, }),
                    order: 1000,
                    metadata: new object[] { new SuppressMatchingMetadata(), }),

                // Conventional routed link generation route 3 - this doesn't match any actions.
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "api/{controller}/{id?}",
                        defaults: new { },
                        parameterPolicies: new { },
                        requiredValues: new { controller = RoutePattern.RequiredValueAny, action = (string)null, area = (string)null, page = (string)null, }),
                    order: 3000,
                    metadata: new object[] { new SuppressMatchingMetadata(), new RouteNameMetadata("custom"), }),

                // Conventional routed link generation route 3 - this doesn't match any actions.
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "api/Foo/{custom2}",
                        defaults: new { },
                        parameterPolicies: new { },
                        requiredValues: new { controller = (string)null, action = (string)null, area = (string)null, page = (string)null, }),
                    order: 3000,
                    metadata: new object[] { new SuppressMatchingMetadata(), new RouteNameMetadata("custom2"), }),

                // Razor Page 1 primary endpoint
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Pages",
                        defaults: new { page = "/Pages/Index", },
                        parameterPolicies: null,
                        requiredValues: new { controller = (string)null, action = (string)null, area = (string)null, page = "/Pages/Index", }),
                    order: 0),

                // Razor Page 1 secondary endpoint
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Pages/Index",
                        defaults: new { page = "/Pages/Index", },
                        parameterPolicies: null,
                        requiredValues: new { controller = (string)null, action = (string)null, area = (string)null, page = "/Pages/Index", }),
                    order: 0,
                    metadata: new object[] { new SuppressLinkGenerationMetadata(), }),

                // Razor Page 2 primary endpoint
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Pages/Help/{id?}",
                        defaults: new { page = "/Pages/Help", },
                        parameterPolicies: null,
                        requiredValues: new { controller = (string)null, action = (string)null, area = (string)null, page = "/Pages/Help", }),
                    order: 0),

                // Razor Page 3 primary endpoint
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Pages/About/{id?}",
                        defaults: new { page = "/Pages/About", },
                        parameterPolicies: null,
                        requiredValues: new { controller = (string)null, action = (string)null, area = (string)null, page = "/Pages/About", }),
                    order: 0),

                // Razor Page 4 with area primary endpoint
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Admin/Pages",
                        defaults: new { page = "/Pages/Index", area = "Admin", },
                        parameterPolicies: null,
                        requiredValues: new { controller = (string)null, action = (string)null, area = "Admin", page = "/Pages/Index", }),
                    order: 0),

                // Razor Page 4 with area secondary endpoint
                EndpointFactory.CreateRouteEndpoint(
                    RoutePatternFactory.Parse(
                        "Admin/Pages/Index",
                        defaults: new { page = "/Pages/Index", area = "Admin", },
                        parameterPolicies: null,
                        requiredValues: new { controller = (string)null, action = (string)null, area = "Admin", page = "/Pages/Index", }),
                    order: 0,
                    metadata: new object[] { new SuppressLinkGenerationMetadata(), }),
            };

        Endpoints = endpoints;
        LinkGenerator = CreateLinkGenerator(endpoints.ToArray());
    }

    private IReadOnlyList<Endpoint> Endpoints { get; }

    private LinkGenerator LinkGenerator { get; }

    #region Without ambient values (simple cases)

    [Fact]
    public void GetPathByAddress_LinkToAttributedAction_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Pets", action = "GetById", id = "17", };
        var ambientValues = new { };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/api/Pets/17", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalAction_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Home", action = "Index", };
        var ambientValues = new { };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalActionInArea_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { area = "Admin", controller = "Users", action = "Add", };
        var ambientValues = new { };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Admin/Users/Add", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalRoute_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Store", id = "17", };
        var ambientValues = new { };
        var address = CreateAddress(routeName: "custom", values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/api/Store/17", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToPage_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { page = "/Pages/Index", };
        var ambientValues = new { };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Pages", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToPageInArea_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { area = "Admin", page = "/Pages/Index", };
        var ambientValues = new { };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Admin/Pages", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToNonExistentAction_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Home", action = "Fake", id = "17", };
        var ambientValues = new { };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Home/Fake/17", path);
    }

    #endregion

    #region With ambient values

    [Fact]
    public void GetPathByAddress_LinkToAttributedAction_FromSameAction_KeepsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Pets", action = "GetById", };
        var ambientValues = new { controller = "Pets", action = "GetById", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/api/Pets/17", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToAttributedAction_FromAnotherAction_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Pets", action = "GetById", };
        var ambientValues = new { controller = "Pets", action = "Update", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Pets/GetById", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToAttributedAction_FromPage_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Pets", action = "GetById", };
        var ambientValues = new { page = "/Pages/Help", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Pets/GetById", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalAction_FromSameAction_KeepsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Home", action = "Index", };
        var ambientValues = new { controller = "Home", action = "Index", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Home/Index/17", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalAction_FromAnotherAction_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Home", action = "Index", };
        var ambientValues = new { controller = "Pets", action = "Update", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalAction_FromPage_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Home", action = "Index", };
        var ambientValues = new { page = "/Pages/Help", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToNonExistentConventionalAction_FromAnotherAction_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Home", action = "Index11", };
        var ambientValues = new { controller = "Pets", action = "Update", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Home/Index11", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToNonExistentAreaAction_FromAnotherAction_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { area = "Admin", controller = "Home", action = "Index11", };
        var ambientValues = new { controller = "Pets", action = "Update", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Admin/Home/Index11", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalRoute_FromAction_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Store", };
        var ambientValues = new { controller = "Home", action = "Index", id = "17", };
        var address = CreateAddress(routeName: "custom", values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/api/Store", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalRoute_WithAmbientValues_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { controller = "Store", id = "17", };
        var ambientValues = new { controller = "Store", };
        var address = CreateAddress(routeName: "custom", values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/api/Store/17", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToConventionalRouteWithoutSharedAmbientValues_WithAmbientValues_GeneratesPath()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { custom2 = "17", };
        var ambientValues = new { controller = "Store", };
        var address = CreateAddress(routeName: "custom2", values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/api/Foo/17", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToPage_FromSamePage_KeepsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { page = "/Pages/Help", };
        var ambientValues = new { page = "/Pages/Help", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Pages/Help/17", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToPage_FromAction_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { page = "/Pages/Help", };
        var ambientValues = new { controller = "Pets", action = "Update", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Pages/Help", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToPage_FromAnotherPage_DiscardsAmbientValues()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { page = "/Pages/Help", };
        var ambientValues = new { page = "/Pages/About", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Pages/Help", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToNonExistentPage_FromAction_MatchesActionConventionalRoute()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { page = "/Pages/Help2", };
        var ambientValues = new { controller = "Pets", action = "Update", id = "17", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Pets/Update?page=%2FPages%2FHelp2", path);
    }

    [Fact]
    public void GetPathByAddress_LinkToPageInSameArea_FromAction_UsingAreaAmbientValue()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        var values = new { page = "/Pages/Index", };
        var ambientValues = new { area = "Admin", controller = "Users", action = "Add", };
        var address = CreateAddress(values: values, ambientValues: ambientValues);

        // Act
        var path = LinkGenerator.GetPathByAddress(
            httpContext,
            address,
            address.ExplicitValues,
            address.AmbientValues);

        // Assert
        Assert.Equal("/Admin/Pages", path);
    }

    #endregion

    private static RouteValuesAddress CreateAddress(string routeName = null, object values = null, object ambientValues = null)
    {
        return new RouteValuesAddress()
        {
            RouteName = routeName,
            ExplicitValues = new RouteValueDictionary(values),
            AmbientValues = new RouteValueDictionary(ambientValues),
        };
    }
}
