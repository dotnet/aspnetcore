// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class MiddlewareFilterAttributeTest
{
    [Fact]
    public void CreatesMiddlewareFilter_WithConfiguredPipeline()
    {
        // Arrange
        var middlewareFilterAttribute = new MiddlewareFilterAttribute(typeof(Pipeline1));
        var services = new ServiceCollection();
        services.AddSingleton(new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider()));
        var serviceProvider = services.BuildServiceProvider();
        var filterBuilderService = serviceProvider.GetRequiredService<MiddlewareFilterBuilder>();
        filterBuilderService.ApplicationBuilder = new ApplicationBuilder(serviceProvider);
        var configureCallCount = 0;
        Pipeline1.ConfigurePipeline = (ab) =>
        {
            configureCallCount++;
            ab.Use((httpContext, next) =>
            {
                return next(httpContext);
            });
        };

        // Act
        var filter = middlewareFilterAttribute.CreateInstance(serviceProvider);

        // Assert
        var middlewareFilter = Assert.IsType<MiddlewareFilter>(filter);
        Assert.NotNull(middlewareFilter);
        Assert.Equal(1, configureCallCount);
    }

    private class Pipeline1
    {
        public static Action<IApplicationBuilder> ConfigurePipeline { get; set; }

        public void Configure(IApplicationBuilder appBuilder)
        {
            ConfigurePipeline(appBuilder);
        }
    }

    private class Pipeline2
    {
        public static Action<IApplicationBuilder> ConfigurePipeline { get; set; }

        public void Configure(IApplicationBuilder appBuilder)
        {
            ConfigurePipeline(appBuilder);
        }
    }
}
