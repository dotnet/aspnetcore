// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor;

public class RazorViewEngineOptionsTest
{
    [Fact]
    public void AreaViewLocationFormats_ContainsExpectedLocations()
    {
        // Arrange
        var services = new ServiceCollection().AddOptions();
        var areaViewLocations = new[]
        {
                "/Areas/{2}/MvcViews/{1}/{0}.cshtml",
                "/Areas/{2}/MvcViews/Shared/{0}.cshtml",
                "/MvcViews/Shared/{0}.cshtml"
            };
        var builder = new MvcBuilder(services, new ApplicationPartManager());
        builder.AddRazorOptions(options =>
        {
            options.AreaViewLocationFormats.Clear();

            foreach (var location in areaViewLocations)
            {
                options.AreaViewLocationFormats.Add(location);
            }
        });
        var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();

        // Act
        var formats = accessor.Value.AreaViewLocationFormats;

        // Assert
        Assert.Equal(areaViewLocations, formats, StringComparer.Ordinal);
    }

    [Fact]
    public void ViewLocationFormats_ContainsExpectedLocations()
    {
        // Arrange
        var services = new ServiceCollection().AddOptions();
        var viewLocations = new[]
        {
                "/MvcViews/{1}/{0}.cshtml",
                "/MvcViews/Shared/{0}.cshtml"
            };
        var builder = new MvcBuilder(services, new ApplicationPartManager());
        builder.AddRazorOptions(options =>
        {
            options.ViewLocationFormats.Clear();

            foreach (var location in viewLocations)
            {
                options.ViewLocationFormats.Add(location);
            }
        });
        var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();

        // Act
        var formats = accessor.Value.ViewLocationFormats;

        // Assert
        Assert.Equal(viewLocations, formats, StringComparer.Ordinal);
    }
}
