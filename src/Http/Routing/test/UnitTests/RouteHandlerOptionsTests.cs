// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

public class RouteHandlerOptionsTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("DEVELOPMENT", true)]
    [InlineData("Production", false)]
    [InlineData("Custom", false)]
    public void ThrowOnBadRequestIsTrueIfInDevelopmentEnvironmentFalseOtherwise(string environmentName, bool expectedThrowOnBadRequest)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRouting();
        services.AddSingleton<IHostEnvironment>(new HostEnvironment
        {
            EnvironmentName = environmentName,
        });
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<RouteHandlerOptions>>().Value;
        Assert.Equal(expectedThrowOnBadRequest, options.ThrowOnBadRequest);
    }

    [Fact]
    public void ThrowOnBadRequestIsNotOverwrittenIfNotInDevelopmentEnvironment()
    {
        var services = new ServiceCollection();

        services.Configure<RouteHandlerOptions>(options =>
        {
            options.ThrowOnBadRequest = true;
        });

        services.AddSingleton<IHostEnvironment>(new HostEnvironment
        {
            EnvironmentName = "Production",
        });

        services.AddOptions();
        services.AddRouting();

        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<RouteHandlerOptions>>().Value;
        Assert.True(options.ThrowOnBadRequest);
    }

    [Fact]
    public void RouteHandlerOptionsCanResolveWithoutHostEnvironment()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRouting();
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<RouteHandlerOptions>>();
        Assert.False(options.Value.ThrowOnBadRequest);
    }

    private class HostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; }
    }
}
