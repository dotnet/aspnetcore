// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class RazorComponentsServiceCollectionExtensionsTest
{
    [Fact]
    public void AddRazorComponents_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        RazorComponentsServiceCollectionExtensions.AddRazorComponents(services);

        // Assert
        var singleRegistrationServiceTypes = SingleRegistrationServiceTypes;
        foreach (var service in services)
        {
            if (singleRegistrationServiceTypes.Contains(service.ServiceType))
            {
                // 'single-registration' services should only have one implementation registered.
                AssertServiceCountEquals(services, service.ServiceType, 1);
            }
            else
            {
                // 'multi-registration' services should only have one *instance* of each implementation registered.
                AssertContainsSingle(services, service.ServiceType, service.ImplementationType);
            }
        }
    }

    [Fact]
    public void AddRazorComponentsTwice_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        RazorComponentsServiceCollectionExtensions.AddRazorComponents(services);
        RazorComponentsServiceCollectionExtensions.AddRazorComponents(services);

        // Assert
        var singleRegistrationServiceTypes = SingleRegistrationServiceTypes;
        foreach (var service in services)
        {
            if (singleRegistrationServiceTypes.Contains(service.ServiceType))
            {
                // 'single-registration' services should only have one implementation registered.
                AssertServiceCountEquals(services, service.ServiceType, 1);
            }
            else
            {
                // 'multi-registration' services should only have one *instance* of each implementation registered.
                AssertContainsSingle(services, service.ServiceType, service.ImplementationType);
            }
        }
    }

    private IEnumerable<Type> SingleRegistrationServiceTypes
    {
        get
        {
            var services = new ServiceCollection();
            RazorComponentsServiceCollectionExtensions.AddRazorComponents(services);

            var multiRegistrationServiceTypes = MultiRegistrationServiceTypes;
            return services
                .Where(sd => !multiRegistrationServiceTypes.ContainsKey(sd.ServiceType))
                .Select(sd => sd.ServiceType);
        }
    }

    private Dictionary<Type, Type[]> MultiRegistrationServiceTypes
    {
        get
        {
            return new Dictionary<Type, Type[]>()
            {
            };
        }
    }

    private void AssertServiceCountEquals(
        IServiceCollection services,
        Type serviceType,
        int expectedServiceRegistrationCount)
    {
        var serviceDescriptors = services.Where(serviceDescriptor => serviceDescriptor.ServiceType == serviceType);
        var actual = serviceDescriptors.Count();

        Assert.True(
            (expectedServiceRegistrationCount == actual),
            $"Expected service type '{serviceType}' to be registered {expectedServiceRegistrationCount}" +
            $" time(s) but was actually registered {actual} time(s).");
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
