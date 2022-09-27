// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class PageViewLocationExpanderTest
{
    [Fact]
    public void PopulateValues_DoesNothing()
    {
        // Arrange
        var context = CreateContext();

        var expander = new PageViewLocationExpander();

        // Act
        expander.PopulateValues(context);

        // Assert
        Assert.Empty(context.Values);
    }

    [Fact]
    public void ExpandLocations_NoOp_ForNonPage()
    {
        // Arrange
        var context = CreateContext(pageName: null);
        var locations = new string[]
        {
                "/ignore-me",
        };

        var expander = new PageViewLocationExpander();

        // Act
        var actual = expander.ExpandViewLocations(context, locations);

        // Assert
        Assert.Equal(locations, actual);
    }

    [Fact]
    public void ExpandLocations_NoOp_ForNonPageWithPageName()
    {
        // Verifies the fix for https://github.com/aspnet/Mvc/issues/6660. This ensures that when PageViewLocationExpander is called
        // from a non-Razor Page with a route value for "
        // Arrange
        var context = CreateContext(pageName: "test");
        context.ActionContext.ActionDescriptor = new ControllerActionDescriptor();
        var locations = new string[]
        {
                "/ignore-me",
        };

        var expander = new PageViewLocationExpander();

        // Act
        var actual = expander.ExpandViewLocations(context, locations);

        // Assert
        Assert.Equal(locations, actual);
    }

    [Fact]
    public void ExpandLocations_NoOp_WhenLocationDoesNotContainPageToken()
    {
        // Arrange
        var context = CreateContext(pageName: null);
        var locations = new string[]
        {
                "/ignore-me",
        };

        var expander = new PageViewLocationExpander();

        // Act
        var actual = expander.ExpandViewLocations(context, locations);

        // Assert
        Assert.Equal(locations, actual);
    }

    [Theory]
    [InlineData("/Index", new string[] { "/{0}.cshtml" })]
    [InlineData("/Edit", new string[] { "/{0}.cshtml" })]
    [InlineData("/Customers/Add", new string[] { "/Customers/{0}.cshtml", "/{0}.cshtml" })]
    public void ExpandLocations_ExpandsDirectories_WhenLocationContainsPage(
        string pageName,
        string[] expected)
    {
        // Arrange
        var context = CreateContext(pageName: pageName);

        var locations = new string[]
        {
                "/{1}/{0}.cshtml",
        };

        var expander = new PageViewLocationExpander();

        // Act
        var actual = expander.ExpandViewLocations(context, locations);

        // Assert
        Assert.Equal(expected, actual.ToArray());
    }

    [Fact]
    public void ExpandLocations_ExpandsDirectories_MultipleLocations()
    {
        // Arrange
        var context = CreateContext(pageName: "/Customers/Edit");

        var locations = new string[]
        {
                "/Pages/{1}/{0}.cshtml",
                "/More/Paths/{1}/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
        };

        var expected = new string[]
        {
                "/Pages/Customers/{0}.cshtml",
                "/Pages/{0}.cshtml",
                "/More/Paths/Customers/{0}.cshtml",
                "/More/Paths/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
        };

        var expander = new PageViewLocationExpander();

        // Act
        var actual = expander.ExpandViewLocations(context, locations);

        // Assert
        Assert.Equal(expected, actual.ToArray());
    }

    [Theory]
    [InlineData("/Index", new[] { "/Areas/{2}/Pages/{0}.cshtml" })]
    [InlineData("/Manage/User", new[] { "/Areas/{2}/Pages/Manage/{0}.cshtml", "/Areas/{2}/Pages/{0}.cshtml" })]
    public void ExpandLocations_ExpandsAreaPaths(string pageName, string[] expected)
    {
        // Arrange
        var context = CreateContext(pageName: pageName);
        var locations = new[]
        {
                "/Areas/{2}/Pages/{1}/{0}.cshtml",
            };

        var expander = new PageViewLocationExpander();

        // Act
        var actual = expander.ExpandViewLocations(context, locations);

        // Assert
        Assert.Equal(expected, actual.ToArray());
    }

    [Fact]
    public void ExpandLocations_ExpandsAreaPaths_MultipleLocations()
    {
        // Arrange
        var context = CreateContext(pageName: "/Customers/Edit");
        var locations = new[]
        {
                "/Areas/{2}/Pages/{1}/{0}.cshtml",
                "/Areas/{2}/Pages/Shared/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml",
                "/Areas/{2}/Pages/Shared/{0}.cshtml",
                "/User/Customized/{1}/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
                "/Pages/Shared/{0}.cshtml",
            };

        var expected = new[]
        {
                "/Areas/{2}/Pages/Customers/{0}.cshtml",
                "/Areas/{2}/Pages/{0}.cshtml",
                "/Areas/{2}/Pages/Shared/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml",
                "/Areas/{2}/Pages/Shared/{0}.cshtml",
                "/User/Customized/Customers/{0}.cshtml",
                "/User/Customized/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
                "/Pages/Shared/{0}.cshtml",
            };

        var expander = new PageViewLocationExpander();

        // Act
        var actual = expander.ExpandViewLocations(context, locations);

        // Assert
        Assert.Equal(expected, actual.ToArray());
    }

    private ViewLocationExpanderContext CreateContext(string viewName = "_LoginPartial.cshtml", string pageName = null)
    {
        var actionContext = new ActionContext
        {
            ActionDescriptor = new PageActionDescriptor(),
        };

        return new ViewLocationExpanderContext(
            actionContext,
            viewName,
            controllerName: null,
            areaName: null,
            pageName: pageName,
            isMainPage: true)
        {
            Values = new Dictionary<string, string>(),
        };
    }
}
