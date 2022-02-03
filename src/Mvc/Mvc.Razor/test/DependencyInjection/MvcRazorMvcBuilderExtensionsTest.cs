// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor.Test.DependencyInjection;

public class MvcRazorMvcBuilderExtensionsTest
{
    [Fact]
    public void AddTagHelpersAsServices_ReplacesTagHelperActivatorAndTagHelperTypeResolver()
    {
        // Arrange
        var services = new ServiceCollection();

        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new TestApplicationPart());

        var builder = new MvcBuilder(services, manager);

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

        manager.FeatureProviders.Add(new TagHelperFeatureProvider());

        var builder = new MvcBuilder(services, manager);

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
}
