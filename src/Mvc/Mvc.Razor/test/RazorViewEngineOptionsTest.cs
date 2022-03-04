// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
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
}
