// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class TempDataProviderServiceCollectionExtensionsTest
{
    [Fact]
    public void AddRazorComponents_RegistersTempDataServices_WithDefaultCookieProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRazorComponents();
        services.AddDataProtection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tempDataProvider = serviceProvider.GetService<ITempDataProvider>();
        var tempDataSerializer = serviceProvider.GetService<ITempDataSerializer>();
        var tempDataService = serviceProvider.GetService<TempDataService>();

        Assert.NotNull(tempDataProvider);
        Assert.IsType<CookieTempDataProvider>(tempDataProvider);
        Assert.NotNull(tempDataSerializer);
        Assert.NotNull(tempDataService);
    }

    [Fact]
    public void AddRazorComponents_WithSessionStorageProvider_RegistersSessionStorageProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRazorComponents();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());

        // Act
        services.Configure<RazorComponentsServiceOptions>(options =>
        {
            options.TempDataProviderType = TempDataProviderType.SessionStorage;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tempDataProvider = serviceProvider.GetService<ITempDataProvider>();
        var tempDataSerializer = serviceProvider.GetService<ITempDataSerializer>();
        var tempDataService = serviceProvider.GetService<TempDataService>();

        Assert.NotNull(tempDataProvider);
        Assert.IsType<SessionStorageTempDataProvider>(tempDataProvider);
        Assert.NotNull(tempDataSerializer);
        Assert.NotNull(tempDataService);
    }

    [Fact]
    public void AddRazorComponents_WithCookieOptions_ConfiguresCookieSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRazorComponents();
        services.AddDataProtection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        var expectedCookieName = ".MyApp.CustomTempData";

        // Act
        services.Configure<RazorComponentsServiceOptions>(options =>
        {
            options.TempDataCookie.Name = expectedCookieName;
            options.TempDataCookie.HttpOnly = false;
            options.TempDataCookie.SameSite = SameSiteMode.Strict;
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();

        // Assert
        Assert.Equal(expectedCookieName, options.Value.TempDataCookie.Name);
        Assert.False(options.Value.TempDataCookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, options.Value.TempDataCookie.SameSite);
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string EnvironmentName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationName { get; set; } = "App";
        public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
