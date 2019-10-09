// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    public class ControllerSaveTempDataPropertyFilterFactoryTest
    {
        [Fact]
        public void CreateInstance_CreatesFilter()
        {
            // Arrange
            var property = typeof(StringController).GetProperty(nameof(StringController.StringProp));
            var lifecycleProperties = new[] { new LifecycleProperty(property, "key") };
            var factory = new ControllerSaveTempDataPropertyFilterFactory(lifecycleProperties);

            // Act
            var filter = factory.CreateInstance(CreateServiceProvider());

            // Assert
            var tempDataFilter = Assert.IsType<ControllerSaveTempDataPropertyFilter>(filter);
            Assert.Same(lifecycleProperties, tempDataFilter.Properties);
        }

        private ServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(Mock.Of<ITempDataProvider>());
            serviceCollection.AddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();
            serviceCollection.AddTransient<ControllerSaveTempDataPropertyFilter>();

            return serviceCollection.BuildServiceProvider();
        }

        private class StringController
        {
            [TempData]
            public string StringProp { get; set; }
        }
    }
}
