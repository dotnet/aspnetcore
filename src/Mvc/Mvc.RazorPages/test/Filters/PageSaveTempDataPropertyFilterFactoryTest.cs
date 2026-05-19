// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class PageSaveTempDataPropertyFilterFactoryTest
{
    [Fact]
    public void CreatesInstanceWithProperties()
    {
        // Arrange
        var property = typeof(TestPageModel).GetProperty(nameof(TestPageModel.Property1));
        var lifecycleProperties = new[] { new LifecycleProperty(property, "key") };
        var factory = new PageSaveTempDataPropertyFilterFactory(lifecycleProperties);
        var serviceProvider = CreateServiceProvider();

        // Act
        var filter = factory.CreateInstance(serviceProvider);

        // Assert
        var pageFilter = Assert.IsType<PageSaveTempDataPropertyFilter>(filter);
        Assert.Same(lifecycleProperties, pageFilter.Properties);
    }

    private ServiceProvider CreateServiceProvider()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton(Mock.Of<ITempDataProvider>());
        serviceCollection.AddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();
        serviceCollection.AddTransient<PageSaveTempDataPropertyFilter>();

        return serviceCollection.BuildServiceProvider();
    }

    private class TestPageModel : PageModel
    {
        [TempData]
        public string Property1 { get; set; }
    }
}
