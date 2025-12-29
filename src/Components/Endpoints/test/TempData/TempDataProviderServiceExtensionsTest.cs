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

        // Act
        services.AddCookieTempDataValueProvider(configureOptions: null);
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
        services.AddCookieTempDataValueProvider(options =>
        {
            options.Cookie.Name = expectedCookieName;
            options.Cookie.HttpOnly = false;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<CookieTempDataProviderOptions>>();

        // Assert
        Assert.Equal(expectedCookieName, options.Value.Cookie.Name);
        Assert.False(options.Value.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, options.Value.Cookie.SameSite);
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
        services.AddCookieTempDataValueProvider(options =>
        {
            options.Cookie.Name = ".Custom.TempData";
        });

        // Options should be configured
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<CookieTempDataProviderOptions>>();
        Assert.Equal(".Custom.TempData", options.Value.Cookie.Name);
    }
}
