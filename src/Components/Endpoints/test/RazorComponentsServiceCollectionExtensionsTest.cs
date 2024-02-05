// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.DependencyInjection;

public class RazorComponentsServiceCollectionExtensionsTest
{
    [Fact]
    public void AddRazorComponents_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
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
                // 'multi-registration' services should not have any duplicate implementation types
                AssertAllImplementationTypesAreDistinct(services, service.ServiceType);
            }
        }
    }

    [Fact]
    public void AddRazorComponentsTwice_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());

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
                // 'multi-registration' services should not have any duplicate implementation types
                AssertAllImplementationTypesAreDistinct(services, service.ServiceType);
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
                [typeof(ICascadingValueSupplier)] = new[]
                {
                    typeof(SupplyParameterFromFormValueProvider),
                    typeof(SupplyParameterFromQueryValueProvider),
                }
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

    private void AssertAllImplementationTypesAreDistinct(
        IServiceCollection services,
        Type serviceType)
    {
        var serviceProvider = services.BuildServiceProvider();
        var implementationTypes = services
            .Where(sd => sd.ServiceType == serviceType)
            .Select(service => service switch
            {
                { ImplementationType: { } type } => type,
                { ImplementationInstance: { } instance } => instance.GetType(),
                { ImplementationFactory: { } factory } => factory(serviceProvider).GetType(),
            })
            .ToArray();

        if (implementationTypes.Length == 0)
        {
            Assert.True(
                false,
                $"Could not find an implementation type for {serviceType}");
        }
        else if (implementationTypes.Length != implementationTypes.Distinct().Count())
        {
            Assert.True(
                false,
                $"Found duplicate implementation types for {serviceType}. Implementation types: {string.Join(", ", implementationTypes.Select(x => x.ToString()))}");
        }
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string EnvironmentName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationName { get; set; } = "App";
        public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
