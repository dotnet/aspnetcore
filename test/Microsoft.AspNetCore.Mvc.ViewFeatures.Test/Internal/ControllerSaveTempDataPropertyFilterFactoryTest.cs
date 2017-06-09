// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ControllerSaveTempDataPropertyFilterFactoryTest
    {
        [Fact]
        public void CreateInstance_CreatesFilter()
        {
            // Arrange
            var factory = new ControllerSaveTempDataPropertyFilterFactory();
            var propertyInfo = typeof(StringController).GetProperty("StringProp");

            factory.TempDataProperties = new List<TempDataProperty>()
            {
                new TempDataProperty("TempDataProperty-StringProp", propertyInfo, null, null)
            };

            // Act
            var filter = factory.CreateInstance(CreateServiceProvider());

            // Assert
            Assert.Collection(
                Assert.IsType<ControllerSaveTempDataPropertyFilter>(filter).Properties,
                property => Assert.Equal(propertyInfo, property.PropertyInfo));
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
            public string StringProp { get; set; }
        }
    }
}
