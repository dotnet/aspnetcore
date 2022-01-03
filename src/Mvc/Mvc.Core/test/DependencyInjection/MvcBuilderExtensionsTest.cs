// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.MvcServiceCollectionExtensionsTestControllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

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

            var assembly = typeof(MvcBuilder).Assembly;

            // Act
            var result = builder.AddApplicationPart(assembly);

            // Assert
            Assert.Same(result, builder);
            var part = Assert.Single(builder.PartManager.ApplicationParts);
            var assemblyPart = Assert.IsType<AssemblyPart>(part);
            Assert.Equal(assembly, assemblyPart.Assembly);
        }

        [Fact]
        public void AddApplicationPart_UsesPartFactory_ToRetrieveApplicationParts()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            var builder = new MvcBuilder(Mock.Of<IServiceCollection>(), manager);
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Test"), AssemblyBuilderAccess.Run);

            var attribute = new CustomAttributeBuilder(typeof(ProvideApplicationPartFactoryAttribute).GetConstructor(
                new[] { typeof(Type) }),
                new[] { typeof(TestApplicationPartFactory) });

            assembly.SetCustomAttribute(attribute);

            // Act
            builder.AddApplicationPart(assembly);

            // Assert
            var part = Assert.Single(builder.PartManager.ApplicationParts);
            Assert.Same(TestApplicationPartFactory.TestPart, part);
        }

        [Fact]
        public void ConfigureApplicationParts_InvokesSetupAction()
        {
            // Arrange
            var builder = new MvcBuilder(
                Mock.Of<IServiceCollection>(),
                new ApplicationPartManager());

            var part = new TestApplicationPart();

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
            var collection = new ServiceCollection();
            var controllerTypes = new[]
            {
                typeof(ControllerTypeA),
                typeof(TypeBController),
            }.Select(t => t.GetTypeInfo()).ToArray();

            var builder = new MvcBuilder(collection, GetApplicationPartManager(controllerTypes));

            // Act
            builder.AddControllersAsServices();

            // Assert
            var services = collection.ToList();
            Assert.Equal(3, services.Count);
            Assert.Equal(typeof(ControllerTypeA), services[0].ServiceType);
            Assert.Equal(typeof(ControllerTypeA), services[0].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[0].Lifetime);

            Assert.Equal(typeof(TypeBController), services[1].ServiceType);
            Assert.Equal(typeof(TypeBController), services[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[1].Lifetime);

            Assert.Equal(typeof(IControllerActivator), services[2].ServiceType);
            Assert.Equal(typeof(ServiceBasedControllerActivator), services[2].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[2].Lifetime);
        }

        [Fact]
        public void AddControllerAsServices_MultipleCalls_RetainsPreviouslyAddedTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(typeof(ControllerOne), typeof(ControllerTwo)));
            manager.FeatureProviders.Add(new TestFeatureProvider());
            var builder = new MvcBuilder(services, manager);

            builder.AddControllersAsServices();

            // Act
            builder.AddControllersAsServices();

            // Assert 2
            var collection = services.ToList();
            Assert.Equal(3, collection.Count);
            Assert.Single(collection, d => d.ServiceType.Equals(typeof(ControllerOne)));
            Assert.Single(collection, d => d.ServiceType.Equals(typeof(ControllerTwo)));
        }

        [Fact]
        public void ConfigureApiBehaviorOptions_InvokesSetupAction()
        {
            // Arrange
            var serviceCollection = new ServiceCollection()
                .AddOptions();

            var builder = new MvcBuilder(
                serviceCollection,
                new ApplicationPartManager());

            var part = new TestApplicationPart();

            // Act
            var result = builder.ConfigureApiBehaviorOptions(o =>
            {
                o.SuppressMapClientErrors = true;
            });

            // Assert
            var options = serviceCollection.
                BuildServiceProvider()
                .GetRequiredService<IOptions<ApiBehaviorOptions>>()
                .Value;
            Assert.True(options.SuppressMapClientErrors);
        }

        private class ControllerOne
        {
        }

        private class ControllerTwo
        {
        }

        private static ApplicationPartManager GetApplicationPartManager(params TypeInfo[] types)
        {
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(types));
            manager.FeatureProviders.Add(new ControllerFeatureProvider());

            return manager;
        }

        private class TestApplicationPartFactory : ApplicationPartFactory
        {
            public static readonly ApplicationPart TestPart = Mock.Of<ApplicationPart>();

            public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
            {
                yield return TestPart;
            }
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
    public class ControllerTypeA : ControllerBase
    {

    }

    public class TypeBController
    {

    }
}
