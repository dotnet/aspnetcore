// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection;

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
            Assert.Fail($"Could not find an instance of {implementationType} registered as {serviceType}");
        }
        else if (matches.Length > 1)
        {
            Assert.Fail($"Found multiple instances of {implementationType} registered as {serviceType}");
        }
    }
}
