// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.MvcServiceCollectionExtensionsTestControllers;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class MvcServiceCollectionExtensionsTest
    {
        [Fact]
        public void WithControllersAsServices_AddsTypesToControllerTypeProviderAndServiceCollection()
        {
            // Arrange
            var collection = new ServiceCollection();
            var controllerTypes = new[] { typeof(ControllerTypeA).GetTypeInfo(), typeof(TypeBController).GetTypeInfo() };

            // Act
            MvcServiceCollectionExtensions.WithControllersAsServices(collection,
                                                                     controllerTypes);

            // Assert
            var services = collection.ToList();
            Assert.Equal(4, services.Count);
            Assert.Equal(typeof(ControllerTypeA), services[0].ServiceType);
            Assert.Equal(typeof(ControllerTypeA), services[0].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[0].Lifetime);

            Assert.Equal(typeof(TypeBController), services[1].ServiceType);
            Assert.Equal(typeof(TypeBController), services[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[1].Lifetime);

            Assert.Equal(typeof(IControllerActivator), services[2].ServiceType);
            Assert.Equal(typeof(ServiceBasedControllerActivator), services[2].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[2].Lifetime);

            Assert.Equal(typeof(IControllerTypeProvider), services[3].ServiceType);
            var typeProvider = Assert.IsType<FixedSetControllerTypeProvider>(services[3].ImplementationInstance);
            Assert.Equal(controllerTypes, typeProvider.ControllerTypes.OrderBy(c => c.Name));
            Assert.Equal(ServiceLifetime.Singleton, services[3].Lifetime);
        }

        [Fact]
        public void WithControllersAsServices_ScansControllersFromSpecifiedAssemblies()
        {
            // Arrange
            var collection = new ServiceCollection();
            var assemblies = new[] { GetType().Assembly };
            var configuration = new Configuration();
            var controllerTypes = new[] { typeof(ControllerTypeA), typeof(TypeBController) };

            // Act
            MvcServiceCollectionExtensions.WithControllersAsServices(collection,
                                                                     assemblies);

            // Assert
            var services = collection.ToList();
            Assert.Equal(4, services.Count);
            Assert.Equal(typeof(ControllerTypeA), services[0].ServiceType);
            Assert.Equal(typeof(ControllerTypeA), services[0].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[0].Lifetime);

            Assert.Equal(typeof(TypeBController), services[1].ServiceType);
            Assert.Equal(typeof(TypeBController), services[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[1].Lifetime);


            Assert.Equal(typeof(IControllerActivator), services[2].ServiceType);
            Assert.Equal(typeof(ServiceBasedControllerActivator), services[2].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[2].Lifetime);

            Assert.Equal(typeof(IControllerTypeProvider), services[3].ServiceType);
            var typeProvider = Assert.IsType<FixedSetControllerTypeProvider>(services[3].ImplementationInstance);
            Assert.Equal(controllerTypes, typeProvider.ControllerTypes.OrderBy(c => c.Name));
            Assert.Equal(ServiceLifetime.Singleton, services[3].Lifetime);
        }

        private class CustomActivator : IControllerActivator
        {
            public object Create(ActionContext context, Type controllerType)
            {
                throw new NotImplementedException();
            }
        }

        public class CustomTypeProvider : IControllerTypeProvider
        {
            public IEnumerable<TypeInfo> ControllerTypes { get; set; }
        }
    }
}

// These controllers are used to test the UseControllersAsServices implementation
// which REQUIRES that they be public top-level classes. To avoid having to stub out the
// implementation of this class to test it, they are just top level classes. Don't reuse
// these outside this test - find a better way or use nested classes to keep the tests
// independent.
namespace Microsoft.AspNet.Mvc.MvcServiceCollectionExtensionsTestControllers
{
    public class ControllerTypeA : Controller
    {

    }

    public class TypeBController
    {

    }
}