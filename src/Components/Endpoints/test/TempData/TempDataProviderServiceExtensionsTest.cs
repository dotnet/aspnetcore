// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class TempDataProviderServiceCollectionExtensionsTest
{
    [Fact]
    public void AddCookieTempDataValueProvider_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddLogging();

        // Act
        services.AddCookieTempDataValueProvider();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tempDataProvider = serviceProvider.GetService<ITempDataProvider>();
        var tempDataSerializer = serviceProvider.GetService<ITempDataSerializer>();
        var tempDataService = serviceProvider.GetService<TempDataService>();

        Assert.NotNull(tempDataProvider);
        Assert.NotNull(tempDataSerializer);
        Assert.NotNull(tempDataService);
    }

    [Fact]
    public void AddSessionStorageTempDataValueProvider_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSessionStorageTempDataValueProvider();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tempDataProvider = serviceProvider.GetService<ITempDataProvider>();
        var tempDataSerializer = serviceProvider.GetService<ITempDataSerializer>();
        var tempDataService = serviceProvider.GetService<TempDataService>();

        Assert.NotNull(tempDataProvider);
        Assert.NotNull(tempDataSerializer);
        Assert.NotNull(tempDataService);
    }

    [Fact]
    public void AddCookieTempDataValueProvider_WithOptions_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataProtection();
        var expectedCookieName = ".MyApp.CustomTempData";

        // Act
        services.Configure<RazorComponentsServiceOptions>(options =>
        {
            options.TempDataCookie.Name = expectedCookieName;
            options.TempDataCookie.HttpOnly = false;
            options.TempDataCookie.SameSite = SameSiteMode.Strict;
        });
        services.AddCookieTempDataValueProvider();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();

        // Assert
        Assert.Equal(expectedCookieName, options.Value.TempDataCookie.Name);
        Assert.False(options.Value.TempDataCookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, options.Value.TempDataCookie.SameSite);
    }

    [Fact]
    public void AddCookieTempDataValueProvider_ReplacesDefaultProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataProtection();
        // Simulate AddRazorComponents calling AddDefaultTempDataValueProvider
        TempDataProviderServiceCollectionExtensions.AddDefaultTempDataValueProvider(services);

        // Act - User calls AddCookieTempDataValueProvider to customize
        services.Configure<RazorComponentsServiceOptions>(options =>
        {
            options.TempDataCookie.Name = ".Custom.TempData";
        });
        services.AddCookieTempDataValueProvider();

        // Options should be configured
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();
        Assert.Equal(".Custom.TempData", options.Value.TempDataCookie.Name);
    }
}
