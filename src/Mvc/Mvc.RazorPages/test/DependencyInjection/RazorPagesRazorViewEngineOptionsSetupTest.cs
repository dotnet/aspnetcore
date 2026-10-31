// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public class RazorPagesRazorViewEngineOptionsSetupTest
{
    [Fact]
    public void Configure_AddsPageViewLocationFormats_WhenPagesRootIsAppRoot()
    {
        // Arrange
        var expected = new[]
        {
                "/{1}/{0}.cshtml",
                "/Shared/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
            };

        var razorPagesOptions = new RazorPagesOptions
        {
            RootDirectory = "/"
        };
        var viewEngineOptions = GetViewEngineOptions();
        var setup = new RazorPagesRazorViewEngineOptionsSetup(
            Options.Options.Create(razorPagesOptions));

        // Act
        setup.Configure(viewEngineOptions);

        // Assert
        Assert.Equal(expected, viewEngineOptions.PageViewLocationFormats);
    }

    [Fact]
    public void Configure_AddsPageViewLocationFormats_WithDefaultPagesRoot()
    {
        // Arrange
        var expected = new[]
        {
                "/Pages/{1}/{0}.cshtml",
                "/Pages/Shared/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
            };

        var razorPagesOptions = new RazorPagesOptions();
        var viewEngineOptions = GetViewEngineOptions();
        var setup = new RazorPagesRazorViewEngineOptionsSetup(
            Options.Options.Create(razorPagesOptions));

        // Act
        setup.Configure(viewEngineOptions);

        // Assert
        Assert.Equal(expected, viewEngineOptions.PageViewLocationFormats);
    }

    [Fact]
    public void Configure_AddsAreaPageViewLocationFormats()
    {
        // Arrange
        var expected = new[]
        {
                "/Areas/{2}/Pages/{1}/{0}.cshtml",
                "/Areas/{2}/Pages/Shared/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml",
                "/Pages/Shared/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
            };

        var razorPagesOptions = new RazorPagesOptions();
        var viewEngineOptions = GetViewEngineOptions();
        var setup = new RazorPagesRazorViewEngineOptionsSetup(
            Options.Options.Create(razorPagesOptions));

        // Act
        setup.Configure(viewEngineOptions);

        // Assert
        Assert.Equal(expected, viewEngineOptions.AreaPageViewLocationFormats);
    }

    [Fact]
    public void Configure_WithCustomRoot_AddsAreaPageViewLocationFormats()
    {
        // Arrange
        var expected = new[]
        {
                "/Areas/{2}/Pages/{1}/{0}.cshtml",
                "/Areas/{2}/Pages/Shared/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml",
                "/RazorFiles/Shared/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
            };

        var razorPagesOptions = new RazorPagesOptions
        {
            RootDirectory = "/RazorFiles/",
        };
        var viewEngineOptions = GetViewEngineOptions();
        var setup = new RazorPagesRazorViewEngineOptionsSetup(
            Options.Options.Create(razorPagesOptions));

        // Act
        setup.Configure(viewEngineOptions);

        // Assert
        Assert.Equal(expected, viewEngineOptions.AreaPageViewLocationFormats);
    }

    [Fact]
    public void Configure_AddsSharedPagesDirectoryToViewLocationFormats()
    {
        // Arrange
        var expected = new[]
        {
                "/Views/{1}/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
                "/PagesRoot/Shared/{0}.cshtml",
            };

        var razorPagesOptions = new RazorPagesOptions
        {
            RootDirectory = "/PagesRoot",
        };
        var viewEngineOptions = GetViewEngineOptions();
        var setup = new RazorPagesRazorViewEngineOptionsSetup(
            Options.Options.Create(razorPagesOptions));

        // Act
        setup.Configure(viewEngineOptions);

        // Assert
        Assert.Equal(expected, viewEngineOptions.ViewLocationFormats);
    }

    [Fact]
    public void Configure_AddsSharedPagesDirectoryToAreaViewLocationFormats()
    {
        // Arrange
        var expected = new[]
        {
                "/Areas/{2}/Views/{1}/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
                "/PagesRoot/Shared/{0}.cshtml",
            };

        var razorPagesOptions = new RazorPagesOptions
        {
            RootDirectory = "/PagesRoot",
        };
        var viewEngineOptions = GetViewEngineOptions();
        var setup = new RazorPagesRazorViewEngineOptionsSetup(
            Options.Options.Create(razorPagesOptions));

        // Act
        setup.Configure(viewEngineOptions);

        // Assert
        Assert.Equal(expected, viewEngineOptions.AreaViewLocationFormats);
    }

    [Fact]
    public void Configure_RegistersPageViewLocationExpander()
    {
        // Arrange
        var viewEngineOptions = GetViewEngineOptions();
        var setup = new RazorPagesRazorViewEngineOptionsSetup(Options.Options.Create(new RazorPagesOptions()));

        // Act
        setup.Configure(viewEngineOptions);

        // Assert
        Assert.Collection(
            viewEngineOptions.ViewLocationExpanders,
            expander => Assert.IsType<PageViewLocationExpander>(expander));
    }

    private static RazorViewEngineOptions GetViewEngineOptions()
    {
        var defaultSetup = new RazorViewEngineOptionsSetup();
        var options = new RazorViewEngineOptions();
        defaultSetup.Configure(options);

        return options;
    }
}
