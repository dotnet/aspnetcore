// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.Framework.Localization.Test
{
    public class LocalizationServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddLocalization_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            collection.AddLocalization();

            // Assert
            var services = collection.ToList();
            Assert.Equal(2, services.Count);

            Assert.Equal(typeof(IStringLocalizerFactory), services[0].ServiceType);
            Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), services[0].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, services[0].Lifetime);

            Assert.Equal(typeof(IStringLocalizer<>), services[1].ServiceType);
            Assert.Equal(typeof(StringLocalizer<>), services[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[1].Lifetime);
        }

        [Fact]
        public void AddLocalizationWithLocalizationOptions_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            collection.AddLocalization(options => options.ResourcesPath = "Resources");

            // Assert
            var services = collection.ToList();
            Assert.Equal(3, services.Count);

            Assert.Equal(typeof(IStringLocalizerFactory), services[0].ServiceType);
            Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), services[0].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, services[0].Lifetime);

            Assert.Equal(typeof(IStringLocalizer<>), services[1].ServiceType);
            Assert.Equal(typeof(StringLocalizer<>), services[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[1].Lifetime);

            Assert.Equal(typeof(IConfigureOptions<LocalizationOptions>), services[2].ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, services[2].Lifetime);
        }
    }
}
