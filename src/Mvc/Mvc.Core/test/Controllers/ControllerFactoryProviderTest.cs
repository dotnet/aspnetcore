// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    public class ControllerFactoryProviderTest
    {
        [Fact]
        public void CreateControllerFactory_InvokesIControllerFactory_IfItIsNotDefaultControllerFactory()
        {
            // Arrange
            var expected = new object();
            var factory = new Mock<IControllerFactory>();
            factory.Setup(f => f.CreateController(It.IsAny<ControllerContext>()))
                .Returns(expected)
                .Verifiable();
            var provider = new ControllerFactoryProvider(
                Mock.Of<IControllerActivatorProvider>(),
                factory.Object,
                Enumerable.Empty<IControllerPropertyActivator>());
            var descriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(object).GetTypeInfo(),
            };

            // Act
            var factoryResult = provider.CreateControllerFactory(descriptor);
            var result = factoryResult(new ControllerContext());

            // Assert
            Assert.Same(result, expected);
            factory.Verify();
        }

        [Fact]
        public void CreateControllerReleaser_InvokesIControllerFactory_IfItIsNotDefaultControllerFactory()
        {
            // Arrange
            var controller = new object();
            var factory = new Mock<IControllerFactory>();
            factory.Setup(f => f.ReleaseController(It.IsAny<ControllerContext>(), controller))
                .Verifiable();
            var provider = new ControllerFactoryProvider(
                Mock.Of<IControllerActivatorProvider>(),
                factory.Object,
                Enumerable.Empty<IControllerPropertyActivator>());
            var descriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(object).GetTypeInfo(),
            };

            // Act
            var releaser = provider.CreateControllerReleaser(descriptor);
            releaser(new ControllerContext(), controller);

            // Assert
            factory.Verify();
        }

        [Fact]
        public void CreateControllerFactory_UsesControllerActivatorAndPropertyActivator()
        {
            // Arrange
            var expectedProperty1 = new object();
            var expectedProperty2 = new object();
            var expectedController = new TestController();
            var factory = new DefaultControllerFactory(
                Mock.Of<IControllerActivator>(),
                Enumerable.Empty<IControllerPropertyActivator>());
            var activatorProvider = new Mock<IControllerActivatorProvider>();
            activatorProvider.Setup(p => p.CreateActivator(It.IsAny<ControllerActionDescriptor>()))
                .Returns(_ => expectedController)
                .Verifiable();

            var propertyActivator1 = new Mock<IControllerPropertyActivator>();
            propertyActivator1.Setup(p => p.GetActivatorDelegate(It.IsAny<ControllerActionDescriptor>()))
                .Returns((context, controllerObject) =>
                {
                    ((TestController)controllerObject).ActivatedValue1 = expectedProperty1;
                })
                .Verifiable();

            var propertyActivator2 = new Mock<IControllerPropertyActivator>();
            propertyActivator2.Setup(p => p.GetActivatorDelegate(It.IsAny<ControllerActionDescriptor>()))
                .Returns((context, controllerObject) =>
                {
                    ((TestController)controllerObject).ActivatedValue2 = expectedProperty2;
                })
                .Verifiable();

            var propertyActivators = new[]
            {
                propertyActivator1.Object,
                propertyActivator2.Object,
            };
            var provider = new ControllerFactoryProvider(
                activatorProvider.Object,
                factory,
                propertyActivators);
            var descriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            };

            // Act
            var factoryDelegate = provider.CreateControllerFactory(descriptor);
            var controller = factoryDelegate(new ControllerContext());

            // Assert
            var actual = Assert.IsType<TestController>(controller);
            Assert.Same(expectedController, actual);
            Assert.Same(expectedProperty1, actual.ActivatedValue1);
            Assert.Same(expectedProperty2, actual.ActivatedValue2);
            activatorProvider.Verify();
            propertyActivator1.Verify();
            propertyActivator2.Verify();
        }

        [Fact]
        public void CreateControllerReleaser_ReturnsReleaser()
        {
            // Arrange
            var controller = new object();
            var factory = new DefaultControllerFactory(
                Mock.Of<IControllerActivator>(),
                Enumerable.Empty<IControllerPropertyActivator>());
            Action<ControllerContext, object> expected = (_, __) => { };
            var activatorProvider = new Mock<IControllerActivatorProvider>();
            activatorProvider.Setup(p => p.CreateReleaser(It.IsAny<ControllerActionDescriptor>()))
                .Returns(expected)
                .Verifiable();
            var provider = new ControllerFactoryProvider(
                activatorProvider.Object,
                factory,
                Enumerable.Empty<IControllerPropertyActivator>());
            var descriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(object).GetTypeInfo(),
            };

            // Act
            var actual = provider.CreateControllerReleaser(descriptor);

            // Assert
            Assert.Same(expected, actual);
            activatorProvider.Verify();
        }

        private class TestController
        {
            public object ActivatedValue1 { get; set; }

            public object ActivatedValue2 { get; set; }
        }
    }
}
