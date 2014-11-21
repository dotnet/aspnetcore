// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class HostingServicesFacts
    {
        [Fact]
        public void CreateImportsServices()
        {
            // Arrange
            var fallbackServices = new ServiceCollection();
            fallbackServices.AddSingleton<IFakeSingletonService, FakeService>();
            var instance = new FakeService();
            var factoryInstance = new FakeFactoryService(instance);
            fallbackServices.AddInstance<IFakeServiceInstance>(instance);
            fallbackServices.AddTransient<IFakeService, FakeService>();
            fallbackServices.AddSingleton<IFactoryService>(serviceProvider => factoryInstance);
            fallbackServices.AddTransient<IFakeScopedService, FakeService>(); // Don't register in manifest

            fallbackServices.AddInstance<IServiceManifest>(new ServiceManifest(
                new Type[] {
                    typeof(IFakeServiceInstance),
                    typeof(IFakeService),
                    typeof(IFakeSingletonService),
                    typeof(IFactoryService),
                    typeof(INonexistentService)
                }));

            var services = HostingServices.Create(fallbackServices.BuildServiceProvider());

            // Act
            var provider = services.BuildServiceProvider();
            var singleton = provider.GetRequiredService<IFakeSingletonService>();
            var transient = provider.GetRequiredService<IFakeService>();
            var factory = provider.GetRequiredService<IFactoryService>();

            // Assert
            Assert.Same(singleton, provider.GetRequiredService<IFakeSingletonService>());
            Assert.NotSame(transient, provider.GetRequiredService<IFakeService>());
            Assert.Same(instance, provider.GetRequiredService<IFakeServiceInstance>());
            Assert.Same(factoryInstance, factory);
            Assert.Same(factory.FakeService, instance);
            Assert.Null(provider.GetService<INonexistentService>());
            Assert.Null(provider.GetService<IFakeScopedService>()); // Make sure we don't leak non manifest services
        }

        [Fact]
        public void CanHideImportedServices()
        {
            // Arrange
            var fallbackServices = new ServiceCollection();
            var fallbackInstance = new FakeService();
            fallbackServices.AddInstance<IFakeService>(fallbackInstance);
            fallbackServices.AddInstance<IServiceManifest>(new ServiceManifest(new Type[] { typeof(IFakeService) }));

            var services = HostingServices.Create(fallbackServices.BuildServiceProvider());
            var realInstance = new FakeService();
            services.AddInstance<IFakeService>(realInstance);

            // Act
            var provider = services.BuildServiceProvider();

            // Assert
            Assert.Equal(realInstance, provider.GetRequiredService<IFakeService>());
        }

        [Fact]
        public void CreateThrowsWithNoManifest()
        {
            // Arrange
            var fallbackServices = new ServiceCollection();
            fallbackServices.AddSingleton<IFakeSingletonService, FakeService>();
            var instance = new FakeService();
            fallbackServices.AddInstance<IFakeServiceInstance>(instance);
            fallbackServices.AddTransient<IFakeService, FakeService>();

            // Act
            var exp = Assert.Throws<Exception>(() => HostingServices.Create(fallbackServices.BuildServiceProvider()));


            // Assert
            Assert.True(exp.Message.Contains("No service for type 'Microsoft.Framework.DependencyInjection.ServiceLookup.IServiceManifest'"));
        }

        private class ServiceManifest : IServiceManifest
        {
            public ServiceManifest(IEnumerable<Type> services)
            {
                Services = services;
            }

            public IEnumerable<Type> Services { get; private set; }
        }

        private class FakeFactoryService : IFactoryService
        {
            public FakeFactoryService(FakeService service)
            {
                FakeService = service;
            }

            public IFakeService FakeService { get; private set; }

            public int Value { get; private set; }
        }
    }
}