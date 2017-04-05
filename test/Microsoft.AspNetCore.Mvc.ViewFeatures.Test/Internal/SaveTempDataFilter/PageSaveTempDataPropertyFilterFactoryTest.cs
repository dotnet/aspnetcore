// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PageSaveTempDataPropertyFilterFactoryTest
    {
        [Fact]
        public void CreatesInstanceWithProperties()
        {
            // Arrange
            var factory = new PageSaveTempDataPropertyFilterFactory();

            var serviceProvider = CreateServiceProvider();

            // Act
            var filter = factory.CreateInstance(serviceProvider);

            // Assert
            var pageFilter = Assert.IsType<PageSaveTempDataPropertyFilter>(filter);
            Assert.Same(factory, pageFilter.FilterFactory);        
        }

        private ServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(Mock.Of<ITempDataProvider>());
            serviceCollection.AddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();
            serviceCollection.AddTransient<PageSaveTempDataPropertyFilter>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
