// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class MvcCoreServiceCollectionExtensionsTest
    {
        // Some MVC services can be registered multiple times, for example, 'IConfigureOptions<MvcOptions>' can
        // be registered by calling 'ConfigureMvc(...)' before the call to 'AddMvc()' in which case the options
        // configuration is run in the order they were registered.
        //
        // For these kind of multi registration service types, we want to make sure that MVC will still add its
        // services if the implementation type is different.
        [Fact]
        public void MultiRegistrationServiceTypes_AreRegistered_MultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();

            // Register a mock implementation of each service, AddMvcServices should add another implementation.
            foreach (var serviceType in MultiRegistrationServiceTypes)
            {
                var mockType = typeof(Mock<>).MakeGenericType(serviceType.Key);
                services.Add(ServiceDescriptor.Transient(serviceType.Key, mockType));
            }

            // Act
            MvcCoreServiceCollectionExtensions.AddMvcCoreServices(services);

            // Assert
            foreach (var serviceType in MultiRegistrationServiceTypes)
            {
                AssertServiceCountEquals(services, serviceType.Key, serviceType.Value.Length + 1);

                foreach (var implementationType in serviceType.Value)
                {
                    AssertContainsSingle(services, serviceType.Key, implementationType);
                }
            }
        }

        [Fact]
        public void SingleRegistrationServiceTypes_AreNotRegistered_MultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();

            // Register a mock implementation of each service, AddMvcServices should not replace it.
            foreach (var serviceType in SingleRegistrationServiceTypes)
            {
                var mockType = typeof(Mock<>).MakeGenericType(serviceType);
                services.Add(ServiceDescriptor.Transient(serviceType, mockType));
            }

            // Act
            MvcCoreServiceCollectionExtensions.AddMvcCoreServices(services);

            // Assert
            foreach (var singleRegistrationType in SingleRegistrationServiceTypes)
            {
                AssertServiceCountEquals(services, singleRegistrationType, 1);
            }
        }

        [Fact]
        public void AddMvcServicesTwice_DoesNotAddDuplicates()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            MvcCoreServiceCollectionExtensions.AddMvcCoreServices(services);
            MvcCoreServiceCollectionExtensions.AddMvcCoreServices(services);

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
        public void AddMvcCore_UsesOriginalPartManager()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            var services = new ServiceCollection();
            services.AddSingleton(manager);

            // Act
            var builder = services.AddMvcCore();

            // Assert
            // SingleRegistrationServiceTypes_AreNotRegistered_MultipleTimes already checks that no other
            // ApplicationPartManager (but manager) is registered.
            Assert.Same(manager, builder.PartManager);
            Assert.Contains(manager.FeatureProviders, provider => provider is ControllerFeatureProvider);
        }

        // Regression test for aspnet/Mvc#5554.
        [Fact]
        public void AddMvcCore_UsesLastPartManager()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockManager = new Mock<ApplicationPartManager>(MockBehavior.Strict);
            services.AddSingleton(mockManager.Object);

            var manager = new ApplicationPartManager();
            services.AddSingleton(manager);

            // Act
            var builder = services.AddMvcCore();

            // Assert
            Assert.Same(manager, builder.PartManager);
            Assert.Contains(manager.FeatureProviders, provider => provider is ControllerFeatureProvider);
        }

        [Fact]
        public void AddMvcCore_UsesOriginalHostingEnvironment()
        {
            // Arrange
            var services = new ServiceCollection();
            var environment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
            environment.SetupGet(e => e.ApplicationName).Returns((string)null).Verifiable();
            services.AddSingleton<IWebHostEnvironment>(environment.Object);

            // Act
            var builder = services.AddMvcCore();

            // Assert
            Assert.NotNull(builder.PartManager);
            Assert.Empty(builder.PartManager.ApplicationParts);
            Assert.Contains(builder.PartManager.FeatureProviders, provider => provider is ControllerFeatureProvider);

            environment.VerifyAll();
        }

        // Second regression test for aspnet/Mvc#5554.
        [Fact]
        public void AddMvcCore_UsesLastHostingEnvironment()
        {
            // Arrange
            var services = new ServiceCollection();
            var environment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
            services.AddSingleton<IWebHostEnvironment>(environment.Object);

            environment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
            environment.SetupGet(e => e.ApplicationName).Returns((string)null).Verifiable();
            services.AddSingleton<IWebHostEnvironment>(environment.Object);

            // Act
            var builder = services.AddMvcCore();

            // Assert
            Assert.NotNull(builder.PartManager);
            Assert.Empty(builder.PartManager.ApplicationParts);
            Assert.Contains(builder.PartManager.FeatureProviders, provider => provider is ControllerFeatureProvider);

            environment.VerifyAll();
        }

        [Fact]
        public void AddMvcCore_GetsPartsForApplication()
        {
            // Arrange
            var services = new ServiceCollection();
            var environment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
            var assemblyName = typeof(MvcCoreServiceCollectionExtensionsTest).GetTypeInfo().Assembly.GetName();
            var applicationName = assemblyName.FullName;
            environment.SetupGet(e => e.ApplicationName).Returns(applicationName).Verifiable();
            services.AddSingleton<IWebHostEnvironment>(environment.Object);

            // Act
            var builder = services.AddMvcCore();

            // Assert
            Assert.NotNull(builder.PartManager);
            Assert.Contains(
                builder.PartManager.ApplicationParts,
                part => string.Equals(assemblyName.Name, part.Name, StringComparison.Ordinal));
            Assert.Contains(builder.PartManager.FeatureProviders, provider => provider is ControllerFeatureProvider);

            environment.VerifyAll();
        }

        private IEnumerable<Type> SingleRegistrationServiceTypes
        {
            get
            {
                var services = new ServiceCollection();
                MvcCoreServiceCollectionExtensions.AddMvcCoreServices(services);

                var multiRegistrationServiceTypes = MultiRegistrationServiceTypes;
                return services
                    .Where(sd => !multiRegistrationServiceTypes.Keys.Contains(sd.ServiceType))
                    .Select(sd => sd.ServiceType);
            }
        }

        private Dictionary<Type, Type[]> MultiRegistrationServiceTypes
        {
            get
            {
                return new Dictionary<Type, Type[]>()
                {
                    {
                        typeof(IConfigureOptions<MvcOptions>),
                        new Type[]
                        {
                            typeof(MvcCoreMvcOptionsSetup),
                        }
                    },
                    {
                        typeof(IPostConfigureOptions<MvcOptions>),
                        new Type[]
                        {
                            typeof(MvcOptionsConfigureCompatibilityOptions),
                            typeof(MvcCoreMvcOptionsSetup),
                        }
                    },
                    {
                        typeof(IConfigureOptions<RouteOptions>),
                        new Type[]
                        {
                            typeof(MvcCoreRouteOptionsSetup),
                        }
                    },
                    {
                        typeof(IConfigureOptions<ApiBehaviorOptions>),
                        new Type[]
                        {
                            typeof(ApiBehaviorOptionsSetup),
                        }
                    },
                    {
                        typeof(IActionConstraintProvider),
                        new Type[]
                        {
                            typeof(DefaultActionConstraintProvider),
                        }
                    },
                    {
                        typeof(IActionDescriptorProvider),
                        new Type[]
                        {
                            typeof(ControllerActionDescriptorProvider),
                        }
                    },
                    {
                        typeof(IActionInvokerProvider),
                        new Type[]
                        {
                            typeof(ControllerActionInvokerProvider),
                        }
                    },
                    {
                        typeof(IFilterProvider),
                        new Type[]
                        {
                            typeof(DefaultFilterProvider),
                        }
                    },
                    {
                        typeof(IControllerPropertyActivator),
                        new Type[]
                        {
                            typeof(DefaultControllerPropertyActivator),
                        }
                    },
                    {
                        typeof(IApplicationModelProvider),
                        new Type[]
                        {
                            typeof(DefaultApplicationModelProvider),
                            typeof(ApiBehaviorApplicationModelProvider),
                        }
                    },
                    {
                        typeof(IStartupFilter),
                        new Type[]
                        {
                            typeof(MiddlewareFilterBuilderStartupFilter)
                        }
                    },
                    {
                        typeof(MatcherPolicy),
                        new Type[]
                        {
                            typeof(ConsumesMatcherPolicy),
                            typeof(ActionConstraintMatcherPolicy),
                            typeof(DynamicControllerEndpointMatcherPolicy),
                        }
                    },
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
}