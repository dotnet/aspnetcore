// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class PageRouteModelFactoryTest
{
    [Fact]
    public void CreateRouteModel_AddsSelector()
    {
        // Arrange
        var relativePath = "/Pages/Users/Profile.cshtml";
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var routeModel = routeModelFactory.CreateRouteModel(relativePath, "{id?}");

        // Assert
        Assert.Equal(relativePath, routeModel.RelativePath);
        Assert.Equal("/Users/Profile", routeModel.ViewEnginePath);
        Assert.Null(routeModel.AreaName);

        Assert.Collection(
            routeModel.Selectors,
            selector => Assert.Equal("Users/Profile/{id?}", selector.AttributeRouteModel.Template));

        Assert.Collection(
            routeModel.RouteValues,
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Users/Profile", kvp.Value);
            });
    }

    [Fact]
    public void CreateRouteModel_AddsMultipleSelectorsForIndexPage()
    {
        // Arrange
        var relativePath = "/Pages/Users/Profile/Index.cshtml";
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var routeModel = routeModelFactory.CreateRouteModel(relativePath, "{id?}");

        // Assert
        Assert.Equal(relativePath, routeModel.RelativePath);
        Assert.Equal("/Users/Profile/Index", routeModel.ViewEnginePath);

        Assert.Collection(
            routeModel.Selectors,
            selector => Assert.Equal("Users/Profile/Index/{id?}", selector.AttributeRouteModel.Template),
            selector => Assert.Equal("Users/Profile/{id?}", selector.AttributeRouteModel.Template));

        Assert.Collection(
            routeModel.RouteValues,
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Users/Profile/Index", kvp.Value);
            });
    }

    [Fact]
    public void CreateRouteModel_DoesNotAddMultipleSelectorsWhenRouteTemplateIsAbsolute()
    {
        // Arrange
        var relativePath = "/Pages/Users/Profile/Index.cshtml";
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var routeModel = routeModelFactory.CreateRouteModel(relativePath, "/my-override");

        // Assert
        Assert.Equal(relativePath, routeModel.RelativePath);
        Assert.Equal("/Users/Profile/Index", routeModel.ViewEnginePath);

        Assert.Collection(
            routeModel.Selectors,
            selector => Assert.Equal("my-override", selector.AttributeRouteModel.Template));

        Assert.Collection(
            routeModel.RouteValues,
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Users/Profile/Index", kvp.Value);
            });
    }

    [Fact]
    public void CreateAreaRouteModel_AddsSelector()
    {
        // Arrange
        var relativePath = "/Areas/TestArea/Pages/Users/Profile.cshtml";
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var routeModel = routeModelFactory.CreateAreaRouteModel(relativePath, "{id?}");

        // Assert
        Assert.Equal(relativePath, routeModel.RelativePath);
        Assert.Equal("/Users/Profile", routeModel.ViewEnginePath);
        Assert.Equal("TestArea", routeModel.AreaName);

        Assert.Collection(
            routeModel.Selectors,
            selector => Assert.Equal("TestArea/Users/Profile/{id?}", selector.AttributeRouteModel.Template));

        Assert.Collection(
            routeModel.RouteValues.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal("TestArea", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Users/Profile", kvp.Value);
            });
    }

    [Fact]
    public void CreateAreaRouteModel_AddsMultipleSelectorsForIndexPage()
    {
        // Arrange
        var relativePath = "/Areas/TestArea/Pages/Users/Profile/Index.cshtml";
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var routeModel = routeModelFactory.CreateAreaRouteModel(relativePath, "{id?}");

        // Assert
        Assert.Equal(relativePath, routeModel.RelativePath);
        Assert.Equal("/Users/Profile/Index", routeModel.ViewEnginePath);
        Assert.Equal("TestArea", routeModel.AreaName);

        Assert.Collection(
            routeModel.Selectors,
            selector => Assert.Equal("TestArea/Users/Profile/Index/{id?}", selector.AttributeRouteModel.Template),
            selector => Assert.Equal("TestArea/Users/Profile/{id?}", selector.AttributeRouteModel.Template));

        Assert.Collection(
            routeModel.RouteValues.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("area", kvp.Key);
                Assert.Equal("TestArea", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Users/Profile/Index", kvp.Value);
            });
    }

    [Theory]
    [InlineData("/Areas/About.cshtml")]
    [InlineData("/Areas/MyArea/Index.cshtml")]
    public void TryParseAreaPath_ReturnsFalse_IfPathDoesNotConform(string path)
    {
        // Arrange
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var success = routeModelFactory.TryParseAreaPath(path, out _);

        // Assert
        Assert.False(success);
    }

    [Theory]
    [InlineData("/Areas/MyArea/Views/About.cshtml")]
    [InlineData("/Areas/MyArea/SubDir/Pages/Index.cshtml")]
    [InlineData("/Areas/MyArea/NotPages/SubDir/About.cshtml")]
    public void TryParseAreaPath_ReturnsFalse_IfPathDoesNotBelongToRootDirectory(string path)
    {
        // Arrange
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var success = routeModelFactory.TryParseAreaPath(path, out _);

        // Assert
        Assert.False(success);
    }

    [Theory]
    [InlineData("/Areas/MyArea/Pages/Index.cshtml", "MyArea", "/Index")]
    [InlineData("/Areas/Accounts/Pages/Manage/Edit.cshtml", "Accounts", "/Manage/Edit")]
    public void TryParseAreaPath_ParsesAreaPath(
        string path,
        string expectedArea,
        string expectedViewEnginePath)
    {
        // Arrange
        var options = new RazorPagesOptions();
        var routeModelFactory = new PageRouteModelFactory(options, NullLogger.Instance);

        // Act
        var success = routeModelFactory.TryParseAreaPath(path, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedArea, result.areaName);
        Assert.Equal(expectedViewEnginePath, result.viewEnginePath);
    }
}
