// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class MvcViewFeaturesMvcBuilderExtensionsTest
{
    [Fact]
    public void AddViewComponentsAsServices_ReplacesViewComponentActivator()
    {
        // Arrange
        var builder = CreateBuilder();

        MvcViewFeaturesMvcCoreBuilderExtensions.AddViewServices(builder.Services);
        builder.ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Add(new TestApplicationPart());
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());
        });

        // Act
        builder.AddViewComponentsAsServices();

        // Assert
        var descriptor = Assert.Single(builder.Services.ToList(), d => d.ServiceType == typeof(IViewComponentActivator));
        Assert.Equal(typeof(ServiceBasedViewComponentActivator), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCookieTempDataProvider_RegistersExpectedTempDataProvider()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddCookieTempDataProvider();

        // Assert
        var descriptor = Assert.Single(builder.Services, item => item.ServiceType == typeof(ITempDataProvider));
        Assert.Equal(typeof(CookieTempDataProvider), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCookieTempDataProvider_DoesNotRegisterOptionsConfiguration()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddCookieTempDataProvider();

        // Assert
        Assert.DoesNotContain(
            builder.Services,
            item => item.ServiceType == typeof(IConfigureOptions<CookieTempDataProviderOptions>));
    }

    [Fact]
    public void AddCookieTempDataProviderWithSetupAction_RegistersExpectedTempDataProvider()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddCookieTempDataProvider(options => { });

        // Assert
        var descriptor = Assert.Single(builder.Services, item => item.ServiceType == typeof(ITempDataProvider));
        Assert.Equal(typeof(CookieTempDataProvider), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCookieTempDataProviderWithSetupAction_RegistersOptionsConfiguration()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddCookieTempDataProvider(options => { });

        // Assert
        Assert.Single(
            builder.Services,
            item => item.ServiceType == typeof(IConfigureOptions<CookieTempDataProviderOptions>));
    }

    [Fact]
    public void AddCookieTempDataProvider_RegistersExpectedTempDataProvider_IfCalledTwice()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddCookieTempDataProvider();
        builder.AddCookieTempDataProvider();

        // Assert
        var descriptor = Assert.Single(builder.Services, item => item.ServiceType == typeof(ITempDataProvider));
        Assert.Equal(typeof(CookieTempDataProvider), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCookieTempDataProviderWithSetupAction_RegistersExpectedTempDataProvider_IfCalledTwice()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.AddCookieTempDataProvider(options => { });
        builder.AddCookieTempDataProvider(options => { });

        // Assert
        var descriptor = Assert.Single(builder.Services, item => item.ServiceType == typeof(ITempDataProvider));
        Assert.Equal(typeof(CookieTempDataProvider), descriptor.ImplementationType);
    }

    [Fact]
    public void AddViewComponentsAsServices_RegistersDiscoveredViewComponents()
    {
        // Arrange
        var services = new ServiceCollection();

        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new TestApplicationPart(
            typeof(ConventionsViewComponent),
            typeof(AttributeViewComponent)));

        manager.FeatureProviders.Add(new TestProvider());

        var builder = new MvcBuilder(services, manager);

        // Act
        builder.AddViewComponentsAsServices();

        // Assert
        var collection = services.ToList();
        Assert.Equal(3, collection.Count);

        Assert.Equal(typeof(ConventionsViewComponent), collection[0].ServiceType);
        Assert.Equal(typeof(ConventionsViewComponent), collection[0].ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, collection[0].Lifetime);

        Assert.Equal(typeof(AttributeViewComponent), collection[1].ServiceType);
        Assert.Equal(typeof(AttributeViewComponent), collection[1].ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, collection[1].Lifetime);

        Assert.Equal(typeof(IViewComponentActivator), collection[2].ServiceType);
        Assert.Equal(typeof(ServiceBasedViewComponentActivator), collection[2].ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, collection[2].Lifetime);
    }

    private static MvcBuilder CreateBuilder()
    {
        var services = new ServiceCollection();
        var manager = new ApplicationPartManager();
        var builder = new MvcBuilder(services, manager);
        return builder;
    }

    public class ConventionsViewComponent
    {
        public string Invoke() => "Hello world";
    }

    [ViewComponent(Name = "AttributesAreGreat")]
    public class AttributeViewComponent
    {
        public Task<string> InvokeAsync() => Task.FromResult("Hello world");
    }

    private class TestProvider : IApplicationFeatureProvider<ViewComponentFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentFeature feature)
        {
            foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(p => p.Types))
            {
                feature.ViewComponents.Add(type);
            }
        }
    }
}
