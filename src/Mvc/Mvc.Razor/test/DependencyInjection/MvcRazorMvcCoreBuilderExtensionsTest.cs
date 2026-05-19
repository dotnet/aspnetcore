// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor.Test.DependencyInjection;

public class MvcRazorMvcCoreBuilderExtensionsTest
{
    [Fact]
    public void AddMvcCore_OnServiceCollectionWithoutIHostingEnvironmentInstance_DoesNotDiscoverApplicationParts()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services
            .AddMvcCore();

        // Assert
        Assert.Empty(builder.PartManager.ApplicationParts);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void AddMvcCore_OnServiceCollectionWithIHostingEnvironmentInstanceWithInvalidApplicationName_DoesNotDiscoverApplicationParts(string applicationName)
    {
        // Arrange
        var services = new ServiceCollection();

        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment
            .Setup(h => h.ApplicationName)
            .Returns(applicationName);

        services.AddSingleton(hostingEnvironment.Object);

        // Act
        var builder = services
            .AddMvcCore();

        // Assert
        Assert.Empty(builder.PartManager.ApplicationParts);
    }

    [Fact]
    public void AddTagHelpersAsServices_ReplacesTagHelperActivatorAndTagHelperTypeResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services
            .AddMvcCore()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(new TestApplicationPart());
            });

        // Act
        builder.AddTagHelpersAsServices();

        // Assert
        var activatorDescriptor = Assert.Single(services.ToList(), d => d.ServiceType == typeof(ITagHelperActivator));
        Assert.Equal(typeof(ServiceBasedTagHelperActivator), activatorDescriptor.ImplementationType);
    }

    [Fact]
    public void AddTagHelpersAsServices_RegistersDiscoveredTagHelpers()
    {
        // Arrange
        var services = new ServiceCollection();

        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new TestApplicationPart(
            typeof(TestTagHelperOne),
            typeof(TestTagHelperTwo)));

        manager.FeatureProviders.Add(new TestFeatureProvider());

        var builder = new MvcCoreBuilder(services, manager);

        // Act
        builder.AddTagHelpersAsServices();

        // Assert
        var collection = services.ToList();
        Assert.Equal(3, collection.Count);

        var tagHelperOne = Assert.Single(collection, t => t.ServiceType == typeof(TestTagHelperOne));
        Assert.Equal(typeof(TestTagHelperOne), tagHelperOne.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, tagHelperOne.Lifetime);

        var tagHelperTwo = Assert.Single(collection, t => t.ServiceType == typeof(TestTagHelperTwo));
        Assert.Equal(typeof(TestTagHelperTwo), tagHelperTwo.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, tagHelperTwo.Lifetime);

        var activator = Assert.Single(collection, t => t.ServiceType == typeof(ITagHelperActivator));
        Assert.Equal(typeof(ServiceBasedTagHelperActivator), activator.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, activator.Lifetime);
    }

    private class TestTagHelperOne : TagHelper
    {
    }

    private class TestTagHelperTwo : TagHelper
    {
    }

    private class TestFeatureProvider : IApplicationFeatureProvider<TagHelperFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, TagHelperFeature feature)
        {
            foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(tp => tp.Types))
            {
                feature.TagHelpers.Add(type);
            }
        }
    }
}
