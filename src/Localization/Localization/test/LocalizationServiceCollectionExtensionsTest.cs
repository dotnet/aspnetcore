// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class LocalizationServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddLocalization_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            LocalizationServiceCollectionExtensions.AddLocalizationServices(collection);

            // Assert
            AssertContainsSingle(collection, typeof(IStringLocalizerFactory), typeof(ResourceManagerStringLocalizerFactory));
            AssertContainsSingle(collection, typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        }

        [Fact]
        public void AddLocalizationWithLocalizationOptions_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            LocalizationServiceCollectionExtensions.AddLocalizationServices(
                collection,
                options => options.ResourcesPath = "Resources");

            AssertContainsSingle(collection, typeof(IStringLocalizerFactory), typeof(ResourceManagerStringLocalizerFactory));
            AssertContainsSingle(collection, typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        }

        private void AssertContainsSingle(
            IServiceCollection services,
            Type serviceType,
            Type implementationType)
        {
            var matches = services
                .Where(sd =>
                    sd.ServiceType == serviceType &&
                    sd.ImplementationType == implementationType)
                .ToArray();

            if (matches.Length == 0)
            {
                Assert.True(
                    false,
                    $"Could not find an instance of {implementationType} registered as {serviceType}");
            }
            else if (matches.Length > 1)
            {
                Assert.True(
                    false,
                    $"Found multiple instances of {implementationType} registered as {serviceType}");
            }
        }
    }
}
