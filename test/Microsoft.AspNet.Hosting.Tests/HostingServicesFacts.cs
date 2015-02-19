// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime;
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
        public void CreateCanAddAdditionalServices()
        {
            // Arrange
            var fallbackServices = new ServiceCollection();
            fallbackServices.AddTransient<IFakeService, FakeService>();
            fallbackServices.AddTransient<IFakeScopedService, FakeService>(); // Don't register in manifest

            fallbackServices.AddInstance<IServiceManifest>(new ServiceManifest(
                new Type[] {
                    typeof(IFakeService),
                }));

            var instance = new FakeService();
            var factoryInstance = new FakeFactoryService(instance);

            var services = HostingServices.Create(fallbackServices.BuildServiceProvider(),
                additionalHostServices =>
                {
                    additionalHostServices.AddSingleton<IFakeSingletonService, FakeService>();
                    additionalHostServices.AddInstance<IFakeServiceInstance>(instance);
                    additionalHostServices.AddSingleton<IFactoryService>(serviceProvider => factoryInstance);
                });

            // Act
            var provider = services.BuildServiceProvider();
            var singleton = provider.GetRequiredService<IFakeSingletonService>();
            var transient = provider.GetRequiredService<IFakeService>();
            var factory = provider.GetRequiredService<IFactoryService>();
            var manifest = provider.GetRequiredService<IServiceManifest>();

            // Assert
            Assert.Same(singleton, provider.GetRequiredService<IFakeSingletonService>());
            Assert.NotSame(transient, provider.GetRequiredService<IFakeService>());
            Assert.Same(instance, provider.GetRequiredService<IFakeServiceInstance>());
            Assert.Same(factoryInstance, factory);
            Assert.Same(factory.FakeService, instance);
            Assert.Null(provider.GetService<INonexistentService>());
            Assert.Null(provider.GetService<IFakeScopedService>()); // Make sure we don't leak non manifest services
            Assert.Contains(typeof(IFakeSingletonService), manifest.Services);
            Assert.Contains(typeof(IFakeServiceInstance), manifest.Services);
            Assert.Contains(typeof(IFactoryService), manifest.Services);
        }

        [Fact]
        public void CreateAdditionalServicesDoNotOverrideFallback()
        {
            // Arrange
            var fallbackServices = new ServiceCollection();
            fallbackServices.AddTransient<IFakeService, FakeService>();

            fallbackServices.AddInstance<IServiceManifest>(new ServiceManifest(
                new Type[] {
                    typeof(IFakeService),
                }));

            var services = HostingServices.Create(fallbackServices.BuildServiceProvider(), 
                additionalHostServices => additionalHostServices.AddSingleton<IFakeService, FakeService>());

            // Act
            var provider = services.BuildServiceProvider();
            var stillTransient = provider.GetRequiredService<IFakeService>();

            // Assert
            Assert.NotSame(stillTransient, provider.GetRequiredService<IFakeService>());
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
            var exception = Assert.Throws<InvalidOperationException>(() => HostingServices.Create(fallbackServices.BuildServiceProvider()));


            // Assert
            Assert.Equal($"No service for type '{typeof(IServiceManifest).FullName}' has been registered.",
                         exception.Message);
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