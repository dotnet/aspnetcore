// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.MvcServiceCollectionExtensionsTestControllers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class MvcBuilderExtensionsTest
    {
        [Fact]
        public void AddApplicationPart_AddsAnApplicationPart_ToTheListOfPartsOnTheBuilder()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            var builder = new MvcBuilder(Mock.Of<IServiceCollection>(), manager);

            var assembly = typeof(MvcBuilder).GetTypeInfo().Assembly;

            // Act
            var result = builder.AddApplicationPart(assembly);

            // Assert
            Assert.Same(result, builder);
            var part = Assert.Single(builder.PartManager.ApplicationParts);
            var assemblyPart = Assert.IsType<AssemblyPart>(part);
            Assert.Equal(assembly, assemblyPart.Assembly);
        }

        [Fact]
        public void ConfigureApplicationParts_InvokesSetupAction()
        {
            // Arrange
            var builder = new MvcBuilder(
                Mock.Of<IServiceCollection>(),
                new ApplicationPartManager());

            var part = new TestPart();

            // Act
            var result = builder.ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(part);
            });

            // Assert
            Assert.Same(result, builder);
            Assert.Equal(new ApplicationPart[] { part }, builder.PartManager.ApplicationParts.ToArray());
        }

        [Fact]
        public void WithControllersAsServices_AddsTypesToControllerTypeProviderAndServiceCollection()
        {
            // Arrange
            var builder = new Mock<IMvcBuilder>();
            var collection = new ServiceCollection();
            builder.SetupGet(b => b.Services).Returns(collection);

            var controllerTypes = new[]
            {
                typeof(ControllerTypeA),
                typeof(TypeBController),
            };

            // Act
            builder.Object.AddControllersAsServices(controllerTypes);

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
            var typeProvider = Assert.IsType<StaticControllerTypeProvider>(services[3].ImplementationInstance);
            Assert.Equal(controllerTypes, typeProvider.ControllerTypes.OrderBy(c => c.Name).Select(t => t.AsType()));
            Assert.Equal(ServiceLifetime.Singleton, services[3].Lifetime);
        }

        private class TestPart : ApplicationPart
        {
            public override string Name => "Test";
        }
    }
}

// These controllers are used to test the UseControllersAsServices implementation
// which REQUIRES that they be public top-level classes. To avoid having to stub out the
// implementation of this class to test it, they are just top level classes. Don't reuse
// these outside this test - find a better way or use nested classes to keep the tests
// independent.
namespace Microsoft.AspNetCore.Mvc.MvcServiceCollectionExtensionsTestControllers
{
    public class ControllerTypeA : Microsoft.AspNetCore.Mvc.Controller
    {

    }

    public class TypeBController
    {

    }
}
